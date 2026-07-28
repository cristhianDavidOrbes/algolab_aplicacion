package com.algolab.backend_werb_mr.controladores;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertInstanceOf;
import static org.junit.jupiter.api.Assertions.assertNull;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.junit.jupiter.api.Test;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.authority.SimpleGrantedAuthority;

import com.algolab.backend_werb_mr.dtos.ActualizarUsuarioRequest;
import com.algolab.backend_werb_mr.dtos.AuthRespuestaDTO;
import com.algolab.backend_werb_mr.dtos.LoginRequest;
import com.algolab.backend_werb_mr.dtos.RegistroUsuarioRequest;
import com.algolab.backend_werb_mr.dtos.UsuarioRespuestaDTO;
import com.algolab.backend_werb_mr.modelos.Rol;
import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.seguridad.JwtServicio;
import com.algolab.backend_werb_mr.servicios.IUsuarioServicio;

class UsuarioControladorTest {
    private final UsuarioServicioPrueba usuarioServicio = new UsuarioServicioPrueba();
    private final UsuarioControlador controlador = new UsuarioControlador(usuarioServicio, new JwtServicioPrueba());

    @Test
    void registroPublicoRechazaRolesPrivilegiados() {
        ResponseEntity<AuthRespuestaDTO> respuesta = controlador.registrarUsuario(
                solicitudRegistro("Admin", "admin@test.com", Rol.ADMINISTRADOR, "123456"));

        assertEquals(HttpStatus.FORBIDDEN, respuesta.getStatusCode());
        assertFalse(respuesta.getBody().isExitoso());
    }

    @Test
    void registroPublicoCreaEstudianteSinToken() {
        ResponseEntity<AuthRespuestaDTO> respuesta = controlador.registrarUsuario(
                solicitudRegistro("Estudiante", "estudiante@test.com", Rol.ESTUDIANTE, "123456"));

        assertEquals(HttpStatus.CREATED, respuesta.getStatusCode());
        assertNull(respuesta.getBody().getToken());
        assertEquals(Rol.ESTUDIANTE, respuesta.getBody().getUsuario().getRol());
    }

    @Test
    void inicioSesionExitosoConCorreoDevuelveToken() {
        Usuario usuario = usuarioServicio.registrar(
                new Usuario(null, "Estudiante", "estudiante@test.com", Rol.ESTUDIANTE, "123456"));
        usuario.setNombreUsuario("estudiante");

        LoginRequest request = new LoginRequest();
        request.setCorreo("estudiante@test.com");
        request.setContrasena("123456");

        ResponseEntity<AuthRespuestaDTO> respuesta = controlador.iniciarSesion(request);

        assertEquals(HttpStatus.OK, respuesta.getStatusCode());
        assertEquals("token-prueba", respuesta.getBody().getToken());
    }

    @Test
    void inicioSesionExitosoConNombreUsuarioDevuelveToken() {
        Usuario usuario = usuarioServicio.registrar(
                new Usuario(null, "Estudiante", "estudiante@test.com", Rol.ESTUDIANTE, "123456"));
        usuario.setNombreUsuario("estudiante");

        LoginRequest request = new LoginRequest();
        request.setCorreo("estudiante");
        request.setContrasena("123456");

        ResponseEntity<AuthRespuestaDTO> respuesta = controlador.iniciarSesion(request);

        assertEquals(HttpStatus.OK, respuesta.getStatusCode());
        assertEquals("token-prueba", respuesta.getBody().getToken());
        assertEquals("estudiante", respuesta.getBody().getUsuario().getNombreUsuario());
    }

    @Test
    void inicioSesionRechazaContrasenaIncorrecta() {
        Usuario usuario = usuarioServicio.registrar(
                new Usuario(null, "Estudiante", "estudiante@test.com", Rol.ESTUDIANTE, "123456"));
        usuario.setNombreUsuario("estudiante");

        LoginRequest request = new LoginRequest();
        request.setCorreo("estudiante");
        request.setContrasena("incorrecta");

        ResponseEntity<AuthRespuestaDTO> respuesta = controlador.iniciarSesion(request);

        assertEquals(HttpStatus.UNAUTHORIZED, respuesta.getStatusCode());
        assertFalse(respuesta.getBody().isExitoso());
    }

    @Test
    void inicioSesionRechazaUsuarioInexistente() {
        LoginRequest request = new LoginRequest();
        request.setCorreo("noexiste");
        request.setContrasena("123456");

        ResponseEntity<AuthRespuestaDTO> respuesta = controlador.iniciarSesion(request);

        assertEquals(HttpStatus.NOT_FOUND, respuesta.getStatusCode());
        assertFalse(respuesta.getBody().isExitoso());
    }

    @Test
    void administradorPuedeCrearDocentesDesdeCrud() {
        ResponseEntity<AuthRespuestaDTO> respuesta = controlador.crearUsuario(
                solicitudRegistro("Docente", "docente@test.com", Rol.DOCENTE, "123456"),
                autenticacion("admin@test.com", Rol.ADMINISTRADOR));

        assertEquals(HttpStatus.CREATED, respuesta.getStatusCode());
        assertNull(respuesta.getBody().getToken());
        assertEquals(Rol.DOCENTE, respuesta.getBody().getUsuario().getRol());
    }

    @Test
    void estudianteNoPuedeListarUsuarios() {
        ResponseEntity<?> respuesta = controlador.listarUsuarios(autenticacion("estudiante@test.com", Rol.ESTUDIANTE));

        assertEquals(HttpStatus.FORBIDDEN, respuesta.getStatusCode());
    }

    @Test
    void docentePuedeListarUsuarios() {
        usuarioServicio.registrar(new Usuario(null, "Estudiante", "estudiante@test.com", Rol.ESTUDIANTE, "123456"));

        ResponseEntity<?> respuesta = controlador.listarUsuarios(autenticacion("docente@test.com", Rol.DOCENTE));

        assertEquals(HttpStatus.OK, respuesta.getStatusCode());
        assertEquals(1, assertInstanceOf(List.class, respuesta.getBody()).size());
    }

    @Test
    void estudianteSoloPuedeActualizarSuUsuarioYSuRolNoCambia() {
        Usuario estudiante = usuarioServicio.registrar(
                new Usuario(null, "Estudiante", "estudiante@test.com", Rol.ESTUDIANTE, "123456"));
        Usuario otroUsuario = usuarioServicio.registrar(
                new Usuario(null, "Otro", "otro@test.com", Rol.ESTUDIANTE, "123456"));

        ActualizarUsuarioRequest solicitud = solicitudActualizacion("Estudiante Editado", "estudiante@test.com",
                Rol.ADMINISTRADOR);
        ResponseEntity<?> respuestaPropia = controlador.actualizarUsuario(
                estudiante.getId(),
                solicitud,
                autenticacion("estudiante@test.com", Rol.ESTUDIANTE));

        assertEquals(HttpStatus.OK, respuestaPropia.getStatusCode());
        UsuarioRespuestaDTO usuario = assertInstanceOf(UsuarioRespuestaDTO.class, respuestaPropia.getBody());
        assertEquals("Estudiante Editado", usuario.getNombre());
        assertEquals(Rol.ESTUDIANTE, usuario.getRol());

        ResponseEntity<?> respuestaAjena = controlador.actualizarUsuario(
                otroUsuario.getId(),
                solicitudActualizacion("Otro Editado", "otro@test.com", Rol.ESTUDIANTE),
                autenticacion("estudiante@test.com", Rol.ESTUDIANTE));

        assertEquals(HttpStatus.FORBIDDEN, respuestaAjena.getStatusCode());
    }

    @Test
    void administradorPuedeCambiarRolYEliminarUsuarios() {
        Usuario usuario = usuarioServicio.registrar(
                new Usuario(null, "Usuario", "usuario@test.com", Rol.ESTUDIANTE, "123456"));

        ResponseEntity<?> respuestaActualizar = controlador.actualizarUsuario(
                usuario.getId(),
                solicitudActualizacion("Usuario", "usuario@test.com", Rol.DOCENTE),
                autenticacion("admin@test.com", Rol.ADMINISTRADOR));

        assertEquals(HttpStatus.OK, respuestaActualizar.getStatusCode());
        assertEquals(Rol.DOCENTE, assertInstanceOf(UsuarioRespuestaDTO.class, respuestaActualizar.getBody()).getRol());

        ResponseEntity<?> respuestaEliminar = controlador.eliminarUsuario(
                usuario.getId(),
                autenticacion("admin@test.com", Rol.ADMINISTRADOR));

        assertEquals(HttpStatus.NO_CONTENT, respuestaEliminar.getStatusCode());
    }

    @Test
    void administradorNoPuedeQuitarSuPropioRolAdministrador() {
        Usuario administrador = usuarioServicio.registrar(
                new Usuario(null, "Admin", "admin@test.com", Rol.ADMINISTRADOR, "123456"));

        ResponseEntity<?> respuesta = controlador.actualizarUsuario(
                administrador.getId(),
                solicitudActualizacion("Admin", "admin@test.com", Rol.DOCENTE),
                autenticacion("admin@test.com", Rol.ADMINISTRADOR));

        assertEquals(HttpStatus.FORBIDDEN, respuesta.getStatusCode());
    }

    @Test
    void administradorNoPuedeEliminarSuPropiaCuenta() {
        Usuario administrador = usuarioServicio.registrar(
                new Usuario(null, "Admin", "admin@test.com", Rol.ADMINISTRADOR, "123456"));

        ResponseEntity<?> respuesta = controlador.eliminarUsuario(
                administrador.getId(),
                autenticacion("admin@test.com", Rol.ADMINISTRADOR));

        assertEquals(HttpStatus.FORBIDDEN, respuesta.getStatusCode());
    }

    private static RegistroUsuarioRequest solicitudRegistro(String nombre, String correo, Rol rol, String contrasena) {
        RegistroUsuarioRequest request = new RegistroUsuarioRequest();
        request.setNombre(nombre);
        request.setCorreo(correo);
        request.setRol(rol.name());
        request.setContrasena(contrasena);
        return request;
    }

    private static ActualizarUsuarioRequest solicitudActualizacion(String nombre, String correo, Rol rol) {
        ActualizarUsuarioRequest request = new ActualizarUsuarioRequest();
        request.setNombre(nombre);
        request.setCorreo(correo);
        request.setRol(rol.name());
        return request;
    }

    private static Authentication autenticacion(String correo, Rol rol) {
        return new UsernamePasswordAuthenticationToken(
                correo,
                null,
                List.of(new SimpleGrantedAuthority("ROLE_" + rol.name())));
    }

    private static class JwtServicioPrueba extends JwtServicio {
        @Override
        public String generarToken(Usuario usuario) {
            return "token-prueba";
        }
    }

    private static class UsuarioServicioPrueba implements IUsuarioServicio {
        private final Map<Long, Usuario> usuarios = new LinkedHashMap<>();
        private long siguienteId = 1;

        @Override
        public Usuario guardar(Usuario usuario) {
            return guardarUsuario(usuario);
        }

        @Override
        public Usuario registrar(Usuario usuario) {
            return guardarUsuario(usuario);
        }

        @Override
        public Optional<Usuario> iniciarSesion(String identificador, String contrasena) {
            return buscarPorCorreoONombreUsuario(identificador)
                    .filter(usuario -> usuario.getContrasena().equals(contrasena));
        }

        @Override
        public Optional<Usuario> buscarPorId(Long id) {
            return Optional.ofNullable(usuarios.get(id));
        }

        @Override
        public List<Usuario> listar() {
            return new ArrayList<>(usuarios.values());
        }

        @Override
        public List<Usuario> listarRankingEstudiantes() {
            return usuarios.values().stream()
                    .filter(usuario -> usuario.getRol() == Rol.ESTUDIANTE)
                    .sorted((a, b) -> Integer.compare(b.getPuntaje(), a.getPuntaje()))
                    .toList();
        }

        @Override
        public Usuario actualizar(Usuario usuario) {
            return guardarUsuario(usuario);
        }

        @Override
        public void eliminarPorId(Long id) {
            usuarios.remove(id);
        }

        @Override
        public Optional<Usuario> buscarPorCorreo(String correo) {
            return usuarios.values().stream()
                    .filter(usuario -> usuario.getCorreo().equals(correo))
                    .findFirst();
        }

        @Override
        public boolean existePorCorreo(String correo) {
            return buscarPorCorreo(correo).isPresent();
        }

        @Override
        public boolean existePorNombreUsuario(String nombreUsuario) {
            return usuarios.values().stream()
                    .anyMatch(usuario -> nombreUsuario.equals(usuario.getNombreUsuario()));
        }

        @Override
        public boolean existePorCorreoONombreUsuario(String identificador) {
            return buscarPorCorreoONombreUsuario(identificador).isPresent();
        }

        private Optional<Usuario> buscarPorCorreoONombreUsuario(String identificador) {
            return usuarios.values().stream()
                    .filter(usuario -> usuario.getCorreo().equals(identificador)
                            || identificador.equals(usuario.getNombreUsuario()))
                    .findFirst();
        }

        private Usuario guardarUsuario(Usuario usuario) {
            if (usuario.getId() == null) {
                usuario.setId(siguienteId++);
            }

            if (usuario.getNombreUsuario() == null) {
                usuario.setNombreUsuario(usuario.getCorreo().split("@", 2)[0]);
            }

            usuarios.put(usuario.getId(), usuario);
            return usuario;
        }
    }
}
