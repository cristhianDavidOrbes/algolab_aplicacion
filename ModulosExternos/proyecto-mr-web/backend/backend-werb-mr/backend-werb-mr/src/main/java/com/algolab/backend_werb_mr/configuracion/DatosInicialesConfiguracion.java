package com.algolab.backend_werb_mr.configuracion;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.CommandLineRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import com.algolab.backend_werb_mr.modelos.Rol;
import com.algolab.backend_werb_mr.modelos.Usuario;
import com.algolab.backend_werb_mr.servicios.IUsuarioServicio;

@Configuration
public class DatosInicialesConfiguracion {
    @Bean
    public CommandLineRunner crearAdministradorInicial(
            IUsuarioServicio usuarioServicio,
            @Value("${app.admin.nombre:Cristhian David}") String nombre,
            @Value("${app.admin.correo:cristhian.david@admin.com}") String correo,
            @Value("${app.admin.contrasena:define-una-contrasena-segura}") String contrasena) {
        return args -> {
            String correoLimpio = correo.trim();

            if (!usuarioServicio.existePorCorreo(correoLimpio)) {
                Usuario administrador = new Usuario(
                        null,
                        nombre.trim(),
                        correoLimpio,
                        Rol.ADMINISTRADOR,
                        contrasena);

                usuarioServicio.registrar(administrador);
            }

            usuarioServicio.listar().stream()
                    .filter(usuario -> usuario.getNombreUsuario() == null || usuario.getNombreUsuario().isBlank())
                    .forEach(usuario -> {
                        usuario.setNombreUsuario(generarNombreUsuario(usuario.getCorreo(), usuarioServicio));
                        usuarioServicio.actualizar(usuario);
                    });
        };
    }

    private String generarNombreUsuario(String correo, IUsuarioServicio usuarioServicio) {
        String base = correo.split("@", 2)[0]
                .toLowerCase()
                .replaceAll("[^a-z0-9._-]", "");

        if (base.isBlank()) {
            base = "usuario";
        }

        String candidato = base;
        int contador = 1;

        while (usuarioServicio.existePorNombreUsuario(candidato)) {
            candidato = base + contador;
            contador++;
        }

        return candidato;
    }
}
