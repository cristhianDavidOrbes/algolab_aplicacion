package com.algolab.backend_werb_mr.controladores;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertInstanceOf;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.junit.jupiter.api.Test;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import com.algolab.backend_werb_mr.dtos.DescripcionNivelDTO;
import com.algolab.backend_werb_mr.modelos.DescripcionNivel;
import com.algolab.backend_werb_mr.servicios.IDescripcionNivelServicio;

class DescripcionNivelControladorTest {
    private final DescripcionNivelServicioPrueba descripcionNivelServicio = new DescripcionNivelServicioPrueba();
    private final DescripcionNivelControlador controlador = new DescripcionNivelControlador(descripcionNivelServicio);

    @Test
    void crearRechazaDatosObligatoriosFaltantes() {
        ResponseEntity<?> respuesta = controlador.crear(new DescripcionNivelDTO());

        assertEquals(HttpStatus.BAD_REQUEST, respuesta.getStatusCode());
    }

    @Test
    void crearGuardaDescripcionNivelValida() {
        ResponseEntity<?> respuesta = controlador.crear(
                new DescripcionNivelDTO(null, "Basico", "Descripcion del nivel basico", 1, "Resolver ejercicios",
                        true, null, null));

        assertEquals(HttpStatus.CREATED, respuesta.getStatusCode());
        DescripcionNivelDTO descripcion = assertInstanceOf(DescripcionNivelDTO.class, respuesta.getBody());
        assertEquals(1L, descripcion.getId());
        assertEquals("Basico", descripcion.getNombre());
        assertEquals(1, descripcion.getNivel());
    }

    @Test
    void actualizarRetornaNotFoundSiNoExiste() {
        ResponseEntity<?> respuesta = controlador.actualizar(
                99L,
                new DescripcionNivelDTO(null, "Intermedio", "Descripcion", 2, null, true, null, null));

        assertEquals(HttpStatus.NOT_FOUND, respuesta.getStatusCode());
    }

    @Test
    void eliminarDescripcionNivelExistente() {
        DescripcionNivel descripcion = descripcionNivelServicio.guardar(
                new DescripcionNivel(null, "Avanzado", "Descripcion avanzada", 3, null, true));

        ResponseEntity<Void> respuesta = controlador.eliminar(descripcion.getId());

        assertEquals(HttpStatus.NO_CONTENT, respuesta.getStatusCode());
    }

    private static class DescripcionNivelServicioPrueba implements IDescripcionNivelServicio {
        private final Map<Long, DescripcionNivel> descripciones = new LinkedHashMap<>();
        private long siguienteId = 1;

        @Override
        public DescripcionNivel guardar(DescripcionNivel descripcionNivel) {
            return guardarDescripcion(descripcionNivel);
        }

        @Override
        public Optional<DescripcionNivel> buscarPorId(Long id) {
            return Optional.ofNullable(descripciones.get(id));
        }

        @Override
        public List<DescripcionNivel> listar() {
            return new ArrayList<>(descripciones.values());
        }

        @Override
        public DescripcionNivel actualizar(DescripcionNivel descripcionNivel) {
            return guardarDescripcion(descripcionNivel);
        }

        @Override
        public void eliminarPorId(Long id) {
            descripciones.remove(id);
        }

        private DescripcionNivel guardarDescripcion(DescripcionNivel descripcionNivel) {
            if (descripcionNivel.getId() == null) {
                descripcionNivel.setId(siguienteId++);
            }

            if (descripcionNivel.getActivo() == null) {
                descripcionNivel.setActivo(true);
            }

            if (descripcionNivel.getFechaCreacion() == null) {
                descripcionNivel.setFechaCreacion(LocalDateTime.now());
            }

            descripcionNivel.setFechaActualizacion(LocalDateTime.now());
            descripciones.put(descripcionNivel.getId(), descripcionNivel);
            return descripcionNivel;
        }
    }
}
