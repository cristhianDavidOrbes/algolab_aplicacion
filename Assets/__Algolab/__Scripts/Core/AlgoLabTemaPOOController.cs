using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AlgoLabTemaPOOController : MonoBehaviour
{
    public enum AccionTema
    {
        Ninguna = 0,
        SpawnearPuertaTema = 1,
        MostrarVariantePuerta = 2,
        SecuenciaColorPuerta = 3,
        CambiarColorPuerta = 4,
        RestaurarColorPuerta = 5,
        AbrirPuerta = 6,
        CerrarPuerta = 7,
        CambiarAModoDiagrama = 8,
        CambiarAModoObjeto = 9,
        EventoPersonalizado = 10,

        SecuenciaModeloPuerta = 11
    }

    [Serializable]
    public class PasoTema
    {
        public string nombrePaso = "Paso";

        [Header("Audio")]
        public AudioClip audio;

        [Header("Acción")]
        public AccionTema accion = AccionTema.Ninguna;

        [Tooltip("Tiempo que espera después de iniciar el audio para ejecutar la acción.")]
        public float retrasoAntesDeAccion = 0f;

        [Header("Parámetros")]
        public int indiceVariantePuerta = 0;
        public Color colorPuerta = Color.white;

        [Header("Después del audio")]
        public float esperaDespuesAudio = 0.25f;

        [Header("Evento opcional")]
        public UnityEvent eventoPersonalizado;
    }

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Controladores")]
    public AlgoLabThemeDoorController puertaController;
    public AlgoLabTemaDoorSpawnBinder doorSpawnBinder;
    public AlgoLabClassDiagramModeManager diagramModeManager;

    [Header("Pasos del tema")]
    public List<PasoTema> pasos = new List<PasoTema>();

    [Header("Inicio")]
    public bool reproducirAlIniciar = false;

    [Header("Secuencias")]
    [Tooltip("Si está activo, espera a que terminen secuencias como colores o modelos antes de avanzar al siguiente audio.")]
    public bool esperarSecuenciasAntesDeSiguientePaso = true;

    [Header("Final")]
    public bool cambiarAModoObjetoAlTerminar = false;
    public UnityEvent OnTemaTerminado;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private Coroutine rutinaTema;
    private int indicePasoActual = -1;
    private bool reproduciendo;

    public int IndicePasoActual => indicePasoActual;
    public bool Reproduciendo => reproduciendo;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (doorSpawnBinder == null)
        {
            doorSpawnBinder = GetComponent<AlgoLabTemaDoorSpawnBinder>();
        }

        if (reproducirAlIniciar)
        {
            ReproducirTema();
        }
    }

    private void OnDisable()
    {
        DetenerTema();
    }

    [ContextMenu("Reproducir tema")]
    public void ReproducirTema()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("No se puede reproducir el tema con su controlador desactivado.");
            return;
        }

        // Un tema siempre debe usar el diagrama explicativo completo. Si el
        // usuario venía de una práctica, el controlador conservaba el modo
        // práctica y solo mostraba los encabezados vacíos de atributos y
        // métodos, incluso después de crear la puerta del tema.
        if (diagramModeManager != null)
        {
            diagramModeManager.SetModoDiagrama();
            if (diagramModeManager.classDiagramController != null)
            {
                diagramModeManager.classDiagramController.CambiarAModoDictado();
            }
        }

        if (rutinaTema != null)
        {
            StopCoroutine(rutinaTema);
        }

        rutinaTema = StartCoroutine(ReproducirSecuencia());
    }

    [ContextMenu("Detener tema")]
    public void DetenerTema()
    {
        if (rutinaTema != null)
        {
            StopCoroutine(rutinaTema);
            rutinaTema = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        reproduciendo = false;
        indicePasoActual = -1;
        puertaController?.DetenerSecuencias(false);
    }

    public void ReproducirDesdePaso(int indice)
    {
        if (pasos == null || indice < 0 || indice >= pasos.Count)
        {
            Debug.LogWarning("Índice de paso fuera de rango: " + indice);
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("No se puede reproducir el tema con su controlador desactivado.");
            return;
        }

        if (rutinaTema != null)
        {
            StopCoroutine(rutinaTema);
        }

        rutinaTema = StartCoroutine(ReproducirSecuenciaDesde(indice));
    }

    private IEnumerator ReproducirSecuencia()
    {
        yield return ReproducirSecuenciaDesde(0);
    }

    private IEnumerator ReproducirSecuenciaDesde(int inicio)
    {
        reproduciendo = true;

        if (pasos == null)
        {
            reproduciendo = false;
            indicePasoActual = -1;
            rutinaTema = null;
            yield break;
        }

        for (int i = inicio; i < pasos.Count; i++)
        {
            indicePasoActual = i;

            PasoTema paso = pasos[i];

            if (paso == null)
            {
                continue;
            }

            yield return ReproducirPaso(paso);
        }

        reproduciendo = false;
        indicePasoActual = -1;
        rutinaTema = null;

        if (cambiarAModoObjetoAlTerminar && diagramModeManager != null)
        {
            diagramModeManager.SetModoObjeto();
        }

        OnTemaTerminado?.Invoke();

        if (mostrarDebug)
        {
            Debug.Log("Tema terminado.");
        }
    }

    private IEnumerator ReproducirPaso(PasoTema paso)
    {
        if (mostrarDebug)
        {
            Debug.Log("Reproduciendo paso: " + paso.nombrePaso);
        }

        float duracionAudio = 0f;

        if (audioSource != null && paso.audio != null)
        {
            audioSource.clip = paso.audio;
            audioSource.Play();
            duracionAudio = paso.audio.length;
        }

        float retrasoAccion = Mathf.Clamp(
            paso.retrasoAntesDeAccion,
            0f,
            duracionAudio
        );

        if (retrasoAccion > 0f)
        {
            yield return new WaitForSecondsRealtime(retrasoAccion);
        }

        EjecutarAccion(paso);

        float tiempoRestante = duracionAudio - retrasoAccion;

        if (tiempoRestante > 0f)
        {
            yield return new WaitForSecondsRealtime(tiempoRestante);
        }

        if (esperarSecuenciasAntesDeSiguientePaso)
        {
            yield return EsperarSecuenciaSiExiste(paso);
        }

        if (paso.esperaDespuesAudio > 0f)
        {
            yield return new WaitForSecondsRealtime(paso.esperaDespuesAudio);
        }
    }

    private IEnumerator EsperarSecuenciaSiExiste(PasoTema paso)
    {
        if (puertaController == null)
        {
            yield break;
        }

        if (paso.accion == AccionTema.SecuenciaModeloPuerta)
        {
            while (puertaController.SecuenciaModelosEnCurso())
            {
                yield return null;
            }
        }

        if (paso.accion == AccionTema.SecuenciaColorPuerta)
        {
            while (puertaController.SecuenciaColorEnCurso())
            {
                yield return null;
            }
        }
    }

    private void EjecutarAccion(PasoTema paso)
    {
        switch (paso.accion)
        {
            case AccionTema.Ninguna:
                break;

            case AccionTema.SpawnearPuertaTema:
                if (doorSpawnBinder != null)
                {
                    doorSpawnBinder.SpawnearPuertaTema();
                }
                break;

            case AccionTema.MostrarVariantePuerta:
                if (puertaController != null)
                {
                    puertaController.CambiarVariante(paso.indiceVariantePuerta);
                }
                break;

            case AccionTema.SecuenciaModeloPuerta:
                if (puertaController != null)
                {
                    puertaController.ReproducirSecuenciaModelosTema();
                }
                break;

            case AccionTema.SecuenciaColorPuerta:
                if (puertaController != null)
                {
                    puertaController.ReproducirSecuenciaColorTema();
                }
                break;

            case AccionTema.CambiarColorPuerta:
                if (puertaController != null)
                {
                    puertaController.CambiarColor(paso.colorPuerta);
                }
                break;

            case AccionTema.RestaurarColorPuerta:
                if (puertaController != null)
                {
                    puertaController.RestaurarColorOriginal();
                }
                break;

            case AccionTema.AbrirPuerta:
                if (puertaController != null)
                {
                    puertaController.AbrirPuerta();
                }
                break;

            case AccionTema.CerrarPuerta:
                if (puertaController != null)
                {
                    puertaController.CerrarPuerta();
                }
                break;

            case AccionTema.CambiarAModoDiagrama:
                if (diagramModeManager != null)
                {
                    diagramModeManager.SetModoDiagrama();
                }
                break;

            case AccionTema.CambiarAModoObjeto:
                if (diagramModeManager != null)
                {
                    diagramModeManager.SetModoObjeto();
                }
                break;

            case AccionTema.EventoPersonalizado:
                break;
        }
        paso.eventoPersonalizado?.Invoke();
        if (mostrarDebug)
        {
            Debug.Log("Acción ejecutada: " + paso.accion);
        }
    }

    public void AsignarPuertaController(AlgoLabThemeDoorController nuevaPuerta)
    {
        puertaController = nuevaPuerta;

        if (mostrarDebug)
        {
            Debug.Log("Puerta controller asignado desde spawn.");
        }
    }
}
