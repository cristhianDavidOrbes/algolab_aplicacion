using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class AlgoLabLevel3EncapsulationSetup
{
    private const string PrefabPath =
        "Assets/__Algolab/Prefabs/Objects/level3/EncapsulationTheme_Audios01_03.prefab";
    private const string ModelPath =
        "Assets/__Algolab/Prefabs/Objects/level3/Pillars/Column_Quaternius.fbx";
    private const string GoldModelPath =
        "Assets/__Algolab/Prefabs/Objects/level3/BankExample/Gold/Gold_Ingots.fbx";
    private const string SafeModelPath =
        "Assets/__Algolab/Prefabs/Objects/level3/BankExample/Safe/AlgoLabAnimatedSafe.prefab";
    private const string PersonModelPath =
        "Assets/__Algolab/Prefabs/Objects/level3/BankExample/Person/Rigged_Character.fbx";
    private const string DanceAnimationPath =
        "Assets/__Algolab/Animations/level3-encapsulation/Dancing_Twerk.fbx";
    private const string FontPath =
        "Assets/__Algolab/Fonts/jd_code SDF.asset";
    private const string MaterialFolder =
        "Assets/__Algolab/Materials/level3-encapsulamiento";

    private static readonly string[] PillarIconPaths =
    {
        "Assets/__Algolab/Image/level3-encapsulamiento/pillar-icons/01_encapsulamiento.png",
        "Assets/__Algolab/Image/level3-encapsulamiento/pillar-icons/02_abstraccion.png",
        "Assets/__Algolab/Image/level3-encapsulamiento/pillar-icons/03_herencia.png",
        "Assets/__Algolab/Image/level3-encapsulamiento/pillar-icons/04_polimorfismo.png"
    };

    private static readonly string[] AccessIconPaths =
    {
        "Assets/__Algolab/Image/level3-encapsulamiento/access-icons/01_publico.png",
        "Assets/__Algolab/Image/level3-encapsulamiento/access-icons/02_privado.png",
        "Assets/__Algolab/Image/level3-encapsulamiento/access-icons/03_protegido.png"
    };

    private static readonly string[] AudioPaths =
    {
        "Assets/__Algolab/Audio/level3-tema/01_intro_cuatro_pilares.mp3",
        "Assets/__Algolab/Audio/level3-tema/02_que_es_encapsulamiento.mp3",
        "Assets/__Algolab/Audio/level3-tema/03_tipos_de_acceso.mp3",
        "Assets/__Algolab/Audio/level3-tema/04_ejemplo_cuenta_bancaria.mp3",
        "Assets/__Algolab/Audio/level3-tema/05_valor_privado.mp3",
        "Assets/__Algolab/Audio/level3-tema/06_metodos_publicos.mp3",
        "Assets/__Algolab/Audio/level3-tema/07_depositar_sueldo.mp3",
        "Assets/__Algolab/Audio/level3-tema/08_intento_modificar_valor.mp3",
        "Assets/__Algolab/Audio/level3-tema/09_acceso_controlado.mp3",
        "Assets/__Algolab/Audio/level3-tema/10_conclusion_encapsulamiento.mp3"
    };

    [MenuItem("Tools/AlgoLab/Nivel 3/Configurar tema audios 1 a 10")]
    private static void ConfigureFromMenu()
    {
        ConfigureBatch();
    }

    public static void ConfigureBatch()
    {
        ConfigureDanceAnimationImporter();
        ValidateRequiredAssets();
        ConfigureTextureImporters(PillarIconPaths);
        ConfigureTextureImporters(AccessIconPaths);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Material[] pillarMaterials = CreateIconMaterials(PillarIconPaths, "Pilar");
        Material[] accessMaterials = CreateIconMaterials(AccessIconPaths, "Acceso");
        Material panelMaterial = CreateColorMaterial("Diagrama_Fondo", new Color(0.045f, 0.06f, 0.09f, 1f));
        Material headerMaterial = CreateColorMaterial("Diagrama_Cabecera", new Color(0.04f, 0.42f, 0.32f, 1f));
        Material publicMaterial = CreateColorMaterial("Diagrama_Publico", new Color(0.06f, 0.48f, 0.34f, 1f));
        Material privateMaterial = CreateColorMaterial("Diagrama_Privado", new Color(0.60f, 0.10f, 0.14f, 1f));
        Material neutralMaterial = CreateColorMaterial("Diagrama_Neutro", new Color(0.12f, 0.18f, 0.27f, 1f));
        GameObject visualPrefab = CreateOrUpdateVisualPrefab(
            pillarMaterials,
            accessMaterials,
            panelMaterial,
            headerMaterial,
            publicMaterial,
            privateMaterial,
            neutralMaterial
        );

        int configuredScenes = 0;
        int configuredPillarControllers = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrWhiteSpace(buildScene.path))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            AlgoLabProgressPanel[] progressPanels =
                UnityEngine.Object.FindObjectsByType<AlgoLabProgressPanel>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            AlgoLabPillarLevelController[] pillarControllers =
                UnityEngine.Object.FindObjectsByType<AlgoLabPillarLevelController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            if (pillarControllers.Length == 0 && progressPanels.Length > 0)
            {
                AlgoLabPillarLevelController createdController =
                    progressPanels[0].GetComponent<AlgoLabPillarLevelController>();
                if (createdController == null)
                {
                    createdController = progressPanels[0].gameObject.AddComponent<AlgoLabPillarLevelController>();
                }

                createdController.AsegurarNivelesPorDefecto();
                pillarControllers = new[] { createdController };
            }

            if (pillarControllers.Length == 0)
            {
                continue;
            }

            AlgoLabEncapsulationThemeController themeController =
                UnityEngine.Object.FindFirstObjectByType<AlgoLabEncapsulationThemeController>(
                    FindObjectsInactive.Include
                );

            if (themeController == null)
            {
                GameObject controllerObject = new GameObject("AlgoLab_EncapsulationThemeController");
                themeController = controllerObject.AddComponent<AlgoLabEncapsulationThemeController>();
            }

            themeController.spawnManager =
                UnityEngine.Object.FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                    FindObjectsInactive.Include
                );
            themeController.themeVisualPrefab = visualPrefab;
            themeController.spawnScale = Vector3.one;
            themeController.maximumConnectWait = 4f;
            themeController.showDebug = false;
            EditorUtility.SetDirty(themeController);

            for (int i = 0; i < pillarControllers.Length; i++)
            {
                pillarControllers[i].encapsulationThemeController = themeController;
                EditorUtility.SetDirty(pillarControllers[i]);
                configuredPillarControllers++;
            }

            for (int i = 0; i < progressPanels.Length; i++)
            {
                if (progressPanels[i].pillarLevelController == null && pillarControllers.Length > 0)
                {
                    progressPanels[i].pillarLevelController = pillarControllers[0];
                    EditorUtility.SetDirty(progressPanels[i]);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, buildScene.path))
            {
                throw new InvalidOperationException(
                    "No se pudo guardar la configuracion del nivel 3 en " + buildScene.path
                );
            }

            configuredScenes++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            "ALGOLAB NIVEL 3: tema de audios 1 a 10 configurado. Escenas=" +
            configuredScenes + ", controladores=" + configuredPillarControllers +
            ", prefab=" + PrefabPath
        );
    }

    public static void DumpPillarModelBoundsBatch()
    {
        GameObject modelPrefab = RequireAsset<GameObject>(ModelPath);
        GameObject instance = UnityEngine.Object.Instantiate(modelPrefab);
        try
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Bounds combined = new Bounds();
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds bounds = renderers[i].bounds;
                Debug.Log(
                    "ALGOLAB COLUMN BOUNDS renderer=" + renderers[i].name +
                    " center=" + bounds.center.ToString("F4") +
                    " size=" + bounds.size.ToString("F4")
                );

                if (!hasBounds)
                {
                    combined = bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(bounds);
                }
            }

            Debug.Log(
                "ALGOLAB COLUMN BOUNDS COMBINED center=" + combined.center.ToString("F4") +
                " size=" + combined.size.ToString("F4") +
                " min=" + combined.min.ToString("F4") +
                " max=" + combined.max.ToString("F4")
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    public static void RenderBankDiagramPreviewBatch()
    {
        GameObject prefab = RequireAsset<GameObject>(PrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        GameObject cameraObject = new GameObject("PreviewCamera");
        GameObject lightObject = new GameObject("PreviewLight");
        RenderTexture renderTexture = null;
        Texture2D image = null;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            AlgoLabEncapsulationBankExampleVisual bankVisual =
                instance.GetComponent<AlgoLabEncapsulationBankExampleVisual>();
            if (bankVisual == null)
            {
                throw new InvalidOperationException("El prefab no contiene el visual bancario.");
            }
            if (bankVisual.KonamiLevelNumber != 3 || bankVisual.konamiDanceClip == null ||
                bankVisual.konamiScaleMultiplier < 2.99f)
            {
                throw new InvalidOperationException(
                    "El efecto Konami del nivel 3 no tiene baile o escala x3 configurados."
                );
            }

            EditorCurveBinding[] danceBindings =
                AnimationUtility.GetCurveBindings(bankVisual.konamiDanceClip);
            bool danceMovesRig = false;
            for (int i = 0; i < danceBindings.Length; i++)
            {
                if (danceBindings[i].path.StartsWith("mixamorig:Hips", StringComparison.OrdinalIgnoreCase))
                {
                    danceMovesRig = true;
                    break;
                }
            }
            if (!danceMovesRig)
            {
                throw new InvalidOperationException(
                    "Dancing_Twerk no contiene curvas compatibles con el esqueleto Mixamo del Usuario."
                );
            }
            bankVisual.ShowCompleteDiagramInstantly();

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0.15f, 0.02f, -1.5f);
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = 0.57f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.018f, 0.03f, 1f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

            renderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            image = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outputPath = Path.Combine(projectRoot, "Logs", "level3-bank-diagram-preview.png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            Debug.Log("ALGOLAB NIVEL 3: vista previa guardada en " + outputPath);
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (renderTexture != null) renderTexture.Release();
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    public static void DumpBankModelHierarchyBatch()
    {
        string[] paths = { PersonModelPath, SafeModelPath, GoldModelPath };
        for (int p = 0; p < paths.Length; p++)
        {
            GameObject prefab = RequireAsset<GameObject>(paths[p]);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Debug.Log("ALGOLAB MODELO: " + paths[p]);
                Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Debug.Log("ALGOLAB HUESO/OBJETO: " + transforms[i].name);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }

    public static void ValidateBankPanelIntegrationBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/version_estable14.unity", OpenSceneMode.Single);
        GameObject prefab = RequireAsset<GameObject>(PrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        instance.name = "Validacion_Nivel3_Encapsulamiento";

        try
        {
            AlgoLabEncapsulationBankExampleVisual bankVisual =
                instance.GetComponent<AlgoLabEncapsulationBankExampleVisual>();
            if (bankVisual == null)
            {
                throw new InvalidOperationException("El prefab no contiene el visual bancario.");
            }

            bankVisual.ShowCompleteDiagramInstantly();

            AlgoLabClassDiagramController controller =
                UnityEngine.Object.FindFirstObjectByType<AlgoLabClassDiagramController>(FindObjectsInactive.Include);
            if (controller == null || controller.cardsContainer == null || controller.cardPrefab == null)
            {
                throw new InvalidOperationException("El panel de diagramas no esta completamente configurado.");
            }

            AlgoLabClassDiagramCardUI userCard = controller.ObtenerTarjetaPorNombreClase("Usuario");
            AlgoLabClassDiagramCardUI accountCard = controller.ObtenerTarjetaPorNombreClase("Cuenta");
            if (userCard == null || accountCard == null)
            {
                throw new InvalidOperationException("El panel no creo exactamente las clases Usuario y Cuenta.");
            }
            if (!userCard.resaltarSoloSignosAcceso || !accountCard.resaltarSoloSignosAcceso)
            {
                throw new InvalidOperationException(
                    "Usuario y Cuenta deben resaltar solamente los signos UML."
                );
            }

            string userAttributes = userCard.textoAtributos != null ? userCard.textoAtributos.text : "";
            string userMethods = userCard.textoMetodos != null ? userCard.textoMetodos.text : "";
            string attributes = accountCard.textoAtributos != null ? accountCard.textoAtributos.text : "";
            string methods = accountCard.textoMetodos != null ? accountCard.textoMetodos.text : "";
            if (!userAttributes.Contains("nombre") || !userAttributes.Contains("sueldo") ||
                !userMethods.Contains("depositar") || !userMethods.Contains("retirar"))
            {
                throw new InvalidOperationException("Usuario no muestra sus atributos y metodos educativos.");
            }
            if (!attributes.Contains("valor") || !attributes.Contains("FF6B73"))
            {
                throw new InvalidOperationException("Cuenta no marca valor unicamente con el signo privado rojo.");
            }
            if (!methods.Contains("63D9A6") ||
                !methods.Contains("depositar") || !methods.Contains("retirar") ||
                !methods.Contains("consultar"))
            {
                throw new InvalidOperationException("Cuenta no muestra sus tres metodos y signos publicos.");
            }
            if (attributes.Contains("[PRIVADO]") || methods.Contains("[PUBLICO]") ||
                attributes.Contains("<color=#FF6B73>valor") ||
                methods.Contains("<color=#63D9A6>depositar") ||
                userAttributes.Contains("100.000") || methods.Contains("cantidad"))
            {
                throw new InvalidOperationException(
                    "El color de acceso debe aplicarse solo al signo, no a toda la linea del diagrama."
                );
            }

            AlgoLabObjetoEducativo[] diagramData =
                instance.GetComponentsInChildren<AlgoLabObjetoEducativo>(true);
            if (diagramData.Length != 2)
            {
                throw new InvalidOperationException("El ejemplo debe aportar solo dos clases al panel; encontro " + diagramData.Length + ".");
            }

            Transform physicalRoot = instance.transform.Find(
                "VisualesFisicos_CuentaBancaria_Audios04_10/ObjetosFisicos");
            if (physicalRoot == null ||
                physicalRoot.Find("Objeto_Usuario") == null ||
                physicalRoot.Find("Objeto_CuentaBancaria") == null ||
                physicalRoot.Find("Objeto_CuentaBancaria/Variable_valor_Oro_DentroCajaFuerte") == null)
            {
                throw new InvalidOperationException("Falta Usuario, CuentaBancaria o el oro dentro de la caja fuerte.");
            }

            Transform accountObject = physicalRoot.Find("Objeto_CuentaBancaria");
            Transform userObject = physicalRoot.Find("Objeto_Usuario");
            AlgoLabAnimatedSafe animatedSafe =
                accountObject.GetComponentInChildren<AlgoLabAnimatedSafe>(true);
            if (animatedSafe == null || animatedSafe.doorPivot == null ||
                animatedSafe.dialPivot == null || animatedSafe.handlePivot == null ||
                animatedSafe.boltRoot == null)
            {
                throw new InvalidOperationException(
                    "La Cuenta no contiene la caja fuerte animada completa."
                );
            }

            Transform leftArm = null;
            Transform leftForeArm = null;
            Transform rightArm = null;
            Transform rightForeArm = null;
            Transform[] userTransforms = userObject.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < userTransforms.Length; i++)
            {
                if (userTransforms[i].name == "mixamorig:LeftArm") leftArm = userTransforms[i];
                else if (userTransforms[i].name == "mixamorig:LeftForeArm") leftForeArm = userTransforms[i];
                else if (userTransforms[i].name == "mixamorig:RightArm") rightArm = userTransforms[i];
                else if (userTransforms[i].name == "mixamorig:RightForeArm") rightForeArm = userTransforms[i];
            }
            if (leftArm == null || leftForeArm == null || rightArm == null || rightForeArm == null)
            {
                throw new InvalidOperationException("El Usuario no conserva el esqueleto necesario para gesticular.");
            }
            Animator userAnimator = userObject.GetComponentInChildren<Animator>(true);
            if (userAnimator == null)
            {
                throw new InvalidOperationException(
                    "El Usuario no contiene el Animator requerido por el baile Konami."
                );
            }
            Transform[] animatedBones = userAnimator.GetComponentsInChildren<Transform>(true);
            Quaternion[] rotationsBeforeDance = new Quaternion[animatedBones.Length];
            Vector3[] positionsBeforeDance = new Vector3[animatedBones.Length];
            for (int i = 0; i < animatedBones.Length; i++)
            {
                rotationsBeforeDance[i] = animatedBones[i].localRotation;
                positionsBeforeDance[i] = animatedBones[i].localPosition;
            }

            float maximumDanceChange = 0f;
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(
                    userAnimator.gameObject,
                    bankVisual.konamiDanceClip,
                    Mathf.Min(0.7f, bankVisual.konamiDanceClip.length * 0.37f)
                );
                AnimationMode.EndSampling();

                for (int i = 0; i < animatedBones.Length; i++)
                {
                    if (animatedBones[i].name.IndexOf("mixamorig", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    maximumDanceChange = Mathf.Max(
                        maximumDanceChange,
                        Quaternion.Angle(rotationsBeforeDance[i], animatedBones[i].localRotation),
                        Vector3.Distance(positionsBeforeDance[i], animatedBones[i].localPosition) * 100f
                    );
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            }
            if (maximumDanceChange < 0.5f)
            {
                throw new InvalidOperationException(
                    "El clip Konami no produjo movimiento real al muestrearlo sobre el Usuario."
                );
            }
            Vector3 targetDirectionLeft = (accountObject.position - leftArm.position).normalized;
            Vector3 targetDirectionRight = (accountObject.position - rightArm.position).normalized;
            float leftPointing = Vector3.Dot((leftForeArm.position - leftArm.position).normalized, targetDirectionLeft);
            float rightPointing = Vector3.Dot((rightForeArm.position - rightArm.position).normalized, targetDirectionRight);
            if (Mathf.Max(leftPointing, rightPointing) < 0.72f)
            {
                throw new InvalidOperationException("El gesto del Usuario no apunta claramente hacia la Cuenta.");
            }

            Transform publicArrow = physicalRoot.Find("Flecha_Objeto_AccesoPublico");
            Transform blockedSign = physicalRoot.Find("Bloqueo_Objeto_X");
            if (publicArrow == null || blockedSign == null ||
                publicArrow.localPosition.x <= userObject.localPosition.x + 0.18f ||
                publicArrow.localPosition.x >= accountObject.localPosition.x - 0.28f ||
                Mathf.Abs(blockedSign.localPosition.y - publicArrow.localPosition.y) < 0.05f)
            {
                throw new InvalidOperationException(
                    "La flecha y el signo de bloqueo no estan centrados y separados de los objetos."
                );
            }

            animatedSafe.SetOpenInstantly(false);
            Quaternion closedRotation = animatedSafe.doorPivot.localRotation;
            Vector3 extendedBolts = animatedSafe.boltRoot.localPosition;
            animatedSafe.SetOpenInstantly(true);
            if (!animatedSafe.IsOpen ||
                Quaternion.Angle(closedRotation, animatedSafe.doorPivot.localRotation) < 80f ||
                Vector3.Distance(extendedBolts, animatedSafe.boltRoot.localPosition) < 0.01f)
            {
                throw new InvalidOperationException(
                    "La puerta o los cerrojos de la caja fuerte no cambian correctamente al abrir."
                );
            }
            animatedSafe.SetOpenInstantly(false);
            if (animatedSafe.IsOpen ||
                Quaternion.Angle(closedRotation, animatedSafe.doorPivot.localRotation) > 0.1f ||
                Vector3.Distance(extendedBolts, animatedSafe.boltRoot.localPosition) > 0.001f)
            {
                throw new InvalidOperationException(
                    "La caja fuerte no regresa completamente a su estado cerrado."
                );
            }

            string[] forbiddenFloatingUi =
            {
                "Diagrama_Usuario",
                "Diagrama_CuentaBancaria",
                "Resumen_Encapsulamiento",
                "Datos internos protegidos",
                "Acciones publicas controladas"
            };
            Transform[] allTransforms = instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                for (int f = 0; f < forbiddenFloatingUi.Length; f++)
                {
                    if (allTransforms[i].name.IndexOf(forbiddenFloatingUi[f], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        throw new InvalidOperationException(
                            "Quedo UI flotante prohibida en el ejemplo: " + allTransforms[i].name
                        );
                    }
                }
            }

            Debug.Log(
                "ALGOLAB NIVEL 3 VALIDACION OK: panel real con Usuario y Cuenta; " +
                "ambas clases tienen atributos y metodos, solo los signos UML reciben color, " +
                "los objetos y flechas estan separados, el oro permanece dentro de la caja fuerte " +
                "y el mecanismo completo de apertura/cierre fue verificado. El secreto Konami usa escala x3, " +
                "rig Mixamo, baile en bucle y materiales multicolor sin desbloquear niveles."
            );

            controller.LimpiarTarjetasInstanciadas();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ValidateRequiredAssets()
    {
        RequireAsset<GameObject>(ModelPath);
        RequireAsset<GameObject>(GoldModelPath);
        RequireAsset<GameObject>(SafeModelPath);
        RequireAsset<GameObject>(PersonModelPath);
        RequireDanceAnimationClip();
        RequireAsset<TMP_FontAsset>(FontPath);
        for (int i = 0; i < PillarIconPaths.Length; i++) RequireAsset<Texture2D>(PillarIconPaths[i]);
        for (int i = 0; i < AccessIconPaths.Length; i++) RequireAsset<Texture2D>(AccessIconPaths[i]);
        for (int i = 0; i < AudioPaths.Length; i++) RequireAsset<AudioClip>(AudioPaths[i]);
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new InvalidOperationException("Falta el recurso requerido: " + path);
        }
        return asset;
    }

    private static AnimationClip RequireDanceAnimationClip()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(DanceAnimationPath);
        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (clip.length > 0.05f) return clip;
        }

        throw new InvalidOperationException(
            "El FBX de baile no contiene un AnimationClip valido: " + DanceAnimationPath
        );
    }

    private static void ConfigureDanceAnimationImporter()
    {
        ModelImporter importer = AssetImporter.GetAtPath(DanceAnimationPath) as ModelImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("No se pudo importar la animacion: " + DanceAnimationPath);
        }

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Generic)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            changed = true;
        }

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            if (!clips[i].loopTime || !clips[i].loopPose)
            {
                clips[i].loopTime = true;
                clips[i].loopPose = true;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureTextureImporters(string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            TextureImporter importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }

    private static Material[] CreateIconMaterials(string[] texturePaths, string prefix)
    {
        EnsureFolder(MaterialFolder);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }
        if (shader == null)
        {
            throw new InvalidOperationException("No se encontro un shader transparente para los iconos.");
        }

        Material[] materials = new Material[texturePaths.Length];
        for (int i = 0; i < texturePaths.Length; i++)
        {
            string materialPath = MaterialFolder + "/" + prefix + "_Icono_" + (i + 1) + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                material.name = prefix + "_Icono_" + (i + 1);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }

            Texture2D texture = RequireAsset<Texture2D>(texturePaths[i]);
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            ConfigureTransparentMaterial(material);
            EditorUtility.SetDirty(material);
            materials[i] = material;
        }

        return materials;
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static Material CreateColorMaterial(string assetName, Color color)
    {
        EnsureFolder(MaterialFolder);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            throw new InvalidOperationException("No se encontro un shader para el diagrama.");
        }

        string path = MaterialFolder + "/" + assetName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            material.name = assetName;
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Geometry;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateOrUpdateVisualPrefab(
        Material[] pillarMaterials,
        Material[] accessMaterials,
        Material panelMaterial,
        Material headerMaterial,
        Material publicMaterial,
        Material privateMaterial,
        Material neutralMaterial)
    {
        EnsureFolder("Assets/__Algolab/Prefabs/Objects/level3");

        GameObject root;
        bool loadedExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        if (loadedExisting)
        {
            root = PrefabUtility.LoadPrefabContents(PrefabPath);
        }
        else
        {
            root = new GameObject("EncapsulationTheme_Audios01_10");
        }

        try
        {
            AlgoLabEncapsulationThemeVisual visual =
                root.GetComponent<AlgoLabEncapsulationThemeVisual>();
            if (visual == null)
            {
                visual = root.AddComponent<AlgoLabEncapsulationThemeVisual>();
            }

            visual.pillarModelPrefab = RequireAsset<GameObject>(ModelPath);
            visual.pillarIconMaterials = pillarMaterials;
            visual.accessIconMaterials = accessMaterials;
            visual.audioIntroCuatroPilares = RequireAsset<AudioClip>(AudioPaths[0]);
            visual.audioQueEsEncapsulamiento = RequireAsset<AudioClip>(AudioPaths[1]);
            visual.audioTiposDeAcceso = RequireAsset<AudioClip>(AudioPaths[2]);

            AlgoLabEncapsulationBankExampleVisual bankVisual =
                root.GetComponent<AlgoLabEncapsulationBankExampleVisual>();
            if (bankVisual == null)
            {
                bankVisual = root.AddComponent<AlgoLabEncapsulationBankExampleVisual>();
            }

            bankVisual.safeModelPrefab = RequireAsset<GameObject>(SafeModelPath);
            bankVisual.personModelPrefab = RequireAsset<GameObject>(PersonModelPath);
            bankVisual.goldModelPrefab = RequireAsset<GameObject>(GoldModelPath);
            bankVisual.userClassName = "Usuario";
            bankVisual.accountClassName = "Cuenta";
            bankVisual.fontAsset = RequireAsset<TMP_FontAsset>(FontPath);
            bankVisual.panelMaterial = panelMaterial;
            bankVisual.headerMaterial = headerMaterial;
            bankVisual.publicMaterial = publicMaterial;
            bankVisual.privateMaterial = privateMaterial;
            bankVisual.neutralMaterial = neutralMaterial;
            bankVisual.publicIconMaterial = accessMaterials[0];
            bankVisual.privateIconMaterial = accessMaterials[1];
            bankVisual.audioEjemploCuentaBancaria = RequireAsset<AudioClip>(AudioPaths[3]);
            bankVisual.audioValorPrivado = RequireAsset<AudioClip>(AudioPaths[4]);
            bankVisual.audioMetodosPublicos = RequireAsset<AudioClip>(AudioPaths[5]);
            bankVisual.audioDepositarSueldo = RequireAsset<AudioClip>(AudioPaths[6]);
            bankVisual.audioIntentoModificarValor = RequireAsset<AudioClip>(AudioPaths[7]);
            bankVisual.audioAccesoControlado = RequireAsset<AudioClip>(AudioPaths[8]);
            bankVisual.audioConclusion = RequireAsset<AudioClip>(AudioPaths[9]);
            bankVisual.konamiDanceClip = RequireDanceAnimationClip();
            bankVisual.konamiScaleMultiplier = 3f;
            bankVisual.konamiRainbowSpeed = 0.32f;
            bankVisual.diagramCenterX = 0.15f;
            bankVisual.diagramDepth = 0.35f;
            bankVisual.diagramBaseY = -0.02f;
            bankVisual.userOffsetX = -0.39f;
            bankVisual.accountOffsetX = 0.34f;
            bankVisual.personHeight = 0.48f;
            bankVisual.safeHeight = 0.42f;
            bankVisual.goldHeight = 0.13f;
            bankVisual.appearDuration = 0.32f;
            bankVisual.focusDuration = 0.25f;
            bankVisual.moveDuration = 0.75f;
            bankVisual.fadedSafeOpacity = 0.16f;
            EditorUtility.SetDirty(bankVisual);

            visual.bankExampleVisual = bankVisual;
            visual.layoutCenterX = 0.15f;
            visual.layoutDepth = 0.35f;
            visual.pillarSpacing = 0.28f;
            visual.pillarHeight = 0.32f;
            visual.pillarBaseY = -0.07f;
            visual.pillarIconSize = 0.18f;
            visual.pillarIconGap = 0.06f;
            visual.selectedForwardOffset = 0.02f;
            visual.selectedRaise = 0.03f;
            visual.selectedScale = 1.08f;
            visual.accessSpacing = 0.30f;
            visual.accessRowY = 0.11f;
            visual.accessForwardOffset = -0.18f;
            visual.accessIconSize = 0.20f;
            visual.accessSelectedScale = 1.14f;
            visual.narrationVolume = 1f;
            visual.showDebug = false;
            EditorUtility.SetDirty(visual);

            AudioSource source = root.GetComponent<AudioSource>();
            if (source == null) source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (saved == null)
            {
                throw new InvalidOperationException("No se pudo crear el prefab " + PrefabPath);
            }

            return saved;
        }
        finally
        {
            if (loadedExisting)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
