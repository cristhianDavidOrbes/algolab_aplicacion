using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tema narrado del nivel 4. Los modelos aparecen en el espacio de trabajo,
/// pero los dos diagramas de clase se publican exclusivamente en el panel de
/// diagramas existente de AlgoLab.
/// </summary>
public class AlgoLabAbstractionThemeVisual : MonoBehaviour
{
    [Header("Modelos")]
    public GameObject pillarModelPrefab;
    public Material[] pillarIconMaterials = new Material[4];
    public GameObject vinylModelPrefab;
    public GameObject musicStoreModelPrefab;
    public GameObject phoneModelPrefab;
    public GameObject internalBoardModelPrefab;
    public Material musicStoreMaterialOverride;

    [Header("Contornos por contexto")]
    public Material outlineMaterialTemplate;
    [Range(0.002f, 0.04f)]
    public float objectOutlineRelativeThickness = 0.016f;
    public Color applicationOutlineColor =
        new Color(0.12f, 0.58f, 1f, 1f);
    public Color storeOutlineColor =
        new Color(0.12f, 0.86f, 0.34f, 1f);

    [Header("Narracion (audios 1 a 6 y cierre 8)")]
    public AudioClip[] narrationClips = new AudioClip[7];
    [Range(0f, 1f)] public float narrationVolume = 1f;

    [Header("Texto")]
    public TMP_FontAsset fontAsset;

    [Header("Distribucion de pilares reutilizada del nivel 3")]
    public float pillarSpacing = 0.28f;
    public float pillarHeight = 0.32f;
    public float pillarBaseY = -0.07f;
    public float pillarIconSize = 0.18f;
    public float pillarIconGap = 0.06f;
    public float pillarSelectedForwardOffset = 0.02f;
    public float pillarSelectedRaise = 0.03f;
    public float pillarSelectedScale = 1.08f;

    [Header("Distribucion del ejemplo")]
    public float centerX = 0.15f;
    public float baseY = 0.02f;
    public float depth = 0.34f;
    public float sideOffset = 0.47f;
    public Vector3 phoneModelEuler = new Vector3(0f, 180f, 0f);
    public Vector3 storeModelEuler = new Vector3(0f, 180f, 0f);
    public Vector3 vinylModelEuler = new Vector3(90f, 180f, 0f);
    public Vector3 boardModelEuler = Vector3.zero;

    [Header("Animacion")]
    public float appearDuration = 0.34f;
    public float transitionDuration = 0.28f;
    public float focusDuration = 0.22f;

    [Header("Eventos")]
    public UnityEvent OnSequenceFinished = new UnityEvent();

    [Header("Debug")]
    public bool showDebug;

    private AudioSource narrationSource;
    private Transform generatedRoot;
    private Transform pillarGroup;
    private Transform vinylGroup;
    private Transform storeGroup;
    private Transform phoneGroup;
    private Transform phoneShellAnchor;
    private Transform boardAnchor;
    private TextMeshPro phoneTitle;
    private Material applicationOutlineMaterial;
    private Material storeOutlineMaterial;

    private readonly List<Transform> pillarAnchors = new List<Transform>();
    private readonly List<Vector3> pillarHomePositions = new List<Vector3>();
    private readonly List<Vector3> pillarHomeScales = new List<Vector3>();

    private AlgoLabObjetoEducativo initialSongDiagramData;
    private AlgoLabObjetoEducativo storeDiagramData;
    private AlgoLabObjetoEducativo appDiagramData;
    private AlgoLabClassDiagramController diagramController;
    private AlgoLabClassDiagramModeManager diagramModeManager;
    private bool diagramSessionActive;
    private bool warnedMissingDiagramPanel;

    private Coroutine sequenceRoutine;
    private bool sequenceFinished;

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

    private void OnDestroy()
    {
        DestroyRuntimeMaterial(ref applicationOutlineMaterial);
        DestroyRuntimeMaterial(ref storeOutlineMaterial);
    }

    [ContextMenu("Reproducir tema de Abstraccion")]
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

        EndDiagramSession();
    }

    private IEnumerator SequenceCoroutine()
    {
        // Audio 1: reutiliza la fila de los cuatro pilares del nivel 3 y
        // adelanta Abstraccion, que es el segundo pilar.
        yield return AnimatePillarAppearance();
        StartNarration(GetClip(0));
        yield return WaitForNarrationTime(2.5f);
        yield return AnimatePillarFocus(1);
        yield return WaitForNarrationEnd();
        yield return HidePillars();

        // Audio 2: define que la abstraccion conserva solo lo necesario.
        // Al decir "Por ejemplo" prepara el vinilo para enlazar sin corte
        // con la frase "Imaginemos una cancion" del audio siguiente.
        StartNarration(GetClip(1));
        yield return WaitForNarrationTime(10.40f);
        ShowInitialSongDiagram();
        yield return ShowGroup(vinylGroup);
        yield return WaitForNarrationEnd();

        // Audio 3: la cancion permanece al centro. Tienda y aplicacion
        // aparecen exactamente cuando la narracion menciona cada contexto.
        StartNarration(GetClip(2));
        yield return WaitForNarrationTime(0.15f);
        yield return Pulse(vinylGroup, 1.06f);
        yield return WaitForNarrationTime(1.85f);
        yield return ShowGroup(storeGroup);
        yield return WaitForNarrationTime(4.45f);
        yield return ShowGroup(phoneGroup);
        yield return WaitForNarrationTime(7.30f);
        ShowBothDiagrams();
        yield return Pulse(storeGroup, 1.04f);
        yield return Pulse(phoneGroup, 1.04f);
        yield return WaitForNarrationEnd();

        // Audio 4: la vista de tienda se publica en el panel de diagramas
        // existente y conserva solo nombre, artista, precio y comprar.
        StartNarration(GetClip(3));
        yield return WaitForNarrationTime(0.20f);
        yield return Pulse(storeGroup, 1.06f);
        yield return WaitForNarrationTime(5.90f);
        yield return Pulse(storeGroup, 1.04f);
        yield return WaitForNarrationEnd();

        // Audio 5: el panel muestra las dos clases con el mismo nombre,
        // Cancion, pero con los datos y acciones propios de cada situacion.
        ShowBothDiagrams();
        StartNarration(GetClip(4));
        yield return WaitForNarrationTime(0.20f);
        yield return Pulse(phoneGroup, 1.06f);
        yield return WaitForNarrationTime(5.35f);
        yield return Pulse(phoneGroup, 1.04f);
        yield return WaitForNarrationTime(9.20f);
        yield return Pulse(vinylGroup, 1.04f);
        yield return WaitForNarrationEnd();

        // Audio 6: al hablar de complejidad interna, la carcasa del telefono
        // se sustituye por su placa. Cuando vuelve a las acciones generales,
        // reaparece el telefono y queda solo su diagrama.
        StartNarration(GetClip(5));
        yield return WaitForNarrationTime(3.55f);
        SetPhoneTitle("COMPLEJIDAD OCULTA");
        yield return CrossFadeScale(phoneShellAnchor, boardAnchor);
        yield return WaitForNarrationTime(6.55f);
        yield return Pulse(boardAnchor, 1.06f);
        yield return WaitForNarrationTime(11.15f);
        yield return CrossFadeScale(boardAnchor, phoneShellAnchor);
        SetPhoneTitle("APLICACION");
        ShowAppDiagram();
        yield return WaitForNarrationEnd();

        // Audio 8: cierre. Vuelven las dos interpretaciones y la placa
        // aparece brevemente al mencionar los sistemas complejos.
        ShowBothDiagrams();
        StartNarration(GetClip(6));
        yield return WaitForNarrationTime(0.20f);
        yield return Pulse(phoneGroup, 1.04f);
        yield return WaitForNarrationTime(5.30f);
        yield return Pulse(vinylGroup, 1.05f);
        yield return WaitForNarrationTime(9.30f);
        SetPhoneTitle("COMPLEJIDAD OCULTA");
        yield return CrossFadeScale(phoneShellAnchor, boardAnchor);
        yield return WaitForNarrationTime(12.75f);
        yield return Pulse(boardAnchor, 1.05f);
        yield return WaitForNarrationTime(13.45f);
        yield return CrossFadeScale(boardAnchor, phoneShellAnchor);
        SetPhoneTitle("APLICACION");
        yield return WaitForNarrationEnd();

        sequenceRoutine = null;
        sequenceFinished = true;
        if (showDebug)
        {
            Debug.Log(
                "ABSTRACCION: secuencia de audios 1 a 6 y cierre 8 terminada."
            );
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

        generatedRoot = new GameObject("VisualesAbstraccion_Audios01_08").transform;
        generatedRoot.SetParent(transform, false);

        CreatePillars();
        CreateVinylVisual();
        CreateStoreVisual();
        CreatePhoneVisual();
        CreateDiagramData();
    }

    private void CreatePillars()
    {
        pillarGroup = CreateGroup("01_CuatroPilares_ReutilizadosNivel3");
        pillarAnchors.Clear();
        pillarHomePositions.Clear();
        pillarHomeScales.Clear();

        for (int i = 0; i < 4; i++)
        {
            Transform anchor = CreateGroup("Pilar_" + (i + 1), pillarGroup);
            float x = centerX + (i - 1.5f) * pillarSpacing;
            Vector3 homePosition = new Vector3(x, pillarBaseY, depth);
            anchor.localPosition = homePosition;
            anchor.localRotation = Quaternion.identity;

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

            CreateIconQuad(
                "IconoPilar_" + (i + 1),
                anchor,
                new Vector3(
                    0f,
                    pillarHeight + pillarIconGap + pillarIconSize * 0.5f,
                    -0.015f
                ),
                pillarIconSize,
                GetMaterial(pillarIconMaterials, i)
            );

            pillarAnchors.Add(anchor);
            pillarHomePositions.Add(homePosition);
            pillarHomeScales.Add(Vector3.one);
        }
    }

    private void CreateVinylVisual()
    {
        vinylGroup = CreateGroup("02_Cancion_Centro");
        Transform modelAnchor = CreateGroup("Vinilo", vinylGroup);
        modelAnchor.localPosition = new Vector3(centerX, baseY + 0.34f, depth);
        modelAnchor.localRotation = Quaternion.Euler(vinylModelEuler);

        if (vinylModelPrefab != null)
        {
            GameObject vinyl = Instantiate(vinylModelPrefab, modelAnchor);
            vinyl.name = "DiscoVinilo";
            NormalizeModel(vinyl.transform, modelAnchor, 0.32f, Vector3.zero);
        }

        CreateLabel(
            "TituloCancion",
            vinylGroup,
            "CANCION",
            new Vector3(centerX, baseY + 0.62f, depth - 0.02f),
            new Vector2(0.44f, 0.11f),
            0.062f
        );
    }

    private void CreateStoreVisual()
    {
        float storeX = centerX - sideOffset;
        storeGroup = CreateGroup("03_Tienda_LadoIzquierdo");
        Transform modelAnchor = CreateGroup("Tienda", storeGroup);
        modelAnchor.localPosition = new Vector3(storeX, baseY + 0.32f, depth + 0.02f);
        modelAnchor.localRotation = Quaternion.Euler(storeModelEuler);

        if (musicStoreModelPrefab != null)
        {
            GameObject store = Instantiate(musicStoreModelPrefab, modelAnchor);
            store.name = "TiendaKayLousberg";
            ApplyMaterialToRenderers(store, musicStoreMaterialOverride);
            NormalizeModel(store.transform, modelAnchor, 0.38f, Vector3.zero);
            ApplyContextOutline(
                store,
                GetContextOutlineMaterial(
                    ref storeOutlineMaterial,
                    storeOutlineColor,
                    "Outline_Tienda_Verde"
                )
            );
        }

        CreateLabel(
            "TituloTienda",
            storeGroup,
            "TIENDA",
            new Vector3(storeX, baseY + 0.62f, depth - 0.02f),
            new Vector2(0.40f, 0.11f),
            0.058f
        );
    }

    private void CreatePhoneVisual()
    {
        float phoneX = centerX + sideOffset;
        phoneGroup = CreateGroup("04_Aplicacion_LadoDerecho");
        phoneShellAnchor = CreateGroup("Telefono", phoneGroup);
        phoneShellAnchor.localPosition = new Vector3(phoneX, baseY + 0.33f, depth);
        phoneShellAnchor.localRotation = Quaternion.Euler(phoneModelEuler);

        if (phoneModelPrefab != null)
        {
            GameObject phone = Instantiate(phoneModelPrefab, phoneShellAnchor);
            phone.name = "TelefonoQuaternius";
            NormalizeModel(phone.transform, phoneShellAnchor, 0.42f, Vector3.zero);
            ApplyContextOutline(
                phone,
                GetContextOutlineMaterial(
                    ref applicationOutlineMaterial,
                    applicationOutlineColor,
                    "Outline_Aplicacion_Azul"
                )
            );
        }

        boardAnchor = CreateGroup("PlacaInternaDentroDelTelefono", phoneGroup);
        boardAnchor.localPosition = phoneShellAnchor.localPosition;
        boardAnchor.localRotation = Quaternion.Euler(boardModelEuler);
        if (internalBoardModelPrefab != null)
        {
            GameObject board = Instantiate(internalBoardModelPrefab, boardAnchor);
            board.name = "PlacaInternaOptimizada";
            NormalizeModel(board.transform, boardAnchor, 0.38f, Vector3.zero);
            ApplyContextOutline(
                board,
                GetContextOutlineMaterial(
                    ref applicationOutlineMaterial,
                    applicationOutlineColor,
                    "Outline_Aplicacion_Azul"
                )
            );
        }

        phoneTitle = CreateLabel(
            "TituloAplicacion",
            phoneGroup,
            "APLICACION",
            new Vector3(phoneX, baseY + 0.64f, depth - 0.02f),
            new Vector2(0.52f, 0.11f),
            0.055f
        );
    }

    private void CreateDiagramData()
    {
        GameObject initialDataObject = new GameObject(
            "Diagrama_Abstraccion_CancionInicial"
        );
        initialDataObject.SetActive(false);
        initialDataObject.transform.SetParent(generatedRoot, false);
        initialSongDiagramData =
            initialDataObject.AddComponent<AlgoLabObjetoEducativo>();
        initialSongDiagramData.nombreObjeto = "Cancion";
        initialSongDiagramData.nombreClase = "Cancion";
        initialSongDiagramData.descripcionObjeto =
            "Concepto general de una cancion antes de elegir el problema.";
        initialSongDiagramData.atributos = new[]
        {
            "nombre",
            "artista",
            "precio",
            "duracion"
        };
        initialSongDiagramData.metodos = new[]
        {
            "comprar()",
            "reproducir()",
            "pausar()"
        };
        initialSongDiagramData.forzarVisibleEnDiagramaTema = true;

        GameObject storeDataObject = new GameObject("Diagrama_Abstraccion_CancionTienda");
        storeDataObject.SetActive(false);
        storeDataObject.transform.SetParent(generatedRoot, false);
        storeDiagramData = storeDataObject.AddComponent<AlgoLabObjetoEducativo>();
        storeDiagramData.nombreObjeto = "Cancion vendida en una tienda";
        storeDiagramData.nombreClase = "Cancion";
        storeDiagramData.descripcionObjeto =
            "Vista abstracta de una cancion necesaria para venderla.";
        storeDiagramData.atributos = new[]
        {
            "nombre",
            "artista",
            "precio"
        };
        storeDiagramData.metodos = new[]
        {
            "comprar()"
        };
        storeDiagramData.forzarVisibleEnDiagramaTema = true;

        GameObject appDataObject = new GameObject("Diagrama_Abstraccion_CancionAplicacion");
        appDataObject.SetActive(false);
        appDataObject.transform.SetParent(generatedRoot, false);
        appDiagramData = appDataObject.AddComponent<AlgoLabObjetoEducativo>();
        appDiagramData.nombreObjeto = "Cancion reproducida en una aplicacion";
        appDiagramData.nombreClase = "Cancion";
        appDiagramData.descripcionObjeto =
            "Vista abstracta de una cancion necesaria para escucharla.";
        appDiagramData.atributos = new[]
        {
            "nombre",
            "duracion"
        };
        appDiagramData.metodos = new[]
        {
            "reproducir()",
            "pausar()"
        };
        appDiagramData.forzarVisibleEnDiagramaTema = true;
    }

    private void ShowInitialSongDiagram()
    {
        BeginDiagramSession();
        if (initialSongDiagramData != null)
        {
            initialSongDiagramData.gameObject.SetActive(true);
        }
        if (storeDiagramData != null)
        {
            storeDiagramData.gameObject.SetActive(false);
        }
        if (appDiagramData != null)
        {
            appDiagramData.gameObject.SetActive(false);
        }
        RefreshDiagramPanel();
    }

    private void ShowBothDiagrams()
    {
        BeginDiagramSession();
        if (initialSongDiagramData != null)
        {
            initialSongDiagramData.gameObject.SetActive(false);
        }
        if (storeDiagramData != null)
        {
            storeDiagramData.gameObject.SetActive(true);
        }
        if (appDiagramData != null)
        {
            appDiagramData.gameObject.SetActive(true);
        }
        RefreshDiagramPanel();
    }

    private void ShowAppDiagram()
    {
        BeginDiagramSession();
        if (initialSongDiagramData != null)
        {
            initialSongDiagramData.gameObject.SetActive(false);
        }
        if (storeDiagramData != null)
        {
            storeDiagramData.gameObject.SetActive(false);
        }
        if (appDiagramData != null)
        {
            appDiagramData.gameObject.SetActive(true);
        }
        RefreshDiagramPanel();
    }

    private void BeginDiagramSession()
    {
        FindDiagramPanel();
        if (diagramController == null)
        {
            if (!warnedMissingDiagramPanel)
            {
                Debug.LogWarning(
                    "ABSTRACCION: no se encontro el panel de diagramas existente."
                );
                warnedMissingDiagramPanel = true;
            }
            return;
        }

        diagramController.CambiarAModoDictadoTema();
        if (diagramModeManager != null)
        {
            diagramModeManager.SetModoSinAnimacion(
                AlgoLabClassDiagramModeManager.ModoPanel.Diagrama
            );
        }
        diagramSessionActive = true;
    }

    private void EndDiagramSession()
    {
        if (initialSongDiagramData != null)
        {
            initialSongDiagramData.gameObject.SetActive(false);
        }
        if (storeDiagramData != null)
        {
            storeDiagramData.gameObject.SetActive(false);
        }
        if (appDiagramData != null)
        {
            appDiagramData.gameObject.SetActive(false);
        }

        if (diagramSessionActive && diagramController != null)
        {
            diagramController.RefrescarDiagramas();
        }
        diagramSessionActive = false;
    }

    private void FindDiagramPanel()
    {
        if (diagramController == null)
        {
            AlgoLabClassDiagramController[] controllers =
                FindObjectsByType<AlgoLabClassDiagramController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
            if (controllers.Length > 0)
            {
                diagramController = controllers[0];
            }
        }

        if (diagramModeManager == null)
        {
            AlgoLabClassDiagramModeManager[] managers =
                FindObjectsByType<AlgoLabClassDiagramModeManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
            if (managers.Length > 0)
            {
                diagramModeManager = managers[0];
            }
        }
    }

    private void RefreshDiagramPanel()
    {
        if (diagramController != null)
        {
            diagramController.RefrescarDiagramas();
            ApplyDiagramContextColors();
        }
    }

    private void ApplyDiagramContextColors()
    {
        if (diagramController == null)
        {
            return;
        }

        AlgoLabClassDiagramCardUI storeCard =
            diagramController.ObtenerTarjetaPorObjeto(storeDiagramData);
        if (storeCard != null)
        {
            storeCard.ConfigurarContornoContexto(storeOutlineColor);
        }

        AlgoLabClassDiagramCardUI appCard =
            diagramController.ObtenerTarjetaPorObjeto(appDiagramData);
        if (appCard != null)
        {
            appCard.ConfigurarContornoContexto(applicationOutlineColor);
        }
    }

    private Transform CreateGroup(string objectName, Transform parent = null)
    {
        Transform group = new GameObject(objectName).transform;
        group.SetParent(parent != null ? parent : generatedRoot, false);
        return group;
    }

    private void CreateIconQuad(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        float size,
        Material material)
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
            Destroy(collider);
        }

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private TextMeshPro CreateLabel(
        string objectName,
        Transform parent,
        string text,
        Vector3 localPosition,
        Vector2 size,
        float fontSize)
    {
        GameObject labelObject = new GameObject(objectName);
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = localPosition;
        labelObject.transform.localRotation = Quaternion.identity;
        const float textScale = 0.025f;
        labelObject.transform.localScale = Vector3.one * textScale;

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.text = text;
        label.font = fontAsset;
        label.fontSize = fontSize * 360f;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = size / textScale;
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return label;
    }

    private void SetPhoneTitle(string title)
    {
        if (phoneTitle != null)
        {
            phoneTitle.text = title;
        }
    }

    private void ResetVisualsInstantly()
    {
        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localPosition = pillarHomePositions[i];
            pillarAnchors[i].localScale = Vector3.zero;
        }

        SetGroupScale(vinylGroup, Vector3.zero);
        SetGroupScale(storeGroup, Vector3.zero);
        SetGroupScale(phoneGroup, Vector3.zero);
        if (phoneShellAnchor != null)
        {
            phoneShellAnchor.localScale = Vector3.one;
        }
        if (boardAnchor != null)
        {
            boardAnchor.localScale = Vector3.zero;
        }
        SetPhoneTitle("APLICACION");

        if (initialSongDiagramData != null)
        {
            initialSongDiagramData.gameObject.SetActive(false);
        }
        if (storeDiagramData != null)
        {
            storeDiagramData.gameObject.SetActive(false);
        }
        if (appDiagramData != null)
        {
            appDiagramData.gameObject.SetActive(false);
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
                pillarAnchors[i].localScale = Vector3.LerpUnclamped(
                    Vector3.zero,
                    pillarHomeScales[i],
                    smooth
                );
            }
            yield return null;
        }

        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localScale = pillarHomeScales[i];
        }
    }

    private IEnumerator AnimatePillarFocus(int selectedIndex)
    {
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
                    ? new Vector3(
                        centerX,
                        pillarBaseY + pillarSelectedRaise,
                        pillarSelectedForwardOffset
                    )
                    : GetDistributedBackgroundPosition(i, selectedIndex);
                Vector3 targetScale = selected
                    ? pillarHomeScales[i] * pillarSelectedScale
                    : pillarHomeScales[i];

                pillarAnchors[i].localPosition = Vector3.LerpUnclamped(
                    startPositions[i],
                    targetPosition,
                    smooth
                );
                pillarAnchors[i].localScale = Vector3.LerpUnclamped(
                    startScales[i],
                    targetScale,
                    smooth
                );
            }
            yield return null;
        }

        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            bool selected = i == selectedIndex;
            pillarAnchors[i].localPosition = selected
                ? new Vector3(
                    centerX,
                    pillarBaseY + pillarSelectedRaise,
                    pillarSelectedForwardOffset
                )
                : GetDistributedBackgroundPosition(i, selectedIndex);
            pillarAnchors[i].localScale = selected
                ? pillarHomeScales[i] * pillarSelectedScale
                : pillarHomeScales[i];
        }
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

        float leftEdge = centerX - 1.5f * pillarSpacing;
        float rightEdge = centerX + 1.5f * pillarSpacing;
        float t = backgroundOrder / 2f;
        return new Vector3(
            Mathf.Lerp(leftEdge, rightEdge, t),
            pillarBaseY,
            depth
        );
    }

    private IEnumerator HidePillars()
    {
        Vector3[] starts = new Vector3[pillarAnchors.Count];
        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            starts[i] = pillarAnchors[i].localScale;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float smooth = Smooth01(
                elapsed / Mathf.Max(0.01f, transitionDuration)
            );
            for (int i = 0; i < pillarAnchors.Count; i++)
            {
                pillarAnchors[i].localScale = Vector3.LerpUnclamped(
                    starts[i],
                    Vector3.zero,
                    smooth
                );
            }
            yield return null;
        }

        for (int i = 0; i < pillarAnchors.Count; i++)
        {
            pillarAnchors[i].localScale = Vector3.zero;
        }
    }

    private IEnumerator ShowGroup(Transform group)
    {
        if (group == null)
        {
            yield break;
        }
        yield return AnimateScale(
            group,
            group.localScale,
            Vector3.one,
            appearDuration
        );
    }

    private IEnumerator AnimateScale(
        Transform target,
        Vector3 start,
        Vector3 end,
        float duration)
    {
        if (target == null)
        {
            yield break;
        }

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth01(elapsed / duration);
            target.localScale = Vector3.LerpUnclamped(start, end, t);
            yield return null;
        }
        target.localScale = end;
    }

    private IEnumerator Pulse(Transform target, float multiplier)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 original = target.localScale;
        Vector3 large = original * multiplier;
        yield return AnimateScale(target, original, large, focusDuration);
        yield return AnimateScale(target, large, original, focusDuration);
    }

    private IEnumerator CrossFadeScale(Transform outgoing, Transform incoming)
    {
        if (outgoing == null || incoming == null)
        {
            yield break;
        }

        Vector3 outgoingStart = outgoing.localScale;
        Vector3 incomingEnd = Vector3.one;
        incoming.localScale = Vector3.zero;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration * 1.25f);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth01(elapsed / duration);
            outgoing.localScale = Vector3.LerpUnclamped(
                outgoingStart,
                Vector3.zero,
                t
            );
            incoming.localScale = Vector3.LerpUnclamped(
                Vector3.zero,
                incomingEnd,
                t
            );
            yield return null;
        }
        outgoing.localScale = Vector3.zero;
        incoming.localScale = incomingEnd;
    }

    private AudioClip GetClip(int index)
    {
        return narrationClips != null &&
            index >= 0 &&
            index < narrationClips.Length
            ? narrationClips[index]
            : null;
    }

    private void StartNarration(AudioClip clip)
    {
        EnsureAudioSource();
        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.volume = narrationVolume;

        if (clip == null)
        {
            Debug.LogError(
                "ABSTRACCION: falta un audio en la secuencia 1 a 6 y cierre 8."
            );
            return;
        }

        narrationSource.Play();
    }

    private IEnumerator WaitForNarrationTime(float targetTime)
    {
        while (
            narrationSource != null &&
            narrationSource.isPlaying &&
            narrationSource.time < targetTime)
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

    private static void ApplyMaterialToRenderers(
        GameObject root,
        Material material)
    {
        if (root == null || material == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = material;
            }
            renderers[i].sharedMaterials = materials;
        }
    }

    private Material GetContextOutlineMaterial(
        ref Material cachedMaterial,
        Color outlineColor,
        string materialName)
    {
        if (cachedMaterial != null || outlineMaterialTemplate == null)
        {
            return cachedMaterial;
        }

        cachedMaterial = new Material(outlineMaterialTemplate);
        cachedMaterial.name = materialName;
        cachedMaterial.hideFlags = HideFlags.DontSave;

        if (cachedMaterial.HasProperty("_OutlineColor"))
        {
            cachedMaterial.SetColor("_OutlineColor", outlineColor);
        }
        else if (cachedMaterial.HasProperty("_BaseColor"))
        {
            cachedMaterial.SetColor("_BaseColor", outlineColor);
        }
        else
        {
            cachedMaterial.color = outlineColor;
        }

        return cachedMaterial;
    }

    private void ApplyContextOutline(
        GameObject root,
        Material outlineMaterial)
    {
        if (root == null || outlineMaterial == null)
        {
            return;
        }

        MeshRenderer[] renderers =
            root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null ||
                renderer.gameObject.name.StartsWith("Outline_") ||
                renderer.GetComponent<MeshFilter>() == null)
            {
                continue;
            }

            AlgoLabOutlineController controller =
                renderer.GetComponent<AlgoLabOutlineController>();
            if (controller == null)
            {
                controller =
                    renderer.gameObject.AddComponent<AlgoLabOutlineController>();
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            float largestLocalDimension = 0f;
            if (filter != null && filter.sharedMesh != null)
            {
                Vector3 meshSize = filter.sharedMesh.bounds.size;
                largestLocalDimension = Mathf.Max(
                    meshSize.x,
                    Mathf.Max(meshSize.y, meshSize.z)
                );
            }

            // El shader desplaza vértices en espacio local. Los FBX de la
            // tienda y el teléfono contienen escalas internas de 100–200,
            // por lo que un grosor absoluto produce siluetas gigantes o
            // separadas. Usar una fracción de cada malla conserva un borde
            // uniforme después de cualquier escala importada.
            float outlineSize = largestLocalDimension > 0.000001f
                ? largestLocalDimension *
                    Mathf.Clamp(objectOutlineRelativeThickness, 0.002f, 0.04f)
                : 0.0001f;

            controller.Configurar(outlineMaterial, true, outlineSize);
        }
    }

    private static void DestroyRuntimeMaterial(ref Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }

        material = null;
    }

    private static Material GetMaterial(Material[] materials, int index)
    {
        return materials != null &&
            index >= 0 &&
            index < materials.Length
            ? materials[index]
            : null;
    }

    private static void SetGroupScale(Transform group, Vector3 scale)
    {
        if (group != null)
        {
            group.localScale = scale;
        }
    }

    private static void NormalizeModel(
        Transform model,
        Transform relativeTo,
        float desiredLargestDimension,
        Vector3 localOffset)
    {
        Bounds bounds;
        if (!TryGetLocalRendererBounds(model, relativeTo, out bounds))
        {
            return;
        }

        float largest = Mathf.Max(
            bounds.size.x,
            Mathf.Max(bounds.size.y, bounds.size.z)
        );
        if (largest < 0.0001f)
        {
            return;
        }

        model.localScale *= desiredLargestDimension / largest;
        if (!TryGetLocalRendererBounds(model, relativeTo, out bounds))
        {
            return;
        }

        model.localPosition += localOffset - bounds.center;
    }

    private static void NormalizeModelHeight(
        Transform model,
        Transform relativeTo,
        float desiredHeight)
    {
        Bounds bounds;
        if (!TryGetLocalRendererBounds(model, relativeTo, out bounds) ||
            bounds.size.y < 0.0001f)
        {
            return;
        }

        model.localScale *= desiredHeight / bounds.size.y;
        if (!TryGetLocalRendererBounds(model, relativeTo, out bounds))
        {
            return;
        }

        Vector3 position = model.localPosition;
        position.y -= bounds.min.y;
        model.localPosition = position;
    }

    private static void EnsureVerticalLongAxis(
        Transform model,
        Transform relativeTo)
    {
        Bounds bounds;
        if (!TryGetLocalRendererBounds(model, relativeTo, out bounds))
        {
            return;
        }

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
            Bounds worldBounds = renderers[i].bounds;
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(
                            extents,
                            new Vector3(x, y, z)
                        );
                        Vector3 localCorner =
                            relativeTo.InverseTransformPoint(corner);
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

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
