from __future__ import annotations

import asyncio
import hashlib
import io
import logging
import os
import tempfile
import threading
import time
import wave
from functools import lru_cache
from pathlib import Path
from typing import Any

import av
import edge_tts
import httpx
from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.responses import Response
from faster_whisper import WhisperModel
from piper import PiperVoice
from pydantic import BaseModel, Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8")

    api_base_url: str | None = None
    backend_base_url: str = "http://localhost:8080"
    backend_admin_email: str = "cristhian.david@admin.com"
    backend_admin_password: str = "define-una-contrasena-segura"
    ollama_base_url: str = "http://localhost:11434"
    ollama_model: str = "qwen2.5-coder:3b"
    whisper_model: str = "base"
    whisper_device: str = "cpu"
    whisper_compute_type: str = "int8"
    tts_voice: str = "es-CO-SalomeNeural"
    piper_model_path: str = "models/piper/es_MX-claude-high.onnx"
    tts_cache_dir: str = ".cache/tts"
    tts_cache_max_files: int = 500

    @property
    def backend_url(self) -> str:
        return (self.api_base_url or self.backend_base_url).rstrip("/")


class PreguntaRequest(BaseModel):
    pregunta: str = Field(..., min_length=1, max_length=4000)
    nivel_id: int | None = Field(default=None, ge=1)


class PreguntaResponse(BaseModel):
    modelo: str
    nivel_usado: dict[str, Any] | list[dict[str, Any]]
    respuesta: str


class VozRequest(BaseModel):
    texto: str = Field(..., min_length=1, max_length=4000)
    voz: str | None = None


settings = Settings()
app = FastAPI(title="API IA AlgoLab", version="1.1.0")
logger = logging.getLogger("algolab.ia")

_token_cache: dict[str, Any] = {"token": None, "expires_at": 0.0}
_piper_synthesis_lock = threading.Lock()
_tts_request_lock = asyncio.Lock()


def resolver_ruta_local(ruta: str) -> Path:
    path = Path(ruta).expanduser()
    if not path.is_absolute():
        path = Path(__file__).resolve().parent / path
    return path.resolve()


@lru_cache(maxsize=1)
def obtener_voz_piper() -> PiperVoice:
    ruta_modelo = resolver_ruta_local(settings.piper_model_path)
    if not ruta_modelo.is_file():
        raise FileNotFoundError(f"No existe el modelo local de Piper: {ruta_modelo}")

    logger.info("Cargando voz local Piper %s...", ruta_modelo.name)
    return PiperVoice.load(ruta_modelo)


def sintetizar_piper_local(texto: str) -> bytes:
    voz = obtener_voz_piper()
    salida = io.BytesIO()
    # La sesion se comparte para no cargar 60 MB en cada solicitud. El bloqueo
    # evita que dos inferencias simultaneas interfieran dentro de ONNX Runtime.
    with _piper_synthesis_lock:
        with wave.open(salida, "wb") as archivo_wav:
            voz.synthesize_wav(texto, archivo_wav)
    return salida.getvalue()


async def sintetizar_edge_mp3(texto: str, voz: str) -> bytes:
    comunicacion = edge_tts.Communicate(texto, voz)
    audio = bytearray()
    async for fragmento in comunicacion.stream():
        if fragmento["type"] == "audio":
            audio.extend(fragmento["data"])
    return bytes(audio)


def convertir_mp3_a_wav(audio_mp3: bytes) -> bytes:
    """Convierte el respaldo de Edge a WAV para mantener un formato estable."""
    entrada = av.open(io.BytesIO(audio_mp3), mode="r", format="mp3")
    remuestreador = av.AudioResampler(format="s16", layout="mono", rate=24000)
    salida = io.BytesIO()
    with wave.open(salida, "wb") as archivo_wav:
        archivo_wav.setnchannels(1)
        archivo_wav.setsampwidth(2)
        archivo_wav.setframerate(24000)

        for cuadro in entrada.decode(audio=0):
            for cuadro_pcm in remuestreador.resample(cuadro):
                archivo_wav.writeframes(cuadro_pcm.to_ndarray().tobytes())

        for cuadro_pcm in remuestreador.resample(None):
            archivo_wav.writeframes(cuadro_pcm.to_ndarray().tobytes())

    entrada.close()
    return salida.getvalue()


def ruta_cache_tts(texto: str) -> Path:
    modelo = resolver_ruta_local(settings.piper_model_path).name
    clave = hashlib.sha256(f"wav-v1|{modelo}|{texto}".encode("utf-8")).hexdigest()
    return resolver_ruta_local(settings.tts_cache_dir) / f"{clave}.wav"


def guardar_cache_tts(ruta: Path, audio: bytes) -> None:
    ruta.parent.mkdir(parents=True, exist_ok=True)
    temporal = ruta.with_suffix(".tmp")
    temporal.write_bytes(audio)
    os.replace(temporal, ruta)

    maximo = max(20, settings.tts_cache_max_files)
    archivos = sorted(ruta.parent.glob("*.wav"), key=lambda item: item.stat().st_mtime, reverse=True)
    for antiguo in archivos[maximo:]:
        try:
            antiguo.unlink()
        except OSError:
            logger.warning("No se pudo eliminar el audio antiguo de cache: %s", antiguo)


@app.on_event("startup")
async def precargar_voz_local() -> None:
    try:
        await asyncio.to_thread(sintetizar_piper_local, "Sistema de voz listo.")
        logger.info("Piper local esta precargado y listo.")
    except Exception:
        # La API puede seguir operando con Edge mientras se diagnostica el modelo.
        logger.exception("No se pudo precargar Piper; se usara Edge como respaldo.")


@lru_cache(maxsize=1)
def obtener_modelo_whisper() -> WhisperModel:
    logger.info("Cargando Whisper %s...", settings.whisper_model)
    return WhisperModel(
        settings.whisper_model,
        device=settings.whisper_device,
        compute_type=settings.whisper_compute_type,
    )


def transcribir_audio_local(audio: bytes, idioma: str | None) -> str:
    modelo = obtener_modelo_whisper()
    ruta = ""
    try:
        with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as archivo:
            archivo.write(audio)
            ruta = archivo.name

        segmentos, _ = modelo.transcribe(
            ruta,
            language=(idioma or "es").split("-")[0],
            beam_size=5,
            vad_filter=True,
        )
        return " ".join(segment.text.strip() for segment in segmentos if segment.text.strip()).strip()
    finally:
        if ruta and os.path.exists(ruta):
            os.unlink(ruta)


async def obtener_token_admin(client: httpx.AsyncClient) -> str:
    ahora = time.time()
    if _token_cache["token"] and _token_cache["expires_at"] > ahora:
        return _token_cache["token"]

    for intento in range(3):
        try:
            response = await client.post(
                f"{settings.backend_url}/api/usuarios/iniciar-sesion",
                json={
                    "correo": settings.backend_admin_email,
                    "contrasena": settings.backend_admin_password,
                },
                timeout=20,
            )
            break
        except httpx.RequestError as exc:
            if intento == 2:
                raise HTTPException(
                    status_code=502,
                    detail=f"No se pudo conectar con el backend para iniciar sesion: {exc}",
                ) from exc
            await asyncio.sleep(1)

    if response.status_code >= 400:
        raise HTTPException(
            status_code=502,
            detail="No se pudo iniciar sesion en el backend con el usuario administrador.",
        )

    data = response.json()
    token = data.get("token")
    if not token:
        raise HTTPException(status_code=502, detail="El backend no devolvio token JWT.")

    _token_cache["token"] = token
    _token_cache["expires_at"] = ahora + 60 * 50
    return token


def obtener_nivel_por_id_o_numero(niveles: list[dict[str, Any]], nivel_id: int | None) -> dict[str, Any] | None:
    if nivel_id is None:
        return None

    for nivel in niveles:
        try:
            id_backend = int(nivel.get("id", -1))
        except (TypeError, ValueError):
            id_backend = -1

        try:
            numero_nivel = int(nivel.get("nivel", -1))
        except (TypeError, ValueError):
            numero_nivel = -1

        if id_backend == int(nivel_id) or numero_nivel == int(nivel_id):
            return nivel

    return None


def construir_contexto_local(nivel_id: int | None) -> dict[str, Any]:
    numero = nivel_id if nivel_id is not None else 1
    return {
        "id": numero,
        "nivel": numero,
        "nombre": f"Nivel {numero}",
        "descripcion": (
            "Contexto local de respaldo de AlgoLab sobre programacion orientada a objetos, "
            "diagramas de clases y practica guiada."
        ),
        "objetivo": (
            "Explicar el concepto solicitado de forma clara, breve y apropiada para un estudiante."
        ),
        "activo": True,
        "origen": "respaldo_local",
    }


async def obtener_niveles_backend() -> list[dict[str, Any]]:
    async with httpx.AsyncClient() as client:
        token = await obtener_token_admin(client)
        for intento in range(3):
            try:
                response = await client.get(
                    f"{settings.backend_url}/api/niveles",
                    headers={"Authorization": f"Bearer {token}"},
                    timeout=20,
                )
                break
            except httpx.RequestError as exc:
                if intento == 2:
                    raise HTTPException(
                        status_code=502,
                        detail=f"No se pudo conectar con el backend para obtener niveles: {exc}",
                    ) from exc
                await asyncio.sleep(1)

    if response.status_code >= 400:
        raise HTTPException(status_code=502, detail="No se pudieron obtener los niveles del backend.")

    niveles = response.json()
    if not isinstance(niveles, list):
        raise HTTPException(status_code=502, detail="El backend no devolvio una lista de niveles.")

    return niveles


def construir_prompt(pregunta: str, niveles: dict[str, Any] | list[dict[str, Any]]) -> list[dict[str, str]]:
    return [
        {
            "role": "system",
            "content": (
                "Eres un asistente experto en programacion para AlgoLab. "
                "Responde siempre en espanol claro, directo y practico. "
                "Limita cada respuesta a un maximo de 80 palabras. "
                "Usa el contexto de niveles entregado por el backend para ajustar tema, "
                "dificultad, descripcion y objetivo. Si falta informacion, dilo brevemente."
            ),
        },
        {
            "role": "user",
            "content": f"Contexto de niveles del backend:\n{niveles}\n\nPregunta del usuario:\n{pregunta}",
        },
    ]


async def preguntar_ollama(pregunta: str, niveles: dict[str, Any] | list[dict[str, Any]]) -> str:
    payload = {
        "model": settings.ollama_model,
        "messages": construir_prompt(pregunta, niveles),
        "stream": False,
        "options": {
            "temperature": 0.25,
            "num_predict": 120,
        },
    }

    async with httpx.AsyncClient() as client:
        try:
            response = await client.post(f"{settings.ollama_base_url}/api/chat", json=payload, timeout=120)
        except httpx.RequestError as exc:
            raise HTTPException(
                status_code=502,
                detail=f"No se pudo conectar con Ollama. Revisa que este encendido: {exc}",
            ) from exc

    if response.status_code >= 400:
        raise HTTPException(
            status_code=502,
            detail=f"Ollama no respondio bien. Revisa que el modelo {settings.ollama_model} este instalado.",
        )

    data = response.json()
    return data.get("message", {}).get("content", "").strip()


@app.get("/api/ia/salud")
async def salud() -> dict[str, str]:
    piper_disponible = resolver_ruta_local(settings.piper_model_path).is_file()
    return {
        "estado": "ok",
        "modelo": settings.ollama_model,
        "backend": settings.backend_url,
        "speech_to_text": f"faster-whisper/{settings.whisper_model}",
        "text_to_speech": (
            f"piper/{resolver_ruta_local(settings.piper_model_path).stem} (local)"
            if piper_disponible
            else f"edge/{settings.tts_voice} (respaldo)"
        ),
    }


@app.post("/api/voz/transcribir")
async def transcribir(
    archivo: UploadFile = File(...),
    idioma: str | None = Form(default="es"),
) -> dict[str, str]:
    audio = await archivo.read()
    if not audio:
        raise HTTPException(status_code=400, detail="El audio esta vacio.")
    if len(audio) > 20 * 1024 * 1024:
        raise HTTPException(status_code=413, detail="El audio supera el limite de 20 MB.")

    try:
        texto = await asyncio.to_thread(transcribir_audio_local, audio, idioma)
    except Exception as exc:
        logger.exception("Fallo Speech-to-Text")
        raise HTTPException(status_code=500, detail=f"No se pudo transcribir el audio: {exc}") from exc

    return {"texto": texto}


@app.post("/api/voz/sintetizar")
async def sintetizar(request: VozRequest) -> Response:
    texto = " ".join(request.texto.split())
    voz = (request.voz or settings.tts_voice).strip()
    ruta_cache = ruta_cache_tts(texto)
    motor = "cache"

    if ruta_cache.is_file():
        audio = await asyncio.to_thread(ruta_cache.read_bytes)
    else:
        async with _tts_request_lock:
            if ruta_cache.is_file():
                audio = await asyncio.to_thread(ruta_cache.read_bytes)
            else:
                try:
                    audio = await asyncio.to_thread(sintetizar_piper_local, texto)
                    motor = "piper-local"
                except Exception as exc_piper:
                    logger.exception("Fallo Piper local; se intenta Edge TTS")
                    try:
                        audio_mp3 = await sintetizar_edge_mp3(texto, voz)
                        audio = await asyncio.to_thread(convertir_mp3_a_wav, audio_mp3)
                        motor = "edge-respaldo"
                    except Exception as exc_edge:
                        logger.exception("Tambien fallo Edge TTS")
                        raise HTTPException(
                            status_code=502,
                            detail=(
                                "No se pudo sintetizar la voz con Piper local ni con Edge. "
                                f"Piper: {exc_piper}; Edge: {exc_edge}"
                            ),
                        ) from exc_edge

                if audio:
                    await asyncio.to_thread(guardar_cache_tts, ruta_cache, audio)

    if not audio:
        raise HTTPException(status_code=502, detail="El proveedor de voz no devolvio audio.")

    return Response(
        content=audio,
        media_type="audio/wav",
        headers={
            "Cache-Control": "no-store",
            "X-Algolab-TTS-Engine": motor,
        },
    )


@app.post("/api/ia/responder", response_model=PreguntaResponse)
async def responder(request: PreguntaRequest) -> PreguntaResponse:
    try:
        niveles = await obtener_niveles_backend()
    except HTTPException as exc:
        logger.warning("Backend no disponible; se usa contexto local: %s", exc.detail)
        nivel_local = construir_contexto_local(request.nivel_id)
        respuesta_local = await preguntar_ollama(request.pregunta, nivel_local)
        return PreguntaResponse(
            modelo=settings.ollama_model,
            nivel_usado=nivel_local,
            respuesta=respuesta_local,
        )

    if request.nivel_id is not None:
        nivel_usado: dict[str, Any] | list[dict[str, Any]] | None = obtener_nivel_por_id_o_numero(
            niveles,
            request.nivel_id,
        )

        if nivel_usado is None:
            raise HTTPException(status_code=404, detail=f"No existe el nivel solicitado: {request.nivel_id}")
    else:
        niveles_activos = [nivel for nivel in niveles if nivel.get("activo") is True]
        nivel_usado = niveles_activos if niveles_activos else niveles

    if isinstance(nivel_usado, list) and not nivel_usado:
        return PreguntaResponse(
            modelo=settings.ollama_model,
            nivel_usado=nivel_usado,
            respuesta=(
                "No hay niveles activos registrados en el backend desplegado. "
                "Crea al menos un nivel en Railway para que pueda responder con el tema, "
                "la descripcion y el objetivo correctos."
            ),
        )

    respuesta = await preguntar_ollama(request.pregunta, nivel_usado)
    return PreguntaResponse(modelo=settings.ollama_model, nivel_usado=nivel_usado, respuesta=respuesta)
