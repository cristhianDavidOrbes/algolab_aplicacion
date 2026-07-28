package com.algolab.backend_werb_mr.dtos;

import com.algolab.backend_werb_mr.modelos.Rol;
import com.algolab.backend_werb_mr.modelos.Usuario;

public class UsuarioSesionDTO {
    private Long id;
    private String nombre;
    private String correo;
    private String nombreUsuario;
    private Rol rol;
    private Integer nivelActual;
    private Integer puntaje;

    public UsuarioSesionDTO() {
    }

    public UsuarioSesionDTO(Long id, String nombre, String correo, String nombreUsuario, Rol rol,
            Integer nivelActual, Integer puntaje) {
        this.id = id;
        this.nombre = nombre;
        this.correo = correo;
        this.nombreUsuario = nombreUsuario;
        this.rol = rol;
        this.nivelActual = nivelActual;
        this.puntaje = puntaje;
    }

    public static UsuarioSesionDTO desdeUsuario(Usuario usuario) {
        return new UsuarioSesionDTO(
                usuario.getId(),
                usuario.getNombre(),
                usuario.getCorreo(),
                usuario.getNombreUsuario(),
                usuario.getRol(),
                usuario.getNivelActual(),
                usuario.getPuntaje());
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
