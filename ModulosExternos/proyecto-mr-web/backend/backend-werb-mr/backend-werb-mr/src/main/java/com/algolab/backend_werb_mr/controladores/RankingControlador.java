package com.algolab.backend_werb_mr.controladores;

import java.util.ArrayList;
import java.util.List;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.algolab.backend_werb_mr.dtos.RankingEstudianteDTO;
import com.algolab.backend_werb_mr.dtos.RankingRespuestaDTO;
import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.servicios.IUsuarioServicio;

@RestController
@RequestMapping("/api/ranking")
public class RankingControlador {
    private final IUsuarioServicio usuarioServicio;

    public RankingControlador(IUsuarioServicio usuarioServicio) {
        this.usuarioServicio = usuarioServicio;
    }

    @GetMapping
    public RankingRespuestaDTO consultarRanking() {
        List<Usuario> estudiantes = usuarioServicio.listarRankingEstudiantes();
        int cantidad = estudiantes.size();
        List<RankingEstudianteDTO> ranking = new ArrayList<>(cantidad);

        for (int i = 0; i < cantidad; i++) {
            ranking.add(new RankingEstudianteDTO(i + 1, estudiantes.get(i)));
        }

        return new RankingRespuestaDTO(ranking);
    }
}
