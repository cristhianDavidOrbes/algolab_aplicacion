package com.algolab.backend_werb_mr.modelos;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.PrePersist;
import jakarta.persistence.Table;


@Entity
@Table(name= "usuarios")
public class Usuario {
    @Id
    @GeneratedValue(strategy=GenerationType.IDENTITY)
    private Long id;

    @Column(nullable= false)
    private String nombre;
    @Column(nullable= false)
    private String correo;

    @Column(unique = true)
    private String nombreUsuario;

    @Enumerated(EnumType.STRING)
    @Column(nullable= false)
    private Rol rol;

    @Column(nullable = false)
    private String contrasena;

    @Column(nullable = false, columnDefinition = "integer default 1")
    private Integer nivelActual = 1;

    @Column(nullable = false, columnDefinition = "integer default 0")
    private Integer puntaje = 0;

    public Usuario() {
    }

    public Usuario(Long id, String nombre, String correo, Rol rol, String contrasena) {
        this.id = id;
        this.nombre = nombre;
        this.correo = correo;
        this.rol = rol;
        this.contrasena = contrasena;
    }

    public Usuario(Long id, String nombre, String correo, Rol rol, String contrasena, Integer nivelActual,
            Integer puntaje) {
        this(id, nombre, correo, rol, contrasena);
        this.nivelActual = nivelActual;
        this.puntaje = puntaje;
    }

    @PrePersist
    public void asignarProgresoInicial() {
        if (nivelActual == null) {
            nivelActual = 1;
        }

        if (puntaje == null) {
            puntaje = 0;
        }
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

    public String getCorreo() {
        return correo;
    }

    public void setCorreo(String correo) {
        this.correo = correo;
    }

    public String getNombreUsuario() {
        return nombreUsuario;
    }

    public void setNombreUsuario(String nombreUsuario) {
        this.nombreUsuario = nombreUsuario;
    }

    public Rol getRol() {
        return rol;
    }

    public void setRol(Rol rol) {
        this.rol = rol;
    }

    public String getContrasena() {
        return contrasena;
    }

    public void setContrasena(String contrasena) {
        this.contrasena = contrasena;
    }

    public Integer getNivelActual() {
        return nivelActual == null ? 1 : nivelActual;
    }

    public void setNivelActual(Integer nivelActual) {
        this.nivelActual = nivelActual;
    }

    public Integer getPuntaje() {
        return puntaje == null ? 0 : puntaje;
    }

    public void setPuntaje(Integer puntaje) {
        this.puntaje = puntaje;
    }
}
