#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlgoLabLevel3RobotPracticeSetup
{
    private const string ResourcesFolder =
        "Assets/__Algolab/Resources/Level3";
    private const string PrefabPath =
        ResourcesFolder + "/AlgoLabRobotPractice.prefab";
    private const string MaterialFolder =
        ResourcesFolder + "/RobotWorkshop/Materials";
    private const string EditableScenePath =
        "Assets/Scenes/Nivel3_Robot_Editable.unity";
    private const string EditableRootName =
        "AlgoLabRobotPractice_EDITABLE";
    private static bool synchronizingEditableScene;

    [InitializeOnLoadMethod]
    private static void RegisterEditableSceneAutoSync()
    {
        EditorSceneManager.sceneSaved -= OnEditableSceneSaved;
        EditorSceneManager.sceneSaved += OnEditableSceneSaved;
    }

    [MenuItem("AlgoLab/Nivel 3/Crear practica del robot")]
    public static void Build()
    {
        EnsureFolder("Assets/__Algolab/Resources");
        EnsureFolder(ResourcesFolder);

        GameObject root = new GameObject("AlgoLabRobotPractice");
        AlgoLabEncapsulationRobotPractice practice =
            root.AddComponent<AlgoLabEncapsulationRobotPractice>();
        practice.energiaInicial = 25;
        practice.temperaturaInicial = 85;
        practice.averiaInicial = true;
        practice.puntajeInicial = 100;
        practice.penalizacionAccesoPrivado = 10;
        practice.energiaMinimaEncendido = 80;
        practice.temperaturaMaximaEncendido = 45;
        practice.completarNivelAutomaticamente = true;
        practice.ConstruirObjetosSiFaltan();

        PersistGeneratedMaterials(root);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null ||
            prefab.GetComponent<AlgoLabEncapsulationRobotPractice>() == null)
        {
            throw new System.InvalidOperationException(
                "No se pudo crear el prefab de la practica del robot."
            );
        }

        Debug.Log("NIVEL 3 ROBOT: prefab creado en " + PrefabPath);
    }

    [MenuItem("AlgoLab/Nivel 3/Robot/Crear o reconstruir escena editable")]
    public static void BuildEditableWorkspace()
    {
        Build();

        Scene authoringScene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance =
            PrefabUtility.InstantiatePrefab(prefab, authoringScene) as GameObject;
        if (instance == null)
            throw new System.InvalidOperationException(
                "No se pudo crear la instancia editable del robot."
            );

        PrefabUtility.UnpackPrefabInstance(
            instance,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction
        );
        instance.name = EditableRootName;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        EditorSceneManager.SaveScene(authoringScene, EditableScenePath);
        EditorSceneManager.CloseScene(authoringScene, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "NIVEL 3 ROBOT: escena editable creada en " + EditableScenePath +
            ". Mueve sus hijos y usa 'Aplicar escena editable al prefab'."
        );
    }

    public static void BuildEditableWorkspaceBatch()
    {
        BuildEditableWorkspace();
    }

    [MenuItem("AlgoLab/Nivel 3/Robot/Abrir escena editable %&r")]
    public static void OpenEditableScene()
    {
        if (!File.Exists(EditableScenePath))
            BuildEditableWorkspace();

        EditorSceneManager.OpenScene(EditableScenePath, OpenSceneMode.Single);
        Selection.activeGameObject =
            GameObject.Find(EditableRootName);
    }

    [MenuItem("AlgoLab/Nivel 3/Robot/Aplicar escena editable al prefab %&s")]
    public static void ApplyEditableSceneToPrefab()
    {
        Scene scene = SceneManager.GetSceneByPath(EditableScenePath);
        bool closeAfter = !scene.IsValid() || !scene.isLoaded;
        if (closeAfter)
        {
            scene = EditorSceneManager.OpenScene(
                EditableScenePath,
                OpenSceneMode.Additive
            );
        }

        GameObject root = FindEditableRoot(scene);
        if (root == null)
            throw new System.InvalidOperationException(
                "La escena editable no contiene AlgoLabRobotPractice_EDITABLE."
            );

        SynchronizeRootToPrefab(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (closeAfter)
            EditorSceneManager.CloseScene(scene, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "NIVEL 3 ROBOT: posiciones de la escena editable aplicadas al prefab usado por el juego."
        );
    }

    private static void OnEditableSceneSaved(Scene scene)
    {
        if (synchronizingEditableScene ||
            scene.path != EditableScenePath)
        {
            return;
        }

        GameObject root = FindEditableRoot(scene);
        if (root == null)
            return;

        SynchronizeRootToPrefab(root);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "NIVEL 3 ROBOT: Ctrl+S sincronizo automaticamente la escena editable con el prefab."
        );
    }

    private static GameObject FindEditableRoot(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == EditableRootName ||
                roots[i].GetComponent<AlgoLabEncapsulationRobotPractice>() != null)
            {
                return roots[i];
            }
        }
        return null;
    }

    private static void SynchronizeRootToPrefab(GameObject root)
    {
        if (root == null || synchronizingEditableScene)
            return;

        synchronizingEditableScene = true;
        try
        {
            PersistGeneratedMaterials(root);
            string originalName = root.name;
            root.name = "AlgoLabRobotPractice";
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            root.name = originalName;
        }
        finally
        {
            synchronizingEditableScene = false;
        }
    }

    public static void BuildBatch()
    {
        Build();
    }

    public static void RenderPreviewBatch()
    {
        Build();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        AlgoLabEncapsulationRobotPractice practice =
            instance.GetComponent<AlgoLabEncapsulationRobotPractice>();
        practice.completarNivelAutomaticamente = false;
        practice.ConstruirObjetosSiFaltan();
        practice.IniciarPractica();

        GameObject cameraObject = new GameObject("PreviewCamera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 43f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.018f, 0.025f, 0.035f, 1f);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 20f;

        GameObject lightObject = new GameObject("PreviewLight", typeof(Light));
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(0.72f, 0.88f, 1f);
        light.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.38f, 0.46f);

        const int width = 1400;
        const int height = 900;
        string outputFront = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "Logs",
            "level3-robot-practice-preview.png"
        );
        string outputRear = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "Logs",
            "level3-robot-practice-preview-rear.png"
        );

        camera.transform.position = new Vector3(0.35f, 0.18f, -3.35f);
        camera.transform.LookAt(new Vector3(0f, 0.12f, 0f));
        light.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
        RenderToPng(camera, outputFront, width, height);

        camera.transform.position = new Vector3(-0.42f, 0.14f, 3.1f);
        camera.transform.LookAt(new Vector3(0f, 0.05f, 0.18f));
        light.transform.rotation = Quaternion.Euler(25f, 180f, 0f);
        RenderToPng(camera, outputRear, width, height);

        Object.DestroyImmediate(instance);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(lightObject);

        if (!File.Exists(outputFront) || !File.Exists(outputRear))
            throw new System.InvalidOperationException("No se pudieron renderizar las vistas del robot.");

        Debug.Log(
            "NIVEL 3 ROBOT: vistas previa frontal y trasera renderizadas en " +
            outputFront + " | " + outputRear
        );
    }

    private static void RenderToPng(Camera camera, string output, int width, int height)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        RenderTexture renderTexture =
            new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = renderTexture;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(output, image.EncodeToPNG());

        RenderTexture.active = previous;
        camera.targetTexture = null;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(renderTexture);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void PersistGeneratedMaterials(GameObject root)
    {
        if (root == null)
            return;

        EnsureFolder(MaterialFolder);
        var persistedByInstance =
            new System.Collections.Generic.Dictionary<int, Material>();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int rendererIndex = 0;
             rendererIndex < renderers.Length;
             rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            Material[] assigned = renderer.sharedMaterials;
            bool changed = false;

            for (int materialIndex = 0;
                 materialIndex < assigned.Length;
                 materialIndex++)
            {
                Material source = assigned[materialIndex];
                if (source == null || AssetDatabase.Contains(source))
                    continue;

                int id = source.GetInstanceID();
                if (!persistedByInstance.TryGetValue(id, out Material asset))
                {
                    string safeName = SanitizeAssetName(source.name);
                    string path =
                        MaterialFolder + "/" + safeName + ".mat";
                    asset = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (asset == null)
                    {
                        asset = new Material(source)
                        {
                            name = safeName
                        };
                        AssetDatabase.CreateAsset(asset, path);
                    }
                    else
                    {
                        asset.CopyPropertiesFromMaterial(source);
                        EditorUtility.SetDirty(asset);
                    }
                    persistedByInstance.Add(id, asset);
                }

                assigned[materialIndex] = asset;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = assigned;
        }
    }

    private static string SanitizeAssetName(string value)
    {
        string name = string.IsNullOrWhiteSpace(value)
            ? "MaterialRobot"
            : value;
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
            name = name.Replace(invalid[i], '_');
        return name.Replace(" (Instance)", string.Empty);
    }
}
#endif
