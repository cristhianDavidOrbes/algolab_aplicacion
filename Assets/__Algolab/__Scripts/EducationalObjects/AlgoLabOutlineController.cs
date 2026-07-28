using UnityEngine;

public class AlgoLabOutlineController : MonoBehaviour
{
    [Header("Material de contorno")]
    public Material outlineMaterial;

    [Header("Prueba")]
    public bool mostrarAlIniciar = false;

    private GameObject outlineObject;
    private MeshRenderer outlineRenderer;
    private float outlineSizeOverride = -1f;

    private void Awake()
    {
        CrearOutline();
        SetOutline(mostrarAlIniciar);
    }

    private void CrearOutline()
    {
        if (outlineObject != null)
        {
            return;
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null)
        {
            Debug.LogWarning("No se pudo crear outline porque falta MeshFilter o MeshRenderer en: " + name);
            return;
        }

        outlineObject = new GameObject("Outline_" + name);
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
        outlineMeshFilter.sharedMesh = meshFilter.sharedMesh;

        outlineRenderer = outlineObject.AddComponent<MeshRenderer>();

        if (outlineMaterial != null)
        {
            outlineRenderer.sharedMaterial = outlineMaterial;
        }
        else
        {
            Debug.LogWarning("No asignaste material de contorno en: " + name);
        }

        AplicarGrosorPersonalizado();
        outlineObject.SetActive(false);
    }

    public void Configurar(Material material, bool activo)
    {
        Configurar(material, activo, -1f);
    }

    public void Configurar(
        Material material,
        bool activo,
        float outlineSize)
    {
        outlineMaterial = material;
        outlineSizeOverride = outlineSize;
        CrearOutline();

        if (outlineObject != null)
        {
            if (outlineRenderer == null)
            {
                outlineRenderer = outlineObject.GetComponent<MeshRenderer>();
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.sharedMaterial = outlineMaterial;
                AplicarGrosorPersonalizado();
            }
        }

        SetOutline(activo);
    }

    private void AplicarGrosorPersonalizado()
    {
        if (outlineRenderer == null || outlineSizeOverride <= 0f)
        {
            return;
        }

        MaterialPropertyBlock propiedades = new MaterialPropertyBlock();
        outlineRenderer.GetPropertyBlock(propiedades);
        propiedades.SetFloat("_OutlineSize", outlineSizeOverride);
        outlineRenderer.SetPropertyBlock(propiedades);
    }

    public void SetOutline(bool activo)
    {
        if (outlineObject != null)
        {
            outlineObject.SetActive(activo);
        }
    }
}
