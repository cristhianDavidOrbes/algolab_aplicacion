package com.algolab.backend_werb_mr.controladores;

import java.util.Map;

import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

@RestControllerAdvice
public class ApiErrorHandler {
    @ExceptionHandler(HttpMessageNotReadableException.class)
    public ResponseEntity<Map<String, Object>> manejarJsonInvalido(HttpMessageNotReadableException error) {
        return ResponseEntity.badRequest().body(Map.of(
                "exitoso", false,
                "mensaje", "JSON invalido. Revise el body enviado"));
    }
}
