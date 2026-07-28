"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { notifyAuthSessionChange } from "@/lib/use-auth-session";

type EstadoEnvio = "idle" | "enviando" | "exito" | "error";

type LoginRespuesta = {
  exitoso?: boolean;
  mensaje?: string;
  token?: string;
  usuario?: {
    id: number;
    nombre: string;
    correo: string;
    rol: string;
    nivelActual: number;
    puntaje: number;
  };
};

export default function IniciarSesionPage() {
  const router = useRouter();
  const [correo, setCorreo] = useState("");
  const [contrasena, setContrasena] = useState("");
  const [estado, setEstado] = useState<EstadoEnvio>("idle");
  const [mensaje, setMensaje] = useState("");

  async function iniciarSesion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setEstado("enviando");
    setMensaje("");

    try {
      const respuesta = await fetch("/api/iniciar-sesion", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          correo,
          contrasena,
        }),
      });

      const datos = (await respuesta.json()) as LoginRespuesta;

      if (!respuesta.ok || datos.exitoso === false || !datos.token) {
        setEstado("error");
        setMensaje(datos.mensaje ?? "No se pudo iniciar sesion.");
        return;
      }

      localStorage.setItem("token", datos.token);
      localStorage.setItem("usuario", JSON.stringify(datos.usuario));
      notifyAuthSessionChange();
      setEstado("exito");
      setMensaje(datos.mensaje ?? "Inicio de sesion exitoso.");
      setContrasena("");

      if (datos.usuario?.rol === "ADMINISTRADOR") {
        router.push("/administrador");
      } else if (datos.usuario?.rol === "DOCENTE") {
        router.push("/docente");
      } else {
        router.push("/estudiante");
      }
    } catch {
      setEstado("error");
      setMensaje("No se pudo conectar con el backend.");
    }
  }

  return (
    <main className="min-h-screen bg-neutral-100 px-4 py-8 text-neutral-950">
      <section className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md items-center">
        <div className="w-full rounded-lg border border-neutral-200 bg-white p-6 shadow-sm">
          <div className="mb-6">
            <p className="text-sm font-medium text-teal-700">AlgoLab</p>
            <h1 className="mt-2 text-2xl font-semibold">Iniciar sesion</h1>
            <p className="mt-2 text-sm leading-6 text-neutral-600">
              Ingresa con un usuario registrado para entrar a su panel.
            </p>
          </div>

          <form className="space-y-4" onSubmit={iniciarSesion}>
            <div>
              <label className="text-sm font-medium" htmlFor="correo">
                Correo
              </label>
              <input
                className="mt-2 h-11 w-full rounded-md border border-neutral-300 px-3 text-sm outline-none transition focus:border-teal-700 focus:ring-2 focus:ring-teal-100"
                id="correo"
                name="correo"
                type="email"
                value={correo}
                onChange={(event) => setCorreo(event.target.value)}
                placeholder="usuario@email.com"
                required
              />
            </div>

            <div>
              <label className="text-sm font-medium" htmlFor="contrasena">
                Contrasena
              </label>
              <input
                className="mt-2 h-11 w-full rounded-md border border-neutral-300 px-3 text-sm outline-none transition focus:border-teal-700 focus:ring-2 focus:ring-teal-100"
                id="contrasena"
                name="contrasena"
                type="password"
                value={contrasena}
                onChange={(event) => setContrasena(event.target.value)}
                placeholder="Tu contrasena"
                required
              />
            </div>

            <button
              className="flex h-11 w-full items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-neutral-400"
              type="submit"
              disabled={estado === "enviando"}
            >
              {estado === "enviando" ? "Ingresando..." : "Iniciar sesion"}
            </button>
          </form>

          {mensaje ? (
            <div
              className={`mt-4 rounded-md border px-3 py-2 text-sm ${
                estado === "exito"
                  ? "border-emerald-200 bg-emerald-50 text-emerald-800"
                  : "border-red-200 bg-red-50 text-red-800"
              }`}
            >
              {mensaje}
            </div>
          ) : null}

          <p className="mt-6 text-center text-sm text-neutral-600">
            No tienes cuenta?{" "}
            <Link className="font-semibold text-teal-700 hover:text-teal-800" href="/registrarse">
              Registrarse
            </Link>
          </p>
        </div>
      </section>
    </main>
  );
}
