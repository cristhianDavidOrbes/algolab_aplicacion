using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

/// <summary>
/// Explicacion fisica de los audios 4 a 10 de Encapsulamiento.
/// Los objetos y las flechas viven en el mundo; los diagramas UML se publican
/// exclusivamente en el panel de diagramas existente de AlgoLab.
/// </summary>
public class AlgoLabEncapsulationBankExampleVisual : MonoBehaviour, IAlgoLabKonamiLevelEffect
{
    [Header("Modelos")]
    public GameObject safeModelPrefab;
    public GameObject personModelPrefab;
    public GameObject goldModelPrefab;

    [Header("Panel de diagramas")]
    public string userClassName = "Usuario";
    public string accountClassName = "Cuenta";

    [Header("Recursos visuales")]
    public TMP_FontAsset fontAsset;
    public Material panelMaterial;
    public Material headerMaterial;
    public Material publicMaterial;
    public Material privateMaterial;
    public Material neutralMaterial;
    public Material publicIconMaterial;
    public Material privateIconMaterial;

    [Header("Audios 4 a 10")]
    public AudioClip audioEjemploCuentaBancaria;
    public AudioClip audioValorPrivado;
    public AudioClip audioMetodosPublicos;
    public AudioClip audioDepositarSueldo;
    public AudioClip audioIntentoModificarValor;
    public AudioClip audioAccesoControlado;
    public AudioClip audioConclusion;

    [Header("Secreto Konami del nivel 3")]
    public AnimationClip konamiDanceClip;
    public float konamiScaleMultiplier = 3f;
    public float konamiRainbowSpeed = 0.32f;

    [Header("Distribucion fisica")]
    public float diagramCenterX = 0.15f;
    public float diagramDepth = 0.35f;
    public float diagramBaseY = -0.02f;
    public float userOffsetX = -0.39f;
    public float accountOffsetX = 0.34f;
    public float personHeight = 0.48f;
    public float safeHeight = 0.42f;
    public float goldHeight = 0.13f;

    [Header("Animacion")]
    public float appearDuration = 0.32f;
    public float focusDuration = 0.25f;
    public float moveDuration = 0.75f;
    [Range(0.05f, 0.5f)] public float fadedSafeOpacity = 0.16f;

    private sealed class SafeMaterialState
    {
        public Material material;
        public Color baseColor;
    }

    private sealed class UserRendererState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] rainbowMaterials;
        public Color[] originalColors;
    }

    private Transform generatedRoot;
    private Transform physicalRoot;
    private Transform userGroup;
    private Transform accountGroup;
    private Transform valueGold;
    private Transform salaryGold;
    private Transform directBlockedArrow;
    private Transform directBlockedX;
    private Transform controlledArrow;
    private TextMeshPro salaryText;
    private Vector3 salaryStartPosition;
    private Vector3 salaryTargetPosition;
    private Vector3 attemptTargetPosition;
    private AudioSource narrationSource;
    private AlgoLabAnimatedSafe animatedSafe;
    private GameObject userModelInstance;
    private Animator userAnimator;
    private Vector3 userModelOriginalLocalPosition;
    private Quaternion userModelOriginalLocalRotation;
    private Transform userLeftArm;
    private Transform userLeftForeArm;
    private Transform userLeftHand;
    private Transform userRightArm;
    private Transform userRightForeArm;
    private Transform userRightHand;
    private Quaternion userLeftArmIdleRotation;
    private Quaternion userLeftForeArmIdleRotation;
    private Quaternion userLeftHandIdleRotation;
    private Quaternion userRightArmIdleRotation;
    private Quaternion userRightForeArmIdleRotation;
    private Quaternion userRightHandIdleRotation;
    private Coroutine userGestureRoutine;
    private bool userRigReady;
    private readonly List<UserRendererState> userRendererStates = new List<UserRendererState>();
    private Coroutine konamiEffectRoutine;
    private PlayableGraph konamiDanceGraph;
    private AnimationClipPlayable konamiDancePlayable;
    private bool konamiEffectActive;
    private Vector3 userScaleBeforeKonami = Vector3.one;

    private readonly List<SafeMaterialState> safeMaterials = new List<SafeMaterialState>();
    private AlgoLabObjetoEducativo userDiagramData;
    private AlgoLabObjetoEducativo accountDiagramData;
    private AlgoLabClassDiagramController diagramController;
    private AlgoLabClassDiagramModeManager diagramModeManager;
    private bool diagramSessionActive;
    private bool warnedMissingDiagramPanel;

    public void PrepareVisuals()
    {
        EnsureVisuals();
        ResetVisualsInstantly();
    }

    private void OnDisable()
    {
        StopKonamiEffect(true);
        ResetUserGestureInstantly();
        HideThemeDiagrams();
    }

    private void OnDestroy()
    {
        StopKonamiEffect(true);
        ReleaseUserRainbowMaterials();
        HideThemeDiagrams();
    }

    public void ResetVisualsInstantly()
    {
        EnsureVisuals();

        SetScale(userGroup, konamiEffectActive ? GetUserVisibleScale() : Vector3.zero);
        SetScale(accountGroup, Vector3.zero);
        SetScale(valueGold, Vector3.zero);
        SetScale(salaryGold, Vector3.zero);
        SetScale(directBlockedArrow, Vector3.zero);
        SetScale(directBlockedX, Vector3.zero);
        SetScale(controlledArrow, Vector3.zero);
        SetSafeOpacityInstantly(1f);
        if (animatedSafe != null) animatedSafe.SetOpenInstantly(false);
        ResetUserGestureInstantly();

        if (salaryGold != null)
        {
            salaryGold.localPosition = salaryStartPosition;
        }

        if (salaryText != null)
        {
            salaryText.text = "SUELDO\n100.000 pesos";
            salaryText.color = new Color(1f, 0.84f, 0.28f, 1f);
        }

        HideThemeDiagrams();
    }

    public void ShowCompleteDiagramInstantly()
    {
        EnsureVisuals();
        ResetVisualsInstantly();
        SetScale(userGroup, GetUserVisibleScale());
        SetScale(accountGroup, Vector3.one);
        SetScale(valueGold, Vector3.one);
        SetScale(controlledArrow, Vector3.one);
        if (animatedSafe != null) animatedSafe.SetOpenInstantly(true);
        else SetSafeOpacityInstantly(0.32f);
        ShowUserDiagram();
        ShowAccountDiagram(true, true, true);
        SetUserGestureInstantly(accountGroup != null ? accountGroup.position : Vector3.zero);
    }

    public IEnumerator PlaySequence(AudioSource sharedNarrationSource, float volume)
    {
        narrationSource = sharedNarrationSource;
        EnsureVisuals();
        ResetVisualsInstantly();
        BeginDiagramSession();

        // Audio 4: la caja fuerte es CuentaBancaria y el oro interior es valor.
        StartNarration(audioEjemploCuentaBancaria, volume);
        yield return WaitForNarrationTime(2.10f);
        ShowAccountDiagram(false, false, false);
        yield return AnimateScale(accountGroup, Vector3.one, appearDuration);
        yield return WaitForNarrationTime(4.98f);
        yield return AnimateScale(valueGold, Vector3.one, appearDuration);
        yield return RevealSafeContents();
        HighlightAccountAttribute("valor", 1.5f);
        yield return Pulse(valueGold, 1.18f);
        yield return WaitForNarrationEnd();
        yield return HideSafeContents();

        // Audio 5: valor pasa a privado; Usuario no puede cambiarlo directamente.
        StartNarration(audioValorPrivado, volume);
        ShowAccountDiagram(true, false, false);
        HighlightAccountAttribute("valor", 2.1f);
        yield return RevealSafeContents();
        yield return Pulse(valueGold, 1.20f);
        yield return WaitForNarrationTime(4.58f);
        yield return HideSafeContents();
        ShowUserDiagram();
        yield return AnimateScale(userGroup, GetUserVisibleScale(), appearDuration);
        yield return WaitForNarrationTime(5.70f);
        StartUserGesture(accountGroup, 1.35f);
        yield return AnimateScale(directBlockedArrow, Vector3.one, focusDuration);
        yield return AnimateScale(directBlockedX, Vector3.one, focusDuration);
        yield return WaitForNarrationEnd();

        // Audio 6: el panel agrega y resalta los tres metodos publicos.
        StartNarration(audioMetodosPublicos, volume);
        yield return AnimateScale(directBlockedArrow, Vector3.zero, focusDuration);
        yield return AnimateScale(directBlockedX, Vector3.zero, focusDuration);
        ShowAccountDiagram(true, true, false);
        HighlightAccountMethods(2.0f);
        yield return WaitForNarrationTime(5.66f);
        HighlightAccountMethod("depositar", 1.0f);
        yield return WaitForNarrationTime(6.82f);
        HighlightAccountMethod("retirar", 1.0f);
        yield return WaitForNarrationTime(7.56f);
        HighlightAccountMethod("consultarValor", 1.0f);
        yield return WaitForNarrationEnd();

        // Audio 7: el Usuario deposita 100.000 mediante la accion publica.
        StartNarration(audioDepositarSueldo, volume);
        salaryGold.localPosition = salaryStartPosition;
        if (salaryText != null)
        {
            salaryText.text = "SUELDO\n100.000 pesos";
            salaryText.color = new Color(1f, 0.84f, 0.28f, 1f);
        }
        yield return AnimateScale(salaryGold, Vector3.one, appearDuration);
        yield return WaitForNarrationTime(3.14f);
        yield return AnimateScale(controlledArrow, Vector3.one, focusDuration);
        StartUserGesture(accountGroup, 2.35f);
        yield return WaitForNarrationTime(5.70f);
        HighlightAccountMethod("depositar", 1.4f);
        yield return WaitForNarrationTime(6.30f);
        yield return RevealSafeContents();
        yield return AnimateMove(salaryGold, salaryTargetPosition, moveDuration);
        SetScale(salaryGold, Vector3.zero);
        yield return Pulse(valueGold, 1.25f);
        ShowAccountDiagram(true, true, true);
        HighlightAccountAttribute("valor", 1.4f);
        yield return WaitForNarrationEnd();
        yield return HideSafeContents();

        // Audio 8: un oro de 500.000 intenta entrar directamente y es rechazado.
        StartNarration(audioIntentoModificarValor, volume);
        yield return AnimateScale(controlledArrow, Vector3.zero, focusDuration);
        salaryGold.localPosition = salaryStartPosition;
        if (salaryText != null)
        {
            salaryText.text = "INTENTO\n500.000 pesos";
            salaryText.color = new Color(1f, 0.38f, 0.38f, 1f);
        }
        yield return AnimateScale(salaryGold, Vector3.one, appearDuration);
        yield return AnimateScale(directBlockedArrow, Vector3.one, focusDuration);
        StartUserGesture(accountGroup, 2.65f);
        yield return WaitForNarrationTime(3.10f);
        yield return AnimateMove(salaryGold, attemptTargetPosition, moveDuration);
        yield return WaitForNarrationTime(5.68f);
        yield return AnimateScale(directBlockedX, Vector3.one, focusDuration);
        yield return Shake(accountGroup, 0.025f, 0.42f);
        yield return AnimateMove(salaryGold, salaryStartPosition, moveDuration * 0.65f);
        yield return AnimateScale(salaryGold, Vector3.zero, focusDuration);
        yield return WaitForNarrationTime(8.50f);
        yield return RevealSafeContents();
        HighlightAccountAttribute("valor", 1.8f);
        yield return Pulse(valueGold, 1.22f);
        yield return WaitForNarrationEnd();
        yield return HideSafeContents();

        // Audio 9: solo las flechas verdes y los metodos publicos permiten el acceso.
        StartNarration(audioAccesoControlado, volume);
        yield return AnimateScale(directBlockedArrow, Vector3.zero, focusDuration);
        yield return AnimateScale(directBlockedX, Vector3.zero, focusDuration);
        yield return WaitForNarrationTime(2.78f);
        yield return AnimateScale(controlledArrow, Vector3.one, appearDuration);
        StartUserGesture(accountGroup, 2.5f);
        HighlightAccountMethods(2.0f);
        yield return WaitForNarrationTime(5.94f);
        HighlightAccountMethod("depositar", 1.1f);
        yield return WaitForNarrationTime(7.02f);
        HighlightAccountMethod("retirar", 1.1f);
        yield return WaitForNarrationTime(8.06f);
        HighlightAccountMethod("consultarValor", 1.1f);
        yield return WaitForNarrationTime(10.04f);
        yield return AnimateScale(directBlockedX, Vector3.one, focusDuration);
        yield return WaitForNarrationEnd();

        // Audio 10: conclusion sin tarjetas flotantes; se usan objetos y panel UML.
        StartNarration(audioConclusion, volume);
        HighlightAccountClass(1.8f);
        StartUserGesture(accountGroup, 1.5f);
        yield return Pulse(accountGroup, 1.08f);
        yield return WaitForNarrationTime(2.16f);
        yield return RevealSafeContents();
        HighlightAccountAttribute("valor", 2.0f);
        yield return Pulse(valueGold, 1.20f);
        yield return WaitForNarrationTime(4.28f);
        yield return HideSafeContents();
        HighlightAccountMethods(2.5f);
        yield return Pulse(controlledArrow, 1.10f);
        yield return WaitForNarrationEnd();
        yield return new WaitForSecondsRealtime(0.65f);
    }

    private void EnsureVisuals()
    {
        if (generatedRoot != null)
        {
            return;
        }

        generatedRoot = new GameObject("VisualesFisicos_CuentaBancaria_Audios04_10").transform;
        generatedRoot.SetParent(transform, false);
        physicalRoot = CreateAnchor("ObjetosFisicos", generatedRoot, Vector3.zero);

        CreatePhysicalModels();
        CreateObjectInteractionArrows();
        CreateDiagramDataObjects();
    }

    private void CreatePhysicalModels()
    {
        float baseY = diagramBaseY - 0.08f;
        float userX = diagramCenterX + userOffsetX;
        float accountX = diagramCenterX + accountOffsetX;

        userGroup = CreateAnchor("Objeto_Usuario", physicalRoot,
            new Vector3(userX, baseY, diagramDepth));
        GameObject personModel = CreateNormalizedModel(personModelPrefab, userGroup, personHeight,
            Quaternion.Euler(0f, 180f, 0f), true);
        userModelInstance = personModel;
        if (userModelInstance != null)
        {
            userModelOriginalLocalPosition = userModelInstance.transform.localPosition;
            userModelOriginalLocalRotation = userModelInstance.transform.localRotation;
            userAnimator = userModelInstance.GetComponent<Animator>();
            if (userAnimator == null) userAnimator = userModelInstance.AddComponent<Animator>();
            userAnimator.applyRootMotion = false;
            userAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
        InitializeUserRig(personModel);
        CreateText("EtiquetaUsuario", userGroup, "USUARIO",
            new Vector3(0f, -0.045f, -0.07f), 0.28f, 0.045f, Color.white, 1.7f);

        accountGroup = CreateAnchor("Objeto_CuentaBancaria", physicalRoot,
            new Vector3(accountX, baseY, diagramDepth));
        GameObject safeModel = CreateNormalizedModel(safeModelPrefab, accountGroup, safeHeight,
            Quaternion.identity, false);
        animatedSafe = safeModel != null ? safeModel.GetComponentInChildren<AlgoLabAnimatedSafe>(true) : null;
        if (animatedSafe == null) PrepareSafeMaterials(safeModel);
        CreateText("EtiquetaCuenta", accountGroup, "CUENTA",
            new Vector3(0f, -0.045f, -0.07f), 0.38f, 0.045f, Color.white, 1.55f);

        valueGold = CreateAnchor("Variable_valor_Oro_DentroCajaFuerte", accountGroup,
            new Vector3(0f, safeHeight * 0.27f, -0.018f));
        CreateNormalizedModel(goldModelPrefab, valueGold, goldHeight, Quaternion.identity, false);
        CreateText("EtiquetaValor", valueGold, "valor",
            new Vector3(0f, goldHeight + 0.025f, -0.035f), 0.18f, 0.04f,
            new Color(1f, 0.78f, 0.18f, 1f), 1.65f);

        salaryStartPosition = new Vector3(userX + 0.12f, baseY + personHeight * 0.28f, diagramDepth - 0.08f);
        salaryTargetPosition = new Vector3(accountX, baseY + safeHeight * 0.27f, diagramDepth - 0.018f);
        attemptTargetPosition = new Vector3(accountX - 0.16f, baseY + safeHeight * 0.34f, diagramDepth - 0.08f);
        salaryGold = CreateAnchor("Oro_Interactivo_Usuario", physicalRoot, salaryStartPosition);
        CreateNormalizedModel(goldModelPrefab, salaryGold, goldHeight, Quaternion.identity, false);
        salaryText = CreateText("EtiquetaOroInteractivo", salaryGold, "SUELDO\n100.000 pesos",
            new Vector3(0f, goldHeight + 0.045f, -0.035f), 0.25f, 0.075f,
            new Color(1f, 0.84f, 0.28f, 1f), 1.45f);
    }

    private void CreateObjectInteractionArrows()
    {
        float baseY = diagramBaseY - 0.08f;
        float startX = diagramCenterX + userOffsetX + 0.235f;
        float endX = diagramCenterX + accountOffsetX - 0.34f;
        float arrowY = baseY + 0.215f;
        float arrowZ = diagramDepth - 0.15f;

        directBlockedArrow = CreateArrow("Flecha_Objeto_AccesoDirectoPrivado", physicalRoot,
            new Vector3(startX, arrowY, arrowZ),
            new Vector3(endX, arrowY, arrowZ), privateMaterial);

        directBlockedX = CreateAnchor("Bloqueo_Objeto_X", physicalRoot,
            new Vector3((startX + endX) * 0.5f, arrowY + 0.085f, arrowZ - 0.015f));
        CreateText("X", directBlockedX, "X", Vector3.zero, 0.11f, 0.10f,
            new Color(1f, 0.2f, 0.24f, 1f), 3.4f);

        controlledArrow = CreateArrow("Flecha_Objeto_AccesoPublico", physicalRoot,
            new Vector3(startX, arrowY, arrowZ),
            new Vector3(endX, arrowY, arrowZ), publicMaterial);
    }

    private void CreateDiagramDataObjects()
    {
        GameObject userDataObject = new GameObject("PanelDiagrama_Datos_Usuario");
        userDataObject.SetActive(false);
        userDataObject.transform.SetParent(generatedRoot, false);
        userDiagramData = userDataObject.AddComponent<AlgoLabObjetoEducativo>();
        userDiagramData.nombreObjeto = "Usuario";
        userDiagramData.nombreClase = userClassName;
        userDiagramData.descripcionObjeto = "Persona que utiliza la cuenta bancaria.";
        userDiagramData.atributos = new[]
        {
            "nombre",
            "sueldo"
        };
        userDiagramData.metodos = new[]
        {
            "depositar()",
            "retirar()"
        };
        userDiagramData.forzarVisibleEnDiagramaTema = true;

        GameObject accountDataObject = new GameObject("PanelDiagrama_Datos_CuentaBancaria");
        accountDataObject.SetActive(false);
        accountDataObject.transform.SetParent(generatedRoot, false);
        accountDiagramData = accountDataObject.AddComponent<AlgoLabObjetoEducativo>();
        accountDiagramData.nombreObjeto = "Cuenta bancaria";
        accountDiagramData.nombreClase = accountClassName;
        accountDiagramData.descripcionObjeto = "Caja fuerte que protege el atributo valor.";
        accountDiagramData.atributos = new string[0];
        accountDiagramData.metodos = new string[0];
        accountDiagramData.forzarVisibleEnDiagramaTema = true;
    }

    private void BeginDiagramSession()
    {
        FindDiagramPanel();
        if (diagramController == null)
        {
            if (!warnedMissingDiagramPanel)
            {
                Debug.LogWarning("ENCAPSULAMIENTO: no se encontro el panel de diagramas; los objetos fisicos continuaran funcionando.");
                warnedMissingDiagramPanel = true;
            }
            return;
        }

        diagramController.CambiarAModoDictadoTema();
        if (diagramModeManager != null)
        {
            diagramModeManager.SetModoSinAnimacion(AlgoLabClassDiagramModeManager.ModoPanel.Diagrama);
        }
        diagramSessionActive = true;
    }

    private void FindDiagramPanel()
    {
        if (diagramController == null)
        {
            AlgoLabClassDiagramController[] controllers = FindObjectsByType<AlgoLabClassDiagramController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (controllers != null && controllers.Length > 0)
            {
                diagramController = controllers[0];
            }
        }

        if (diagramModeManager == null)
        {
            AlgoLabClassDiagramModeManager[] managers = FindObjectsByType<AlgoLabClassDiagramModeManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (managers != null && managers.Length > 0)
            {
                diagramModeManager = managers[0];
            }
        }
    }

    private void ShowUserDiagram()
    {
        BeginDiagramSessionIfNeeded();
        if (userDiagramData == null) return;
        userDiagramData.gameObject.SetActive(true);
        RefreshDiagramPanel();
        if (diagramController != null)
        {
            AlgoLabClassDiagramCardUI card =
                diagramController.ObtenerTarjetaPorNombreClase(userClassName);
            if (card != null) card.ConfigurarResaltadoSoloSignos(true);
            diagramController.ResaltarClase(userClassName, 1.3f);
        }
    }

    private void ShowAccountDiagram(bool isPrivate, bool showMethods, bool hasDepositedValue)
    {
        BeginDiagramSessionIfNeeded();
        if (accountDiagramData == null) return;

        string value = "valor";
        accountDiagramData.atributos = new[]
        {
            value
        };
        accountDiagramData.metodos = showMethods
            ? new[]
            {
                "depositar()",
                "retirar()",
                "consultar()"
            }
            : new string[0];
        accountDiagramData.gameObject.SetActive(true);
        RefreshDiagramPanel();
        if (diagramController != null)
        {
            AlgoLabClassDiagramCardUI card =
                diagramController.ObtenerTarjetaPorNombreClase(accountClassName);
            if (card != null) card.ConfigurarResaltadoSoloSignos(true);
        }
    }

    private void BeginDiagramSessionIfNeeded()
    {
        if (!diagramSessionActive)
        {
            BeginDiagramSession();
        }
    }

    private void RefreshDiagramPanel()
    {
        FindDiagramPanel();
        if (diagramController != null)
        {
            diagramController.RefrescarDiagramas();
        }
    }

    private void HideThemeDiagrams()
    {
        bool changed = false;
        if (userDiagramData != null && userDiagramData.gameObject.activeSelf)
        {
            userDiagramData.gameObject.SetActive(false);
            changed = true;
        }
        if (accountDiagramData != null && accountDiagramData.gameObject.activeSelf)
        {
            accountDiagramData.gameObject.SetActive(false);
            changed = true;
        }

        if (changed && diagramController != null)
        {
            diagramController.RefrescarDiagramas();
        }
        diagramSessionActive = false;
    }

    private void HighlightAccountClass(float duration)
    {
        if (diagramController != null) diagramController.ResaltarClase(accountClassName, duration);
    }

    private void HighlightAccountAttribute(string attribute, float duration)
    {
        if (diagramController == null) return;
        AlgoLabClassDiagramCardUI card = diagramController.ObtenerTarjetaPorNombreClase(accountClassName);
        if (card != null) card.ResaltarAtributoPorTiempo(attribute, duration);
    }

    private void HighlightAccountMethods(float duration)
    {
        if (diagramController != null) diagramController.ResaltarMetodos(accountClassName, duration);
    }

    private void HighlightAccountMethod(string method, float duration)
    {
        if (diagramController == null) return;
        AlgoLabClassDiagramCardUI card = diagramController.ObtenerTarjetaPorNombreClase(accountClassName);
        if (card != null) card.ResaltarMetodoPorTiempo(method, duration);
    }

    private void PrepareSafeMaterials(GameObject safeModel)
    {
        safeMaterials.Clear();
        if (safeModel == null) return;

        Renderer[] renderers = safeModel.GetComponentsInChildren<Renderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            Material[] sourceMaterials = renderers[r].sharedMaterials;
            Material[] runtimeMaterials = new Material[sourceMaterials.Length];
            for (int m = 0; m < sourceMaterials.Length; m++)
            {
                Material source = sourceMaterials[m];
                if (source == null) continue;
                Material runtime = new Material(source);
                runtime.name = source.name + "_TransparenteNivel3";
                ConfigureTransparentMaterial(runtime);
                Color baseColor = GetMaterialColor(runtime);
                safeMaterials.Add(new SafeMaterialState { material = runtime, baseColor = baseColor });
                runtimeMaterials[m] = runtime;
            }
            renderers[r].sharedMaterials = runtimeMaterials;
        }
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material == null) return;
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static Color GetMaterialColor(Material material)
    {
        if (material != null && material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        if (material != null && material.HasProperty("_Color")) return material.GetColor("_Color");
        return Color.white;
    }

    private void SetSafeOpacityInstantly(float opacity)
    {
        opacity = Mathf.Clamp01(opacity);
        for (int i = 0; i < safeMaterials.Count; i++)
        {
            SafeMaterialState state = safeMaterials[i];
            if (state == null || state.material == null) continue;
            Color color = state.baseColor;
            color.a *= opacity;
            if (state.material.HasProperty("_BaseColor")) state.material.SetColor("_BaseColor", color);
            if (state.material.HasProperty("_Color")) state.material.SetColor("_Color", color);
        }
    }

    private IEnumerator RevealSafeContents()
    {
        if (animatedSafe != null)
        {
            SetSafeOpacityInstantly(1f);
            yield return animatedSafe.OpenSequence(Mathf.Max(0.55f, moveDuration));
            yield break;
        }

        yield return FadeSafeOpacity(fadedSafeOpacity, focusDuration);
    }

    private IEnumerator HideSafeContents()
    {
        if (animatedSafe != null)
        {
            yield return animatedSafe.CloseSequence(Mathf.Max(0.55f, moveDuration));
            yield break;
        }

        yield return FadeSafeOpacity(1f, focusDuration);
    }

    private IEnumerator FadeSafeOpacity(float targetOpacity, float duration)
    {
        float startOpacity = GetCurrentSafeOpacity();
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetSafeOpacityInstantly(Mathf.Lerp(startOpacity, targetOpacity, Smooth01(elapsed / duration)));
            yield return null;
        }
        SetSafeOpacityInstantly(targetOpacity);
    }

    private float GetCurrentSafeOpacity()
    {
        if (safeMaterials.Count == 0 || safeMaterials[0].material == null) return 1f;
        SafeMaterialState state = safeMaterials[0];
        float baseAlpha = Mathf.Max(0.0001f, state.baseColor.a);
        return Mathf.Clamp01(GetMaterialColor(state.material).a / baseAlpha);
    }

    private static Transform CreateAnchor(string name, Transform parent, Vector3 localPosition)
    {
        GameObject anchorObject = new GameObject(name);
        Transform anchor = anchorObject.transform;
        anchor.SetParent(parent, false);
        anchor.localPosition = localPosition;
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;
        return anchor;
    }

    public int KonamiLevelNumber => 3;

    public bool ActivateKonamiLevelEffect()
    {
        if (!isActiveAndEnabled || konamiDanceClip == null)
        {
            Debug.LogWarning(
                "KONAMI NIVEL 3: el visual no esta activo o falta la animacion de baile."
            );
            return false;
        }

        EnsureVisuals();
        if (userGroup == null || userModelInstance == null || userAnimator == null)
        {
            Debug.LogWarning("KONAMI NIVEL 3: no se encontro el Usuario animable.");
            return false;
        }

        if (konamiEffectActive) return true;

        userScaleBeforeKonami = userGroup.localScale.sqrMagnitude > 0.0001f
            ? userGroup.localScale
            : Vector3.one;
        ResetUserGestureInstantly();
        PrepareUserRainbowMaterials();
        StartKonamiDance();

        konamiEffectActive = true;
        konamiEffectRoutine = StartCoroutine(KonamiEffectRoutine());
        Debug.Log(
            "KONAMI NIVEL 3: Usuario x3, baile y color multicolor activados; " +
            "el progreso de niveles no fue modificado."
        );
        return true;
    }

    private IEnumerator KonamiEffectRoutine()
    {
        Vector3 targetScale = GetUserVisibleScale();
        Vector3 startScale = userGroup.localScale.sqrMagnitude > 0.0001f
            ? userGroup.localScale
            : Vector3.one;
        float elapsed = 0f;
        const float growDuration = 0.48f;
        while (elapsed < growDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            userGroup.localScale = Vector3.LerpUnclamped(
                startScale,
                targetScale,
                Smooth01(elapsed / growDuration)
            );
            ApplyUserRainbow();
            yield return null;
        }

        while (konamiEffectActive)
        {
            userGroup.localScale = targetScale;
            ApplyUserRainbow();

            if (konamiDancePlayable.IsValid() && konamiDanceClip != null && konamiDanceClip.length > 0f)
            {
                double time = konamiDancePlayable.GetTime();
                if (time >= konamiDanceClip.length)
                {
                    konamiDancePlayable.SetTime(time % konamiDanceClip.length);
                    konamiDancePlayable.SetDone(false);
                }
            }

            yield return new WaitForEndOfFrame();
            if (userModelInstance != null)
            {
                userModelInstance.transform.localPosition = userModelOriginalLocalPosition;
                userModelInstance.transform.localRotation = userModelOriginalLocalRotation;
            }
        }
    }

    private void StartKonamiDance()
    {
        if (konamiDanceGraph.IsValid()) konamiDanceGraph.Destroy();

        konamiDanceGraph = PlayableGraph.Create("AlgoLab_Konami_Nivel3_Baile");
        konamiDanceGraph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
        konamiDancePlayable = AnimationClipPlayable.Create(konamiDanceGraph, konamiDanceClip);
        konamiDancePlayable.SetApplyFootIK(false);
        konamiDancePlayable.SetApplyPlayableIK(false);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(
            konamiDanceGraph,
            "Usuario_Dancing_Twerk",
            userAnimator
        );
        output.SetSourcePlayable(konamiDancePlayable);
        konamiDanceGraph.Play();
    }

    private void PrepareUserRainbowMaterials()
    {
        if (userRendererStates.Count > 0 || userModelInstance == null) return;

        Renderer[] renderers = userModelInstance.GetComponentsInChildren<Renderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            Material[] originals = renderers[r].sharedMaterials;
            Material[] rainbow = new Material[originals.Length];
            Color[] colors = new Color[originals.Length];
            for (int m = 0; m < originals.Length; m++)
            {
                Material source = originals[m];
                if (source == null) continue;
                Material runtime = new Material(source)
                {
                    name = source.name + "_KonamiArcoiris"
                };
                rainbow[m] = runtime;
                colors[m] = GetMaterialColor(runtime);
            }

            renderers[r].sharedMaterials = rainbow;
            userRendererStates.Add(new UserRendererState
            {
                renderer = renderers[r],
                originalMaterials = originals,
                rainbowMaterials = rainbow,
                originalColors = colors
            });
        }
    }

    private void ApplyUserRainbow()
    {
        float baseHue = Mathf.Repeat(Time.unscaledTime * konamiRainbowSpeed, 1f);
        for (int r = 0; r < userRendererStates.Count; r++)
        {
            UserRendererState state = userRendererStates[r];
            if (state == null || state.rainbowMaterials == null) continue;
            for (int m = 0; m < state.rainbowMaterials.Length; m++)
            {
                Material material = state.rainbowMaterials[m];
                if (material == null) continue;
                Color rainbow = Color.HSVToRGB(
                    Mathf.Repeat(baseHue + r * 0.19f + m * 0.11f, 1f),
                    0.82f,
                    1f
                );
                rainbow.a = state.originalColors[m].a;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", rainbow);
                if (material.HasProperty("_Color")) material.SetColor("_Color", rainbow);
            }
        }
    }

    private void StopKonamiEffect(bool restoreScale)
    {
        bool wasActive = konamiEffectActive || konamiEffectRoutine != null || konamiDanceGraph.IsValid();
        konamiEffectActive = false;
        if (konamiEffectRoutine != null)
        {
            StopCoroutine(konamiEffectRoutine);
            konamiEffectRoutine = null;
        }
        if (konamiDanceGraph.IsValid()) konamiDanceGraph.Destroy();

        if (userAnimator != null)
        {
            userAnimator.Rebind();
            userAnimator.Update(0f);
        }
        if (userModelInstance != null)
        {
            userModelInstance.transform.localPosition = userModelOriginalLocalPosition;
            userModelInstance.transform.localRotation = userModelOriginalLocalRotation;
        }
        if (restoreScale && wasActive && userGroup != null)
        {
            userGroup.localScale = userScaleBeforeKonami;
        }

        RestoreUserMaterialColors();
    }

    private void RestoreUserMaterialColors()
    {
        for (int r = 0; r < userRendererStates.Count; r++)
        {
            UserRendererState state = userRendererStates[r];
            if (state == null || state.rainbowMaterials == null) continue;
            for (int m = 0; m < state.rainbowMaterials.Length; m++)
            {
                Material material = state.rainbowMaterials[m];
                if (material == null) continue;
                Color color = state.originalColors[m];
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            }
        }
    }

    private void ReleaseUserRainbowMaterials()
    {
        for (int r = 0; r < userRendererStates.Count; r++)
        {
            UserRendererState state = userRendererStates[r];
            if (state == null) continue;
            if (state.renderer != null) state.renderer.sharedMaterials = state.originalMaterials;
            if (state.rainbowMaterials == null) continue;
            for (int m = 0; m < state.rainbowMaterials.Length; m++)
            {
                if (state.rainbowMaterials[m] != null)
                {
                    DestroyGeneratedObject(state.rainbowMaterials[m]);
                }
            }
        }
        userRendererStates.Clear();
    }

    private Vector3 GetUserVisibleScale()
    {
        return Vector3.one * (konamiEffectActive ? Mathf.Max(1f, konamiScaleMultiplier) : 1f);
    }

    private void InitializeUserRig(GameObject personModel)
    {
        if (personModel == null) return;

        userLeftArm = FindDescendant(personModel.transform, "mixamorig:LeftArm");
        userLeftForeArm = FindDescendant(personModel.transform, "mixamorig:LeftForeArm");
        userLeftHand = FindDescendant(personModel.transform, "mixamorig:LeftHand");
        userRightArm = FindDescendant(personModel.transform, "mixamorig:RightArm");
        userRightForeArm = FindDescendant(personModel.transform, "mixamorig:RightForeArm");
        userRightHand = FindDescendant(personModel.transform, "mixamorig:RightHand");

        userRigReady =
            userLeftArm != null && userLeftForeArm != null && userLeftHand != null &&
            userRightArm != null && userRightForeArm != null && userRightHand != null;
        if (!userRigReady)
        {
            Debug.LogWarning("ENCAPSULAMIENTO: el modelo Usuario no contiene el rig completo de brazos.");
            return;
        }

        userLeftArmIdleRotation = userLeftArm.localRotation;
        userLeftForeArmIdleRotation = userLeftForeArm.localRotation;
        userLeftHandIdleRotation = userLeftHand.localRotation;
        userRightArmIdleRotation = userRightArm.localRotation;
        userRightForeArmIdleRotation = userRightForeArm.localRotation;
        userRightHandIdleRotation = userRightHand.localRotation;
    }

    private void StartUserGesture(Transform target, float holdDuration)
    {
        if (konamiEffectActive || !userRigReady || target == null || !isActiveAndEnabled) return;
        if (userGestureRoutine != null) StopCoroutine(userGestureRoutine);
        userGestureRoutine = StartCoroutine(AnimateUserGesture(target.position, holdDuration));
    }

    private IEnumerator AnimateUserGesture(Vector3 worldTarget, float holdDuration)
    {
        Transform arm;
        Transform foreArm;
        Transform hand;
        Quaternion idleArm;
        Quaternion idleForeArm;
        Quaternion idleHand;
        SelectGestureArm(worldTarget, out arm, out foreArm, out hand,
            out idleArm, out idleForeArm, out idleHand);

        Quaternion currentArm = arm.localRotation;
        Quaternion currentForeArm = foreArm.localRotation;
        Quaternion currentHand = hand.localRotation;
        Quaternion targetArm;
        Quaternion targetForeArm;
        Quaternion targetHand;
        CalculateGesturePose(arm, foreArm, hand, idleArm, idleForeArm, idleHand,
            worldTarget, out targetArm, out targetForeArm, out targetHand);

        yield return AnimateUserBones(arm, foreArm, hand,
            currentArm, currentForeArm, currentHand,
            targetArm, targetForeArm, targetHand, 0.32f);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, holdDuration));
        yield return AnimateUserBones(arm, foreArm, hand,
            arm.localRotation, foreArm.localRotation, hand.localRotation,
            idleArm, idleForeArm, idleHand, 0.32f);
        userGestureRoutine = null;
    }

    private IEnumerator AnimateUserBones(
        Transform arm, Transform foreArm, Transform hand,
        Quaternion startArm, Quaternion startForeArm, Quaternion startHand,
        Quaternion targetArm, Quaternion targetForeArm, Quaternion targetHand,
        float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth01(elapsed / duration);
            arm.localRotation = Quaternion.SlerpUnclamped(startArm, targetArm, t);
            foreArm.localRotation = Quaternion.SlerpUnclamped(startForeArm, targetForeArm, t);
            hand.localRotation = Quaternion.SlerpUnclamped(startHand, targetHand, t);
            yield return null;
        }

        arm.localRotation = targetArm;
        foreArm.localRotation = targetForeArm;
        hand.localRotation = targetHand;
    }

    private void SetUserGestureInstantly(Vector3 worldTarget)
    {
        if (konamiEffectActive || !userRigReady) return;
        ResetUserGestureInstantly();

        Transform arm;
        Transform foreArm;
        Transform hand;
        Quaternion idleArm;
        Quaternion idleForeArm;
        Quaternion idleHand;
        SelectGestureArm(worldTarget, out arm, out foreArm, out hand,
            out idleArm, out idleForeArm, out idleHand);
        Quaternion targetArm;
        Quaternion targetForeArm;
        Quaternion targetHand;
        CalculateGesturePose(arm, foreArm, hand, idleArm, idleForeArm, idleHand,
            worldTarget, out targetArm, out targetForeArm, out targetHand);
        arm.localRotation = targetArm;
        foreArm.localRotation = targetForeArm;
        hand.localRotation = targetHand;
    }

    private void ResetUserGestureInstantly()
    {
        if (userGestureRoutine != null)
        {
            StopCoroutine(userGestureRoutine);
            userGestureRoutine = null;
        }
        if (!userRigReady) return;

        userLeftArm.localRotation = userLeftArmIdleRotation;
        userLeftForeArm.localRotation = userLeftForeArmIdleRotation;
        userLeftHand.localRotation = userLeftHandIdleRotation;
        userRightArm.localRotation = userRightArmIdleRotation;
        userRightForeArm.localRotation = userRightForeArmIdleRotation;
        userRightHand.localRotation = userRightHandIdleRotation;
    }

    private void SelectGestureArm(
        Vector3 worldTarget,
        out Transform arm, out Transform foreArm, out Transform hand,
        out Quaternion idleArm, out Quaternion idleForeArm, out Quaternion idleHand)
    {
        bool useLeft =
            (worldTarget - userLeftArm.position).sqrMagnitude <=
            (worldTarget - userRightArm.position).sqrMagnitude;
        arm = useLeft ? userLeftArm : userRightArm;
        foreArm = useLeft ? userLeftForeArm : userRightForeArm;
        hand = useLeft ? userLeftHand : userRightHand;
        idleArm = useLeft ? userLeftArmIdleRotation : userRightArmIdleRotation;
        idleForeArm = useLeft ? userLeftForeArmIdleRotation : userRightForeArmIdleRotation;
        idleHand = useLeft ? userLeftHandIdleRotation : userRightHandIdleRotation;
    }

    private static void CalculateGesturePose(
        Transform arm, Transform foreArm, Transform hand,
        Quaternion idleArm, Quaternion idleForeArm, Quaternion idleHand,
        Vector3 worldTarget,
        out Quaternion targetArm, out Quaternion targetForeArm, out Quaternion targetHand)
    {
        Quaternion previousArm = arm.localRotation;
        Quaternion previousForeArm = foreArm.localRotation;
        Quaternion previousHand = hand.localRotation;
        arm.localRotation = idleArm;
        foreArm.localRotation = idleForeArm;
        hand.localRotation = idleHand;

        Vector3 target = worldTarget + Vector3.up * 0.04f;
        PointBoneTowards(arm, foreArm, target - arm.position);
        targetArm = arm.localRotation;
        PointBoneTowards(foreArm, hand, target - foreArm.position);
        targetForeArm = foreArm.localRotation;
        targetHand = idleHand * Quaternion.Euler(0f, 0f, -12f);

        arm.localRotation = previousArm;
        foreArm.localRotation = previousForeArm;
        hand.localRotation = previousHand;
    }

    private TextMeshPro CreateText(string name, Transform parent, string content,
        Vector3 localPosition, float worldWidth, float worldHeight, Color color, float fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        const float textScale = 0.025f;
        textObject.transform.localScale = Vector3.one * textScale;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        if (fontAsset != null) text.font = fontAsset;
        text.text = content;
        text.color = color;
        text.fontSize = fontSize * 12f;
        text.alignment = TextAlignmentOptions.Center;
        text.overflowMode = TextOverflowModes.Overflow;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.sizeDelta = new Vector2(worldWidth / textScale, worldHeight / textScale);
        return text;
    }

    private Transform CreateArrow(string name, Transform parent, Vector3 start, Vector3 end, Material material)
    {
        Transform root = CreateAnchor(name, parent, start);
        Vector3 direction = end - start;
        float length = new Vector2(direction.x, direction.y).magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        root.localRotation = Quaternion.Euler(0f, 0f, angle);

        const float headLength = 0.055f;
        const float thickness = 0.014f;
        float bodyLength = Mathf.Max(0.01f, length - headLength);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Cuerpo";
        body.transform.SetParent(root, false);
        body.transform.localPosition = new Vector3(bodyLength * 0.5f, 0f, 0f);
        body.transform.localScale = new Vector3(bodyLength, thickness, 0.009f);
        Collider bodyCollider = body.GetComponent<Collider>();
        if (bodyCollider != null) DestroyGeneratedObject(bodyCollider);
        MeshRenderer bodyRenderer = body.GetComponent<MeshRenderer>();
        if (bodyRenderer != null && material != null) bodyRenderer.sharedMaterial = material;

        GameObject head = new GameObject("Punta");
        head.transform.SetParent(root, false);
        MeshFilter filter = head.AddComponent<MeshFilter>();
        MeshRenderer renderer = head.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh { name = name + "_PuntaMesh" };
        mesh.vertices = new[]
        {
            new Vector3(bodyLength, thickness * 2.4f, 0f),
            new Vector3(length, 0f, 0f),
            new Vector3(bodyLength, -thickness * 2.4f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        filter.sharedMesh = mesh;
        if (material != null) renderer.sharedMaterial = material;
        return root;
    }

    private GameObject CreateNormalizedModel(GameObject prefab, Transform anchor,
        float desiredHeight, Quaternion localRotation, bool applyNaturalPersonPose)
    {
        if (prefab == null) return null;

        GameObject model = Instantiate(prefab, anchor);
        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = localRotation;
        model.transform.localScale = Vector3.one;

        if (applyNaturalPersonPose) ApplyNaturalPersonPose(model.transform);

        Bounds bounds;
        if (!TryGetLocalRendererBounds(model.transform, anchor, out bounds) || bounds.size.y < 0.0001f)
        {
            return model;
        }

        float scale = desiredHeight / bounds.size.y;
        model.transform.localScale *= scale;
        if (!TryGetLocalRendererBounds(model.transform, anchor, out bounds)) return model;

        Vector3 position = model.transform.localPosition;
        position.x -= bounds.center.x;
        position.y -= bounds.min.y;
        position.z -= bounds.center.z;
        model.transform.localPosition = position;
        return model;
    }

    private static void ApplyNaturalPersonPose(Transform modelRoot)
    {
        Transform leftArm = FindDescendant(modelRoot, "mixamorig:LeftArm");
        Transform rightArm = FindDescendant(modelRoot, "mixamorig:RightArm");
        Transform leftForeArm = FindDescendant(modelRoot, "mixamorig:LeftForeArm");
        Transform rightForeArm = FindDescendant(modelRoot, "mixamorig:RightForeArm");
        Transform leftHand = FindDescendant(modelRoot, "mixamorig:LeftHand");
        Transform rightHand = FindDescendant(modelRoot, "mixamorig:RightHand");
        PointBoneTowards(leftArm, leftForeArm, modelRoot.TransformDirection(new Vector3(-0.12f, -1f, 0.06f).normalized));
        PointBoneTowards(rightArm, rightForeArm, modelRoot.TransformDirection(new Vector3(0.12f, -1f, 0.06f).normalized));
        PointBoneTowards(leftForeArm, leftHand, modelRoot.TransformDirection(new Vector3(-0.06f, -1f, 0.12f).normalized));
        PointBoneTowards(rightForeArm, rightHand, modelRoot.TransformDirection(new Vector3(0.06f, -1f, 0.12f).normalized));
    }

    private static void PointBoneTowards(Transform bone, Transform child, Vector3 desiredDirection)
    {
        if (bone == null || child == null) return;
        Vector3 currentDirection = child.position - bone.position;
        if (currentDirection.sqrMagnitude < 0.000001f) return;
        bone.rotation = Quaternion.FromToRotation(currentDirection.normalized, desiredDirection.normalized) * bone.rotation;
    }

    private static Transform FindDescendant(Transform root, string exactName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == exactName) return transforms[i];
        }
        return null;
    }

    private static bool TryGetLocalRendererBounds(Transform visualRoot, Transform relativeTo, out Bounds localBounds)
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
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                Vector3 localCorner = relativeTo.InverseTransformPoint(corner);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else localBounds.Encapsulate(localCorner);
            }
        }
        return hasBounds;
    }

    private void StartNarration(AudioClip clip, float volume)
    {
        if (narrationSource == null || clip == null)
        {
            Debug.LogError("ENCAPSULAMIENTO: falta AudioSource o un audio entre 4 y 10.");
            return;
        }
        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.volume = volume;
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

    private IEnumerator AnimateScale(Transform target, Vector3 targetScale, float duration)
    {
        if (target == null) yield break;
        Vector3 startScale = target.localScale;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localScale = Vector3.LerpUnclamped(startScale, targetScale, Smooth01(elapsed / duration));
            yield return null;
        }
        target.localScale = targetScale;
    }

    private IEnumerator AnimateMove(Transform target, Vector3 targetPosition, float duration)
    {
        if (target == null) yield break;
        Vector3 startPosition = target.localPosition;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, Smooth01(elapsed / duration));
            yield return null;
        }
        target.localPosition = targetPosition;
    }

    private IEnumerator Pulse(Transform target, float scaleMultiplier)
    {
        if (target == null) yield break;
        Vector3 baseScale = target.localScale;
        yield return AnimateScale(target, baseScale * scaleMultiplier, focusDuration * 0.5f);
        yield return AnimateScale(target, baseScale, focusDuration * 0.5f);
    }

    private IEnumerator Shake(Transform target, float distance, float duration)
    {
        if (target == null) yield break;
        Vector3 origin = target.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            target.localPosition = origin + Vector3.right * (Mathf.Sin(elapsed * 62f) * distance * fade);
            yield return null;
        }
        target.localPosition = origin;
    }

    private static void SetScale(Transform target, Vector3 scale)
    {
        if (target != null) target.localScale = scale;
    }

    private static void DestroyGeneratedObject(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
