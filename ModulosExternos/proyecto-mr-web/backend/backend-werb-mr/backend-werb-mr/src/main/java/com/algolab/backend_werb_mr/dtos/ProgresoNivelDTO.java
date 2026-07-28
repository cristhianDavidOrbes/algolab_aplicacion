package com.algolab.backend_werb_mr.dtos;

import com.algolab.backend_werb_mr.modelos.ProgresoNivel;

public class ProgresoNivelDTO {
    private Integer nivel;
    private Boolean completado;
    private Integer puntaje;
    private Integer tiempoRestante;
    private Integer intentos;

    public ProgresoNivelDTO() {
    }

    public ProgresoNivelDTO(Integer nivel, Boolean completado, Integer puntaje, Integer tiempoRestante,
            Integer intentos) {
        this.nivel = nivel;
        this.completado = completado;
        this.puntaje = puntaje;
        this.tiempoRestante = tiempoRestante;
        this.intentos = intentos;
    }

    public static ProgresoNivelDTO desdeModelo(ProgresoNivel progreso) {
        return new ProgresoNivelDTO(
                progreso.getNivel(),
                progreso.getCompletado(),
                progreso.getPuntaje(),
                progreso.getTiempoRestante(),
                progreso.getIntentos());
    }

    public Integer getNivel() {
        return nivel;
    }

    public void setNivel(Integer nivel) {
        this.nivel = nivel;
    }

    public Boolean getCompletado() {
        return completado;
    }

    public void setCompletado(Boolean completado) {
        this.completado = completado;
    }

    public Integer getPuntaje() {
        return puntaje;
    }

    public void setPuntaje(Integer puntaje) {
        this.puntaje = puntaje;
    }

    public Integer getTiempoRestante() {
        return tiempoRestante;
    }

    public void setTiempoRestante(Integer tiempoRestante) {
        this.tiempoRestante = tiempoRestante;
    }

    public Integer getIntentos() {
        return intentos;
    }

    public void setIntentos(Integer intentos) {
        this.intentos = intentos;
    }
}
