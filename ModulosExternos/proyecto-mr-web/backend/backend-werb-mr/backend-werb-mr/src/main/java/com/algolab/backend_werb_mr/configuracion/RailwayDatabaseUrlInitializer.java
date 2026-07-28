package com.algolab.backend_werb_mr.configuracion;

import java.net.URI;
import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.LinkedHashMap;
import java.util.Map;

import org.springframework.context.ApplicationContextInitializer;
import org.springframework.context.ConfigurableApplicationContext;
import org.springframework.core.env.ConfigurableEnvironment;
import org.springframework.core.env.MapPropertySource;

public class RailwayDatabaseUrlInitializer implements ApplicationContextInitializer<ConfigurableApplicationContext> {
    @Override
    public void initialize(ConfigurableApplicationContext applicationContext) {
        ConfigurableEnvironment environment = applicationContext.getEnvironment();

        if (tieneValorNoLocal(environment, "SPRING_DATASOURCE_URL") || tieneValorNoLocal(environment, "DB_URL")) {
            return;
        }

        String databaseUrl = environment.getProperty("DATABASE_URL");
        if (databaseUrl == null || databaseUrl.isBlank()) {
            return;
        }

        URI uri = URI.create(databaseUrl);
        String scheme = uri.getScheme();
        if (!"postgres".equalsIgnoreCase(scheme) && !"postgresql".equalsIgnoreCase(scheme)) {
            return;
        }

        Map<String, Object> properties = new LinkedHashMap<>();
        String database = uri.getPath() == null ? "" : uri.getPath();
        String jdbcUrl = "jdbc:postgresql://" + uri.getHost() + ":" + obtenerPuerto(uri) + database;
        properties.put("spring.datasource.url", jdbcUrl);

        String userInfo = uri.getUserInfo();
        if (userInfo != null && !userInfo.isBlank()) {
            String[] partes = userInfo.split(":", 2);
            properties.put("spring.datasource.username", decodificar(partes[0]));

            if (partes.length > 1) {
                properties.put("spring.datasource.password", decodificar(partes[1]));
            }
        }

        environment.getPropertySources().addFirst(new MapPropertySource("railwayDatabaseUrl", properties));
    }

    private boolean tieneValor(ConfigurableEnvironment environment, String propiedad) {
        String valor = environment.getProperty(propiedad);
        return valor != null && !valor.isBlank();
    }

    private boolean tieneValorNoLocal(ConfigurableEnvironment environment, String propiedad) {
        String valor = environment.getProperty(propiedad);
        return valor != null && !valor.isBlank() && !valor.contains("localhost");
    }

    private int obtenerPuerto(URI uri) {
        return uri.getPort() > 0 ? uri.getPort() : 5432;
    }

    private String decodificar(String valor) {
        return URLDecoder.decode(valor, StandardCharsets.UTF_8);
    }
}
