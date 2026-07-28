package com.algolab.backend_werb_mr.repositorio;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.modelos.Rol;

@Repository
public interface IUsuarioRepositorio extends JpaRepository<Usuario, Long> {
    @Query("SELECT usuario FROM Usuario usuario WHERE usuario.correo = :correo")
    Optional<Usuario> buscarPorCorreo(@Param("correo") String correo);

    @Query("SELECT usuario FROM Usuario usuario WHERE usuario.correo = :identificador OR usuario.nombreUsuario = :identificador")
    Optional<Usuario> buscarPorCorreoONombreUsuario(@Param("identificador") String identificador);

    @Query("SELECT COUNT(usuario) > 0 FROM Usuario usuario WHERE usuario.correo = :correo")
    boolean existePorCorreo(@Param("correo") String correo);

    @Query("SELECT COUNT(usuario) > 0 FROM Usuario usuario WHERE usuario.nombreUsuario = :nombreUsuario")
    boolean existePorNombreUsuario(@Param("nombreUsuario") String nombreUsuario);

    @Query("SELECT COUNT(usuario) > 0 FROM Usuario usuario WHERE usuario.correo = :identificador OR usuario.nombreUsuario = :identificador")
    boolean existePorCorreoONombreUsuario(@Param("identificador") String identificador);

    @Query("""
            SELECT usuario
            FROM Usuario usuario
            WHERE usuario.rol = :rol
            ORDER BY COALESCE(usuario.puntaje, 0) DESC,
                     COALESCE(usuario.nivelActual, 1) DESC,
                     LOWER(usuario.nombre) ASC,
                     usuario.id ASC
            """)
    List<Usuario> listarPorRolParaRanking(@Param("rol") Rol rol);
}
