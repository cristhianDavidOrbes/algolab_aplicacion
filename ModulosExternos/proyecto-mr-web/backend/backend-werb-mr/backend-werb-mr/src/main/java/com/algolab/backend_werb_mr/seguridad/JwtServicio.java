package com.algolab.backend_werb_mr.seguridad;

import java.nio.charset.StandardCharsets;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.time.Instant;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;

import javax.crypto.Mac;
import javax.crypto.SecretKey;
import javax.crypto.spec.SecretKeySpec;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import com.algolab.backend_werb_mr.modelos.Usuario;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;

@Service
public class JwtServicio {
    private static final String ALGORITMO = "HmacSHA256";
    private static final Base64.Encoder BASE64_URL_ENCODER = Base64.getUrlEncoder().withoutPadding();
    private static final Base64.Decoder BASE64_URL_DECODER = Base64.getUrlDecoder();

    private final ObjectMapper objectMapper = new ObjectMapper();

    @Value("${app.jwt.secret}")
    private String secreto;

    @Value("${app.jwt.expiracion-ms}")
    private long expiracionMs;

    public String generarToken(Usuario usuario) {
        long ahora = Instant.now().toEpochMilli();
        long expiracion = ahora + expiracionMs;

        Map<String, Object> header = Map.of(
                "alg", "HS256",
                "typ", "JWT");

        Map<String, Object> payload = new HashMap<>();
        payload.put("sub", usuario.getCorreo());
        payload.put("id", usuario.getId());
        payload.put("nombre", usuario.getNombre());
        payload.put("rol", usuario.getRol().name());
        payload.put("iat", ahora);
        payload.put("exp", expiracion);

        String headerBase64 = codificarJson(header);
        String payloadBase64 = codificarJson(payload);
        String contenido = headerBase64 + "." + payloadBase64;

        return contenido + "." + firmar(contenido);
    }

    public boolean tokenValido(String token) {
        try {
            String[] partes = token.split("\\.");

            if (partes.length != 3) {
                return false;
            }

            String contenido = partes[0] + "." + partes[1];
            String firmaEsperada = firmar(contenido);

            if (!firmaEsperada.equals(partes[2])) {
                return false;
            }

            Long expiracion = obtenerValor(token, "exp", Long.class);
            return expiracion != null && expiracion > Instant.now().toEpochMilli();
        } catch (RuntimeException error) {
            return false;
        }
    }

    public String obtenerCorreo(String token) {
        return obtenerValor(token, "sub", String.class);
    }

    public String obtenerRol(String token) {
        return obtenerValor(token, "rol", String.class);
    }

    private String codificarJson(Map<String, Object> datos) {
        try {
            return BASE64_URL_ENCODER.encodeToString(objectMapper.writeValueAsBytes(datos));
        } catch (JsonProcessingException error) {
            throw new IllegalStateException("No se pudo crear el token JWT", error);
        }
    }

    private <T> T obtenerValor(String token, String clave, Class<T> tipo) {
        try {
            String[] partes = token.split("\\.");
            String payloadJson = new String(BASE64_URL_DECODER.decode(partes[1]), StandardCharsets.UTF_8);
            Map<String, Object> payload = objectMapper.readValue(payloadJson, new TypeReference<Map<String, Object>>() {
            });
            Object valor = payload.get(clave);

            if (valor == null) {
                return null;
            }

            if (tipo == Long.class && valor instanceof Number numero) {
                return tipo.cast(numero.longValue());
            }

            return tipo.cast(valor);
        } catch (JsonProcessingException | IllegalArgumentException error) {
            throw new IllegalStateException("Token JWT invalido", error);
        }
    }

    private String firmar(String contenido) {
        try {
            Mac mac = Mac.getInstance(ALGORITMO);
            mac.init(obtenerClave());
            byte[] firma = mac.doFinal(contenido.getBytes(StandardCharsets.UTF_8));
            return BASE64_URL_ENCODER.encodeToString(firma);
        } catch (NoSuchAlgorithmException | InvalidKeyException error) {
            throw new IllegalStateException("No se pudo firmar el token JWT", error);
        }
    }

    private SecretKey obtenerClave() {
        return new SecretKeySpec(secreto.getBytes(StandardCharsets.UTF_8), ALGORITMO);
    }
}
