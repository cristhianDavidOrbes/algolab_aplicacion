package com.algolab.backend_werb_mr.servicios;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import com.algolab.backend_werb_mr.dtos.GuardarProgresoRequest;
import com.algolab.backend_werb_mr.dtos.ProgresoNivelDTO;
import com.algolab.backend_werb_mr.dtos.ProgresoUsuarioDTO;
import com.algolab.backend_werb_mr.modelos.DescripcionNivel;
import com.algolab.backend_werb_mr.modelos.ProgresoNivel;
import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.repositorio.IProgresoNivelRepositorio;

@Service
public class ProgresoServicio implements IProgresoServicio {
    private static final Logger logger = LoggerFactory.getLogger(ProgresoServicio.class);

    private final IProgresoNivelRepositorio progresoNivelRepositorio;
    private final IUsuarioServicio usuarioServicio;
    private final IDescripcionNivelServicio descripcionNivelServicio;

    public ProgresoServicio(
            IProgresoNivelRepositorio progresoNivelRepositorio,
            IUsuarioServicio usuarioServicio,
            IDescripcionNivelServicio descripcionNivelServicio) {
        this.progresoNivelRepositorio = progresoNivelRepositorio;
        this.usuarioServicio = usuarioServicio;
        this.descripcionNivelServicio = descripcionNivelServicio;
    }

    @Override
    @Transactional(readOnly = true)
    public Optional<Usuario> buscarUsuarioAutenticado(String correo) {
        Optional<Usuario> usuario = usuarioServicio.buscarPorCorreo(correo);

        if (usuario.isEmpty()) {
            logger.warn("No se encontro usuario autenticado con correo {}", correo);
        }

        return usuario;
    }

    @Override
    @Transactional(readOnly = true)
    public ProgresoUsuarioDTO consultarProgreso(Usuario usuario) {
        logger.info("Consultando progreso del usuario {}", usuario.getId());
        List<ProgresoNivel> progresos = progresoNivelRepositorio.findByUsuarioOrderByNivelAsc(usuario);
        return construirRespuesta(usuario, progresos);
    }

    @Override
    @Transactional
    public ProgresoUsuarioDTO guardarProgreso(Usuario usuario, GuardarProgresoRequest request) {
        logger.info("Guardando progreso del usuario {} en nivel {}", usuario.getId(), request.getNivel());

        ProgresoNivel progreso = progresoNivelRepositorio
                .findByUsuarioAndNivel(usuario, request.getNivel())
                .orElseGet(() -> crearProgreso(usuario, request.getNivel()));

        boolean yaCompletado = Boolean.TRUE.equals(progreso.getCompletado());
        boolean completadoNuevo = Boolean.TRUE.equals(request.getCompletado());

        progreso.setCompletado(yaCompletado || completadoNuevo);
        progreso.setPuntaje(Math.max(progreso.getPuntaje(), request.getPuntaje()));
        progreso.setTiempoRestante(Math.max(progreso.getTiempoRestante(), request.getTiempoRestante()));
        progreso.setIntentos(calcularIntentos(progreso.getIntentos(), request.getIntentos()));
        progreso.setFechaUltimoIntento(LocalDateTime.now());

        if (!yaCompletado && completadoNuevo) {
            progreso.setFechaCompletado(LocalDateTime.now());
        }

        progresoNivelRepositorio.save(progreso);

        List<ProgresoNivel> progresos = progresoNivelRepositorio.findByUsuarioOrderByNivelAsc(usuario);
        int puntajeTotal = calcularPuntajeTotal(progresos);
        usuario.setPuntaje(puntajeTotal);

        if (completadoNuevo) {
            actualizarNivelActual(usuario, request.getNivel());
        }

        usuarioServicio.actualizar(usuario);
        return construirRespuesta(usuario, progresos);
    }

    private ProgresoNivel crearProgreso(Usuario usuario, Integer nivel) {
        ProgresoNivel progreso = new ProgresoNivel();
        progreso.setUsuario(usuario);
        progreso.setNivel(nivel);
        return progreso;
    }

    private int calcularIntentos(Integer intentosActuales, Integer intentosRequest) {
        int actuales = intentosActuales == null ? 0 : intentosActuales;
        int enviados = intentosRequest == null ? 0 : intentosRequest;

        if (actuales == 0) {
            return enviados;
        }

        return Math.max(actuales + 1, enviados);
    }

    private int calcularPuntajeTotal(List<ProgresoNivel> progresos) {
        return progresos.stream()
                .filter(progreso -> Boolean.TRUE.equals(progreso.getCompletado()))
                .mapToInt(ProgresoNivel::getPuntaje)
                .sum();
    }

    private void actualizarNivelActual(Usuario usuario, Integer nivelCompletado) {
        int nivelSiguiente = nivelCompletado + 1;
        int maximoNivel = descripcionNivelServicio.listar().stream()
                .map(DescripcionNivel::getNivel)
                .filter(nivel -> nivel != null && nivel >= 1)
                .mapToInt(Integer::intValue)
                .max()
                .orElse(nivelSiguiente);

        int nuevoNivelActual = Math.min(nivelSiguiente, maximoNivel);

        if (nuevoNivelActual > usuario.getNivelActual()) {
            logger.info("Actualizando nivelActual del usuario {} de {} a {}",
                    usuario.getId(),
                    usuario.getNivelActual(),
                    nuevoNivelActual);
            usuario.setNivelActual(nuevoNivelActual);
        }
    }

    private ProgresoUsuarioDTO construirRespuesta(Usuario usuario, List<ProgresoNivel> progresos) {
        List<ProgresoNivelDTO> niveles = progresos.stream()
                .map(ProgresoNivelDTO::desdeModelo)
                .toList();

        return new ProgresoUsuarioDTO(
                usuario.getId(),
                usuario.getNivelActual(),
                calcularPuntajeTotal(progresos),
                niveles);
    }
}
