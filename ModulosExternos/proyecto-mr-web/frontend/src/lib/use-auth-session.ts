"use client";

import { useMemo, useSyncExternalStore } from "react";

export type UsuarioSesion = {
  id: number;
  nombre: string;
  correo: string;
  rol: "ESTUDIANTE" | "DOCENTE" | "ADMINISTRADOR";
  nivelActual: number;
  puntaje: number;
};

function subscribe(callback: () => void) {
  window.addEventListener("storage", callback);
  window.addEventListener("auth-session-change", callback);

  return () => {
    window.removeEventListener("storage", callback);
    window.removeEventListener("auth-session-change", callback);
  };
}

function getLocalStorageValue(key: string) {
  return window.localStorage.getItem(key);
}

function getServerSnapshot() {
  return null;
}

function useLocalStorageValue(key: string) {
  return useSyncExternalStore(
    subscribe,
    () => getLocalStorageValue(key),
    getServerSnapshot,
  );
}

export function notifyAuthSessionChange() {
  window.dispatchEvent(new Event("auth-session-change"));
}

export function useAuthSession() {
  const token = useLocalStorageValue("token") ?? "";
  const usuarioTexto = useLocalStorageValue("usuario");
  const hydrated = useSyncExternalStore(
    subscribe,
    () => true,
    () => false,
  );

  const usuario = useMemo(() => {
    if (!usuarioTexto) {
      return null;
    }

    try {
      return JSON.parse(usuarioTexto) as UsuarioSesion;
    } catch {
      return null;
    }
  }, [usuarioTexto]);

  return {
    hydrated,
    token,
    usuario,
  };
}
