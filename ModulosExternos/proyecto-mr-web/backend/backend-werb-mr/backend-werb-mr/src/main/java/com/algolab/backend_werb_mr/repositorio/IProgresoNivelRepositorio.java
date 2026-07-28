package com.algolab.backend_werb_mr.repositorio;

import java.util.List;
import java.util.Optional;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import com.algolab.backend_werb_mr.modelos.ProgresoNivel;
import com.algolab.backend_werb_mr.modelos.Usuario;

@Repository
public interface IProgresoNivelRepositorio extends JpaRepository<ProgresoNivel, Long> {
    List<ProgresoNivel> findByUsuarioOrderByNivelAsc(Usuario usuario);

    Optional<ProgresoNivel> findByUsuarioAndNivel(Usuario usuario, Integer nivel);
}
