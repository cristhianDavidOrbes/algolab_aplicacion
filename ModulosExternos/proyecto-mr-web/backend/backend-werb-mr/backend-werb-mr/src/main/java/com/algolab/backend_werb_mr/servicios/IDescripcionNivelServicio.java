package com.algolab.backend_werb_mr.servicios;

import java.util.List;
import java.util.Optional;

import com.algolab.backend_werb_mr.modelos.DescripcionNivel;

public interface IDescripcionNivelServicio {
    DescripcionNivel guardar(DescripcionNivel descripcionNivel);

    Optional<DescripcionNivel> buscarPorId(Long id);

    List<DescripcionNivel> listar();

    DescripcionNivel actualizar(DescripcionNivel descripcionNivel);

    void eliminarPorId(Long id);
}
