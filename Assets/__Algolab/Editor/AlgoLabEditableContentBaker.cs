using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Materializa en los prefabs las jerarquías que antes solo se construían en
/// ejecución y crea un catálogo EditorOnly en la escena principal.
/// </summary>
public static class AlgoLabEditableContentBaker
{
    private const string MainScene =
        "Assets/Scenes/version_estable14.unity";
    private const string CatalogName =
        "[CONTENIDO_EDITABLE_ALGOLAB]";

    private static readonly string[] EditablePrefabPaths =
    {
        "Assets/__Algolab/Prefabs/Objects/level1/PuertaTemaRoot.prefab",
        "Assets/__Algolab/Prefabs/Objects/level1/CarPracticeRoot.prefab",
        "Assets/__Algolab/Prefabs/Objects/level2/VehicleLevel02Root.prefab",
        "Assets/__Algolab/Prefabs/Objects/level3/EncapsulationTheme_Audios01_03.prefab",
        "Assets/__Algolab/Resources/Level3/AlgoLabRobotPractice.prefab",
        "Assets/__Algolab/Prefabs/Objects/level4/AbstractionTheme_Audios01_06.prefab"
    };

    private static readonly string[] EditableNames =
    {
        "Nivel 1 - Tema Puerta",
        "Nivel 1 - Practica Vehiculo",
        "Nivel 2 - Tema y practica Vehiculos",
        "Nivel 3 - Tema Encapsulamiento",
        "Nivel 3 - Practica Robot",
        "Nivel 4 - Tema Abstraccion"
    };

    [MenuItem("AlgoLab/Autoría/Preparar todo el contenido editable")]
    public static void BakeAll()
    {
        BakeThemePrefab(
            EditablePrefabPaths[3],
            root =>
            {
                AlgoLabEncapsulationThemeVisual visual =
                    root.GetComponent<AlgoLabEncapsulationThemeVisual>();
                if (visual == null)
                {
                    throw new InvalidOperationException(
                        "El prefab del nivel 3 no contiene su controlador visual."
                    );
                }
                visual.PrepareEditableHierarchy();
                visual.ShowEditablePreview();
            }
        );

        BakeThemePrefab(
            EditablePrefabPaths[5],
            root =>
            {
                AlgoLabAbstractionThemeVisual visual =
                    root.GetComponent<AlgoLabAbstractionThemeVisual>();
                if (visual == null)
                {
                    throw new InvalidOperationException(
                        "El prefab del nivel 4 no contiene su controlador visual."
                    );
                }
                visual.PrepareEditableHierarchy();
                visual.ShowEditablePreview();
            }
        );

        CreateSceneCatalog();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "ALGOLAB AUTORIA: jerarquías editables y catálogo preparados correctamente."
        );
    }

    public static void BakeAllBatch()
    {
        try
        {
            BakeAll();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void BakeThemePrefab(
        string prefabPath,
        Action<GameObject> prepare)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            prepare(root);
            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void CreateSceneCatalog()
    {
        Scene scene =
            EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        GameObject previous =
            GameObject.Find(CatalogName);
        if (previous != null)
        {
            UnityEngine.Object.DestroyImmediate(previous);
        }

        GameObject catalogRoot = new GameObject(CatalogName);
        catalogRoot.tag = "EditorOnly";
        AlgoLabEditableContentCatalog catalog =
            catalogRoot.AddComponent<AlgoLabEditableContentCatalog>();

        List<GameObject> instances = new List<GameObject>();
        for (int i = 0; i < EditablePrefabPaths.Length; i++)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    EditablePrefabPaths[i]
                );
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el prefab editable: " +
                    EditablePrefabPaths[i]
                );
            }

            GameObject instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = EditableNames[i];
            instance.transform.SetParent(catalogRoot.transform, false);
            instances.Add(instance);
        }

        catalog.contenidoEditable = instances.ToArray();
        catalog.tutorialesEnEscena =
            FindTutorialObjects(scene).ToArray();
        catalog.controladorNiveles =
            UnityEngine.Object.FindFirstObjectByType<AlgoLabPillarLevelController>(
                FindObjectsInactive.Include
            );
        catalog.administradorObjetos =
            UnityEngine.Object.FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                FindObjectsInactive.Include
            );

        catalogRoot.SetActive(false);
        EditorUtility.SetDirty(catalog);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, MainScene))
        {
            throw new InvalidOperationException(
                "Unity no pudo guardar el catálogo en la escena principal."
            );
        }
    }

    private static IEnumerable<GameObject> FindTutorialObjects(Scene scene)
    {
        return scene
            .GetRootGameObjects()
            .SelectMany(
                root => root.GetComponentsInChildren<MonoBehaviour>(true)
            )
            .Where(
                component =>
                    component != null &&
                    component.GetType().Name.IndexOf(
                        "Tutorial",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
            )
            .Select(component => component.gameObject)
            .Distinct()
            .OrderBy(gameObject => GetHierarchyPath(gameObject.transform));
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
