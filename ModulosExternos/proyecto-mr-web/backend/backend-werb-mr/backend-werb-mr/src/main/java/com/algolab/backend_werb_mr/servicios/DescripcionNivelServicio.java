package com.algolab.backend_werb_mr.servicios;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;

import com.algolab.backend_werb_mr.modelos.DescripcionNivel;
import com.algolab.backend_werb_mr.repositorio.IDescripcionNivelRepositorio;

@Service
public class DescripcionNivelServicio implements IDescripcionNivelServicio {
    private final IDescripcionNivelRepositorio descripcionNivelRepositorio;

    public DescripcionNivelServicio(IDescripcionNivelRepositorio descripcionNivelRepositorio) {
        this.descripcionNivelRepositorio = descripcionNivelRepositorio;
    }

    @Override
    public DescripcionNivel guardar(DescripcionNivel descripcionNivel) {
        return descripcionNivelRepositorio.save(descripcionNivel);
    }

    @Override
    public Optional<DescripcionNivel> buscarPorId(Long id) {
        return descripcionNivelRepositorio.findById(id);
    }

    @Override
    public List<DescripcionNivel> listar() {
        return descripcionNivelRepositorio.findAll();
    }

    @Override
    public DescripcionNivel actualizar(DescripcionNivel descripcionNivel) {
        return descripcionNivelRepositorio.save(descripcionNivel);
    }

    @Override
    public void eliminarPorId(Long id) {
        descripcionNivelRepositorio.deleteById(id);
    }
}
