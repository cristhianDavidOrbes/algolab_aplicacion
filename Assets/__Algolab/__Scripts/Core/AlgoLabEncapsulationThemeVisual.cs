using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Contenido visual y narrado de los diez audios de Encapsulamiento.
/// El prefab que contiene este componente se instancia mediante
/// AlgoLabManualPanelSpawnManager para conservar la calibracion de postura.
/// </summary>
public class AlgoLabEncapsulationThemeVisual : MonoBehaviour
{
    [Header("Contenido")]
    public GameObject pillarModelPrefab;
    public Material[] pillarIconMaterials = new Material[4];
    public Material[] accessIconMaterials = new Material[3];
    public AudioClip audioIntroCuatroPilares;
    public AudioClip audioQueEsEncapsulamiento;
    public AudioClip audioTiposDeAcceso;
    public AlgoLabEncapsulationBankExampleVisual bankExampleVisual;

    [Header("Distribucion")]
    public float layoutCenterX = 0.15f;
    public float layoutDepth = 0.35f;
    public float pillarSpacing = 0.28f;
    public float pillarHeight = 0.32f;
    public float pillarBaseY = -0.07f;
    public float pillarIconSize = 0.18f;
    public float pillarIconGap = 0.06f;
    public float selectedForwardOffset = 0.02f;
    public float selectedRaise = 0.03f;
    public float selectedScale = 1.08f;
    public float accessSpacing = 0.30f;
    public float accessRowY = 0.11f;
    public float accessForwardOffset = -0.18f;
    public float accessIconSize = 0.20f;
    public float accessSelectedScale = 1.14f;

    [Header("Animacion")]
    public float appearDuration = 0.42f;
    public float focusDuration = 0.30f;
    public float disappearDuration = 0.34f;

    [Header("Audio")]
    [Range(0f, 1f)] public float narrationVolume = 1f;

    [Header("Eventos")]
    public UnityEvent OnSequenceFinished = new UnityEvent();

    [Header("Debug")]
    public bool showDebug;

    private readonly List<Transform> pillarAnchors = new List<Transform>();
    private readonly List<Vector3> pillarHomePositions = new List<Vector3>();
    private readonly List<Vector3> pillarHomeScales = new List<Vector3>();
    private readonly List<Transform> accessAnchors = new List<Transform>();
    private readonly List<Vector3> accessHomeScales = new List<Vector3>();

    private AudioSource narrationSource;
    private Transform generatedRoot;
    private Coroutine sequenceRoutine;
    private bool sequenceFinished;
    private int focusedPillarIndex = -1;

    public bool IsPlaying => sequenceRoutine != null;
    public bool IsFinished => sequenceFinished;

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void OnDisable()
    {
        StopSequence();
    }

    [ContextMenu("Reproducir audios 1 a 10")]
    public void PlaySequence()
    {
        StopSequence();
        EnsureVisuals();
        ResetVisualsInstantly();

        sequenceFinished = false;
        sequenceRoutine = StartCoroutine(SequenceCoroutine());
    }

    public void StopSequence()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (narrationSource != null)
        {
            narrationSource.Stop();
            narrationSource.clip = null;
        }
    }

    private IEnumerator SequenceCoroutine()
    {
        yield return AnimatePillarAppearance();

        // Audio 1: marcas obtenidas del audio real con timestamps por palabra.
        StartNarration(audioIntroCuatroPilares);
        yield return WaitForNarrationTime(8.78f);
        yield return AnimatePillarFocus(0, false);
        yield return WaitForNarrationTime(10.42f);
        yield return AnimatePillarFocus(1, false);
        yield return WaitForNarrationTime(11.70f);
        yield return AnimatePillarFocus(2, false);
        yield return WaitForNarrationTime(12.50f);
        yield return AnimatePillarFocus(3, false);
        yield return WaitForNarrationTime(13.96f);
        yield return AnimatePillarFocus(0, true);
        yield return WaitForNarrationEnd();
        yield return HideNonEncapsulationPillars();

        // Audio 2: el pilar de Encapsulamiento permanece al frente.
        StartNarration(audioQueEsEncapsulamiento);
        yield return WaitForNarrationEnd();

        // Audio 3: los tres niveles aparecen delante del pilar y se resaltan al nombrarlos.
        yield return AnimateAccessAppearance();
        StartNarration(audioTiposDeAcceso);
        yield return WaitForNarrationTime(3.94f);
        yield return AnimateAccessFocus(0);
        yield return WaitForNarrationTime(8.36f);
        yield return AnimateAccessFocus(1);
        yield return WaitForNarrationTime(13.34f);
        yield return AnimateAccessFocus(2);
        yield return WaitForNarrationEnd();
        yield return HideAccessIcons();

        // Audios 4 a 10: ejemplo completo con las clases Usuario y CuentaBancaria.
        yield return HideAllPillars();
        if (bankExampleVisual != null)
        {
            yield return bankExampleVisual.PlaySequence(narrationSource, narrationVolume);
        }
        else
        {
            Debug.LogError("ENCAPSULAMIENTO: falta el visual del ejemplo bancario (audios 4 a 10).");
        }

        sequenceRoutine = null;
        sequenceFinished = true;

        if (showDebug)
        {
            Debug.Log("ENCAPSULAMIENTO: secuencia de audios 1 a 10 terminada.");
        }

        OnSequenceFinished.Invoke();
    }

    private void EnsureAudioSource()
    {
        narrationSource = GetComponent<AudioSource>();
        if (narrationSource == null)
        {
            narrationSource = gameObject.AddComponent<AudioSource>();
        }

        narrationSource.playOnAwake = false;
        narrationSource.loop = false;
        narrationSource.spatialBlend = 0f;
        narrationSource.volume = narrationVolume;
    }

    private void EnsureVisuals()
    {
        if (generatedRoot != null)
        {
            return;
        }

        Transform authoredRoot = transform.Find("VisualesEncapsulamiento_Audios01_03");
        if (authoredRoot != null)
        {
            generatedRoot = authoredRoot;
            if (BindAuthoredVisuals())
            {
                PrepareBankExampleVisual();
                return;
            }

            Debug.LogWarning(
                "ENCAPSULAMIENTO: la jerarquia visual editable estaba incompleta y sera reconstruida.",
                this
            );
            DestroyHierarchy(authoredRoot.gameObject);
            generatedRoot = null;
        }

        GameObject rootObject = new GameObject("VisualesEncapsulamiento_Audios01_03");
        generatedRoot = rootObject.transform;
        generatedRoot.SetParent(transform, false);

        CreatePillars();
        CreateAccessIcons();
        PrepareBankExampleVisual();
    }

    [ContextMenu("Preparar jerarquia visual editable")]
    public void PrepareEditableHierarchy()
    {
        EnsureVisuals();
    }

    [ContextMenu("Mostrar vista previa editable completa")]
    public void ShowEditablePreview()
    {
        EnsureVisuals();
        focusedPillarIndex = -1;
        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localPosition = pillarHomePositions[i];
            pillarAnchors[i].localScale = pillarHomeScales[i];
        }
        for (int i = 0; i < accessAnchors.Count; i++)
        {
            accessAnchors[i].localScale = accessHomeScales[i];
        }
        if (bankExampleVisual != null)
        {
            bankExampleVisual.ShowEditablePreview();
        }
    }

    private void PrepareBankExampleVisual()
    {
        if (bankExampleVisual == null)
        {
            bankExampleVisual = GetComponent<AlgoLabEncapsulationBankExampleVisual>();
        }
        if (bankExampleVisual != null)
        {
            bankExampleVisual.PrepareVisuals();
        }
    }

    private bool BindAuthoredVisuals()
    {
        pillarAnchors.Clear();
        pillarHomePositions.Clear();
        pillarHomeScales.Clear();
        accessAnchors.Clear();
        accessHomeScales.Clear();

        for (int i = 0; i < 4; i++)
        {
            Transform anchor = generatedRoot.Find("Pilar_" + (i + 1));
            if (anchor == null)
            {
                return false;
            }

            pillarAnchors.Add(anchor);
            pillarHomePositions.Add(anchor.localPosition);
            pillarHomeScales.Add(Vector3.one);
        }

        for (int i = 0; i < 3; i++)
        {
            Transform anchor = generatedRoot.Find("Acceso_" + (i + 1));
            if (anchor == null)
            {
                return false;
            }

            accessAnchors.Add(anchor);
            accessHomeScales.Add(Vector3.one);
        }

        return true;
    }

    private static void DestroyHierarchy(GameObject hierarchy)
    {
        if (hierarchy == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(hierarchy);
        }
        else
        {
            DestroyImmediate(hierarchy);
        }
    }

    private void CreatePillars()
    {
        pillarAnchors.Clear();
        pillarHomePositions.Clear();
        pillarHomeScales.Clear();

        for (int i = 0; i < 4; i++)
        {
            GameObject anchorObject = new GameObject("Pilar_" + (i + 1));
            Transform anchor = anchorObject.transform;
            anchor.SetParent(generatedRoot, false);

            float x = layoutCenterX + (i - 1.5f) * pillarSpacing;
            Vector3 homePosition = new Vector3(x, pillarBaseY, layoutDepth);
            anchor.localPosition = homePosition;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;

            if (pillarModelPrefab != null)
            {
                GameObject model = Instantiate(pillarModelPrefab, anchor);
                model.name = "Columna";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                EnsureVerticalLongAxis(model.transform, anchor);
                NormalizeModelHeight(model.transform, anchor, pillarHeight);
            }

            Material iconMaterial = GetMaterial(pillarIconMaterials, i);
            CreateIconQuad(
                "IconoPilar_" + (i + 1),
                anchor,
                iconMaterial,
                new Vector3(0f, pillarHeight + pillarIconGap + pillarIconSize * 0.5f, -0.015f),
                pillarIconSize
            );

            pillarAnchors.Add(anchor);
            pillarHomePositions.Add(homePosition);
            pillarHomeScales.Add(Vector3.one);
        }
    }

    private void CreateAccessIcons()
    {
        accessAnchors.Clear();
        accessHomeScales.Clear();

        for (int i = 0; i < 3; i++)
        {
            GameObject anchorObject = new GameObject("Acceso_" + (i + 1));
            Transform anchor = anchorObject.transform;
            anchor.SetParent(generatedRoot, false);
            anchor.localPosition = new Vector3(
                layoutCenterX + (i - 1f) * accessSpacing,
                accessRowY,
                accessForwardOffset
            );
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.zero;

            CreateIconQuad(
                "IconoAcceso_" + (i + 1),
                anchor,
                GetMaterial(accessIconMaterials, i),
                Vector3.zero,
                accessIconSize
            );

            accessAnchors.Add(anchor);
            accessHomeScales.Add(Vector3.one);
        }
    }

    private static Material GetMaterial(Material[] materials, int index)
    {
        return materials != null && index >= 0 && index < materials.Length
            ? materials[index]
            : null;
    }

    private static void CreateIconQuad(
        string objectName,
        Transform parent,
        Material material,
        Vector3 localPosition,
        float size)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = objectName;
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = Vector3.one * size;

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void NormalizeModelHeight(Transform model, Transform relativeTo, float desiredHeight)
    {
        Bounds bounds;
        if (!TryGetLocalRendererBounds(model, relativeTo, out bounds) || bounds.size.y < 0.0001f)
        {
            return;
        }

        float scale = desiredHeight / bounds.size.y;
        model.localScale *= scale;

        if (!TryGetLocalRendererBounds(model, relativeTo, out bounds))
        {
            return;
        }

        Vector3 position = model.localPosition;
        position.y -= bounds.min.y;
        model.localPosition = position;
    }

    private static void EnsureVerticalLongAxis(Transform model, Transform relativeTo)
    {
        Bounds bounds;
        if (!TryGetLocalRendererBounds(model, relativeTo, out bounds))
        {
            return;
        }

        // El eje mas largo del modelo debe ser Y. Esto evita que un FBX con otra
        // convencion de ejes aparezca acostado como una plataforma frente al usuario.
        if (bounds.size.x > bounds.size.y && bounds.size.x >= bounds.size.z)
        {
            model.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        else if (bounds.size.z > bounds.size.y && bounds.size.z > bounds.size.x)
        {
            model.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }
    }

    private static bool TryGetLocalRendererBounds(
        Transform visualRoot,
        Transform relativeTo,
        out Bounds localBounds)
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        localBounds = new Bounds();
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Bounds worldBounds = renderer.bounds;
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 localCorner = relativeTo.InverseTransformPoint(worldCorner);

                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return hasBounds;
    }

    private void ResetVisualsInstantly()
    {
        focusedPillarIndex = -1;

        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localPosition = pillarHomePositions[i];
            pillarAnchors[i].localScale = Vector3.zero;
        }

        for (int i = 0; i < accessAnchors.Count; i++)
        {
            accessAnchors[i].localScale = Vector3.zero;
        }

        if (bankExampleVisual != null)
        {
            bankExampleVisual.ResetVisualsInstantly();
        }
    }

    private IEnumerator AnimatePillarAppearance()
    {
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(elapsed / Mathf.Max(0.01f, appearDuration));
            for (int i = 0; i < pillarAnchors.Count; i++)
            {
                pillarAnchors[i].localScale = Vector3.LerpUnclamped(Vector3.zero, pillarHomeScales[i], smooth);
            }
            yield return null;
        }

        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localScale = pillarHomeScales[i];
        }
    }

    private IEnumerator AnimatePillarFocus(int selectedIndex, bool keepEncapsulationCentered)
    {
        if (focusedPillarIndex >= 0 && focusedPillarIndex != selectedIndex)
        {
            yield return AnimatePillarsHome();
        }

        Vector3[] startPositions = new Vector3[pillarAnchors.Count];
        Vector3[] startScales = new Vector3[pillarAnchors.Count];
        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            startPositions[i] = pillarAnchors[i].localPosition;
            startScales[i] = pillarAnchors[i].localScale;
        }

        float elapsed = 0f;
        while (elapsed < focusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(elapsed / Mathf.Max(0.01f, focusDuration));

            for (int i = 0; i < pillarAnchors.Count; i++)
            {
                bool selected = i == selectedIndex;
                Vector3 targetPosition = selected
                    ? new Vector3(layoutCenterX, pillarBaseY + selectedRaise, selectedForwardOffset)
                    : GetDistributedBackgroundPosition(i, selectedIndex);
                Vector3 targetScale = selected
                    ? pillarHomeScales[i] * selectedScale
                    : pillarHomeScales[i];

                pillarAnchors[i].localPosition = Vector3.LerpUnclamped(startPositions[i], targetPosition, smooth);
                pillarAnchors[i].localScale = Vector3.LerpUnclamped(startScales[i], targetScale, smooth);
            }
            yield return null;
        }

        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            bool selected = i == selectedIndex;
            pillarAnchors[i].localPosition = selected
                ? new Vector3(layoutCenterX, pillarBaseY + selectedRaise, selectedForwardOffset)
                : GetDistributedBackgroundPosition(i, selectedIndex);
            pillarAnchors[i].localScale = selected
                ? pillarHomeScales[i] * selectedScale
                : pillarHomeScales[i];
        }

        focusedPillarIndex = selectedIndex;

        // El parametro documenta la transicion final y evita que futuras extensiones
        // devuelvan Encapsulamiento a la fila antes de los audios 2 y 3.
        if (keepEncapsulationCentered && selectedIndex == 0)
        {
            pillarAnchors[0].localPosition = new Vector3(
                layoutCenterX,
                pillarBaseY + selectedRaise,
                selectedForwardOffset
            );
            pillarAnchors[0].localScale = pillarHomeScales[0] * selectedScale;
        }
    }

    private IEnumerator AnimatePillarsHome()
    {
        Vector3[] startPositions = new Vector3[pillarAnchors.Count];
        Vector3[] startScales = new Vector3[pillarAnchors.Count];
        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            startPositions[i] = pillarAnchors[i].localPosition;
            startScales[i] = pillarAnchors[i].localScale;
        }

        float duration = Mathf.Max(0.01f, focusDuration * 0.65f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(elapsed / duration);
            for (int i = 0; i < pillarAnchors.Count; i++)
            {
                pillarAnchors[i].localPosition = Vector3.LerpUnclamped(
                    startPositions[i],
                    pillarHomePositions[i],
                    smooth
                );
                pillarAnchors[i].localScale = Vector3.LerpUnclamped(
                    startScales[i],
                    pillarHomeScales[i],
                    smooth
                );
            }
            yield return null;
        }

        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localPosition = pillarHomePositions[i];
            pillarAnchors[i].localScale = pillarHomeScales[i];
        }

        focusedPillarIndex = -1;
    }

    private Vector3 GetDistributedBackgroundPosition(int pillarIndex, int selectedIndex)
    {
        int backgroundOrder = 0;
        for (int i = 0; i < pillarIndex; i++)
        {
            if (i != selectedIndex)
            {
                backgroundOrder++;
            }
        }

        float leftEdge = layoutCenterX - 1.5f * pillarSpacing;
        float rightEdge = layoutCenterX + 1.5f * pillarSpacing;
        float t = backgroundOrder / 2f;
        return new Vector3(Mathf.Lerp(leftEdge, rightEdge, t), pillarBaseY, layoutDepth);
    }

    private IEnumerator HideNonEncapsulationPillars()
    {
        Vector3[] starts = new Vector3[pillarAnchors.Count];
        for (int i = 1; i < pillarAnchors.Count; i++)
        {
            starts[i] = pillarAnchors[i].localScale;
        }

        float elapsed = 0f;
        while (elapsed < disappearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(elapsed / Mathf.Max(0.01f, disappearDuration));
            for (int i = 1; i < pillarAnchors.Count; i++)
            {
                pillarAnchors[i].localScale = Vector3.LerpUnclamped(starts[i], Vector3.zero, smooth);
            }
            yield return null;
        }

        for (int i = 1; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localScale = Vector3.zero;
        }
    }

    private IEnumerator HideAllPillars()
    {
        Vector3[] starts = new Vector3[pillarAnchors.Count];
        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            starts[i] = pillarAnchors[i].localScale;
        }

        float elapsed = 0f;
        while (elapsed < disappearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(elapsed / Mathf.Max(0.01f, disappearDuration));
            for (int i = 0; i < pillarAnchors.Count; i++)
            {
                pillarAnchors[i].localScale = Vector3.LerpUnclamped(starts[i], Vector3.zero, smooth);
            }
            yield return null;
        }

        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localScale = Vector3.zero;
        }
    }

    private IEnumerator AnimateAccessAppearance()
    {
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(elapsed / Mathf.Max(0.01f, appearDuration));
            for (int i = 0; i < accessAnchors.Count; i++)
            {
                accessAnchors[i].localScale = Vector3.LerpUnclamped(Vector3.zero, accessHomeScales[i], smooth);
            }
            yield return null;
        }
    }

    private IEnumerator AnimateAccessFocus(int selectedIndex)
    {
        Vector3[] starts = new Vector3[accessAnchors.Count];
        for (int i = 0; i < accessAnchors.Count; i++)
        {
            starts[i] = accessAnchors[i].localScale;
        }

        float elapsed = 0f;
        while (elapsed < focusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(elapsed / Mathf.Max(0.01f, focusDuration));
            for (int i = 0; i < accessAnchors.Count; i++)
            {
                Vector3 target = accessHomeScales[i] * (i == selectedIndex ? accessSelectedScale : 1f);
                accessAnchors[i].localScale = Vector3.LerpUnclamped(starts[i], target, smooth);
            }
            yield return null;
        }
    }

    private IEnumerator HideAccessIcons()
    {
        Vector3[] starts = new Vector3[accessAnchors.Count];
        for (int i = 0; i < accessAnchors.Count; i++)
        {
            starts[i] = accessAnchors[i].localScale;
        }

        float elapsed = 0f;
        while (elapsed < disappearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(elapsed / Mathf.Max(0.01f, disappearDuration));
            for (int i = 0; i < accessAnchors.Count; i++)
            {
                accessAnchors[i].localScale = Vector3.LerpUnclamped(starts[i], Vector3.zero, smooth);
            }
            yield return null;
        }

        for (int i = 0; i < accessAnchors.Count; i++)
        {
            accessAnchors[i].localScale = Vector3.zero;
        }
    }

    private void StartNarration(AudioClip clip)
    {
        EnsureAudioSource();
        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.volume = narrationVolume;

        if (clip == null)
        {
            Debug.LogError("ENCAPSULAMIENTO: falta un audio en la secuencia 1 a 10.");
            return;
        }

        narrationSource.Play();
    }

    private IEnumerator WaitForNarrationTime(float targetTime)
    {
        while (narrationSource != null && narrationSource.isPlaying && narrationSource.time < targetTime)
        {
            yield return null;
        }
    }

    private IEnumerator WaitForNarrationEnd()
    {
        while (narrationSource != null && narrationSource.isPlaying)
        {
            yield return null;
        }
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
