package com.algolab.backend_werb_mr.dtos;

import java.util.List;

public class RankingRespuestaDTO {
    private Integer total;
    private List<RankingEstudianteDTO> estudiantes;

    public RankingRespuestaDTO() {
    }

    public RankingRespuestaDTO(List<RankingEstudianteDTO> estudiantes) {
        this.estudiantes = estudiantes;
        this.total = estudiantes == null ? 0 : estudiantes.size();
    }

    public Integer getTotal() {
        return total;
    }

    public void setTotal(Integer total) {
        this.total = total;
    }

    public List<RankingEstudianteDTO> getEstudiantes() {
        return estudiantes;
    }

    public void setEstudiantes(List<RankingEstudianteDTO> estudiantes) {
        this.estudiantes = estudiantes;
        this.total = estudiantes == null ? 0 : estudiantes.size();
    }
}
