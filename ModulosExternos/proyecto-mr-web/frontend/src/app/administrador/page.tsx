"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useAuthSession } from "@/lib/use-auth-session";

type Rol = "ESTUDIANTE" | "DOCENTE" | "ADMINISTRADOR";

type Usuario = {
  id: number;
  nombre: string;
  correo: string;
  rol: Rol;
  nivelActual: number;
  puntaje: number;
};

type Nivel = {
  id: number;
  nombre: string;
  descripcion: string;
  nivel: number;
  objetivo?: string;
  activo?: boolean;
};

type UsuarioForm = {
  id: number | null;
  nombre: string;
  correo: string;
  rol: Rol;
  nivelActual: string;
  puntaje: string;
};

type NivelForm = {
  id: number | null;
  nombre: string;
  descripcion: string;
  nivel: string;
  objetivo: string;
  activo: boolean;
};

const inputClass =
  "mt-2 h-10 w-full rounded-md border border-neutral-300 px-3 text-sm outline-none transition focus:border-teal-700 focus:ring-2 focus:ring-teal-100";

const buttonClass =
  "inline-flex h-10 items-center justify-center rounded-md px-4 text-sm font-semibold transition disabled:cursor-not-allowed disabled:bg-neutral-400";

const usuarioInicial: UsuarioForm = {
  id: null,
  nombre: "",
  correo: "",
  rol: "ESTUDIANTE",
  nivelActual: "1",
  puntaje: "0",
};

const nivelInicial: NivelForm = {
  id: null,
  nombre: "",
  descripcion: "",
  nivel: "1",
  objetivo: "",
  activo: true,
};

export default function AdministradorPage() {
  const { hydrated, token, usuario: usuarioActual } = useAuthSession();
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [niveles, setNiveles] = useState<Nivel[]>([]);
  const [busqueda, setBusqueda] = useState("");
  const [mensaje, setMensaje] = useState("");
  const [datosCargados, setDatosCargados] = useState(false);
  const [usuarioForm, setUsuarioForm] = useState<UsuarioForm>(usuarioInicial);
  const [nivelForm, setNivelForm] = useState<NivelForm>(nivelInicial);

  const usuariosFiltrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();

    if (!texto) {
      return usuarios;
    }

    return usuarios.filter((usuario) =>
      `${usuario.nombre} ${usuario.correo} ${usuario.rol}`.toLowerCase().includes(texto),
    );
  }, [busqueda, usuarios]);

  useEffect(() => {
    if (!hydrated || !token) {
      return;
    }

    async function cargarDatosIniciales() {
      try {
        const [usuariosRespuesta, nivelesRespuesta] = await Promise.all([
          fetch("/api/usuarios", {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }),
          fetch("/api/niveles", {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }),
        ]);

        const usuariosDatos = await usuariosRespuesta.json();
        const nivelesDatos = await nivelesRespuesta.json();

        if (!usuariosRespuesta.ok) {
          throw new Error(usuariosDatos.mensaje ?? "No se pudieron cargar los usuarios.");
        }

        if (!nivelesRespuesta.ok) {
          throw new Error(nivelesDatos.mensaje ?? "No se pudieron cargar los niveles.");
        }

        setUsuarios(usuariosDatos as Usuario[]);
        setNiveles(nivelesDatos as Nivel[]);
      } catch (error) {
        setMensaje(error instanceof Error ? error.message : "Error al cargar datos.");
      } finally {
        setDatosCargados(true);
      }
    }

    void cargarDatosIniciales();
  }, [hydrated, token]);

  async function api<T>(path: string, options: RequestInit = {}) {
    const respuesta = await fetch(path, {
      ...options,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
        ...options.headers,
      },
    });

    const texto = await respuesta.text();
    const datos = texto ? JSON.parse(texto) : null;

    if (!respuesta.ok) {
      throw new Error(datos?.mensaje ?? "La operacion no se pudo completar.");
    }

    return datos as T;
  }

  async function guardarUsuario(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMensaje("");

    if (!usuarioForm.id) {
      setMensaje("Selecciona un usuario para editarlo.");
      return;
    }

    if (usuarioActual?.id === usuarioForm.id && usuarioForm.rol !== "ADMINISTRADOR") {
      setMensaje("No puedes quitarte tu propio rol de administrador.");
      return;
    }

    try {
      const usuarioActualizado = await api<Usuario>(`/api/usuarios/${usuarioForm.id}`, {
        method: "PUT",
        body: JSON.stringify({
          nombre: usuarioForm.nombre,
          correo: usuarioForm.correo,
          rol: usuarioForm.rol,
          nivelActual: Number(usuarioForm.nivelActual),
          puntaje: Number(usuarioForm.puntaje),
        }),
      });

      setUsuarios((actuales) =>
        actuales.map((usuario) =>
          usuario.id === usuarioActualizado.id ? usuarioActualizado : usuario,
        ),
      );
      setUsuarioForm(usuarioInicial);
      setMensaje("Usuario actualizado correctamente.");
    } catch (error) {
      setMensaje(error instanceof Error ? error.message : "No se pudo actualizar el usuario.");
    }
  }

  async function eliminarUsuario(id: number) {
    setMensaje("");

    if (usuarioActual?.id === id) {
      setMensaje("No puedes borrar tu propia cuenta de administrador.");
      return;
    }

    try {
      await api<null>(`/api/usuarios/${id}`, {
        method: "DELETE",
      });

      setUsuarios((actuales) => actuales.filter((usuario) => usuario.id !== id));
      setMensaje("Usuario eliminado correctamente.");
    } catch (error) {
      setMensaje(error instanceof Error ? error.message : "No se pudo eliminar el usuario.");
    }
  }

  async function guardarNivel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMensaje("");

    const payload = {
      nombre: nivelForm.nombre,
      descripcion: nivelForm.descripcion,
      nivel: Number(nivelForm.nivel),
      objetivo: nivelForm.objetivo || null,
      activo: nivelForm.activo,
    };

    try {
      if (nivelForm.id) {
        const nivelActualizado = await api<Nivel>(`/api/niveles/${nivelForm.id}`, {
          method: "PUT",
          body: JSON.stringify(payload),
        });

        setNiveles((actuales) =>
          actuales.map((nivel) => (nivel.id === nivelActualizado.id ? nivelActualizado : nivel)),
        );
        setMensaje("Nivel actualizado correctamente.");
      } else {
        const nivelCreado = await api<Nivel>("/api/niveles", {
          method: "POST",
          body: JSON.stringify(payload),
        });

        setNiveles((actuales) => [...actuales, nivelCreado]);
        setMensaje("Nivel creado correctamente.");
      }

      setNivelForm(nivelInicial);
    } catch (error) {
      setMensaje(error instanceof Error ? error.message : "No se pudo guardar el nivel.");
    }
  }

  async function eliminarNivel(id: number) {
    setMensaje("");

    try {
      await api<null>(`/api/niveles/${id}`, {
        method: "DELETE",
      });

      setNiveles((actuales) => actuales.filter((nivel) => nivel.id !== id));
      setMensaje("Nivel eliminado correctamente.");
    } catch (error) {
      setMensaje(error instanceof Error ? error.message : "No se pudo eliminar el nivel.");
    }
  }

  function editarUsuario(usuario: Usuario) {
    setUsuarioForm({
      id: usuario.id,
      nombre: usuario.nombre,
      correo: usuario.correo,
      rol: usuario.rol,
      nivelActual: String(usuario.nivelActual ?? 0),
      puntaje: String(usuario.puntaje ?? 0),
    });
  }

  function editarNivel(nivel: Nivel) {
    setNivelForm({
      id: nivel.id,
      nombre: nivel.nombre,
      descripcion: nivel.descripcion,
      nivel: String(nivel.nivel),
      objetivo: nivel.objetivo ?? "",
      activo: nivel.activo ?? true,
    });
  }

  const accesoDenegado = usuarioActual && usuarioActual.rol !== "ADMINISTRADOR";
  const mensajeVisible =
    mensaje || (hydrated && !token ? "Inicia sesion como administrador para entrar." : "");
  const cargando = hydrated && Boolean(token) && !datosCargados;

  return (
    <main className="min-h-screen bg-neutral-100 px-4 py-8 text-neutral-950">
      <section className="mx-auto w-full max-w-7xl">
        <div className="flex flex-wrap items-start justify-between gap-4 border-b border-neutral-200 pb-5">
          <div>
            <p className="text-sm font-medium text-teal-700">Panel administrador</p>
            <h1 className="mt-2 text-2xl font-semibold">
              {hydrated && usuarioActual ? `Hola, ${usuarioActual.nombre}` : "Administracion"}
            </h1>
            <p className="mt-2 text-sm text-neutral-600">
              Gestiona niveles y usuarios registrados.
            </p>
          </div>
          <Link
            className={`${buttonClass} bg-white text-neutral-900 ring-1 ring-neutral-300 hover:bg-neutral-50`}
            href="/iniciar-sesion"
          >
            Cambiar usuario
          </Link>
        </div>

        {mensajeVisible ? (
          <div className="mt-4 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900">
            {mensajeVisible}
          </div>
        ) : null}

        {accesoDenegado ? (
          <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-5 text-sm text-red-800">
            Este panel solo esta disponible para administradores.
          </div>
        ) : (
          <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.2fr)_minmax(360px,0.8fr)]">
            <section className="rounded-lg border border-neutral-200 bg-white p-5 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold">Usuarios</h2>
                  <p className="mt-1 text-sm text-neutral-600">
                    Busca, cambia roles o elimina usuarios.
                  </p>
                </div>
                <input
                  className="h-10 w-full rounded-md border border-neutral-300 px-3 text-sm outline-none transition focus:border-teal-700 focus:ring-2 focus:ring-teal-100 sm:w-72"
                  placeholder="Buscar por nombre, correo o rol"
                  value={busqueda}
                  onChange={(event) => setBusqueda(event.target.value)}
                />
              </div>

              <div className="mt-4 overflow-x-auto">
                <table className="w-full min-w-[860px] border-collapse text-left text-sm">
                  <thead>
                    <tr className="border-b border-neutral-200 text-neutral-600">
                      <th className="py-3 pr-3 font-semibold">Nombre</th>
                      <th className="py-3 pr-3 font-semibold">Correo</th>
                      <th className="py-3 pr-3 font-semibold">Rol</th>
                      <th className="py-3 pr-3 font-semibold">Nivel</th>
                      <th className="py-3 pr-3 font-semibold">Puntaje</th>
                      <th className="py-3 pr-3 font-semibold">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    {usuariosFiltrados.map((usuario) => (
                      <tr className="border-b border-neutral-100" key={usuario.id}>
                        <td className="py-3 pr-3">{usuario.nombre}</td>
                        <td className="py-3 pr-3 text-neutral-600">{usuario.correo}</td>
                        <td className="py-3 pr-3">
                          <span className="rounded-md bg-neutral-100 px-2 py-1 text-xs font-semibold">
                            {usuario.rol}
                          </span>
                        </td>
                        <td className="py-3 pr-3">{usuario.nivelActual ?? 0}</td>
                        <td className="py-3 pr-3">{usuario.puntaje ?? 0}</td>
                        <td className="py-3 pr-3">
                          <div className="flex gap-2">
                            <button
                              className={`${buttonClass} bg-neutral-900 text-white hover:bg-neutral-700`}
                              type="button"
                              onClick={() => editarUsuario(usuario)}
                            >
                              Editar
                            </button>
                            <button
                              className={`${buttonClass} ${
                                usuarioActual?.id === usuario.id
                                  ? "bg-neutral-300 text-neutral-600"
                                  : "bg-red-700 text-white hover:bg-red-800"
                              }`}
                              type="button"
                              disabled={usuarioActual?.id === usuario.id}
                              onClick={() => eliminarUsuario(usuario.id)}
                            >
                              Borrar
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {cargando ? (
                <p className="mt-4 text-sm text-neutral-600">Cargando datos...</p>
              ) : null}
            </section>

            <section className="rounded-lg border border-neutral-200 bg-white p-5 shadow-sm">
              <h2 className="text-lg font-semibold">Editar usuario</h2>
              <form className="mt-4 space-y-4" onSubmit={guardarUsuario}>
                <div>
                  <label className="text-sm font-medium" htmlFor="usuario-nombre">
                    Nombre
                  </label>
                  <input
                    className={inputClass}
                    id="usuario-nombre"
                    value={usuarioForm.nombre}
                    onChange={(event) =>
                      setUsuarioForm((actual) => ({ ...actual, nombre: event.target.value }))
                    }
                    required
                  />
                </div>

                <div>
                  <label className="text-sm font-medium" htmlFor="usuario-correo">
                    Correo
                  </label>
                  <input
                    className={inputClass}
                    id="usuario-correo"
                    type="email"
                    value={usuarioForm.correo}
                    onChange={(event) =>
                      setUsuarioForm((actual) => ({ ...actual, correo: event.target.value }))
                    }
                    required
                  />
                </div>

                <div>
                  <label className="text-sm font-medium" htmlFor="usuario-rol">
                    Rol
                  </label>
                  <select
                    className={inputClass}
                    id="usuario-rol"
                    value={usuarioForm.rol}
                    disabled={usuarioActual?.id === usuarioForm.id}
                    onChange={(event) =>
                      setUsuarioForm((actual) => ({
                        ...actual,
                        rol: event.target.value as Rol,
                      }))
                    }
                  >
                    <option value="ESTUDIANTE">ESTUDIANTE</option>
                    <option value="DOCENTE">DOCENTE</option>
                    <option value="ADMINISTRADOR">ADMINISTRADOR</option>
                  </select>
                  {usuarioActual?.id === usuarioForm.id ? (
                    <p className="mt-2 text-xs text-neutral-500">
                      No puedes cambiar tu propio rango de administrador.
                    </p>
                  ) : null}
                </div>

                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium" htmlFor="usuario-nivel-actual">
                      Nivel actual
                    </label>
                    <input
                      className={inputClass}
                      id="usuario-nivel-actual"
                      max="5"
                      min="1"
                      type="number"
                      value={usuarioForm.nivelActual}
                      onChange={(event) =>
                        setUsuarioForm((actual) => ({
                          ...actual,
                          nivelActual: event.target.value,
                        }))
                      }
                      required
                    />
                  </div>

                  <div>
                    <label className="text-sm font-medium" htmlFor="usuario-puntaje">
                      Puntaje
                    </label>
                    <input
                      className={inputClass}
                      id="usuario-puntaje"
                      step="1"
                      type="number"
                      value={usuarioForm.puntaje}
                      onChange={(event) =>
                        setUsuarioForm((actual) => ({
                          ...actual,
                          puntaje: event.target.value,
                        }))
                      }
                      required
                    />
                  </div>
                </div>

                <div className="flex gap-2">
                  <button className={`${buttonClass} bg-teal-700 text-white hover:bg-teal-800`} type="submit">
                    Guardar usuario
                  </button>
                  <button
                    className={`${buttonClass} bg-white text-neutral-900 ring-1 ring-neutral-300 hover:bg-neutral-50`}
                    type="button"
                    onClick={() => setUsuarioForm(usuarioInicial)}
                  >
                    Limpiar
                  </button>
                </div>
              </form>
            </section>

            <section className="rounded-lg border border-neutral-200 bg-white p-5 shadow-sm xl:col-span-2">
              <div className="grid gap-6 lg:grid-cols-[360px_minmax(0,1fr)]">
                <div>
                  <h2 className="text-lg font-semibold">
                    {nivelForm.id ? "Editar nivel" : "Crear nivel"}
                  </h2>
                  <form className="mt-4 space-y-4" onSubmit={guardarNivel}>
                    <div>
                      <label className="text-sm font-medium" htmlFor="nivel-nombre">
                        Nombre
                      </label>
                      <input
                        className={inputClass}
                        id="nivel-nombre"
                        value={nivelForm.nombre}
                        onChange={(event) =>
                          setNivelForm((actual) => ({ ...actual, nombre: event.target.value }))
                        }
                        required
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium" htmlFor="nivel-descripcion">
                        Descripcion
                      </label>
                      <textarea
                        className="mt-2 min-h-24 w-full rounded-md border border-neutral-300 px-3 py-2 text-sm outline-none transition focus:border-teal-700 focus:ring-2 focus:ring-teal-100"
                        id="nivel-descripcion"
                        value={nivelForm.descripcion}
                        onChange={(event) =>
                          setNivelForm((actual) => ({
                            ...actual,
                            descripcion: event.target.value,
                          }))
                        }
                        required
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium" htmlFor="nivel-numero">
                        Nivel
                      </label>
                      <input
                        className={inputClass}
                        id="nivel-numero"
                        min="1"
                        type="number"
                        value={nivelForm.nivel}
                        onChange={(event) =>
                          setNivelForm((actual) => ({ ...actual, nivel: event.target.value }))
                        }
                        required
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium" htmlFor="nivel-objetivo">
                        Objetivo
                      </label>
                      <input
                        className={inputClass}
                        id="nivel-objetivo"
                        value={nivelForm.objetivo}
                        onChange={(event) =>
                          setNivelForm((actual) => ({ ...actual, objetivo: event.target.value }))
                        }
                      />
                    </div>

                    <label className="flex items-center gap-2 text-sm font-medium">
                      <input
                        checked={nivelForm.activo}
                        onChange={(event) =>
                          setNivelForm((actual) => ({ ...actual, activo: event.target.checked }))
                        }
                        type="checkbox"
                      />
                      Activo
                    </label>

                    <div className="flex gap-2">
                      <button className={`${buttonClass} bg-teal-700 text-white hover:bg-teal-800`} type="submit">
                        Guardar nivel
                      </button>
                      <button
                        className={`${buttonClass} bg-white text-neutral-900 ring-1 ring-neutral-300 hover:bg-neutral-50`}
                        type="button"
                        onClick={() => setNivelForm(nivelInicial)}
                      >
                        Limpiar
                      </button>
                    </div>
                  </form>
                </div>

                <div>
                  <h2 className="text-lg font-semibold">Niveles</h2>
                  <div className="mt-4 grid gap-3">
                    {niveles.map((nivel) => (
                      <article className="rounded-md border border-neutral-200 p-4" key={nivel.id}>
                        <div className="flex flex-wrap items-start justify-between gap-3">
                          <div>
                            <h3 className="font-semibold">{nivel.nombre}</h3>
                            <p className="mt-1 text-sm leading-6 text-neutral-600">
                              {nivel.descripcion}
                            </p>
                          </div>
                          <span className="rounded-md bg-neutral-100 px-2 py-1 text-xs font-semibold">
                            Nivel {nivel.nivel}
                          </span>
                        </div>
                        <div className="mt-4 flex gap-2">
                          <button
                            className={`${buttonClass} bg-neutral-900 text-white hover:bg-neutral-700`}
                            type="button"
                            onClick={() => editarNivel(nivel)}
                          >
                            Editar
                          </button>
                          <button
                            className={`${buttonClass} bg-red-700 text-white hover:bg-red-800`}
                            type="button"
                            onClick={() => eliminarNivel(nivel.id)}
                          >
                            Borrar
                          </button>
                        </div>
                      </article>
                    ))}
                  </div>
                </div>
              </div>
            </section>
          </div>
        )}
      </section>
    </main>
  );
}
