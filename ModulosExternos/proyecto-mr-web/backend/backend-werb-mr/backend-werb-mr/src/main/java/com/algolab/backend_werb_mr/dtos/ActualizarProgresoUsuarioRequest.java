package com.algolab.backend_werb_mr.dtos;

public class ActualizarProgresoUsuarioRequest {
    private Integer nivelActual;
    private Integer puntaje;

    public ActualizarProgresoUsuarioRequest() {
    }

    public Integer getNivelActual() {
        return nivelActual;
    }

    public void setNivelActual(Integer nivelActual) {
        this.nivelActual = nivelActual;
    }

    public Integer getPuntaje() {
        return puntaje;
    }

    public void setPuntaje(Integer puntaje) {
        this.puntaje = puntaje;
    }
}
