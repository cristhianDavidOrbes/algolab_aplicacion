#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlgoLabMonitorAuthoringSetup
{
    private const string FbxPath =
        "Assets/__Algolab/Resources/Level3/RobotWorkshop/Models/Monitor/AlgoLabMonitor.fbx";
    private const string TextureFolder =
        "Assets/__Algolab/Resources/Level3/RobotWorkshop/Textures/Monitor";
    private const string MaterialFolder =
        "Assets/__Algolab/Resources/Level3/RobotWorkshop/Materials/Monitor";
    private const string PrefabFolder =
        "Assets/__Algolab/Resources/Level3/RobotWorkshop/Prefabs";
    private const string PrefabPath =
        PrefabFolder + "/MonitorNivel3.prefab";
    private const string EditableScenePath =
        "Assets/Scenes/Nivel3_Monitor_Editable.unity";
    private const string EditableRootName =
        "MonitorNivel3_EDITABLE";
    private const string SessionKey =
        "AlgoLabMonitorAuthoringSetup_20260726_v1";

    private static bool synchronizing;
    private static int remainingImportAttempts;

    private struct MaterialDefinition
    {
        public string name;
        public Color color;
        public float metallic;
        public float smoothness;
        public string texture;
    }

    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorSceneManager.sceneSaved -= OnSceneSaved;
        EditorSceneManager.sceneSaved += OnSceneSaved;

        if (Application.isBatchMode ||
            SessionState.GetBool(SessionKey, false) ||
            AssetDatabase.LoadAssetAtPath<SceneAsset>(EditableScenePath) != null)
        {
            return;
        }

        remainingImportAttempts = 30;
        EditorApplication.delayCall += TryBuildAndOpen;
    }

    private static void TryBuildAndOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (model == null)
        {
            remainingImportAttempts--;
            if (remainingImportAttempts > 0)
                EditorApplication.delayCall += TryBuildAndOpen;
            return;
        }

        SessionState.SetBool(SessionKey, true);
        BuildEditableWorkspace();
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            OpenEditableScene();
    }

    [MenuItem("AlgoLab/Nivel 3/Monitor/Crear o reconstruir monitor editable")]
    public static void BuildEditableWorkspace()
    {
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);
        Dictionary<string, Material> materials = BuildMaterials();

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (source == null)
            throw new System.InvalidOperationException(
                "Unity aun no ha importado AlgoLabMonitor.fbx."
            );

        Scene buildScene = EditorSceneManager.NewPreviewScene();
        GameObject root = new GameObject("MonitorNivel3");
        SceneManager.MoveGameObjectToScene(root, buildScene);
        GameObject model =
            PrefabUtility.InstantiatePrefab(source, buildScene) as GameObject;
        if (model == null)
            throw new System.InvalidOperationException(
                "No se pudo instanciar el monitor."
            );

        PrefabUtility.UnpackPrefabInstance(
            model,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction
        );
        model.name = "ModeloMonitor";
        model.transform.SetParent(root.transform, false);
        model.transform.localRotation = source.transform.localRotation;
        model.transform.localScale = Vector3.one;
        AssignMaterials(model, materials);
        NormalizeForAuthoring(model.transform, root.transform, 1.65f);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        EditorSceneManager.ClosePreviewScene(buildScene);
        AssetDatabase.SaveAssets();

        Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance =
            PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        PrefabUtility.UnpackPrefabInstance(
            instance,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction
        );
        instance.name = EditableRootName;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        EditorSceneManager.SaveScene(scene, EditableScenePath);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "NIVEL 3 MONITOR: prefab y escena editable creados."
        );
    }

    [MenuItem("AlgoLab/Nivel 3/Monitor/Abrir monitor editable")]
    public static void OpenEditableScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EditableScenePath) == null)
            BuildEditableWorkspace();

        EditorSceneManager.OpenScene(EditableScenePath, OpenSceneMode.Single);
        Selection.activeGameObject = GameObject.Find(EditableRootName);
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
    }

    [MenuItem("AlgoLab/Nivel 3/Monitor/Aplicar cambios al prefab")]
    public static void ApplyToPrefab()
    {
        Scene scene = SceneManager.GetSceneByPath(EditableScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(
                EditableScenePath,
                OpenSceneMode.Additive
            );
        }

        GameObject root = FindEditableRoot(scene);
        if (root == null)
            throw new System.InvalidOperationException(
                "No se encontro MonitorNivel3_EDITABLE."
            );

        Synchronize(root);
        AssetDatabase.SaveAssets();
    }

    private static void OnSceneSaved(Scene scene)
    {
        if (synchronizing || scene.path != EditableScenePath)
            return;
        GameObject root = FindEditableRoot(scene);
        if (root == null)
            return;
        Synchronize(root);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "NIVEL 3 MONITOR: Ctrl+S sincronizo el monitor con su prefab."
        );
    }

    private static void Synchronize(GameObject root)
    {
        if (root == null || synchronizing)
            return;

        synchronizing = true;
        try
        {
            string originalName = root.name;
            root.name = "MonitorNivel3";
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            root.name = originalName;
        }
        finally
        {
            synchronizing = false;
        }
    }

    private static GameObject FindEditableRoot(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == EditableRootName)
                return roots[i];
        }
        return null;
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        var definitions = new[]
        {
            Definition("Main", new Color(0.402f, 0.402f, 0.402f), 0f, 0f),
            Definition("DarkGrey", new Color(0.2395f, 0.2395f, 0.2395f), 0f, 0f),
            Definition("Black", new Color(0.042f, 0.042f, 0.042f), 0f, 0f),
            Definition("Accent", new Color(0.5583f, 0.2961f, 0.0666f), 1f, 0f),
            Definition("DarkAccent", new Color(0.2266f, 0.1234f, 0.0301f), 0f, 0f),
            Definition("Material.001", new Color(0.0975f, 0.0975f, 0.0975f), 0f, 0.6f),
            Definition("Material.002", new Color(0f, 0.4431f, 1f), 0f, 0.6f),
            Definition("aspasMaterial", Color.white, 0f, 0.6f, "aspasImagen"),
            Definition("botonMaterial", Color.white, 0f, 0.6f, "ImagenBoton"),
            Definition("CargadorMateiral", Color.white, 0f, 0.6f, "imagenCargador"),
            Definition("MaterialPalanca1", Color.white, 0f, 0.6f, "ImagenPalanca1"),
            Definition("MaterialPalanca2", Color.white, 0f, 0.6f, "palanca2Imagen"),
            Definition("ventiladorMaterial", Color.white, 0f, 0.6f, "ventiladorImagen"),
        };

        var result = new Dictionary<string, Material>();
        for (int i = 0; i < definitions.Length; i++)
        {
            MaterialDefinition definition = definitions[i];
            string path = MaterialFolder + "/" +
                          Sanitize(definition.name) + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.name = definition.name;
            material.color = definition.color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", definition.color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", definition.metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", definition.smoothness);

            Texture2D texture = string.IsNullOrEmpty(definition.texture)
                ? null
                : AssetDatabase.LoadAssetAtPath<Texture2D>(
                    TextureFolder + "/" + definition.texture + ".png"
                );
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
            result[definition.name] = material;
        }
        return result;
    }

    private static MaterialDefinition Definition(
        string name,
        Color color,
        float metallic,
        float smoothness,
        string texture = null)
    {
        return new MaterialDefinition
        {
            name = name,
            color = color,
            metallic = metallic,
            smoothness = smoothness,
            texture = texture,
        };
    }

    private static void AssignMaterials(
        GameObject model,
        Dictionary<string, Material> materials)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] assigned = renderers[i].sharedMaterials;
            for (int materialIndex = 0;
                 materialIndex < assigned.Length;
                 materialIndex++)
            {
                Material current = assigned[materialIndex];
                if (current != null &&
                    materials.TryGetValue(current.name, out Material replacement))
                {
                    assigned[materialIndex] = replacement;
                }
            }
            renderers[i].sharedMaterials = assigned;
        }
    }

    private static void NormalizeForAuthoring(
        Transform model,
        Transform root,
        float targetWidth)
    {
        if (!TryGetBounds(model, out Bounds bounds) || bounds.size.x < 0.001f)
            return;

        float scale = targetWidth / bounds.size.x;
        model.localScale = Vector3.one * scale;
        if (!TryGetBounds(model, out bounds))
            return;

        Vector3 targetCenter =
            new Vector3(0f, bounds.extents.y, 0f);
        model.position += targetCenter - bounds.center;
    }

    private static bool TryGetBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!found)
            {
                bounds = renderers[i].bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }
        return found;
    }

    private static string Sanitize(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
            value = value.Replace(invalid[i], '_');
        return value;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) &&
            !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
