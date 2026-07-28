# API IA AlgoLab

API local hecha con FastAPI para conectar AlgoLab con un modelo de IA ejecutado en Ollama.

La API recibe preguntas desde Unity, obtiene los niveles desde el backend de AlgoLab, arma un prompt con ese contexto y devuelve una respuesta generada por el modelo local.

## URLs actuales

Base local:

```txt
http://localhost:8001
```

Base publica por ngrok:

```txt
https://appetite-tuesday-empty.ngrok-free.dev
```

Backend usado por la API:

```txt
https://backendfrontendpaginawebmr-production.up.railway.app
```

Endpoint principal para Unity:

```txt
https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder
```

Si llamas la API publica desde un cliente HTTP y ngrok muestra advertencia, agrega este header:

```txt
ngrok-skip-browser-warning: true
```

## Endpoints de esta API

Además de la IA, esta API proporciona la voz usada por las Quest sin depender de
los assets eliminados de ElevenLabs:

- `POST /api/voz/transcribir`: recibe `multipart/form-data` con `archivo` e
  `idioma` y transcribe localmente con Faster Whisper.
- `POST /api/voz/sintetizar`: recibe JSON con `texto` y devuelve audio WAV con
  Piper `es_MX-claude-high`, ejecutado completamente en este computador. Las
  frases generadas se guardan en caché y Edge TTS queda como respaldo automático.

Piper no consume cuotas por frase y sigue funcionando sin Internet. El campo
`voz` se conserva para escoger la voz de Edge solamente cuando el motor local
no puede responder. La cabecera `X-Algolab-TTS-Engine` indica `piper-local`,
`cache` o `edge-respaldo` para facilitar el diagnóstico.

Unity utiliza estos endpoints mediante `AlgoLabLocalVoiceProvider.asset`.

### 1. Salud de la API

Verifica que la API este encendida y muestra que modelo y backend esta usando.

```http
GET /api/ia/salud
```

URL local:

```txt
http://localhost:8001/api/ia/salud
```

URL publica:

```txt
https://appetite-tuesday-empty.ngrok-free.dev/api/ia/salud
```

Headers:

```json
{
  "ngrok-skip-browser-warning": "true"
}
```

Body:

```json
{}
```

Respuesta exitosa:

```json
{
  "estado": "ok",
  "modelo": "llama3.2:latest",
  "backend": "https://backendfrontendpaginawebmr-production.up.railway.app"
}
```

Campos de respuesta:

| Campo | Tipo | Descripcion |
| --- | --- | --- |
| `estado` | `string` | Estado de la API. Si responde `ok`, FastAPI esta funcionando. |
| `modelo` | `string` | Modelo configurado en Ollama. |
| `backend` | `string` | URL del backend de AlgoLab que usa la API para consultar niveles. |

Ejemplo en PowerShell:

```powershell
Invoke-RestMethod `
  -Uri "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/salud" `
  -Headers @{ "ngrok-skip-browser-warning" = "true" }
```

Ejemplo con curl:

```bash
curl -H "ngrok-skip-browser-warning: true" \
  https://appetite-tuesday-empty.ngrok-free.dev/api/ia/salud
```

### 2. Responder pregunta con IA

Recibe una pregunta, consulta los niveles en el backend y responde usando Ollama.

```http
POST /api/ia/responder
```

URL local:

```txt
http://localhost:8001/api/ia/responder
```

URL publica:

```txt
https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder
```

Headers:

```json
{
  "Content-Type": "application/json",
  "ngrok-skip-browser-warning": "true"
}
```

Body consultando todos los niveles activos:

```json
{
  "pregunta": "Que debo practicar ahora?"
}
```

Body consultando un nivel especifico:

```json
{
  "pregunta": "Explica el tema del nivel y dame un ejemplo en JavaScript",
  "nivel_id": 1
}
```

Campos del body:

| Campo | Tipo | Obligatorio | Reglas | Descripcion |
| --- | --- | --- | --- | --- |
| `pregunta` | `string` | Si | Minimo 1 caracter, maximo 4000 | Pregunta que el usuario le hace a la IA. |
| `nivel_id` | `number` o `null` | No | Debe ser mayor o igual a 1 | Si se envia, la API busca ese valor contra `id` y contra `nivel` dentro de la lista de niveles. Si no se envia, usa todos los niveles activos. |

Respuesta exitosa:

```json
{
  "modelo": "llama3.2:latest",
  "nivel_usado": [
    {
      "id": 1,
      "nombre": "POO",
      "descripcion": "En este nivel el estudiante aprende los conceptos basicos de la Programacion Orientada a Objetos mediante el ejemplo de un vehiculo.",
      "nivel": 1,
      "objetivo": "entender el concepto de POO",
      "activo": true,
      "fechaCreacion": "2026-05-03T02:04:19.910937",
      "fechaActualizacion": "2026-05-03T02:04:19.910937"
    }
  ],
  "respuesta": "Si, estoy funcionando. En este nivel estas practicando Programacion Orientada a Objetos."
}
```

Respuesta exitosa cuando se consulta un solo nivel:

```json
{
  "modelo": "llama3.2:latest",
  "nivel_usado": {
    "id": 1,
    "nombre": "POO",
    "descripcion": "En este nivel el estudiante aprende los conceptos basicos de la Programacion Orientada a Objetos mediante el ejemplo de un vehiculo.",
    "nivel": 1,
    "objetivo": "entender el concepto de POO",
    "activo": true,
    "fechaCreacion": "2026-05-03T02:04:19.910937",
    "fechaActualizacion": "2026-05-03T02:04:19.910937"
  },
  "respuesta": "La Programacion Orientada a Objetos permite crear clases como moldes y objetos con atributos y metodos."
}
```

Respuesta cuando no hay niveles activos:

```json
{
  "modelo": "llama3.2:latest",
  "nivel_usado": [],
  "respuesta": "No hay niveles activos registrados en el backend desplegado. Crea al menos un nivel en Railway para que pueda responder con el tema, la descripcion y el objetivo correctos."
}
```

Campos de respuesta:

| Campo | Tipo | Descripcion |
| --- | --- | --- |
| `modelo` | `string` | Modelo de Ollama usado para generar la respuesta. |
| `nivel_usado` | `object` o `array` | Nivel o niveles devueltos por el backend y usados como contexto. |
| `respuesta` | `string` | Texto generado por la IA. |

Ejemplo en PowerShell:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder" `
  -Headers @{ "ngrok-skip-browser-warning" = "true" } `
  -ContentType "application/json" `
  -Body '{"pregunta":"Explica el tema del nivel y dame un ejemplo en JavaScript","nivel_id":1}'
```

Ejemplo con curl:

```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "ngrok-skip-browser-warning: true" \
  -d '{"pregunta":"Explica el tema del nivel y dame un ejemplo en JavaScript","nivel_id":1}' \
  https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder
```

Ejemplo para Unity usando `UnityWebRequest`:

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class IaClient : MonoBehaviour
{
    private const string Url = "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder";

    public IEnumerator Preguntar()
    {
        string json = "{\"pregunta\":\"Explica POO con un ejemplo\",\"nivel_id\":1}";
        byte[] body = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(Url, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("ngrok-skip-browser-warning", "true");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            yield break;
        }

        Debug.Log(request.downloadHandler.text);
    }
}
```

## Errores posibles

### Body invalido

Ocurre cuando falta `pregunta`, esta vacia, supera 4000 caracteres o `nivel_id` es menor que 1.

```http
422 Unprocessable Entity
```

Ejemplo:

```json
{
  "detail": [
    {
      "type": "missing",
      "loc": ["body", "pregunta"],
      "msg": "Field required"
    }
  ]
}
```

### Nivel no existe

Ocurre cuando se envia un `nivel_id` que no coincide con ningun campo `id` ni `nivel` de los niveles devueltos por el backend.

```http
404 Not Found
```

Respuesta:

```json
{
  "detail": "No existe el nivel solicitado: 99"
}
```

### Error iniciando sesion en el backend

Ocurre si las credenciales del administrador son incorrectas o el backend rechaza el login.

```http
502 Bad Gateway
```

Respuesta:

```json
{
  "detail": "No se pudo iniciar sesion en el backend con el usuario administrador."
}
```

### Backend no devuelve token

Ocurre si el login responde correctamente pero no incluye `token`.

```http
502 Bad Gateway
```

Respuesta:

```json
{
  "detail": "El backend no devolvio token JWT."
}
```

### Error consultando niveles

Ocurre si el backend responde con error al consultar `/api/niveles`.

```http
502 Bad Gateway
```

Respuesta:

```json
{
  "detail": "No se pudieron obtener los niveles del backend."
}
```

### Error con Ollama

Ocurre si Ollama no esta encendido, el modelo no esta instalado o Ollama responde con error.

```http
502 Bad Gateway
```

Respuesta:

```json
{
  "detail": "Ollama no respondio bien. Revisa que el modelo llama3.2:latest este instalado."
}
```

## Endpoints externos que consume esta API

Esta API no guarda datos propios. Para responder, consume servicios externos.

### Login de administrador en backend

```http
POST {BACKEND_URL}/api/usuarios/iniciar-sesion
```

Body enviado por la API:

```json
{
  "correo": "cristhian.david@admin.com",
  "contrasena": "********"
}
```

Respuesta esperada:

```json
{
  "exitoso": true,
  "mensaje": "Inicio de sesion exitoso",
  "token": "jwt-del-backend",
  "usuario": {
    "id": 1,
    "nombre": "Cristhian David",
    "correo": "cristhian.david@admin.com",
    "rol": "ADMINISTRADOR"
  }
}
```

La API guarda el token en memoria durante aproximadamente 50 minutos para no iniciar sesion en cada pregunta.

### Consultar todos los niveles

```http
GET {BACKEND_URL}/api/niveles
```

Header enviado por la API:

```json
{
  "Authorization": "Bearer jwt-del-backend"
}
```

Respuesta esperada:

```json
[
  {
    "id": 1,
    "nombre": "POO",
    "descripcion": "Descripcion del nivel",
    "nivel": 1,
    "objetivo": "Objetivo del nivel",
    "activo": true,
    "fechaCreacion": "2026-05-03T02:04:19.910937",
    "fechaActualizacion": "2026-05-03T02:04:19.910937"
  }
]
```

Cuando el backend devuelve una lista, la API usa primero los niveles con `activo: true`. Si no hay activos, usa la lista original.

### Buscar nivel solicitado

La API IA no llama directamente a `{BACKEND_URL}/api/niveles/{nivel_id}`.

Primero consulta todos los niveles:

```http
GET {BACKEND_URL}/api/niveles
```

Luego, si Unity envia `nivel_id`, busca el nivel dentro de esa lista comparando contra los dos campos posibles:

```json
{
  "nivel_id": 1
}
```

Coincide si el backend devuelve:

```json
{
  "id": 1,
  "nivel": 3
}
```

Tambien coincide si el backend devuelve:

```json
{
  "id": 10,
  "nivel": 1
}
```

Esto permite que Unity mande `nivel_id: 1` y la API encuentre el nivel aunque el identificador real este en `id` o en `nivel`.

### Chat de Ollama

```http
POST {OLLAMA_BASE_URL}/api/chat
```

URL por defecto:

```txt
http://localhost:11434/api/chat
```

Body enviado por la API:

```json
{
  "model": "llama3.2:latest",
  "messages": [
    {
      "role": "system",
      "content": "Eres un asistente experto en programacion para AlgoLab. Responde siempre en espanol claro, directo y practico."
    },
    {
      "role": "user",
      "content": "Contexto de niveles del backend:\n[...]\n\nPregunta del usuario:\nExplica POO"
    }
  ],
  "stream": false,
  "options": {
    "temperature": 0.25,
    "num_predict": 450
  }
}
```

Respuesta esperada de Ollama:

```json
{
  "model": "llama3.2:latest",
  "message": {
    "role": "assistant",
    "content": "Respuesta generada por la IA."
  },
  "done": true
}
```

## Configuracion

Variables de entorno usadas por `main.py`:

| Variable | Obligatoria | Valor por defecto | Descripcion |
| --- | --- | --- | --- |
| `API_BASE_URL` | No | `null` | Si existe, tiene prioridad sobre `BACKEND_BASE_URL`. |
| `BACKEND_BASE_URL` | No | `http://localhost:8080` | URL del backend de AlgoLab. |
| `BACKEND_ADMIN_EMAIL` | No | `cristhian.david@admin.com` | Correo usado para iniciar sesion como administrador. |
| `BACKEND_ADMIN_PASSWORD` | No | `define-una-contrasena-segura` | Contrasena usada para iniciar sesion como administrador. |
| `OLLAMA_BASE_URL` | No | `http://localhost:11434` | URL local donde corre Ollama. |
| `OLLAMA_MODEL` | No | `qwen2.5-coder:3b` | Modelo que la API le pide a Ollama. |
| `WHISPER_MODEL` | No | `base` | Modelo local usado para Speech-to-Text. |
| `WHISPER_DEVICE` | No | `cpu` | Dispositivo usado por Faster Whisper. |
| `WHISPER_COMPUTE_TYPE` | No | `int8` | Tipo de cálculo de Whisper. |
| `TTS_VOICE` | No | `es-CO-SalomeNeural` | Voz española usada para Text-to-Speech. |
| `PIPER_MODEL_PATH` | No | `models/piper/es_MX-claude-high.onnx` | Modelo local principal de Text-to-Speech. |
| `TTS_CACHE_DIR` | No | `.cache/tts` | Carpeta de audios WAV generados. |
| `TTS_CACHE_MAX_FILES` | No | `500` | Máximo de frases conservadas en caché. |

Ejemplo de `.env` actual:

```env
API_BASE_URL=https://backendfrontendpaginawebmr-production.up.railway.app
BACKEND_BASE_URL=https://backendfrontendpaginawebmr-production.up.railway.app
BACKEND_ADMIN_EMAIL=cristhian.david@admin.com
BACKEND_ADMIN_PASSWORD=define-una-contrasena-segura
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=llama3.2:latest
```

## Requisitos

- Python 3.11 o superior.
- Ollama instalado.
- Un modelo descargado en Ollama.
- El modelo de voz Piper `es_MX-claude-high` dentro de `models/piper`.
- Acceso al backend de AlgoLab.
- ngrok instalado o disponible en `.tools/ngrok-current/ngrok.exe`.

Instalar dependencias Python:

```powershell
cd C:\Users\Cristian\Desktop\organizar\ia
python -m pip install -r requirements.txt
```

El proyecto ya incluye el modelo Piper usado por las gafas. Si fuera necesario
recuperarlo, se puede descargar nuevamente con:

```powershell
python -m piper.download_voices --data-dir .\models\piper es_MX-claude-high
```

Instalar Ollama:

```powershell
winget install Ollama.Ollama
```

Ver modelos instalados:

```powershell
ollama list
```

Descargar modelo recomendado para este proyecto:

```powershell
ollama pull qwen2.5-coder:3b
```

Si ese modelo no esta instalado, puedes usar el modelo disponible actualmente:

```env
OLLAMA_MODEL=llama3.2:latest
```

## Activar todo con un solo comando

Abre un CMD nuevo y ejecuta:

```cmd
activate_chat
```

El comando inicia, si hace falta, Ollama, la API de FastAPI con Faster Whisper
y Piper/Edge, precarga el modelo de chat y publica el puerto 8001 mediante el
dominio fijo de ngrok. Al finalizar debe mostrar `IA local lista` e
`IA publica lista`. Se puede ejecutar otra vez sin duplicar los procesos.

El lanzador del proyecto esta en `ModulosExternos/ia/activate_chat.cmd` y el
acceso global se instala en `%LOCALAPPDATA%\AlgoLab\bin`.

Para apagar de forma segura la API, Ollama y el tunel de AlgoLab:

```cmd
breake_chat
```

Este comando verifica los procesos antes de cerrarlos y se puede ejecutar
varias veces aunque los servicios ya esten apagados.

## Levantar la API manualmente

Desde la carpeta del proyecto:

```powershell
cd C:\Users\Cristian\Desktop\organizar\ia
python -m uvicorn main:app --host 127.0.0.1 --port 8001
```

Con recarga automatica durante desarrollo:

```powershell
python -m uvicorn main:app --host 127.0.0.1 --port 8001 --reload
```

Probar localmente:

```powershell
Invoke-RestMethod -Uri "http://localhost:8001/api/ia/salud"
```

## Levantar ngrok

Ejecutable local:

```txt
C:\Users\Cristian\Desktop\organizar\ia\.tools\ngrok-current\ngrok.exe
```

Comando con dominio fijo:

```powershell
.\.tools\ngrok-current\ngrok.exe http --url=appetite-tuesday-empty.ngrok-free.dev 8001
```

Comando sin dominio fijo:

```powershell
.\.tools\ngrok-current\ngrok.exe http 8001
```

Panel local de ngrok:

```txt
http://127.0.0.1:4040
```

API local de ngrok para ver tuneles activos:

```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:4040/api/tunnels"
```

## Docker

Levantar API y Ollama con Docker:

```powershell
cd C:\Users\Cristian\Desktop\organizar\ia
copy .env.docker.example .env
docker compose up --build
```

Usar soporte GPU si Docker Desktop y NVIDIA estan configurados:

```powershell
docker compose -f docker-compose.yml -f docker-compose.gpu.yml up --build
```

Si falla por GPU, usa el comando normal sin `docker-compose.gpu.yml`.

En Docker, si quieres usar backend local en vez de Railway, cambia las URLs por:

```env
API_BASE_URL=http://host.docker.internal:8080
BACKEND_BASE_URL=http://host.docker.internal:8080
```

## Flujo interno

1. Unity o cualquier cliente llama `POST /api/ia/responder`.
2. La API valida el JSON recibido.
3. La API inicia sesion en el backend como administrador, si no tiene token en cache.
4. La API consulta siempre `/api/niveles`.
5. Si llega `nivel_id`, busca coincidencia por `id` o por `nivel`.
6. Si no llega `nivel_id`, usa los niveles con `activo: true`; si no hay activos, usa la lista completa.
7. La API construye un prompt con el contexto de niveles.
8. La API llama a Ollama en `/api/chat`.
9. La API devuelve `modelo`, `nivel_usado` y `respuesta`.

## Archivos importantes

| Archivo | Descripcion |
| --- | --- |
| `main.py` | Codigo principal de FastAPI. Define configuracion, modelos y endpoints. |
| `requirements.txt` | Dependencias Python. |
| `.env` | Configuracion local real. No deberia subirse a repositorios publicos. |
| `.env.example` | Plantilla de configuracion. |
| `docker-compose.yml` | Levanta API y Ollama con Docker. |
| `docker-compose.gpu.yml` | Configuracion adicional para intentar usar GPU. |
| `.tools/ngrok-current/ngrok.exe` | Ejecutable local de ngrok. |

## Comandos rapidos

Ver salud local:

```powershell
Invoke-RestMethod -Uri "http://localhost:8001/api/ia/salud"
```

Preguntar local:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8001/api/ia/responder" `
  -ContentType "application/json" `
  -Body '{"pregunta":"Que es una clase en POO?","nivel_id":1}'
```

Ver salud publica:

```powershell
Invoke-RestMethod `
  -Uri "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/salud" `
  -Headers @{ "ngrok-skip-browser-warning" = "true" }
```

Preguntar por ngrok:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder" `
  -Headers @{ "ngrok-skip-browser-warning" = "true" } `
  -ContentType "application/json" `
  -Body '{"pregunta":"Que es una clase en POO?","nivel_id":1}'
```

Ver procesos relacionados:

```powershell
Get-Process | Where-Object { $_.ProcessName -match "python|uvicorn|ngrok|ollama" }
```

Ver puertos usados:

```powershell
Get-NetTCPConnection -State Listen |
  Where-Object { $_.LocalPort -in 8001,11434,4040,8080 }
```

Ver logs de FastAPI:

```powershell
Get-Content .\uvicorn.log -Tail 80
```
