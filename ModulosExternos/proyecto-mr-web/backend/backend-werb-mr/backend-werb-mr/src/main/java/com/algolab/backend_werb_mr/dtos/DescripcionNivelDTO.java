package com.algolab.backend_werb_mr.dtos;

import java.time.LocalDateTime;

import com.algolab.backend_werb_mr.modelos.DescripcionNivel;

public class DescripcionNivelDTO {
    private Long id;
    private String nombre;
    private String descripcion;
    private Integer nivel;
    private String objetivo;
    private Boolean activo;
    private LocalDateTime fechaCreacion;
    private LocalDateTime fechaActualizacion;

    public DescripcionNivelDTO() {
    }

    public DescripcionNivelDTO(Long id, String nombre, String descripcion, Integer nivel, String objetivo,
            Boolean activo, LocalDateTime fechaCreacion, LocalDateTime fechaActualizacion) {
        this.id = id;
        this.nombre = nombre;
        this.descripcion = descripcion;
        this.nivel = nivel;
        this.objetivo = objetivo;
        this.activo = activo;
        this.fechaCreacion = fechaCreacion;
        this.fechaActualizacion = fechaActualizacion;
    }

    public static DescripcionNivelDTO desdeModelo(DescripcionNivel descripcionNivel) {
        return new DescripcionNivelDTO(
                descripcionNivel.getId(),
                descripcionNivel.getNombre(),
                descripcionNivel.getDescripcion(),
                descripcionNivel.getNivel(),
                descripcionNivel.getObjetivo(),
                descripcionNivel.getActivo(),
                descripcionNivel.getFechaCreacion(),
                descripcionNivel.getFechaActualizacion());
    }

    public DescripcionNivel aModelo() {
        DescripcionNivel descripcionNivel = new DescripcionNivel(id, nombre, descripcion, nivel, objetivo, activo);
        descripcionNivel.setFechaCreacion(fechaCreacion);
        descripcionNivel.setFechaActualizacion(fechaActualizacion);
        return descripcionNivel;
    }

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getNombre() {
        return nombre;
    }

    public void setNombre(String nombre) {
        this.nombre = nombre;
    }

    public String getDescripcion() {
        return descripcion;
    }

    public void setDescripcion(String descripcion) {
        this.descripcion = descripcion;
    }

    public Integer getNivel() {
        return nivel;
    }

    public void setNivel(Integer nivel) {
        this.nivel = nivel;
    }

    public String getObjetivo() {
        return objetivo;
    }

    public void setObjetivo(String objetivo) {
        this.objetivo = objetivo;
    }

    public Boolean getActivo() {
        return activo;
    }

    public void setActivo(Boolean activo) {
        this.activo = activo;
    }

    public LocalDateTime getFechaCreacion() {
        return fechaCreacion;
    }

    public void setFechaCreacion(LocalDateTime fechaCreacion) {
        this.fechaCreacion = fechaCreacion;
    }

    public LocalDateTime getFechaActualizacion() {
        return fechaActualizacion;
    }

    public void setFechaActualizacion(LocalDateTime fechaActualizacion) {
        this.fechaActualizacion = fechaActualizacion;
    }
}
