# Frontend AlgoLab

Frontend hecho con Next.js y Tailwind para probar registro, inicio de sesion y paneles por rol contra el backend.

## Tecnologias

- Next.js 16.2.4
- React 19.2.4
- React DOM 19.2.4
- TypeScript 5
- Tailwind CSS 4
- ESLint 9
- eslint-config-next 16.2.4
- App Router de Next.js
- Route Handlers de Next.js para hacer proxy al backend
- Geist Font con `next/font`

## Como funciona

El frontend no llama directamente al backend desde el navegador. Usa rutas internas en `/api` para hacer proxy al backend y evitar problemas de CORS en desarrollo.

Backend por defecto:

```txt
https://backendfrontendpaginawebmr-production.up.railway.app
```

Se puede cambiar con la variable:

```env
API_BASE_URL=https://backendfrontendpaginawebmr-production.up.railway.app
```

Flujo actual:

1. El usuario se registra desde `/registrarse`.
2. El usuario inicia sesion desde `/iniciar-sesion`.
3. El backend devuelve un JWT solo en inicio de sesion junto con datos basicos del usuario, nivel actual y puntaje.
4. El frontend guarda en `localStorage`:

```txt
token
usuario
```

5. Segun el rol, el login redirige a:

```txt
ESTUDIANTE -> /estudiante
DOCENTE -> /docente
ADMINISTRADOR -> /administrador
```

El hook `useAuthSession` lee la sesion de forma compatible con SSR para evitar errores de hidratacion.

## Paginas

| Ruta | Descripcion |
|---|---|
| `/` | Redirige a `/registrarse` |
| `/registrarse` | Formulario para registrar estudiantes |
| `/iniciar-sesion` | Formulario de login |
| `/estudiante` | Panel simple para usuarios estudiantes |
| `/docente` | Panel para consultar niveles |
| `/administrador` | Panel para gestionar usuarios y niveles |

## Pagina de registro

Ruta:

```txt
/registrarse
```

Permite registrar estudiantes. Envia este body al backend:

```json
{
  "nombre": "Juan Perez",
  "correo": "juan@email.com",
  "rol": "ESTUDIANTE",
  "contrasena": "123456"
}
```

El registro no guarda token. Despues de registrarse, el usuario debe iniciar sesion.

## Pagina de inicio de sesion

Ruta:

```txt
/iniciar-sesion
```

Envia este body al backend:

```json
{
  "correo": "usuario@email.com",
  "contrasena": "123456"
}
```

Si el login es correcto, guarda el token y el usuario en `localStorage` y redirige segun el rol.

## Panel estudiante

Ruta:

```txt
/estudiante
```

Muestra informacion basica del usuario autenticado:

- nombre
- correo
- rol
- nivel actual
- puntaje

## Panel docente

Ruta:

```txt
/docente
```

Usa el token guardado para consultar:

```txt
GET /api/niveles
```

Muestra los niveles disponibles con nombre, descripcion y numero de nivel.

## Panel administrador

Ruta:

```txt
/administrador
```

Funciones disponibles:

- listar usuarios registrados
- buscar usuarios por nombre, correo o rol
- editar nombre, correo, rol, nivel actual y puntaje de usuarios
- borrar usuarios
- crear niveles
- editar niveles
- borrar niveles

Restricciones en el panel:

- El administrador no puede borrar su propia cuenta.
- El administrador no puede quitarse a si mismo el rol `ADMINISTRADOR`.
- Esas reglas tambien estan protegidas en el backend.

## Rutas proxy internas

Estas rutas viven en el frontend y reenvian la peticion al backend:

| Metodo | Ruta frontend | Ruta backend |
|---|---|---|
| `POST` | `/api/registrar` | `/api/usuarios/registrar` |
| `POST` | `/api/iniciar-sesion` | `/api/usuarios/iniciar-sesion` |
| `GET` | `/api/usuarios` | `/api/usuarios` |
| `POST` | `/api/usuarios` | `/api/usuarios` |
| `GET` | `/api/usuarios/{id}` | `/api/usuarios/{id}` |
| `PUT` | `/api/usuarios/{id}` | `/api/usuarios/{id}` |
| `PATCH` | `/api/usuarios/{id}` | `/api/usuarios/{id}` |
| `DELETE` | `/api/usuarios/{id}` | `/api/usuarios/{id}` |
| `GET` | `/api/niveles` | `/api/niveles` |
| `POST` | `/api/niveles` | `/api/niveles` |
| `GET` | `/api/niveles/{id}` | `/api/niveles/{id}` |
| `PUT` | `/api/niveles/{id}` | `/api/niveles/{id}` |
| `DELETE` | `/api/niveles/{id}` | `/api/niveles/{id}` |

## Comandos

Instalar dependencias:

```bash
npm install
```

Ejecutar en desarrollo:

```bash
npm run dev
```

En Windows, si PowerShell bloquea `npm`, usar:

```powershell
npm.cmd run dev
```

Validar lint:

```powershell
npm.cmd run lint
```

Compilar:

```powershell
npm.cmd run build
```

Iniciar build de produccion:

```powershell
npm.cmd run start
```

## Notas de prueba

Administrador inicial para probar el panel:

```json
{
  "correo": "cristhian.david@admin.com",
  "contrasena": "tu-contrasena-segura"
}
```

Antes de probar el frontend, el backend debe estar desplegado o corriendo en la URL configurada con `API_BASE_URL`.
