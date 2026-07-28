using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoorScript;

public class AlgoLabThemeDoorController : MonoBehaviour
{
    [Serializable]
    public class VariantePuerta
    {
        [Header("Identificación")]
        public string nombre = "Puerta";

        [Header("Root de la variante")]
        public GameObject root;

        [Header("Script original de la puerta")]
        public Door doorScript;

        [Header("Renderers para color")]
        public Renderer[] renderersParaColor;
    }

    [Header("Variantes de puerta")]
    public List<VariantePuerta> variantes = new List<VariantePuerta>();

    [Header("Inicio")]
    public int varianteInicial = 0;
    public bool cerrarAlIniciar = true;

    [Header("Color con smooth")]
    public Color colorTema1 = new Color(0.45f, 0.25f, 0.10f, 1f);
    public Color colorTema2 = Color.white;

    [Tooltip("Duración del cambio suave de un color a otro.")]
    public float duracionCambioColor = 0.8f;

    [Tooltip("Tiempo que espera entre el primer color y el segundo color.")]
    public float tiempoEntreColores = 2f;

    [Tooltip("Tiempo que espera antes de volver al color original después del segundo color.")]
    public float tiempoAntesDeRestaurar = 2f;

    [Header("Secuencia de modelos")]
    [Tooltip("Tiempo que se muestra cada modelo durante la explicación del atributo modelo.")]
    public float tiempoEntreModelos = 2f;

    [Tooltip("Modelo al que vuelve cuando termina la secuencia. Normalmente 0 para volver al primero.")]
    public int varianteFinalAlTerminarSecuencia = 0;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private int indiceActual = -1;
    private Coroutine rutinaColor;
    private Coroutine rutinaModelos;
    private int generacionColor;
    private int generacionModelos;

    private readonly Dictionary<Material, Color> coloresOriginales =
        new Dictionary<Material, Color>();

    public int IndiceActual => indiceActual;

    private void Awake()
    {
        PrepararVariantes();
        GuardarColoresOriginales();
        CambiarVariante(varianteInicial);

        if (cerrarAlIniciar)
        {
            CerrarPuertaInstantaneo();
        }
    }

    private void Start()
    {
        PrepararVariantes();
    }

    private void OnDisable()
    {
        DetenerSecuencias(false);
    }

    public void DetenerSecuencias(bool restaurarColor = false)
    {
        generacionColor++;
        generacionModelos++;

        if (rutinaColor != null)
        {
            StopCoroutine(rutinaColor);
            rutinaColor = null;
        }

        if (rutinaModelos != null)
        {
            StopCoroutine(rutinaModelos);
            rutinaModelos = null;
        }

        if (restaurarColor)
            RestaurarColoresOriginalesInmediato();
    }

    private void PrepararVariantes()
    {
        if (variantes == null)
        {
            return;
        }

        for (int i = 0; i < variantes.Count; i++)
        {
            VariantePuerta variante = variantes[i];

            if (variante == null || variante.root == null)
            {
                continue;
            }

            if (variante.doorScript == null)
            {
                variante.doorScript = variante.root.GetComponentInChildren<Door>(true);
            }

            if (variante.renderersParaColor == null || variante.renderersParaColor.Length == 0)
            {
                variante.renderersParaColor = variante.root.GetComponentsInChildren<Renderer>(true);
            }
        }
    }

    private void GuardarColoresOriginales()
    {
        coloresOriginales.Clear();

        if (variantes == null)
        {
            return;
        }

        for (int i = 0; i < variantes.Count; i++)
        {
            VariantePuerta variante = variantes[i];

            if (variante == null || variante.root == null)
            {
                continue;
            }

            Renderer[] renderers = variante.root.GetComponentsInChildren<Renderer>(true);

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];

                if (renderer == null)
                {
                    continue;
                }

                Material[] materiales = renderer.materials;

                for (int m = 0; m < materiales.Length; m++)
                {
                    Material material = materiales[m];

                    if (material == null || coloresOriginales.ContainsKey(material))
                    {
                        continue;
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        coloresOriginales.Add(material, material.GetColor("_BaseColor"));
                    }
                    else if (material.HasProperty("_Color"))
                    {
                        coloresOriginales.Add(material, material.GetColor("_Color"));
                    }
                }
            }
        }
    }

    [ContextMenu("Cambiar a variante 0")]
    public void CambiarAVariante0()
    {
        CambiarVariante(0);
    }

    [ContextMenu("Cambiar a variante 1")]
    public void CambiarAVariante1()
    {
        CambiarVariante(1);
    }

    [ContextMenu("Cambiar a variante 2")]
    public void CambiarAVariante2()
    {
        CambiarVariante(2);
    }

    [ContextMenu("Siguiente variante")]
    public void CambiarASiguienteVariante()
    {
        if (variantes == null || variantes.Count == 0)
        {
            Debug.LogWarning("No hay variantes configuradas.");
            return;
        }

        int siguiente = indiceActual + 1;

        if (siguiente >= variantes.Count)
        {
            siguiente = 0;
        }

        CambiarVariante(siguiente);
    }

    public void CambiarVariante(int indice)
    {
        PrepararVariantes();

        if (variantes == null || variantes.Count == 0)
        {
            Debug.LogWarning("No hay variantes de puerta configuradas.");
            return;
        }

        if (indice < 0 || indice >= variantes.Count)
        {
            Debug.LogWarning("Índice de puerta fuera de rango: " + indice);
            return;
        }

        bool estabaAbierta = EstaAbierta();

        for (int i = 0; i < variantes.Count; i++)
        {
            VariantePuerta variante = variantes[i];

            if (variante == null || variante.root == null)
            {
                continue;
            }

            bool esLaSeleccionada = i == indice;

            ReiniciarPuerta(variante);
            variante.root.SetActive(esLaSeleccionada);

            if (mostrarDebug)
            {
                Debug.Log(
                    "Variante " + i +
                    " | " + variante.nombre +
                    " | Activa = " + esLaSeleccionada
                );
            }
        }

        indiceActual = indice;

        if (estabaAbierta)
        {
            AbrirPuerta();
        }
        else
        {
            CerrarPuertaInstantaneo();
        }

        if (mostrarDebug)
        {
            Debug.Log(
                "Modelo de puerta cambiado a índice: " +
                indice + " | " + variantes[indice].nombre
            );
        }
    }

    private void ReiniciarPuerta(VariantePuerta variante)
    {
        if (variante == null)
        {
            return;
        }

        if (variante.doorScript == null && variante.root != null)
        {
            variante.doorScript = variante.root.GetComponentInChildren<Door>(true);
        }

        if (variante.doorScript == null)
        {
            return;
        }

        variante.doorScript.open = false;
        variante.doorScript.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public bool SecuenciaModelosEnCurso()
    {
        return rutinaModelos != null;
    }

    public void ReproducirSecuenciaModelosTema()
    {
        if (rutinaModelos != null)
        {
            StopCoroutine(rutinaModelos);
        }

        int miGeneracion = ++generacionModelos;

        if (!isActiveAndEnabled)
        {
            if (variantes != null && variantes.Count > 0)
            {
                CambiarVariante(Mathf.Clamp(varianteFinalAlTerminarSecuencia, 0, variantes.Count - 1));
            }

            rutinaModelos = null;
            return;
        }

        rutinaModelos = StartCoroutine(SecuenciaModelosTema(miGeneracion));
    }

    private IEnumerator SecuenciaModelosTema(int miGeneracion)
    {
        if (variantes == null || variantes.Count == 0)
        {
            rutinaModelos = null;
            yield break;
        }

        for (int i = 0; i < variantes.Count && miGeneracion == generacionModelos; i++)
        {
            CambiarVariante(i);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoEntreModelos));
        }

        if (miGeneracion != generacionModelos)
            yield break;

        int indiceFinal = Mathf.Clamp(
            varianteFinalAlTerminarSecuencia,
            0,
            variantes.Count - 1
        );

        CambiarVariante(indiceFinal);

        if (miGeneracion == generacionModelos)
            rutinaModelos = null;
    }

    public bool SecuenciaColorEnCurso()
    {
        return rutinaColor != null;
    }

    public void ReproducirSecuenciaColorTema()
    {
        if (rutinaColor != null)
        {
            StopCoroutine(rutinaColor);
        }

        int miGeneracion = ++generacionColor;

        if (!isActiveAndEnabled)
        {
            RestaurarColoresOriginalesInmediato();
            rutinaColor = null;
            return;
        }

        rutinaColor = StartCoroutine(SecuenciaColorTema(miGeneracion));
    }

    private IEnumerator SecuenciaColorTema(int miGeneracion)
    {
        yield return CambiarColorSuave(colorTema1);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoEntreColores));

        yield return CambiarColorSuave(colorTema2);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoAntesDeRestaurar));

        yield return RestaurarColorOriginalSuave();

        if (miGeneracion == generacionColor)
            rutinaColor = null;
    }

    public void CambiarColor(Color nuevoColor)
    {
        if (rutinaColor != null)
        {
            StopCoroutine(rutinaColor);
        }

        int miGeneracion = ++generacionColor;

        if (!isActiveAndEnabled)
        {
            AplicarColorActualInmediato(nuevoColor);
            rutinaColor = null;
            return;
        }

        rutinaColor = StartCoroutine(CambiarColorOperacion(nuevoColor, miGeneracion));
    }

    private IEnumerator CambiarColorOperacion(Color colorDestino, int miGeneracion)
    {
        yield return CambiarColorSuave(colorDestino);

        if (miGeneracion == generacionColor)
            rutinaColor = null;
    }

    private IEnumerator CambiarColorSuave(Color colorDestino)
    {
        Renderer[] renderers = ObtenerRenderersActuales();

        if (renderers == null || renderers.Length == 0)
        {
            yield break;
        }

        List<Material> materiales = ObtenerMateriales(renderers);
        List<Color> coloresInicio = new List<Color>();

        for (int i = 0; i < materiales.Count; i++)
        {
            coloresInicio.Add(ObtenerColorMaterial(materiales[i]));
        }

        float tiempo = 0f;

        while (tiempo < duracionCambioColor)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionCambioColor);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < materiales.Count; i++)
            {
                Color color = Color.Lerp(coloresInicio[i], colorDestino, smooth);
                AsignarColorMaterial(materiales[i], color);
            }

            yield return null;
        }

        for (int i = 0; i < materiales.Count; i++)
        {
            AsignarColorMaterial(materiales[i], colorDestino);
        }
    }

    public void RestaurarColorOriginal()
    {
        if (rutinaColor != null)
        {
            StopCoroutine(rutinaColor);
        }

        int miGeneracion = ++generacionColor;

        if (!isActiveAndEnabled)
        {
            RestaurarColoresOriginalesInmediato();
            rutinaColor = null;
            return;
        }

        rutinaColor = StartCoroutine(RestaurarColorOperacion(miGeneracion));
    }

    private IEnumerator RestaurarColorOperacion(int miGeneracion)
    {
        yield return RestaurarColorOriginalSuave();

        if (miGeneracion == generacionColor)
            rutinaColor = null;
    }

    private IEnumerator RestaurarColorOriginalSuave()
    {
        Renderer[] renderers = ObtenerRenderersActuales();

        if (renderers == null || renderers.Length == 0)
        {
            yield break;
        }

        List<Material> materiales = ObtenerMateriales(renderers);
        List<Color> coloresInicio = new List<Color>();
        List<Color> coloresDestino = new List<Color>();

        for (int i = 0; i < materiales.Count; i++)
        {
            Material material = materiales[i];

            coloresInicio.Add(ObtenerColorMaterial(material));

            if (coloresOriginales.ContainsKey(material))
            {
                coloresDestino.Add(coloresOriginales[material]);
            }
            else
            {
                coloresDestino.Add(Color.white);
            }
        }

        float tiempo = 0f;

        while (tiempo < duracionCambioColor)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionCambioColor);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < materiales.Count; i++)
            {
                Color color = Color.Lerp(coloresInicio[i], coloresDestino[i], smooth);
                AsignarColorMaterial(materiales[i], color);
            }

            yield return null;
        }

        for (int i = 0; i < materiales.Count; i++)
        {
            AsignarColorMaterial(materiales[i], coloresDestino[i]);
        }
    }

    private Renderer[] ObtenerRenderersActuales()
    {
        VariantePuerta variante = ObtenerVarianteActual();

        if (variante == null || variante.root == null)
        {
            return null;
        }

        if (variante.renderersParaColor == null || variante.renderersParaColor.Length == 0)
        {
            variante.renderersParaColor = variante.root.GetComponentsInChildren<Renderer>(true);
        }

        return variante.renderersParaColor;
    }

    private List<Material> ObtenerMateriales(Renderer[] renderers)
    {
        List<Material> materiales = new List<Material>();

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer renderer = renderers[r];

            if (renderer == null)
            {
                continue;
            }

            Material[] mats = renderer.materials;

            for (int m = 0; m < mats.Length; m++)
            {
                Material material = mats[m];

                if (material != null && !materiales.Contains(material))
                {
                    materiales.Add(material);

                    if (!coloresOriginales.ContainsKey(material))
                    {
                        coloresOriginales.Add(material, ObtenerColorMaterial(material));
                    }
                }
            }
        }

        return materiales;
    }

    private void AplicarColorActualInmediato(Color color)
    {
        Renderer[] renderers = ObtenerRenderersActuales();

        if (renderers == null)
            return;

        List<Material> materiales = ObtenerMateriales(renderers);

        for (int i = 0; i < materiales.Count; i++)
            AsignarColorMaterial(materiales[i], color);
    }

    private void RestaurarColoresOriginalesInmediato()
    {
        Renderer[] renderers = ObtenerRenderersActuales();

        if (renderers == null)
            return;

        List<Material> materiales = ObtenerMateriales(renderers);

        for (int i = 0; i < materiales.Count; i++)
        {
            Material material = materiales[i];
            Color color = coloresOriginales.TryGetValue(material, out Color original)
                ? original
                : Color.white;

            AsignarColorMaterial(material, color);
        }
    }

    private Color ObtenerColorMaterial(Material material)
    {
        if (material == null)
        {
            return Color.white;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }

    private void AsignarColorMaterial(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    [ContextMenu("Abrir puerta")]
    public void AbrirPuerta()
    {
        Door door = ObtenerDoorActual();

        if (door == null)
        {
            Debug.LogWarning("La variante actual no tiene DoorScript.Door asignado.");
            return;
        }

        EstablecerEstadoPuertaSeguro(door, true);

        if (mostrarDebug)
        {
            Debug.Log("Puerta abierta.");
        }
    }

    [ContextMenu("Cerrar puerta")]
    public void CerrarPuerta()
    {
        Door door = ObtenerDoorActual();

        if (door == null)
        {
            Debug.LogWarning("La variante actual no tiene DoorScript.Door asignado.");
            return;
        }

        EstablecerEstadoPuertaSeguro(door, false);

        if (mostrarDebug)
        {
            Debug.Log("Puerta cerrada.");
        }
    }

    public void CerrarPuertaInstantaneo()
    {
        Door door = ObtenerDoorActual();

        if (door == null)
        {
            return;
        }

        door.open = false;
        door.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    private static void EstablecerEstadoPuertaSeguro(Door door, bool abrir)
    {
        if (door == null || door.open == abrir)
        {
            return;
        }

        // Las puertas se instancian y se configuran en el mismo frame. En ese punto
        // Start() del script del paquete todavía no asignó el AudioSource, por lo que
        // llamar a OpenDoor() producía NullReferenceException. Cambiamos el estado
        // directamente y reproducimos audio solo cuando todos los datos existen.
        door.open = abrir;

        if (door.asource == null)
        {
            door.asource = door.GetComponent<AudioSource>();
        }

        AudioClip clip = abrir ? door.openDoor : door.closeDoor;
        if (door.asource != null && clip != null)
        {
            door.asource.clip = clip;
            door.asource.Play();
        }
    }

    public bool EstaAbierta()
    {
        Door door = ObtenerDoorActual();

        if (door == null)
        {
            return false;
        }

        return door.open;
    }

    private Door ObtenerDoorActual()
    {
        VariantePuerta variante = ObtenerVarianteActual();

        if (variante == null)
        {
            return null;
        }

        if (variante.doorScript == null && variante.root != null)
        {
            variante.doorScript = variante.root.GetComponentInChildren<Door>(true);
        }

        return variante.doorScript;
    }

    private VariantePuerta ObtenerVarianteActual()
    {
        if (indiceActual < 0 || variantes == null || indiceActual >= variantes.Count)
        {
            return null;
        }

        return variantes[indiceActual];
    }
}
