package com.algolab.backend_werb_mr.repositorio;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import com.algolab.backend_werb_mr.modelos.DescripcionNivel;

@Repository
public interface IDescripcionNivelRepositorio extends JpaRepository<DescripcionNivel, Long> {
}
