using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class AlgoLabLevel4AbstractionSetup
{
    private const string PrefabPath =
        "Assets/__Algolab/Prefabs/Objects/level4/AbstractionTheme_Audios01_06.prefab";
    private const string PillarModelPath =
        "Assets/__Algolab/Prefabs/Objects/level3/Pillars/Column_Quaternius.fbx";
    private const string VinylModelPath =
        "Assets/__Algolab/Prefabs/Objects/level4/Models/Vinyl_CC0_Optimized.fbx";
    private const string StoreModelPath =
        "Assets/__Algolab/Prefabs/Objects/level4/Models/MusicStore_KayLousberg_CC0.fbx";
    private const string PhoneModelPath =
        "Assets/__Algolab/Prefabs/Objects/level4/Models/Phone_Quaternius_CC0.fbx";
    private const string BoardModelPath =
        "Assets/__Algolab/Prefabs/Objects/level4/Models/Phone_Internal_Board_CC0_Optimized.fbx";
    private const string OutlineMaterialPath =
        "Assets/__Algolab/Shaders/material/seleccion.mat";
    private static readonly string[] PillarIconMaterialPaths =
    {
        "Assets/__Algolab/Materials/level3-encapsulamiento/Pilar_Icono_1.mat",
        "Assets/__Algolab/Materials/level3-encapsulamiento/Pilar_Icono_2.mat",
        "Assets/__Algolab/Materials/level3-encapsulamiento/Pilar_Icono_3.mat",
        "Assets/__Algolab/Materials/level3-encapsulamiento/Pilar_Icono_4.mat"
    };
    private const string StoreTexturePath =
        "Assets/__Algolab/Prefabs/Objects/level4/Models/Textures/MusicStore_citybits.png";
    private const string FontPath =
        "Assets/__Algolab/Fonts/jd_code SDF.asset";
    private const string MaterialFolder =
        "Assets/__Algolab/Materials/level4-abstraccion";

    private static readonly string[] AudioPaths =
    {
        "Assets/__Algolab/Audio/level4-abstraccion/01_abstraccion.mp3",
        "Assets/__Algolab/Audio/level4-abstraccion/02_abstraccion.mp3",
        "Assets/__Algolab/Audio/level4-abstraccion/03_abstraccion.mp3",
        "Assets/__Algolab/Audio/level4-abstraccion/04_abstraccion.mp3",
        "Assets/__Algolab/Audio/level4-abstraccion/05_abstraccion.mp3",
        "Assets/__Algolab/Audio/level4-abstraccion/06_abstraccion.mp3",
        "Assets/__Algolab/Audio/level4-abstraccion/08_abstraccion.mp3"
    };

    private static bool autoConfigureQueued;
    private static bool isConfiguring;

    [InitializeOnLoadMethod]
    private static void AutoConfigureWhenMissing()
    {
        if (Application.isBatchMode ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null ||
            autoConfigureQueued)
        {
            return;
        }

        autoConfigureQueued = true;
        EditorApplication.delayCall += TryAutoConfigure;
    }

    private static void TryAutoConfigure()
    {
        autoConfigureQueued = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            autoConfigureQueued = true;
            EditorApplication.delayCall += TryAutoConfigure;
            return;
        }

        try
        {
            ConfigureBatch();
        }
        catch (Exception exception)
        {
            Debug.LogError("ALGOLAB NIVEL 4: no se pudo autoconfigurar. " + exception);
        }
    }

    [MenuItem("Tools/AlgoLab/Nivel 4/Configurar tema de Abstraccion")]
    private static void ConfigureFromMenu()
    {
        ConfigureBatch();
    }

    public static void ConfigureBatch()
    {
        if (isConfiguring)
        {
            return;
        }

        isConfiguring = true;
        try
        {
            ValidateRequiredAssets();
            ConfigureStoreTextureImporter();
            ConfigureModelImporters();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Material storeMaterial = CreateStoreMaterial();
            GameObject visualPrefab = CreateOrUpdateVisualPrefab(storeMaterial);

            int configuredScenes = ConfigureBuildScenes(visualPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "ALGOLAB NIVEL 4: tema de Abstraccion configurado. Escenas=" +
                configuredScenes + ", prefab=" + PrefabPath
            );
        }
        finally
        {
            isConfiguring = false;
        }
    }

    private static int ConfigureBuildScenes(GameObject visualPrefab)
    {
        int configuredScenes = 0;
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
                AlgoLabPillarLevelController created =
                    progressPanels[0].GetComponent<AlgoLabPillarLevelController>();
                if (created == null)
                {
                    created = progressPanels[0].gameObject.AddComponent<AlgoLabPillarLevelController>();
                }
                created.AsegurarNivelesPorDefecto();
                pillarControllers = new[] { created };
            }

            if (pillarControllers.Length == 0)
            {
                Debug.LogWarning(
                    "ALGOLAB NIVEL 4: la escena no tiene controlador de pilares: " +
                    buildScene.path
                );
                continue;
            }

            AlgoLabAbstractionThemeController abstractionController =
                UnityEngine.Object.FindFirstObjectByType<AlgoLabAbstractionThemeController>(
                    FindObjectsInactive.Include
                );
            if (abstractionController == null)
            {
                GameObject controllerObject = new GameObject(
                    "AlgoLab_AbstractionThemeController"
                );
                abstractionController =
                    controllerObject.AddComponent<AlgoLabAbstractionThemeController>();
            }

            abstractionController.spawnManager =
                UnityEngine.Object.FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                    FindObjectsInactive.Include
                );
            abstractionController.themeVisualPrefab = visualPrefab;
            abstractionController.spawnScale = Vector3.one;
            abstractionController.maximumConnectWait = 4f;
            abstractionController.showDebug = false;
            EditorUtility.SetDirty(abstractionController);

            for (int i = 0; i < pillarControllers.Length; i++)
            {
                pillarControllers[i].abstractionThemeController = abstractionController;
                pillarControllers[i].AsegurarNivelesPorDefecto();
                EditorUtility.SetDirty(pillarControllers[i]);
            }

            for (int i = 0; i < progressPanels.Length; i++)
            {
                if (progressPanels[i].pillarLevelController == null)
                {
                    progressPanels[i].pillarLevelController = pillarControllers[0];
                    EditorUtility.SetDirty(progressPanels[i]);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, buildScene.path))
            {
                throw new InvalidOperationException(
                    "No se pudo guardar el nivel 4 en " + buildScene.path
                );
            }
            configuredScenes++;
        }
        return configuredScenes;
    }

    private static GameObject CreateOrUpdateVisualPrefab(Material storeMaterial)
    {
        EnsureFolder("Assets/__Algolab/Prefabs/Objects/level4");
        bool loadedExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        GameObject root = loadedExisting
            ? PrefabUtility.LoadPrefabContents(PrefabPath)
            : new GameObject("AbstractionTheme_Audios01_08");

        try
        {
            root.name = "AbstractionTheme_Audios01_08";
            AlgoLabAbstractionThemeVisual visual =
                root.GetComponent<AlgoLabAbstractionThemeVisual>();
            if (visual == null)
            {
                visual = root.AddComponent<AlgoLabAbstractionThemeVisual>();
            }

            visual.pillarModelPrefab = RequireAsset<GameObject>(PillarModelPath);
            visual.vinylModelPrefab = RequireAsset<GameObject>(VinylModelPath);
            visual.musicStoreModelPrefab = RequireAsset<GameObject>(StoreModelPath);
            visual.phoneModelPrefab = RequireAsset<GameObject>(PhoneModelPath);
            visual.internalBoardModelPrefab = RequireAsset<GameObject>(BoardModelPath);
            visual.outlineMaterialTemplate =
                RequireAsset<Material>(OutlineMaterialPath);
            visual.pillarIconMaterials = new Material[PillarIconMaterialPaths.Length];
            for (int i = 0; i < PillarIconMaterialPaths.Length; i++)
            {
                visual.pillarIconMaterials[i] =
                    RequireAsset<Material>(PillarIconMaterialPaths[i]);
            }
            visual.musicStoreMaterialOverride = storeMaterial;
            visual.fontAsset = RequireAsset<TMP_FontAsset>(FontPath);
            visual.narrationClips = new AudioClip[AudioPaths.Length];
            for (int i = 0; i < AudioPaths.Length; i++)
            {
                visual.narrationClips[i] = RequireAsset<AudioClip>(AudioPaths[i]);
            }

            visual.centerX = 0.15f;
            visual.baseY = 0.02f;
            visual.depth = 0.34f;
            visual.sideOffset = 0.47f;
            visual.pillarSpacing = 0.28f;
            visual.pillarHeight = 0.32f;
            visual.pillarBaseY = -0.07f;
            visual.pillarIconSize = 0.18f;
            visual.pillarIconGap = 0.06f;
            visual.pillarSelectedForwardOffset = 0.02f;
            visual.pillarSelectedRaise = 0.03f;
            visual.pillarSelectedScale = 1.08f;
            visual.phoneModelEuler = new Vector3(0f, 180f, 0f);
            visual.storeModelEuler = new Vector3(0f, 180f, 0f);
            visual.vinylModelEuler = new Vector3(90f, 180f, 0f);
            visual.boardModelEuler = Vector3.zero;
            visual.appearDuration = 0.34f;
            visual.transitionDuration = 0.28f;
            visual.focusDuration = 0.22f;
            visual.narrationVolume = 1f;
            visual.showDebug = false;
            EditorUtility.SetDirty(visual);

            AudioSource source = root.GetComponent<AudioSource>();
            if (source == null)
            {
                source = root.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (saved == null)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el prefab " + PrefabPath
                );
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

    private static void ValidateRequiredAssets()
    {
        string[] paths =
        {
            PillarModelPath,
            VinylModelPath,
            StoreModelPath,
            PhoneModelPath,
            BoardModelPath,
            OutlineMaterialPath,
            StoreTexturePath,
            FontPath
        };

        for (int i = 0; i < paths.Length; i++)
        {
            if (AssetDatabase.LoadMainAssetAtPath(paths[i]) == null)
            {
                throw new InvalidOperationException("Falta el recurso requerido: " + paths[i]);
            }
        }

        for (int i = 0; i < PillarIconMaterialPaths.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(
                PillarIconMaterialPaths[i]
            ) == null)
            {
                throw new InvalidOperationException(
                    "Falta el material de pilar reutilizado: " +
                    PillarIconMaterialPaths[i]
                );
            }
        }

        for (int i = 0; i < AudioPaths.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPaths[i]) == null)
            {
                throw new InvalidOperationException(
                    "Falta el audio de Abstraccion: " + AudioPaths[i]
                );
            }
        }
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new InvalidOperationException("No se pudo cargar " + path);
        }
        return asset;
    }

    private static void ConfigureStoreTextureImporter()
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(StoreTexturePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static void ConfigureModelImporters()
    {
        string[] paths =
        {
            VinylModelPath,
            StoreModelPath,
            PhoneModelPath,
            BoardModelPath
        };
        for (int i = 0; i < paths.Length; i++)
        {
            ModelImporter importer = AssetImporter.GetAtPath(paths[i]) as ModelImporter;
            if (importer == null)
            {
                continue;
            }
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }

    private static Material CreateStoreMaterial()
    {
        EnsureFolder(MaterialFolder);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }
        if (shader == null)
        {
            throw new InvalidOperationException(
                "No se encontro shader para la textura de la tienda."
            );
        }

        string path = MaterialFolder + "/Abstraccion_TiendaKayLousberg.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            material.name = "Abstraccion_TiendaKayLousberg";
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        Texture2D texture = RequireAsset<Texture2D>(StoreTexturePath);
        material.mainTexture = texture;
        material.color = Color.white;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)CullMode.Off);
        }
        material.renderQueue = (int)RenderQueue.Geometry;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateIconMaterial(string texturePath, string assetName)
    {
        EnsureFolder(MaterialFolder);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }
        if (shader == null)
        {
            throw new InvalidOperationException("No se encontro shader para el icono.");
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

        Texture2D texture = RequireAsset<Texture2D>(texturePath);
        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)CullMode.Off);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateColorMaterial(string assetName, Color color)
    {
        EnsureFolder(MaterialFolder);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            throw new InvalidOperationException("No se encontro shader para el tema.");
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
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)CullMode.Off);
        }
        material.renderQueue = (int)RenderQueue.Geometry;
        EditorUtility.SetDirty(material);
        return material;
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
