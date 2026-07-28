package com.algolab.backend_werb_mr.servicios;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Optional;

import org.junit.jupiter.api.Test;

import com.algolab.backend_werb_mr.dtos.GuardarProgresoRequest;
import com.algolab.backend_werb_mr.dtos.ProgresoNivelDTO;
import com.algolab.backend_werb_mr.dtos.ProgresoUsuarioDTO;
import com.algolab.backend_werb_mr.modelos.DescripcionNivel;
import com.algolab.backend_werb_mr.modelos.ProgresoNivel;
import com.algolab.backend_werb_mr.modelos.Rol;
import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.repositorio.IProgresoNivelRepositorio;

class ProgresoServicioTest {
    @Test
    void guardaMejorPuntajePorNivelYRecalculaTotalSinAcumularRepeticiones() {
        IProgresoNivelRepositorio progresoRepositorio = mock(IProgresoNivelRepositorio.class);
        IUsuarioServicio usuarioServicio = mock(IUsuarioServicio.class);
        IDescripcionNivelServicio descripcionNivelServicio = mock(IDescripcionNivelServicio.class);
        List<ProgresoNivel> progresosGuardados = new ArrayList<>();
        Usuario usuario = new Usuario(1L, "Estudiante", "estudiante@test.com", Rol.ESTUDIANTE, "123456");

        when(progresoRepositorio.findByUsuarioAndNivel(eq(usuario), any(Integer.class)))
                .thenAnswer(invocacion -> buscarProgreso(progresosGuardados, invocacion.getArgument(1)));
        when(progresoRepositorio.findByUsuarioOrderByNivelAsc(usuario))
                .thenAnswer(invocacion -> progresosGuardados.stream()
                        .sorted(Comparator.comparing(ProgresoNivel::getNivel))
                        .toList());
        when(progresoRepositorio.save(any(ProgresoNivel.class))).thenAnswer(invocacion -> {
            ProgresoNivel progreso = invocacion.getArgument(0);
            if (!progresosGuardados.contains(progreso)) {
                progresosGuardados.add(progreso);
            }
            return progreso;
        });
        when(usuarioServicio.actualizar(any(Usuario.class))).thenAnswer(invocacion -> invocacion.getArgument(0));
        when(descripcionNivelServicio.listar()).thenReturn(List.of(
                new DescripcionNivel(1L, "POO", "Nivel 1", 1, null, true),
                new DescripcionNivel(2L, "Vehiculos", "Nivel 2", 2, null, true)));

        ProgresoServicio servicio = new ProgresoServicio(
                progresoRepositorio,
                usuarioServicio,
                descripcionNivelServicio);

        ProgresoUsuarioDTO nivel1 = servicio.guardarProgreso(usuario, progreso(1, 100));
        ProgresoUsuarioDTO nivel2 = servicio.guardarProgreso(usuario, progreso(2, 130));
        ProgresoUsuarioDTO repeticionMejor = servicio.guardarProgreso(usuario, progreso(1, 120));
        ProgresoUsuarioDTO repeticionPeor = servicio.guardarProgreso(usuario, progreso(1, 80));

        assertEquals(100, nivel1.getPuntajeTotal());
        assertEquals(230, nivel2.getPuntajeTotal());
        assertEquals(250, repeticionMejor.getPuntajeTotal());
        assertEquals(250, repeticionPeor.getPuntajeTotal());
        assertEquals(2, repeticionPeor.getNivelActual());
        assertEquals(120, puntajeNivel(repeticionPeor, 1));
        assertEquals(130, puntajeNivel(repeticionPeor, 2));
    }

    private static GuardarProgresoRequest progreso(Integer nivel, Integer puntaje) {
        GuardarProgresoRequest request = new GuardarProgresoRequest();
        request.setNivel(nivel);
        request.setCompletado(true);
        request.setPuntaje(puntaje);
        request.setTiempoRestante(puntaje);
        request.setIntentos(1);
        return request;
    }

    private static Optional<ProgresoNivel> buscarProgreso(List<ProgresoNivel> progresos, Integer nivel) {
        return progresos.stream()
                .filter(progreso -> progreso.getNivel().equals(nivel))
                .findFirst();
    }

    private static Integer puntajeNivel(ProgresoUsuarioDTO progresoUsuario, Integer nivel) {
        return progresoUsuario.getNiveles().stream()
                .filter(progreso -> progreso.getNivel().equals(nivel))
                .findFirst()
                .map(ProgresoNivelDTO::getPuntaje)
                .orElseThrow();
    }
}
