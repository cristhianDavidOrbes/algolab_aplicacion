package com.algolab.backend_werb_mr.controladores;

import java.util.List;
import java.util.Map;

import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.algolab.backend_werb_mr.dtos.DescripcionNivelDTO;
import com.algolab.backend_werb_mr.modelos.DescripcionNivel;
import com.algolab.backend_werb_mr.servicios.IDescripcionNivelServicio;

@RestController
@RequestMapping("/api/niveles")
public class DescripcionNivelControlador {
    private final IDescripcionNivelServicio descripcionNivelServicio;

    public DescripcionNivelControlador(IDescripcionNivelServicio descripcionNivelServicio) {
        this.descripcionNivelServicio = descripcionNivelServicio;
    }

    @GetMapping
    public ResponseEntity<List<DescripcionNivelDTO>> listar() {
        List<DescripcionNivelDTO> descripciones = descripcionNivelServicio.listar().stream()
                .map(DescripcionNivelDTO::desdeModelo)
                .toList();

        return ResponseEntity.ok(descripciones);
    }

    @GetMapping("/{id}")
    public ResponseEntity<DescripcionNivelDTO> buscarPorId(@PathVariable Long id) {
        return descripcionNivelServicio.buscarPorId(id)
                .map(DescripcionNivelDTO::desdeModelo)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> crear(@RequestBody DescripcionNivelDTO request) {
        ResponseEntity<Map<String, String>> error = validar(request);
        if (error != null) {
            return error;
        }

        DescripcionNivel descripcionNivel = request.aModelo();
        descripcionNivel.setId(null);

        DescripcionNivel descripcionGuardada = descripcionNivelServicio.guardar(descripcionNivel);

        return ResponseEntity.status(HttpStatus.CREATED).body(DescripcionNivelDTO.desdeModelo(descripcionGuardada));
    }

    @PutMapping(value = "/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> actualizar(@PathVariable Long id, @RequestBody DescripcionNivelDTO request) {
        ResponseEntity<Map<String, String>> error = validar(request);
        if (error != null) {
            return error;
        }

        return descripcionNivelServicio.buscarPorId(id)
                .map(descripcionExistente -> {
                    descripcionExistente.setNombre(limpiar(request.getNombre()));
                    descripcionExistente.setDescripcion(limpiar(request.getDescripcion()));
                    descripcionExistente.setNivel(request.getNivel());
                    descripcionExistente.setObjetivo(limpiar(request.getObjetivo()));
                    descripcionExistente.setActivo(request.getActivo() == null || request.getActivo());

                    DescripcionNivel descripcionActualizada = descripcionNivelServicio.actualizar(descripcionExistente);
                    return ResponseEntity.ok(DescripcionNivelDTO.desdeModelo(descripcionActualizada));
                })
                .orElse(ResponseEntity.notFound().build());
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> eliminar(@PathVariable Long id) {
        if (descripcionNivelServicio.buscarPorId(id).isEmpty()) {
            return ResponseEntity.notFound().build();
        }

        descripcionNivelServicio.eliminarPorId(id);
        return ResponseEntity.noContent().build();
    }

    private ResponseEntity<Map<String, String>> validar(DescripcionNivelDTO request) {
        if (request == null || limpiar(request.getNombre()) == null || limpiar(request.getDescripcion()) == null
                || request.getNivel() == null) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "Debe enviar nombre, descripcion y nivel"));
        }

        if (request.getNivel() < 1) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "El nivel debe ser mayor o igual a 1"));
        }

        return null;
    }

    private String limpiar(String valor) {
        if (valor == null || valor.isBlank()) {
            return null;
        }

        return valor.trim();
    }
}
