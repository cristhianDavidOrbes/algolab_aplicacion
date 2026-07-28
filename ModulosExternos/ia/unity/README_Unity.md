# Integracion Unity: voz -> IA -> respuesta

Este ejemplo usa el reconocimiento de voz de Windows desde Unity. Es la opcion mas rapida para PC/Windows Mixed Reality porque evita enviar audio al servidor: Unity convierte la voz a texto y solo manda el texto a la API IA.

## Flujo

1. El usuario presiona un boton en realidad mixta.
2. Unity ejecuta `AlternarGrabacion()`.
3. Windows Speech convierte la voz a texto.
4. Unity envia el texto a `POST /api/ia/responder`.
5. La API obtiene el nivel desde el backend y consulta Ollama.
6. Unity recibe `respuesta` y la muestra o la usa en otro componente.

## Como usar

1. Copia `AlgoLabVoiceAssistant.cs` a `Assets/Scripts/AlgoLabVoiceAssistant.cs`.
2. Crea un GameObject en la escena, por ejemplo `IA Voice Assistant`.
3. Agrega el componente `AlgoLabVoiceAssistant`.
4. En tu boton de MRTK/XRI/UI, conecta el evento de click a:

```txt
IA Voice Assistant -> AlgoLabVoiceAssistant.AlternarGrabacion()
```

5. Para conectar usando ngrok, deja esta URL en `Ia Api Url`:

```txt
https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder
```

El script ya agrega el header requerido por ngrok:

```txt
ngrok-skip-browser-warning: true
```

6. Si vas a correr Unity en el mismo PC sin ngrok, puedes usar:

```txt
http://localhost:8001/api/ia/responder
```

7. Si vas a correr en un visor/dispositivo separado sin ngrok, cambia `localhost` por la IP del PC:

```txt
http://192.168.X.X:8001/api/ia/responder
```

En ese caso Windows Firewall debe permitir entrada al puerto `8001`.

## Permisos

En Unity/Windows asegúrate de permitir microfono:

- Project Settings > Player > Publishing Settings / Capabilities: Microphone, si compilas para UWP/HoloLens.
- Project Settings > Player > Publishing Settings / Capabilities: InternetClient.
- Si la API esta en otro equipo de la red local, activa tambien PrivateNetworkClientServer.
- En Windows: Configuracion > Privacidad y seguridad > Microfono, permitir acceso a la app.

## Respuesta

Puedes conectar el evento `alResponderIa` a otro script para:

- Mostrar la respuesta en un panel 3D.
- Activar animaciones.
- Mandarla a un sistema de texto a voz.

Para Quest/Android, este script no sirve como STT porque `DictationRecognizer` es de Windows. En ese caso conviene agregar Whisper/faster-whisper al contenedor de la API y enviar audio desde Unity.
