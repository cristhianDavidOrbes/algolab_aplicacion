package com.algolab.backend_werb_mr.controladores;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.List;
import java.util.Optional;

import org.junit.jupiter.api.Test;

import com.algolab.backend_werb_mr.dtos.RankingRespuestaDTO;
import com.algolab.backend_werb_mr.modelos.Rol;
import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.servicios.IUsuarioServicio;

class RankingControladorTest {
    @Test
    void devuelveRankingOrdenadoSinDatosPrivados() {
        Usuario primero = estudiante(4L, "Ada", "ada", 3, 900);
        Usuario segundo = estudiante(8L, "Linus", "linus", 2, 650);
        RankingControlador controlador = new RankingControlador(
                new UsuarioServicioRankingPrueba(List.of(primero, segundo)));

        RankingRespuestaDTO respuesta = controlador.consultarRanking();

        assertEquals(2, respuesta.getTotal());
        assertEquals(1, respuesta.getEstudiantes().get(0).getPosicion());
        assertEquals(4L, respuesta.getEstudiantes().get(0).getUsuarioId());
        assertEquals("Ada", respuesta.getEstudiantes().get(0).getNombre());
        assertEquals(900, respuesta.getEstudiantes().get(0).getPuntaje());
    }

    @Test
    void devuelveTodosLosEstudiantes() {
        RankingControlador controlador = new RankingControlador(
                new UsuarioServicioRankingPrueba(List.of(
                        estudiante(1L, "Uno", "uno", 2, 300),
                        estudiante(2L, "Dos", "dos", 2, 200))));

        RankingRespuestaDTO respuesta = controlador.consultarRanking();

        assertEquals(2, respuesta.getTotal());
        assertEquals("Uno", respuesta.getEstudiantes().get(0).getNombre());
        assertEquals("Dos", respuesta.getEstudiantes().get(1).getNombre());
    }

    private static Usuario estudiante(Long id, String nombre, String nombreUsuario, int nivel, int puntaje) {
        Usuario usuario = new Usuario(id, nombre, nombreUsuario + "@test.com", Rol.ESTUDIANTE, "hash", nivel, puntaje);
        usuario.setNombreUsuario(nombreUsuario);
        return usuario;
    }

    private static class UsuarioServicioRankingPrueba implements IUsuarioServicio {
        private final List<Usuario> ranking;

        UsuarioServicioRankingPrueba(List<Usuario> ranking) {
            this.ranking = ranking;
        }

        @Override
        public List<Usuario> listarRankingEstudiantes() {
            return ranking;
        }

        @Override public Usuario guardar(Usuario usuario) { throw new UnsupportedOperationException(); }
        @Override public Usuario registrar(Usuario usuario) { throw new UnsupportedOperationException(); }
        @Override public Optional<Usuario> iniciarSesion(String correo, String contrasena) { return Optional.empty(); }
        @Override public Optional<Usuario> buscarPorId(Long id) { return Optional.empty(); }
        @Override public List<Usuario> listar() { return ranking; }
        @Override public Usuario actualizar(Usuario usuario) { throw new UnsupportedOperationException(); }
        @Override public void eliminarPorId(Long id) { throw new UnsupportedOperationException(); }
        @Override public Optional<Usuario> buscarPorCorreo(String correo) { return Optional.empty(); }
        @Override public boolean existePorCorreo(String correo) { return false; }
        @Override public boolean existePorNombreUsuario(String nombreUsuario) { return false; }
        @Override public boolean existePorCorreoONombreUsuario(String identificador) { return false; }
    }
}
