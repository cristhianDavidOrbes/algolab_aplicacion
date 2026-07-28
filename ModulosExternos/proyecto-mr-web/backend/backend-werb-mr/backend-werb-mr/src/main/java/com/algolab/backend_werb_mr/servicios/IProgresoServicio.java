package com.algolab.backend_werb_mr.servicios;

import java.util.Optional;

import com.algolab.backend_werb_mr.dtos.GuardarProgresoRequest;
import com.algolab.backend_werb_mr.dtos.ProgresoUsuarioDTO;
import com.algolab.backend_werb_mr.modelos.Usuario;

public interface IProgresoServicio {
    Optional<Usuario> buscarUsuarioAutenticado(String correo);

    ProgresoUsuarioDTO consultarProgreso(Usuario usuario);

    ProgresoUsuarioDTO guardarProgreso(Usuario usuario, GuardarProgresoRequest request);
}
