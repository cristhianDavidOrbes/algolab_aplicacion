package com.algolab.backend_werb_mr.controladores;

import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.algolab.backend_werb_mr.dtos.GuardarProgresoRequest;
import com.algolab.backend_werb_mr.dtos.ProgresoUsuarioDTO;
import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.servicios.IProgresoServicio;

@RestController
@RequestMapping("/api/progreso")
public class ProgresoControlador {
    private static final Logger logger = LoggerFactory.getLogger(ProgresoControlador.class);

    private final IProgresoServicio progresoServicio;

    public ProgresoControlador(IProgresoServicio progresoServicio) {
        this.progresoServicio = progresoServicio;
    }

    @GetMapping("/me")
    public ResponseEntity<?> consultarProgreso(Authentication authentication) {
        Usuario usuario = obtenerUsuario(authentication);

        if (usuario == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(Map.of(
                    "mensaje", "Usuario autenticado no encontrado"));
        }

        ProgresoUsuarioDTO progreso = progresoServicio.consultarProgreso(usuario);
        return ResponseEntity.ok(progreso);
    }

    @PostMapping(consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> guardarProgreso(@RequestBody GuardarProgresoRequest request,
            Authentication authentication) {
        ResponseEntity<Map<String, String>> error = validar(request);

        if (error != null) {
            return error;
        }

        Usuario usuario = obtenerUsuario(authentication);

        if (usuario == null) {
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).body(Map.of(
                    "mensaje", "Usuario autenticado no encontrado"));
        }

        ProgresoUsuarioDTO progreso = progresoServicio.guardarProgreso(usuario, request);
        return ResponseEntity.ok(progreso);
    }

    private Usuario obtenerUsuario(Authentication authentication) {
        if (authentication == null) {
            logger.warn("No se encontro usuario autenticado al consultar progreso");
            return null;
        }

        return progresoServicio.buscarUsuarioAutenticado(authentication.getName()).orElse(null);
    }

    private ResponseEntity<Map<String, String>> validar(GuardarProgresoRequest request) {
        if (request == null) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "Debe enviar los datos del progreso"));
        }

        if (request.getNivel() == null || request.getNivel() < 1) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "El nivel debe ser mayor o igual a 1"));
        }

        if (request.getPuntaje() == null || request.getPuntaje() < 0) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "El puntaje no debe ser negativo"));
        }

        if (request.getTiempoRestante() == null || request.getTiempoRestante() < 0) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "El tiempoRestante no debe ser negativo"));
        }

        if (request.getIntentos() == null || request.getIntentos() < 0) {
            return ResponseEntity.badRequest().body(Map.of(
                    "mensaje", "Los intentos deben ser mayores o iguales a 0"));
        }

        if (request.getCompletado() == null) {
            request.setCompletado(false);
        }

        return null;
    }
}
