"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useAuthSession } from "@/lib/use-auth-session";

type Nivel = {
  id: number;
  nombre: string;
  descripcion: string;
  nivel: number;
};

export default function DocentePage() {
  const { hydrated, token, usuario } = useAuthSession();
  const [niveles, setNiveles] = useState<Nivel[]>([]);
  const [mensaje, setMensaje] = useState("");
  const mensajeVisible = mensaje || (hydrated && !token ? "Inicia sesion para ver los niveles." : "");

  useEffect(() => {
    if (!hydrated || !token) {
      return;
    }

    fetch("/api/niveles", {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
      .then(async (respuesta) => {
        const datos = await respuesta.json();

        if (!respuesta.ok) {
          throw new Error(datos.mensaje ?? "No se pudieron cargar los niveles.");
        }

        setNiveles(datos as Nivel[]);
      })
      .catch((error: Error) => setMensaje(error.message));
  }, [hydrated, token]);

  return (
    <main className="min-h-screen bg-neutral-100 px-4 py-8 text-neutral-950">
      <section className="mx-auto w-full max-w-5xl">
        <div className="rounded-lg border border-neutral-200 bg-white p-6 shadow-sm">
          <p className="text-sm font-medium text-teal-700">Panel docente</p>
          <h1 className="mt-2 text-2xl font-semibold">
            {hydrated && usuario ? `Hola, ${usuario.nombre}` : "Niveles disponibles"}
          </h1>
          <p className="mt-2 text-sm text-neutral-600">
            El docente puede consultar los niveles disponibles.
          </p>
        </div>

        {mensajeVisible ? (
          <div className="mt-4 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
            {mensajeVisible}
          </div>
        ) : null}

        <div className="mt-6 grid gap-3">
          {niveles.map((nivel) => (
            <article
              className="rounded-lg border border-neutral-200 bg-white p-5 shadow-sm"
              key={nivel.id}
            >
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h2 className="text-lg font-semibold">{nivel.nombre}</h2>
                <span className="rounded-md bg-neutral-100 px-2 py-1 text-xs font-semibold text-neutral-700">
                  Nivel {nivel.nivel}
                </span>
              </div>
              <p className="mt-2 text-sm leading-6 text-neutral-600">
                {nivel.descripcion}
              </p>
            </article>
          ))}
        </div>

        <Link
          className="mt-6 inline-flex h-10 items-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white hover:bg-teal-800"
          href="/iniciar-sesion"
        >
          Volver al inicio de sesion
        </Link>
      </section>
    </main>
  );
}
