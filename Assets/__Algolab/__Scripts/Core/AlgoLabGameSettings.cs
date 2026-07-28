using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class AlgoLabGameSettings : MonoBehaviour
{
    public static AlgoLabGameSettings Instance { get; private set; }

    private const string Prefijo = "ALGOLAB_CONFIG_";
    private const string KeyModoPostura = Prefijo + "MODO_POSTURA";
    private const string KeyAlturaSentado = Prefijo + "ALTURA_SENTADO";
    private const string KeyAlturaParado = Prefijo + "ALTURA_PARADO";
    private const string KeySuavizarAltura = Prefijo + "SUAVIZAR_ALTURA";
    private const string KeyVolumenGeneral = Prefijo + "VOLUMEN_GENERAL";
    private const string KeyVolumenVoz = Prefijo + "VOLUMEN_VOZ";
    private const string KeyVolumenEfectos = Prefijo + "VOLUMEN_EFECTOS";
    private const string KeyModoSalidaIA = Prefijo + "MODO_SALIDA_IA";
    private const string KeyPerfilGrafico = Prefijo + "PERFIL_GRAFICO";
    private const string KeyEscalaRender = Prefijo + "ESCALA_RENDER";
    private const string KeyFpsObjetivo = Prefijo + "FPS_OBJETIVO";
    private const string KeyMigracionPosturaAutomatica20260722 =
        Prefijo + "MIGRACION_POSTURA_AUTOMATICA_20260722";

    public int ModoPostura { get; private set; }
    public float AlturaSentado { get; private set; } = 1.2f;
    public float AlturaParado { get; private set; } = 1.5f;
    public bool SuavizarAltura { get; private set; } = true;
    public float VolumenGeneral { get; private set; } = 0.9f;
    public float VolumenVoz { get; private set; } = 1f;
    public float VolumenEfectos { get; private set; } = 1f;
    public int ModoSalidaIA { get; private set; } = 2;
    public bool MostrarSubtitulosIA => ModoSalidaIA != 1;
    public bool ReproducirAudioIA => ModoSalidaIA != 0;
    public int PerfilGrafico { get; private set; } = 1;
    public float EscalaRender { get; private set; } = 1f;
    public int FpsObjetivo { get; private set; } = 72;

    public event Action AjustesCambiaron;

    private readonly Dictionary<AudioSource, FuenteAudioRegistrada> fuentesAudio =
        new Dictionary<AudioSource, FuenteAudioRegistrada>();
    private readonly List<XRDisplaySubsystem> pantallasXR = new List<XRDisplaySubsystem>();
    private float proximaBusquedaAudio;
    private float proximoIntentoFrecuenciaVisor;
    private bool guardadoPendiente;
    private float momentoProximoGuardado;

    private class FuenteAudioRegistrada
    {
        public float volumenBase;
        public bool esVoz;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearServicio()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("[ALGOLAB_GAME_SETTINGS]");
        root.AddComponent<AlgoLabGameSettings>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Cargar();
        AplicarAudio();
        AplicarGraficos();
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            GuardarPendienteAhora();
            SceneManager.sceneLoaded -= AlCargarEscena;
            Instance = null;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime >= proximaBusquedaAudio)
        {
            proximaBusquedaAudio = Time.unscaledTime + 2f;
            RegistrarFuentesAudioNuevas();
        }

        if (Time.unscaledTime >= proximoIntentoFrecuenciaVisor)
        {
            proximoIntentoFrecuenciaVisor = Time.unscaledTime + 2f;
            AplicarFrecuenciaVisor();
        }

        if (guardadoPendiente && Time.unscaledTime >= momentoProximoGuardado)
        {
            GuardarPendienteAhora();
        }
    }

    private void OnApplicationPause(bool pausado)
    {
        if (pausado)
        {
            GuardarPendienteAhora();
        }
    }

    private void OnApplicationQuit()
    {
        GuardarPendienteAhora();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        proximaBusquedaAudio = 0f;
        proximoIntentoFrecuenciaVisor = 0f;
        AplicarAudio();
        AplicarGraficos();
        AplicarPaneles();
    }

    private void Cargar()
    {
        ModoPostura = Mathf.Clamp(PlayerPrefs.GetInt(KeyModoPostura, 0), 0, 2);

        // Versiones anteriores podian dejar el modo manual guardado por un click
        // lejano del panel de opciones. Esta migracion devuelve una sola vez las
        // instalaciones existentes a Automatico; las elecciones posteriores del
        // usuario vuelven a guardarse y respetarse normalmente.
        if (PlayerPrefs.GetInt(KeyMigracionPosturaAutomatica20260722, 0) == 0)
        {
            ModoPostura = 0;
            PlayerPrefs.SetInt(KeyModoPostura, ModoPostura);
            PlayerPrefs.SetInt(KeyMigracionPosturaAutomatica20260722, 1);
            PlayerPrefs.Save();
        }

        AlturaSentado = Sanitizar(PlayerPrefs.GetFloat(KeyAlturaSentado, 1.2f), 1.2f, 0.75f, 1.8f);
        AlturaParado = Sanitizar(PlayerPrefs.GetFloat(KeyAlturaParado, 1.5f), 1.5f, 1f, 2.4f);
        AlturaSentado = Mathf.Min(AlturaSentado, AlturaParado);
        SuavizarAltura = PlayerPrefs.GetInt(KeySuavizarAltura, 1) == 1;
        VolumenGeneral = Sanitizar(PlayerPrefs.GetFloat(KeyVolumenGeneral, 0.9f), 0.9f, 0f, 1f);
        VolumenVoz = Sanitizar(PlayerPrefs.GetFloat(KeyVolumenVoz, 1f), 1f, 0f, 1f);
        VolumenEfectos = Sanitizar(PlayerPrefs.GetFloat(KeyVolumenEfectos, 1f), 1f, 0f, 1f);
        ModoSalidaIA = Mathf.Clamp(PlayerPrefs.GetInt(KeyModoSalidaIA, 2), 0, 2);
        PerfilGrafico = Mathf.Clamp(PlayerPrefs.GetInt(KeyPerfilGrafico, 1), 0, 2);
        EscalaRender = Sanitizar(PlayerPrefs.GetFloat(KeyEscalaRender, 1f), 1f, 0.75f, 1.2f);
        FpsObjetivo = NormalizarFps(PlayerPrefs.GetInt(KeyFpsObjetivo, 72));
    }

    private void Guardar()
    {
        PlayerPrefs.SetInt(KeyModoPostura, ModoPostura);
        PlayerPrefs.SetFloat(KeyAlturaSentado, AlturaSentado);
        PlayerPrefs.SetFloat(KeyAlturaParado, AlturaParado);
        PlayerPrefs.SetInt(KeySuavizarAltura, SuavizarAltura ? 1 : 0);
        PlayerPrefs.SetFloat(KeyVolumenGeneral, VolumenGeneral);
        PlayerPrefs.SetFloat(KeyVolumenVoz, VolumenVoz);
        PlayerPrefs.SetFloat(KeyVolumenEfectos, VolumenEfectos);
        PlayerPrefs.SetInt(KeyModoSalidaIA, ModoSalidaIA);
        PlayerPrefs.SetInt(KeyPerfilGrafico, PerfilGrafico);
        PlayerPrefs.SetFloat(KeyEscalaRender, EscalaRender);
        PlayerPrefs.SetInt(KeyFpsObjetivo, FpsObjetivo);
        guardadoPendiente = true;
        momentoProximoGuardado = Time.unscaledTime + 0.5f;
    }

    private void GuardarPendienteAhora()
    {
        if (!guardadoPendiente)
        {
            return;
        }

        PlayerPrefs.Save();
        guardadoPendiente = false;
    }

    public void SetModoPostura(int modo)
    {
        int nuevo = Mathf.Clamp(modo, 0, 2);
        if (ModoPostura == nuevo)
        {
            return;
        }

        ModoPostura = nuevo;
        GuardarYAplicarPaneles();
    }

    public void SetAlturaSentado(float valor)
    {
        float nuevo = Sanitizar(valor, AlturaSentado, 0.75f, 1.8f);
        nuevo = Mathf.Min(nuevo, AlturaParado);
        if (Mathf.Approximately(AlturaSentado, nuevo))
        {
            return;
        }

        AlturaSentado = nuevo;
        GuardarYAplicarPaneles();
    }

    public void SetAlturaParado(float valor)
    {
        float nuevo = Sanitizar(valor, AlturaParado, 1f, 2.4f);
        nuevo = Mathf.Max(nuevo, AlturaSentado);
        if (Mathf.Approximately(AlturaParado, nuevo))
        {
            return;
        }

        AlturaParado = nuevo;
        GuardarYAplicarPaneles();
    }

    public void SetSuavizarAltura(bool valor)
    {
        if (SuavizarAltura == valor)
        {
            return;
        }

        SuavizarAltura = valor;
        GuardarYAplicarPaneles();
    }

    public void SetVolumenGeneral(float valor)
    {
        float nuevo = Sanitizar(valor, VolumenGeneral, 0f, 1f);
        if (Mathf.Approximately(VolumenGeneral, nuevo))
        {
            return;
        }

        VolumenGeneral = nuevo;
        Guardar();
        AplicarAudio();
        AjustesCambiaron?.Invoke();
    }

    public void SetVolumenVoz(float valor)
    {
        float nuevo = Sanitizar(valor, VolumenVoz, 0f, 1f);
        if (Mathf.Approximately(VolumenVoz, nuevo))
        {
            return;
        }

        VolumenVoz = nuevo;
        Guardar();
        AplicarAudio();
        AjustesCambiaron?.Invoke();
    }

    public void SetVolumenEfectos(float valor)
    {
        float nuevo = Sanitizar(valor, VolumenEfectos, 0f, 1f);
        if (Mathf.Approximately(VolumenEfectos, nuevo))
        {
            return;
        }

        VolumenEfectos = nuevo;
        Guardar();
        AplicarAudio();
        AjustesCambiaron?.Invoke();
    }

    public void SetModoSalidaIA(int modo)
    {
        int nuevo = Mathf.Clamp(modo, 0, 2);
        if (ModoSalidaIA == nuevo)
        {
            return;
        }

        ModoSalidaIA = nuevo;
        Guardar();
        AjustesCambiaron?.Invoke();
    }

    public void SetPerfilGrafico(int perfil)
    {
        int nuevo = Mathf.Clamp(perfil, 0, 2);
        if (PerfilGrafico == nuevo)
        {
            return;
        }

        PerfilGrafico = nuevo;
        Guardar();
        AplicarGraficos();
        AjustesCambiaron?.Invoke();
    }

    public void SetEscalaRender(float valor)
    {
        float nuevo = Sanitizar(valor, EscalaRender, 0.75f, 1.2f);
        if (Mathf.Approximately(EscalaRender, nuevo))
        {
            return;
        }

        EscalaRender = nuevo;
        Guardar();
        AplicarGraficos();
        AjustesCambiaron?.Invoke();
    }

    public void SetFpsObjetivo(int fps)
    {
        int nuevo = NormalizarFps(fps);
        if (FpsObjetivo == nuevo)
        {
            return;
        }

        FpsObjetivo = nuevo;
        Guardar();
        AplicarGraficos();
        AjustesCambiaron?.Invoke();
    }

    public void RecolocarPaneles()
    {
        AlgoLabManualPanelSpawnManager manager = BuscarManagerPaneles();
        if (manager != null)
        {
            manager.RecolocarPanelesPredeterminados();
        }
    }

    public void RestablecerPredeterminados()
    {
        ModoPostura = 0;
        AlturaSentado = 1.2f;
        AlturaParado = 1.5f;
        SuavizarAltura = true;
        VolumenGeneral = 0.9f;
        VolumenVoz = 1f;
        VolumenEfectos = 1f;
        ModoSalidaIA = 2;
        PerfilGrafico = 1;
        EscalaRender = 1f;
        FpsObjetivo = 72;

        Guardar();
        AplicarAudio();
        AplicarGraficos();
        AplicarPaneles();
        RecolocarPaneles();
        AjustesCambiaron?.Invoke();
    }

    public void AplicarPaneles()
    {
        AlgoLabManualPanelSpawnManager manager = BuscarManagerPaneles();
        if (manager == null)
        {
            return;
        }

        manager.AplicarConfiguracionAltura(
            ModoPostura,
            AlturaSentado,
            AlturaParado,
            SuavizarAltura
        );
    }

    private void GuardarYAplicarPaneles()
    {
        Guardar();
        AplicarPaneles();
        AjustesCambiaron?.Invoke();
    }

    private AlgoLabManualPanelSpawnManager BuscarManagerPaneles()
    {
        if (AlgoLabManualPanelSpawnManager.Instance != null)
        {
            return AlgoLabManualPanelSpawnManager.Instance;
        }

        return FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(FindObjectsInactive.Include);
    }

    private void AplicarAudio()
    {
        AudioListener.volume = VolumenGeneral;
        RegistrarFuentesAudioNuevas();

        List<AudioSource> eliminadas = null;
        foreach (KeyValuePair<AudioSource, FuenteAudioRegistrada> par in fuentesAudio)
        {
            if (par.Key == null)
            {
                if (eliminadas == null)
                {
                    eliminadas = new List<AudioSource>();
                }

                eliminadas.Add(par.Key);
                continue;
            }

            float categoria = par.Value.esVoz ? VolumenVoz : VolumenEfectos;
            par.Key.volume = par.Value.volumenBase * categoria;
        }

        if (eliminadas != null)
        {
            for (int i = 0; i < eliminadas.Count; i++)
            {
                fuentesAudio.Remove(eliminadas[i]);
            }
        }
    }

    private void RegistrarFuentesAudioNuevas()
    {
        AudioSource[] fuentes = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < fuentes.Length; i++)
        {
            AudioSource fuente = fuentes[i];
            if (fuente == null || fuentesAudio.ContainsKey(fuente))
            {
                continue;
            }

            FuenteAudioRegistrada registro = new FuenteAudioRegistrada
            {
                volumenBase = fuente.volume,
                esVoz = EsFuenteDeVoz(fuente)
            };
            fuentesAudio.Add(fuente, registro);

            float categoria = registro.esVoz ? VolumenVoz : VolumenEfectos;
            fuente.volume = registro.volumenBase * categoria;
        }
    }

    private bool EsFuenteDeVoz(AudioSource fuente)
    {
        if (fuente.GetComponentInParent<AlgoLabTutorialPanelController>(true) != null)
        {
            return true;
        }

        string nombre = fuente.name.ToLowerInvariant();
        return nombre.Contains("tutorial") ||
               nombre.Contains("voice") ||
               nombre.Contains("voz") ||
               nombre.Contains("speech") ||
               nombre.Contains("narr") ||
               nombre.Contains("tts") ||
               nombre.Contains("ia");
    }

    private void AplicarGraficos()
    {
        string[] niveles = QualitySettings.names;
        if (niveles != null && niveles.Length > 0)
        {
            int indice;
            if (PerfilGrafico == 0)
            {
                indice = 0;
            }
            else if (PerfilGrafico == 2)
            {
                indice = niveles.Length - 1;
            }
            else
            {
                indice = (niveles.Length - 1) / 2;
            }

            QualitySettings.SetQualityLevel(indice, true);
        }

        XRSettings.eyeTextureResolutionScale = EscalaRender;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FpsObjetivo;
        AplicarFrecuenciaVisor();
    }

    private void AplicarFrecuenciaVisor()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            float[] frecuenciasDisponibles = OVRPlugin.systemDisplayFrequenciesAvailable;
            if (frecuenciasDisponibles == null || frecuenciasDisponibles.Length == 0)
            {
                return;
            }

            float frecuenciaObjetivo = frecuenciasDisponibles[0];
            float distanciaMenor = Mathf.Abs(frecuenciaObjetivo - FpsObjetivo);

            for (int i = 1; i < frecuenciasDisponibles.Length; i++)
            {
                float distancia = Mathf.Abs(frecuenciasDisponibles[i] - FpsObjetivo);
                if (distancia < distanciaMenor)
                {
                    frecuenciaObjetivo = frecuenciasDisponibles[i];
                    distanciaMenor = distancia;
                }
            }

            float frecuenciaActual = OVRPlugin.systemDisplayFrequency;
            if (frecuenciaActual <= 1f || Mathf.Abs(frecuenciaActual - frecuenciaObjetivo) > 0.1f)
            {
                OVRPlugin.systemDisplayFrequency = frecuenciaObjetivo;
            }

            Application.targetFrameRate = Mathf.RoundToInt(frecuenciaObjetivo);
        }
        catch (Exception excepcion)
        {
            Debug.LogWarning("ALGOLAB_CONFIG: No se pudo aplicar la frecuencia del visor: " + excepcion.Message);
        }
#endif
    }

    public float ObtenerFrecuenciaPantallaActual()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            float frecuenciaMeta = OVRPlugin.systemDisplayFrequency;
            if (frecuenciaMeta > 1f)
            {
                return frecuenciaMeta;
            }
        }
        catch (Exception)
        {
            // El subsistema XR de Unity se usa como respaldo.
        }
#endif

        pantallasXR.Clear();
        SubsystemManager.GetSubsystems(pantallasXR);

        for (int i = 0; i < pantallasXR.Count; i++)
        {
            XRDisplaySubsystem pantalla = pantallasXR[i];
            if (pantalla != null &&
                pantalla.running &&
                pantalla.TryGetDisplayRefreshRate(out float frecuencia) &&
                frecuencia > 1f)
            {
                return frecuencia;
            }
        }

        return 0f;
    }

    private int NormalizarFps(int fps)
    {
        // El perfil expone un rango seguro de 60 a 72 FPS. En Quest la
        // frecuencia real disponible puede ser distinta; AplicarFrecuenciaVisor
        // elige la frecuencia del visor más cercana al objetivo.
        if (fps >= 69)
        {
            return 72;
        }

        if (fps >= 63)
        {
            return 66;
        }

        return 60;
    }

    private static float Sanitizar(float valor, float respaldo, float minimo, float maximo)
    {
        if (float.IsNaN(valor) || float.IsInfinity(valor))
        {
            valor = respaldo;
        }

        return Mathf.Clamp(valor, minimo, maximo);
    }
}
