package com.algolab.backend_werb_mr.dtos;

import java.util.List;

public class ProgresoUsuarioDTO {
    private Long usuarioId;
    private Integer nivelActual;
    private Integer puntajeTotal;
    private List<ProgresoNivelDTO> niveles;

    public ProgresoUsuarioDTO() {
    }

    public ProgresoUsuarioDTO(Long usuarioId, Integer nivelActual, Integer puntajeTotal,
            List<ProgresoNivelDTO> niveles) {
        this.usuarioId = usuarioId;
        this.nivelActual = nivelActual;
        this.puntajeTotal = puntajeTotal;
        this.niveles = niveles;
    }

    public Long getUsuarioId() {
        return usuarioId;
    }

    public void setUsuarioId(Long usuarioId) {
        this.usuarioId = usuarioId;
    }

    public Integer getNivelActual() {
        return nivelActual;
    }

    public void setNivelActual(Integer nivelActual) {
        this.nivelActual = nivelActual;
    }

    public Integer getPuntajeTotal() {
        return puntajeTotal;
    }

    public void setPuntajeTotal(Integer puntajeTotal) {
        this.puntajeTotal = puntajeTotal;
    }

    public List<ProgresoNivelDTO> getNiveles() {
        return niveles;
    }

    public void setNiveles(List<ProgresoNivelDTO> niveles) {
        this.niveles = niveles;
    }
}
