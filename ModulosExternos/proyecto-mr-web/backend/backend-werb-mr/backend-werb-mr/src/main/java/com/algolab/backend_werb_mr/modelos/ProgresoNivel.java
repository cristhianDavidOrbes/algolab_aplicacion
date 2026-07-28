package com.algolab.backend_werb_mr.modelos;

import java.time.LocalDateTime;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.PrePersist;
import jakarta.persistence.PreUpdate;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;

@Entity
@Table(name = "progresos_niveles", uniqueConstraints = {
        @UniqueConstraint(name = "uk_progreso_usuario_nivel", columnNames = { "usuario_id", "nivel" })
})
public class ProgresoNivel {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "usuario_id", nullable = false)
    private Usuario usuario;

    @Column(nullable = false)
    private Integer nivel;

    @Column(nullable = false)
    private Boolean completado = false;

    @Column(nullable = false)
    private Integer puntaje = 0;

    @Column(nullable = false)
    private Integer tiempoRestante = 0;

    @Column(nullable = false)
    private Integer intentos = 0;

    @Column(nullable = false)
    private LocalDateTime fechaUltimoIntento;

    private LocalDateTime fechaCompletado;

    public ProgresoNivel() {
    }

    @PrePersist
    public void antesDeCrear() {
        LocalDateTime ahora = LocalDateTime.now();

        if (fechaUltimoIntento == null) {
            fechaUltimoIntento = ahora;
        }

        normalizarValores();
    }

    @PreUpdate
    public void antesDeActualizar() {
        fechaUltimoIntento = LocalDateTime.now();
        normalizarValores();
    }

    private void normalizarValores() {
        if (completado == null) {
            completado = false;
        }

        if (puntaje == null) {
            puntaje = 0;
        }

        if (tiempoRestante == null) {
            tiempoRestante = 0;
        }

        if (intentos == null) {
            intentos = 0;
        }
    }

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public Usuario getUsuario() {
        return usuario;
    }

    public void setUsuario(Usuario usuario) {
        this.usuario = usuario;
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

    public LocalDateTime getFechaUltimoIntento() {
        return fechaUltimoIntento;
    }

    public void setFechaUltimoIntento(LocalDateTime fechaUltimoIntento) {
        this.fechaUltimoIntento = fechaUltimoIntento;
    }

    public LocalDateTime getFechaCompletado() {
        return fechaCompletado;
    }

    public void setFechaCompletado(LocalDateTime fechaCompletado) {
        this.fechaCompletado = fechaCompletado;
    }
}
