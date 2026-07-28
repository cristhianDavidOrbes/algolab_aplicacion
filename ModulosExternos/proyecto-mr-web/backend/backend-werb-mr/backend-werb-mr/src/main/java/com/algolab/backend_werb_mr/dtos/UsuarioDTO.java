package com.algolab.backend_werb_mr.dtos;

import com.algolab.backend_werb_mr.modelos.Rol;

public class UsuarioDTO {
    private Long id;
    private String name;
    private String mail;
    private String username;
    private Rol rol;
    private Integer nivelActual;
    private Integer puntaje;

    public UsuarioDTO() {
    }

    public UsuarioDTO(Long id, String name, String mail, Rol rol) {
        this.id = id;
        this.name = name;
        this.mail = mail;
        this.rol = rol;
        this.nivelActual = 1;
        this.puntaje = 0;
    }

    public UsuarioDTO(Long id, String name, String mail, String username, Rol rol, Integer nivelActual,
            Integer puntaje) {
        this.id = id;
        this.name = name;
        this.mail = mail;
        this.username = username;
        this.rol = rol;
        this.nivelActual = nivelActual;
        this.puntaje = puntaje;
    }

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getMail() {
        return mail;
    }

    public void setMail(String mail) {
        this.mail = mail;
    }

    public String getUsername() {
        return username;
    }

    public void setUsername(String username) {
        this.username = username;
    }

    public Rol getRol() {
        return rol;
    }

    public void setRol(Rol rol) {
        this.rol = rol;
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
