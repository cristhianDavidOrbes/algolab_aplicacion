"use client";

import Link from "next/link";
import { useAuthSession } from "@/lib/use-auth-session";

export default function EstudiantePage() {
  const { hydrated, usuario } = useAuthSession();

  return (
    <main className="min-h-screen bg-neutral-100 px-4 py-8 text-neutral-950">
      <section className="mx-auto w-full max-w-3xl">
        <div className="rounded-lg border border-neutral-200 bg-white p-6 shadow-sm">
          <p className="text-sm font-medium text-teal-700">Panel estudiante</p>
          <h1 className="mt-2 text-2xl font-semibold">
            {hydrated && usuario ? `Hola, ${usuario.nombre}` : "Hola"}
          </h1>
          <p className="mt-2 text-sm text-neutral-600">
            Esta pagina confirma que un estudiante puede entrar al frontend
            despues de iniciar sesion.
          </p>

          <div className="mt-6 grid gap-3 rounded-md border border-neutral-200 bg-neutral-50 p-4 text-sm">
            <p>
              <span className="font-semibold">Correo:</span>{" "}
              {hydrated ? usuario?.correo ?? "No disponible" : "Cargando..."}
            </p>
            <p>
              <span className="font-semibold">Rol:</span>{" "}
              {hydrated ? usuario?.rol ?? "ESTUDIANTE" : "Cargando..."}
            </p>
            <p>
              <span className="font-semibold">Nivel actual:</span>{" "}
              {hydrated ? usuario?.nivelActual ?? 1 : "Cargando..."}
            </p>
            <p>
              <span className="font-semibold">Puntaje:</span>{" "}
              {hydrated ? usuario?.puntaje ?? 0 : "Cargando..."}
            </p>
          </div>

          <Link
            className="mt-6 inline-flex h-10 items-center rounded-md bg-teal-700 px-4 text-sm font-semibold text-white hover:bg-teal-800"
            href="/iniciar-sesion"
          >
            Volver al inicio de sesion
          </Link>
        </div>
      </section>
    </main>
  );
}
