using System.Collections.Generic;
using UnityEngine;

public class AlgoLabLevel02VehicleBodyController : MonoBehaviour
{
    [System.Serializable]
    public class CarcasaVehiculo
    {
        [Header("Identificación")]
        public string nombreCarcasa;

        [Header("Objeto visual")]
        public GameObject root;

        [Header("Color opcional")]
        [Tooltip("Si dejas vacío esto, se pintarán todos los renderers de esta carcasa.")]
        public Renderer[] renderersParaColor;

        [Header("Transform opcional")]
        public bool usarEscalaPersonalizada = false;
        public Vector3 escalaLocal = Vector3.one;
    }

    [Header("Carcasas disponibles")]
    public List<CarcasaVehiculo> carcasas = new List<CarcasaVehiculo>();

    [Header("Inicio")]
    public int indiceCarcasaInicial = 0;

    [Header("Collider automático")]
    public bool ajustarBoxColliderAutomatico = true;
    public Vector3 margenCollider = new Vector3(0.15f, 0.1f, 0.15f);

    [Header("Debug")]
    public bool mostrarDebug = true;

    private int indiceActual = -1;
    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        MostrarCarcasa(indiceCarcasaInicial);
    }

    [ContextMenu("Auto llenar carcasas desde hijos")]
    public void AutoLlenarCarcasasDesdeHijos()
    {
        carcasas.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform hijo = transform.GetChild(i);

            if (hijo == null)
            {
                continue;
            }

            CarcasaVehiculo nueva = new CarcasaVehiculo();
            nueva.nombreCarcasa = hijo.name;
            nueva.root = hijo.gameObject;
            nueva.escalaLocal = hijo.localScale;

            carcasas.Add(nueva);
        }

        if (mostrarDebug)
        {
            Debug.Log("Carcasas cargadas desde hijos: " + carcasas.Count);
        }
    }

    public void MostrarCarcasa(int indice)
    {
        if (carcasas == null || carcasas.Count == 0)
        {
            Debug.LogWarning("No hay carcasas configuradas.");
            return;
        }

        indice = Mathf.Clamp(indice, 0, carcasas.Count - 1);
        indiceActual = indice;

        for (int i = 0; i < carcasas.Count; i++)
        {
            if (carcasas[i] == null || carcasas[i].root == null)
            {
                continue;
            }

            bool activa = i == indice;
            carcasas[i].root.SetActive(activa);

            if (activa)
            {
                carcasas[i].root.transform.localPosition = Vector3.zero;
                carcasas[i].root.transform.localRotation = Quaternion.identity;

                if (carcasas[i].usarEscalaPersonalizada)
                {
                    carcasas[i].root.transform.localScale = carcasas[i].escalaLocal;
                }
            }
        }

        if (ajustarBoxColliderAutomatico)
        {
            AjustarColliderACarcasaActiva();
        }

        if (mostrarDebug)
        {
            Debug.Log("Carcasa activa: " + ObtenerNombreCarcasaActual());
        }
    }

    public void MostrarCarcasaPorNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }

        string nombreNormalizado = Normalizar(nombre);

        for (int i = 0; i < carcasas.Count; i++)
        {
            if (carcasas[i] == null)
            {
                continue;
            }

            if (Normalizar(carcasas[i].nombreCarcasa).Contains(nombreNormalizado) ||
                nombreNormalizado.Contains(Normalizar(carcasas[i].nombreCarcasa)))
            {
                MostrarCarcasa(i);
                return;
            }
        }

        Debug.LogWarning("No se encontró carcasa con nombre: " + nombre);
    }

    public void AplicarColor(Color color)
    {
        CarcasaVehiculo carcasa = ObtenerCarcasaActual();

        if (carcasa == null || carcasa.root == null)
        {
            return;
        }

        Renderer[] renderers;

        if (carcasa.renderersParaColor != null && carcasa.renderersParaColor.Length > 0)
        {
            renderers = carcasa.renderersParaColor;
        }
        else
        {
            renderers = carcasa.root.GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material[] materiales = renderers[i].materials;

            for (int j = 0; j < materiales.Length; j++)
            {
                if (materiales[j] != null)
                {
                    materiales[j].color = color;
                }
            }

            renderers[i].materials = materiales;
        }
    }

    public string ObtenerNombreCarcasaActual()
    {
        CarcasaVehiculo carcasa = ObtenerCarcasaActual();

        if (carcasa == null)
        {
            return "";
        }

        return carcasa.nombreCarcasa;
    }

    public int ObtenerIndiceCarcasaActual()
    {
        return indiceActual;
    }

    private CarcasaVehiculo ObtenerCarcasaActual()
    {
        if (carcasas == null || carcasas.Count == 0)
        {
            return null;
        }

        if (indiceActual < 0 || indiceActual >= carcasas.Count)
        {
            return null;
        }

        return carcasas[indiceActual];
    }

    [ContextMenu("Siguiente carcasa")]
    public void SiguienteCarcasa()
    {
        if (carcasas == null || carcasas.Count == 0)
        {
            return;
        }

        int siguiente = indiceActual + 1;

        if (siguiente >= carcasas.Count)
        {
            siguiente = 0;
        }

        MostrarCarcasa(siguiente);
    }

    [ContextMenu("Carcasa anterior")]
    public void CarcasaAnterior()
    {
        if (carcasas == null || carcasas.Count == 0)
        {
            return;
        }

        int anterior = indiceActual - 1;

        if (anterior < 0)
        {
            anterior = carcasas.Count - 1;
        }

        MostrarCarcasa(anterior);
    }

    [ContextMenu("Ajustar collider a carcasa activa")]
    public void AjustarColliderACarcasaActiva()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        CarcasaVehiculo carcasa = ObtenerCarcasaActual();

        if (carcasa == null || carcasa.root == null || boxCollider == null)
        {
            return;
        }

        Renderer[] renderers = carcasa.root.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        bool hayBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hayBounds)
            {
                bounds = renderers[i].bounds;
                hayBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (!hayBounds)
        {
            return;
        }

        Vector3 centroLocal = transform.InverseTransformPoint(bounds.center);

        Vector3 escala = transform.lossyScale;

        float escalaX = Mathf.Abs(escala.x) < 0.001f ? 1f : Mathf.Abs(escala.x);
        float escalaY = Mathf.Abs(escala.y) < 0.001f ? 1f : Mathf.Abs(escala.y);
        float escalaZ = Mathf.Abs(escala.z) < 0.001f ? 1f : Mathf.Abs(escala.z);

        Vector3 sizeLocal = new Vector3(
            bounds.size.x / escalaX,
            bounds.size.y / escalaY,
            bounds.size.z / escalaZ
        );

        boxCollider.center = centroLocal;
        boxCollider.size = sizeLocal + margenCollider;

        if (mostrarDebug)
        {
            Debug.Log("BoxCollider ajustado a carcasa: " + ObtenerNombreCarcasaActual());
        }
    }

    private string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        return texto.Trim().ToLower();
    }
}