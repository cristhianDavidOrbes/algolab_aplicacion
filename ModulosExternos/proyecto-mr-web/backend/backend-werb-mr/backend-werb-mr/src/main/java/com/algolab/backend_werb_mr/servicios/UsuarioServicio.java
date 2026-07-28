package com.algolab.backend_werb_mr.servicios;

import java.util.List;
import java.util.Optional;

import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.modelos.Rol;
import com.algolab.backend_werb_mr.repositorio.Repositorio;

@Service
public class UsuarioServicio implements IUsuarioServicio {
    private final Repositorio repositorio;
    private final PasswordEncoder passwordEncoder;

    public UsuarioServicio(Repositorio repositorio, PasswordEncoder passwordEncoder) {
        this.repositorio = repositorio;
        this.passwordEncoder = passwordEncoder;
    }

    @Override
    public Usuario guardar(Usuario usuario) {
        return repositorio.guardar(usuario);
    }

    @Override
    public Usuario registrar(Usuario usuario) {
        if (usuario.getNombreUsuario() == null || usuario.getNombreUsuario().isBlank()) {
            usuario.setNombreUsuario(generarNombreUsuario(usuario.getCorreo()));
        }

        usuario.setContrasena(passwordEncoder.encode(usuario.getContrasena()));
        return repositorio.guardar(usuario);
    }

    @Override
    public Optional<Usuario> iniciarSesion(String identificador, String contrasena) {
        return repositorio.buscarPorCorreoONombreUsuario(identificador)
                .filter(usuario -> passwordEncoder.matches(contrasena, usuario.getContrasena()));
    }

    @Override
    public Optional<Usuario> buscarPorId(Long id) {
        return repositorio.buscarPorId(id);
    }

    @Override
    public List<Usuario> listar() {
        return repositorio.listar();
    }

    @Override
    public List<Usuario> listarRankingEstudiantes() {
        return repositorio.listarPorRolParaRanking(Rol.ESTUDIANTE);
    }

    @Override
    public Usuario actualizar(Usuario usuario) {
        return repositorio.actualizar(usuario);
    }

    @Override
    public void eliminarPorId(Long id) {
        repositorio.eliminarPorId(id);
    }

    @Override
    public Optional<Usuario> buscarPorCorreo(String correo) {
        return repositorio.buscarPorCorreo(correo);
    }

    @Override
    public boolean existePorCorreo(String correo) {
        return repositorio.existePorCorreo(correo);
    }

    @Override
    public boolean existePorNombreUsuario(String nombreUsuario) {
        return repositorio.existePorNombreUsuario(nombreUsuario);
    }

    @Override
    public boolean existePorCorreoONombreUsuario(String identificador) {
        return repositorio.existePorCorreoONombreUsuario(identificador);
    }

    private String generarNombreUsuario(String correo) {
        String base = correo.split("@", 2)[0]
                .toLowerCase()
                .replaceAll("[^a-z0-9._-]", "");

        if (base.isBlank()) {
            base = "usuario";
        }

        String candidato = base;
        int contador = 1;

        while (repositorio.existePorNombreUsuario(candidato)) {
            candidato = base + contador;
            contador++;
        }

        return candidato;
    }
}
