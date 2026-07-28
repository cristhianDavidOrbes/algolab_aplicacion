package com.algolab.backend_werb_mr.repositorio;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.modelos.Rol;

@Component
@Transactional
public class Repositorio {
    private final IUsuarioRepositorio usuarioRepositorio;

    public Repositorio(IUsuarioRepositorio usuarioRepositorio) {
        this.usuarioRepositorio = usuarioRepositorio;
    }

    public Usuario guardar(Usuario usuario) {
        return usuarioRepositorio.save(usuario);
    }

    @Transactional(readOnly = true)
    public Optional<Usuario> buscarPorId(Long id) {
        return usuarioRepositorio.findById(id);
    }

    @Transactional(readOnly = true)
    public List<Usuario> listar() {
        return usuarioRepositorio.findAll();
    }

    @Transactional(readOnly = true)
    public List<Usuario> listarPorRolParaRanking(Rol rol) {
        return usuarioRepositorio.listarPorRolParaRanking(rol);
    }

    public Usuario actualizar(Usuario usuario) {
        return usuarioRepositorio.save(usuario);
    }

    public void eliminarPorId(Long id) {
        usuarioRepositorio.deleteById(id);
    }

    @Transactional(readOnly = true)
    public Optional<Usuario> buscarPorCorreo(String correo) {
        return usuarioRepositorio.buscarPorCorreo(correo);
    }

    @Transactional(readOnly = true)
    public Optional<Usuario> buscarPorCorreoONombreUsuario(String identificador) {
        return usuarioRepositorio.buscarPorCorreoONombreUsuario(identificador);
    }

    @Transactional(readOnly = true)
    public boolean existePorCorreo(String correo) {
        return usuarioRepositorio.existePorCorreo(correo);
    }

    @Transactional(readOnly = true)
    public boolean existePorNombreUsuario(String nombreUsuario) {
        return usuarioRepositorio.existePorNombreUsuario(nombreUsuario);
    }

    @Transactional(readOnly = true)
    public boolean existePorCorreoONombreUsuario(String identificador) {
        return usuarioRepositorio.existePorCorreoONombreUsuario(identificador);
    }
}
