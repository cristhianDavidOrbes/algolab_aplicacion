package com.algolab.backend_werb_mr.controladores;

import java.util.List;
import java.util.Map;

import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.GrantedAuthority;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.algolab.backend_werb_mr.dtos.ActualizarProgresoUsuarioRequest;
import com.algolab.backend_werb_mr.dtos.ActualizarUsuarioRequest;
import com.algolab.backend_werb_mr.dtos.AuthRespuestaDTO;
import com.algolab.backend_werb_mr.dtos.LoginRequest;
import com.algolab.backend_werb_mr.dtos.RegistroUsuarioRequest;
import com.algolab.backend_werb_mr.dtos.UsuarioSesionDTO;
import com.algolab.backend_werb_mr.dtos.UsuarioRespuestaDTO;
import com.algolab.backend_werb_mr.modelos.Rol;
import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.seguridad.JwtServicio;
import com.algolab.backend_werb_mr.servicios.IUsuarioServicio;

@RestController
@RequestMapping("/api/usuarios")
public class UsuarioControlador {
    private static final Logger logger = LoggerFactory.getLogger(UsuarioControlador.class);

    private final IUsuarioServicio usuarioServicio;
    private final JwtServicio jwtServicio;

    public UsuarioControlador(IUsuarioServicio usuarioServicio, JwtServicio jwtServicio) {
        this.usuarioServicio = usuarioServicio;
        this.jwtServicio = jwtServicio;
    }

    @PostMapping(value = "/iniciar-sesion", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<AuthRespuestaDTO> iniciarSesion(@RequestBody LoginRequest request) {
        if (request == null) {
            return ResponseEntity.badRequest().body(new AuthRespuestaDTO(
                    false,
                    "Debe enviar correo y contrasena",
                    null,
                    null));
        }

        String identificador = limpiar(request.getCorreo());
        String contrasena = limpiar(request.getContrasena());

        if (identificador == null || contrasena == null) {
            return ResponseEntity.badRequest().body(new AuthRespuestaDTO(
                    false,
                    "Debe enviar correo y contrasena",
                    null,
                    null));
        }

        if (!usuarioServicio.existePorCorreoONombreUsuario(identificador)) {
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(new AuthRespuestaDTO(
                    false,
                    "El usuario no existe en la base de datos",
                    null,
                    null));
        }

        Usuario usuarioEncontrado = usuarioServicio.iniciarSesion(identificador, contrasena).orElse(null);

        if (usuarioEncontrado == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(new AuthRespuestaDTO(
                    false,
                    "Contrasena incorrecta",
                    null,
                    null));
        }

        return ResponseEntity.ok(new AuthRespuestaDTO(
                true,
                "Inicio de sesion exitoso",
                jwtServicio.generarToken(usuarioEncontrado),
                UsuarioRespuestaDTO.desdeUsuario(usuarioEncontrado)));
    }

    @PostMapping(value = "/registrar", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<AuthRespuestaDTO> registrarUsuario(@RequestBody RegistroUsuarioRequest request) {
        return registrarUsuarioInterno(request, false);
    }

    private ResponseEntity<AuthRespuestaDTO> registrarUsuarioInterno(RegistroUsuarioRequest request,
            boolean permitirRolesPrivilegiados) {
        if (request == null) {
            return ResponseEntity.badRequest().body(new AuthRespuestaDTO(
                    false,
                    "Debe enviar nombre, correo, rol y contrasena",
                    null,
                    null));
        }

        String nombre = limpiar(request.getNombre());
        String correo = limpiar(request.getCorreo());
        String rolTexto = limpiar(request.getRol());
        String contrasena = limpiar(request.getContrasena());

        if (nombre == null || correo == null || rolTexto == null || contrasena == null) {
            return ResponseEntity.badRequest().body(new AuthRespuestaDTO(
                    false,
                    "Debe enviar nombre, correo, rol y contrasena",
                    null,
                    null));
        }

        Rol rol;
        try {
            rol = Rol.valueOf(rolTexto.toUpperCase());
        } catch (IllegalArgumentException error) {
            return ResponseEntity.badRequest().body(new AuthRespuestaDTO(
                    false,
                    "Rol invalido. Use ESTUDIANTE, DOCENTE o ADMINISTRADOR",
                    null,
                    null));
        }

        if (!permitirRolesPrivilegiados && rol != Rol.ESTUDIANTE) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(new AuthRespuestaDTO(
                    false,
                    "El registro publico solo permite crear usuarios ESTUDIANTE",
                    null,
                    null));
        }

        if (usuarioServicio.existePorCorreo(correo)) {
            return ResponseEntity.status(HttpStatus.CONFLICT).body(new AuthRespuestaDTO(
                    false,
                    "El usuario ya existe en la base de datos",
                    null,
                    null));
        }

        Usuario usuario = new Usuario(null, nombre, correo, rol, contrasena);
        Usuario usuarioGuardado = usuarioServicio.registrar(usuario);

        return ResponseEntity.status(HttpStatus.CREATED).body(new AuthRespuestaDTO(
                true,
                "Usuario registrado correctamente",
                null,
                UsuarioRespuestaDTO.desdeUsuario(usuarioGuardado)));
    }

    @GetMapping
    public ResponseEntity<?> listarUsuarios(Authentication authentication) {
        if (!tieneRol(authentication, Rol.DOCENTE) && !tieneRol(authentication, Rol.ADMINISTRADOR)) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(Map.of(
                    "mensaje", "Solo el docente y el administrador pueden listar usuarios"));
        }

        List<UsuarioRespuestaDTO> usuarios = usuarioServicio.listar().stream()
                .map(UsuarioRespuestaDTO::desdeUsuario)
                .toList();

        return ResponseEntity.ok(usuarios);
    }

    @GetMapping("/me")
    public ResponseEntity<?> obtenerUsuarioAutenticado(Authentication authentication) {
        if (authentication == null) {
            logger.warn("No se encontro usuario autenticado al consultar /api/usuarios/me");
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(Map.of(
                    "mensaje", "No hay usuario autenticado"));
        }

        Usuario usuario = usuarioServicio.buscarPorCorreo(authentication.getName()).orElse(null);

        if (usuario == null) {
            logger.warn("No se encontro usuario autenticado con correo {}", authentication.getName());
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(Map.of(
                    "mensaje", "Usuario autenticado no encontrado"));
        }

        return ResponseEntity.ok(UsuarioSesionDTO.desdeUsuario(usuario));
    }

    @GetMapping("/{id}")
    public ResponseEntity<?> buscarUsuario(@PathVariable Long id, Authentication authentication) {
        Usuario usuario = usuarioServicio.buscarPorId(id).orElse(null);

        if (usuario == null) {
            return ResponseEntity.notFound().build();
        }

        if (!puedeConsultarUsuario(authentication, usuario)) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(Map.of(
                    "mensaje", "No tiene permiso para consultar este usuario"));
        }

        return ResponseEntity.ok(UsuarioRespuestaDTO.desdeUsuario(usuario));
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<AuthRespuestaDTO> crearUsuario(@RequestBody RegistroUsuarioRequest request,
            Authentication authentication) {
        if (!tieneRol(authentication, Rol.ADMINISTRADOR)) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(new AuthRespuestaDTO(
                    false,
                    "Solo el administrador puede crear usuarios desde el CRUD",
                    null,
                    null));
        }

        return registrarUsuarioInterno(request, true);
    }

    @PutMapping(value = "/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> actualizarUsuario(@PathVariable Long id, @RequestBody ActualizarUsuarioRequest request,
            Authentication authentication) {
        if (request == null) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "Debe enviar los datos del usuario"));
        }

        Usuario usuario = usuarioServicio.buscarPorId(id).orElse(null);

        if (usuario == null) {
            return ResponseEntity.notFound().build();
        }

        if (!puedeActualizarUsuario(authentication, usuario)) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(Map.of(
                    "mensaje", "No tiene permiso para actualizar este usuario"));
        }

        String nombre = limpiar(request.getNombre());
        String correo = limpiar(request.getCorreo());

        if (nombre == null || correo == null) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "Debe enviar nombre y correo"));
        }

        Usuario usuarioConCorreo = usuarioServicio.buscarPorCorreo(correo).orElse(null);
        if (usuarioConCorreo != null && !usuarioConCorreo.getId().equals(id)) {
            return ResponseEntity.status(HttpStatus.CONFLICT).body(Map.of(
                    "mensaje", "El correo ya esta registrado por otro usuario"));
        }

        usuario.setNombre(nombre);
        usuario.setCorreo(correo);

        ResponseEntity<Map<String, String>> errorProgreso = validarYAsignarProgreso(
                usuario,
                request.getNivelActual(),
                request.getPuntaje());

        if (errorProgreso != null) {
            return errorProgreso;
        }

        if (tieneRol(authentication, Rol.ADMINISTRADOR) && limpiar(request.getRol()) != null) {
            Rol rol = obtenerRol(request.getRol());
            if (rol == null) {
                return ResponseEntity.badRequest().body(Map.of(
                        "mensaje", "Rol invalido. Use ESTUDIANTE, DOCENTE o ADMINISTRADOR"));
            }

            if (esUsuarioAutenticado(authentication, usuario) && usuario.getRol() == Rol.ADMINISTRADOR
                    && rol != Rol.ADMINISTRADOR) {
                return ResponseEntity.status(HttpStatus.FORBIDDEN).body(Map.of(
                        "mensaje", "No puede quitarse su propio rol de administrador"));
            }

            usuario.setRol(rol);
        }

        Usuario usuarioActualizado = usuarioServicio.actualizar(usuario);
        return ResponseEntity.ok(UsuarioRespuestaDTO.desdeUsuario(usuarioActualizado));
    }

    @PatchMapping(value = "/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> actualizarProgresoUsuario(@PathVariable Long id,
            @RequestBody ActualizarProgresoUsuarioRequest request,
            Authentication authentication) {
        if (request == null) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "Debe enviar nivelActual o puntaje"));
        }

        Usuario usuario = usuarioServicio.buscarPorId(id).orElse(null);

        if (usuario == null) {
            return ResponseEntity.notFound().build();
        }

        if (!puedeActualizarUsuario(authentication, usuario)) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(Map.of(
                    "mensaje", "No tiene permiso para actualizar este usuario"));
        }

        if (request.getNivelActual() == null && request.getPuntaje() == null) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "Debe enviar nivelActual o puntaje"));
        }

        ResponseEntity<Map<String, String>> errorProgreso = validarYAsignarProgreso(
                usuario,
                request.getNivelActual(),
                request.getPuntaje());

        if (errorProgreso != null) {
            return errorProgreso;
        }

        Usuario usuarioActualizado = usuarioServicio.actualizar(usuario);
        return ResponseEntity.ok(UsuarioRespuestaDTO.desdeUsuario(usuarioActualizado));
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<?> eliminarUsuario(@PathVariable Long id, Authentication authentication) {
        if (!tieneRol(authentication, Rol.ADMINISTRADOR)) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(Map.of(
                    "mensaje", "Solo el administrador puede eliminar usuarios"));
        }

        Usuario usuario = usuarioServicio.buscarPorId(id).orElse(null);

        if (usuario == null) {
            return ResponseEntity.notFound().build();
        }

        if (esUsuarioAutenticado(authentication, usuario)) {
            return ResponseEntity.status(HttpStatus.FORBIDDEN).body(Map.of(
                    "mensaje", "No puede eliminar su propia cuenta de administrador"));
        }

        usuarioServicio.eliminarPorId(id);
        return ResponseEntity.noContent().build();
    }

    @GetMapping("/perfil")
    public ResponseEntity<Map<String, Object>> obtenerPerfil(Authentication authentication) {
        List<String> roles = authentication.getAuthorities().stream()
                .map(GrantedAuthority::getAuthority)
                .toList();

        return ResponseEntity.ok(Map.of(
                "correo", authentication.getName(),
                "roles", roles));
    }

    private boolean puedeConsultarUsuario(Authentication authentication, Usuario usuario) {
        return tieneRol(authentication, Rol.ADMINISTRADOR)
                || tieneRol(authentication, Rol.DOCENTE)
                || usuario.getCorreo().equals(authentication.getName());
    }

    private boolean puedeActualizarUsuario(Authentication authentication, Usuario usuario) {
        return tieneRol(authentication, Rol.ADMINISTRADOR)
                || usuario.getCorreo().equals(authentication.getName());
    }

    private boolean esUsuarioAutenticado(Authentication authentication, Usuario usuario) {
        return authentication != null && usuario.getCorreo().equals(authentication.getName());
    }

    private boolean tieneRol(Authentication authentication, Rol rol) {
        if (authentication == null) {
            return false;
        }

        return authentication.getAuthorities().stream()
                .map(GrantedAuthority::getAuthority)
                .anyMatch(authority -> authority.equals("ROLE_" + rol.name()));
    }

    private Rol obtenerRol(String rolTexto) {
        String rolLimpio = limpiar(rolTexto);

        if (rolLimpio == null) {
            return null;
        }

        try {
            return Rol.valueOf(rolLimpio.toUpperCase());
        } catch (IllegalArgumentException error) {
            return null;
        }
    }

    private String limpiar(String valor) {
        if (valor == null || valor.isBlank()) {
            return null;
        }

        return valor.trim();
    }

    private ResponseEntity<Map<String, String>> validarYAsignarProgreso(Usuario usuario, Integer nivelActual,
            Integer puntaje) {
        if (nivelActual != null) {
            if (nivelActual < 1 || nivelActual > 5) {
                return ResponseEntity.badRequest().body(Map.of(
                        "mensaje", "El nivel actual debe ser un numero entero entre 1 y 5"));
            }

            usuario.setNivelActual(nivelActual);
        }

        if (puntaje != null) {
            usuario.setPuntaje(puntaje);
        }

        return null;
    }
}
