import Link from "next/link";
import Image from "next/image";

const buttonClass =
  "inline-flex h-11 items-center justify-center rounded-md px-5 text-sm font-semibold transition";

const features = [
  {
    title: "Aprendizaje guiado",
    text: "Niveles pensados para conectar conceptos de programacion orientada a objetos con decisiones dentro del juego.",
  },
  {
    title: "Progreso guardado",
    text: "Los estudiantes autenticados conservan nivel actual, puntaje, intentos y avance por cada reto completado.",
  },
  {
    title: "Paneles por rol",
    text: "Docentes y administradores pueden consultar usuarios, niveles y contenido desde una experiencia web simple.",
  },
];

const steps = ["Inicia sesion", "Juega los niveles", "Guarda tu avance"];

export default function Home() {
  return (
    <main className="min-h-screen bg-neutral-100 text-neutral-950">
      <section className="relative min-h-[92vh] overflow-hidden">
        <Image
          alt="Estudiante aprendiendo programacion en una interfaz interactiva"
          className="absolute inset-0 h-full w-full object-cover"
          fill
          priority
          src="https://images.unsplash.com/photo-1515879218367-8466d910aaa4?auto=format&fit=crop&w=1800&q=85"
        />
        <div className="absolute inset-0 bg-neutral-950/70" />

        <div className="relative mx-auto flex min-h-[92vh] w-full max-w-7xl flex-col px-4 py-6 sm:px-6 lg:px-8">
          <header className="flex flex-wrap items-center justify-between gap-4 border-b border-white/15 pb-5 text-white">
            <Link className="text-lg font-semibold tracking-normal" href="/">
              AlgoLab
            </Link>
            <nav className="flex w-full items-center gap-2 sm:w-auto">
              <Link
                className={`${buttonClass} flex-1 bg-white/10 text-white ring-1 ring-white/25 hover:bg-white/15 sm:flex-none`}
                href="/iniciar-sesion"
              >
                Iniciar sesion
              </Link>
              <Link
                className={`${buttonClass} flex-1 bg-teal-600 text-white hover:bg-teal-700 sm:flex-none`}
                href="/registrarse"
              >
                Registrarse
              </Link>
            </nav>
          </header>

          <div className="grid flex-1 items-center gap-10 py-12 lg:grid-cols-[minmax(0,1fr)_420px]">
            <div className="max-w-3xl text-white">
              <p className="text-sm font-semibold uppercase tracking-[0.18em] text-teal-200">
                Videojuego educativo
              </p>
              <h1 className="mt-4 text-4xl font-semibold leading-tight sm:text-5xl lg:text-6xl">
                AlgoLab
              </h1>
              <p className="mt-5 max-w-2xl text-base leading-8 text-neutral-100 sm:text-lg">
                Aprende programacion orientada a objetos resolviendo retos
                interactivos, creando vehiculos y avanzando nivel por nivel con
                tu progreso guardado.
              </p>

              <div className="mt-8 flex flex-wrap gap-3">
                <Link
                  className={`${buttonClass} bg-teal-600 text-white hover:bg-teal-700`}
                  href="/registrarse"
                >
                  Crear cuenta
                </Link>
                <Link
                  className={`${buttonClass} bg-white text-neutral-950 ring-1 ring-white hover:bg-neutral-100`}
                  href="/iniciar-sesion"
                >
                  Ya tengo cuenta
                </Link>
              </div>
            </div>

            <aside className="rounded-lg border border-white/20 bg-white/95 p-5 shadow-xl">
              <p className="text-sm font-semibold text-teal-700">Tu avance</p>
              <div className="mt-4 grid gap-3">
                {steps.map((step, index) => (
                  <div
                    className="flex items-center gap-3 rounded-md border border-neutral-200 bg-neutral-50 p-3"
                    key={step}
                  >
                    <span className="flex size-8 items-center justify-center rounded-md bg-teal-700 text-sm font-semibold text-white">
                      {index + 1}
                    </span>
                    <span className="text-sm font-medium text-neutral-800">{step}</span>
                  </div>
                ))}
              </div>
              <div className="mt-5 rounded-md bg-neutral-900 p-4 text-white">
                <p className="text-sm text-neutral-300">Nivel actual</p>
                <p className="mt-2 text-3xl font-semibold">POO</p>
                <p className="mt-3 text-sm leading-6 text-neutral-300">
                  Conceptos, atributos, metodos y objetos aplicados dentro de
                  una practica interactiva.
                </p>
              </div>
            </aside>
          </div>
        </div>
      </section>

      <section className="border-y border-neutral-200 bg-white px-4 py-12 sm:px-6 lg:px-8">
        <div className="mx-auto grid w-full max-w-7xl gap-5 md:grid-cols-3">
          {features.map((feature) => (
            <article
              className="rounded-lg border border-neutral-200 bg-white p-5 shadow-sm"
              key={feature.title}
            >
              <h2 className="text-base font-semibold text-neutral-950">{feature.title}</h2>
              <p className="mt-3 text-sm leading-6 text-neutral-600">{feature.text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="bg-neutral-100 px-4 py-12 sm:px-6 lg:px-8">
        <div className="mx-auto flex w-full max-w-7xl flex-col gap-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-sm font-semibold text-teal-700">Listo para empezar</p>
            <h2 className="mt-2 text-2xl font-semibold">Entra a AlgoLab y continua tu ruta.</h2>
          </div>
          <div className="flex flex-wrap gap-3">
            <Link
              className={`${buttonClass} bg-neutral-900 text-white hover:bg-neutral-700`}
              href="/iniciar-sesion"
            >
              Iniciar sesion
            </Link>
            <Link
              className={`${buttonClass} bg-white text-neutral-900 ring-1 ring-neutral-300 hover:bg-neutral-50`}
              href="/registrarse"
            >
              Registrarse
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}
