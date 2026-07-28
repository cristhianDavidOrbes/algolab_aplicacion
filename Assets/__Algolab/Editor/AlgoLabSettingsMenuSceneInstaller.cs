using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AlgoLabSettingsMenuSceneInstaller
{
    private const string ScenePath = "Assets/Scenes/version_estable14.unity";
    private const string RootName = "[ALGOLAB_SETTINGS_MENU]";

    [MenuItem("Tools/AlgoLab/Instalar menu de configuracion en escena")]
    private static void InstallFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Install(false);
    }

    public static void InstallBatch()
    {
        Install(true);
    }

    [MenuItem("Tools/AlgoLab/Capturar vista previa del menu de configuracion")]
    private static void CapturePreviewFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        CapturePreviewBatch();
    }

    public static void CapturePreviewBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject root = null;

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name == RootName)
            {
                root = sceneRoot;
                break;
            }
        }

        if (root == null)
        {
            throw new System.InvalidOperationException(
                "No existe " + RootName + " en " + ScenePath + "."
            );
        }

        Transform canvasTransform = root.transform.Find("AlgoLabSettingsCanvas");
        Canvas settingsCanvas = canvasTransform != null
            ? canvasTransform.GetComponent<Canvas>()
            : null;

        if (settingsCanvas == null)
        {
            throw new System.InvalidOperationException(
                "El menu serializado no contiene un Canvas valido."
            );
        }

        root.SetActive(true);
        canvasTransform.gameObject.SetActive(true);

        List<GameObject> hiddenRoots = new List<GameObject>();
        List<bool> originalRootStates = new List<bool>();
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot == root)
            {
                continue;
            }

            hiddenRoots.Add(sceneRoot);
            originalRootStates.Add(sceneRoot.activeSelf);
            sceneRoot.SetActive(false);
        }

        GameObject cameraObject = new GameObject("AlgoLabSettingsPreviewCamera");
        Camera previewCamera = cameraObject.AddComponent<Camera>();
        Vector3 forward = root.transform.forward.sqrMagnitude > 0.001f
            ? root.transform.forward.normalized
            : Vector3.forward;
        Vector3 up = root.transform.up.sqrMagnitude > 0.001f
            ? root.transform.up.normalized
            : Vector3.up;

        cameraObject.transform.position = root.transform.position - forward * 1.45f;
        cameraObject.transform.rotation = Quaternion.LookRotation(forward, up);
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.035f, 0.045f, 0.05f, 1f);
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 10f;
        previewCamera.fieldOfView = 50f;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = true;
        previewCamera.cullingMask = ~0;

        const int width = 1600;
        const int height = 1000;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        Transform content = canvasTransform.Find("Contenido");
        TMP_Text title = canvasTransform.Find("TituloVista")?.GetComponent<TMP_Text>();
        string[] viewNames =
        {
            "VistaPaneles",
            "VistaSonido",
            "VistaGraficos",
            "VistaSesion",
            "VistaRanking"
        };
        string[] viewTitles =
        {
            "Paneles",
            "Audio e IA",
            "Graficos",
            "Sesion",
            "Ranking"
        };
        Transform[] views = new Transform[viewNames.Length];
        bool[] originalViewStates = new bool[viewNames.Length];

        if (content == null || title == null)
        {
            throw new System.InvalidOperationException(
                "El menu serializado no contiene su area de contenido o titulo."
            );
        }

        for (int i = 0; i < viewNames.Length; i++)
        {
            views[i] = content.Find(viewNames[i]);
            if (views[i] == null)
            {
                throw new System.InvalidOperationException(
                    "El menu serializado no contiene " + viewNames[i] + "."
                );
            }

            originalViewStates[i] = views[i].gameObject.activeSelf;
        }

        string originalTitle = title.text;

        try
        {
            previewCamera.targetTexture = renderTexture;
            settingsCanvas.worldCamera = previewCamera;
            string outputDirectory = Path.GetFullPath("Logs");
            Directory.CreateDirectory(outputDirectory);

            for (int viewIndex = 0; viewIndex < views.Length; viewIndex++)
            {
                for (int i = 0; i < views.Length; i++)
                {
                    views[i].gameObject.SetActive(i == viewIndex);
                }

                title.text = viewTitles[viewIndex];
                settingsCanvas.enabled = false;
                settingsCanvas.enabled = true;
                Graphic[] graphics = canvasTransform.GetComponentsInChildren<Graphic>(true);
                for (int graphicIndex = 0; graphicIndex < graphics.Length; graphicIndex++)
                {
                    graphics[graphicIndex].SetAllDirty();
                }

                Canvas.ForceUpdateCanvases();
                previewCamera.Render();
                Canvas.ForceUpdateCanvases();
                previewCamera.Render();

                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply(false, false);

                byte[] png = image.EncodeToPNG();
                string outputPath = Path.Combine(
                    outputDirectory,
                    "AlgoLabSettingsPreview-" + viewTitles[viewIndex] + ".png"
                );
                File.WriteAllBytes(outputPath, png);

                if (viewIndex == 0)
                {
                    File.WriteAllBytes(
                        Path.Combine(outputDirectory, "AlgoLabSettingsPreview.png"),
                        png
                    );
                }

                Debug.Log("ALGOLAB SETTINGS: vista previa guardada en " + outputPath + ".");
            }
        }
        finally
        {
            title.text = originalTitle;
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null)
                {
                    views[i].gameObject.SetActive(originalViewStates[i]);
                }
            }

            previewCamera.targetTexture = null;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);

            for (int i = 0; i < hiddenRoots.Count; i++)
            {
                if (hiddenRoots[i] != null)
                {
                    hiddenRoots[i].SetActive(originalRootStates[i]);
                }
            }
        }
    }

    private static void Install(bool batchMode)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject root = null;

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name != RootName)
            {
                continue;
            }

            if (root == null)
            {
                root = sceneRoot;
            }
            else
            {
                Object.DestroyImmediate(sceneRoot);
            }
        }

        bool rootCreado = root == null;

        if (rootCreado)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        AlgoLabSettingsMenuController controller =
            root.GetComponent<AlgoLabSettingsMenuController>();

        if (controller == null)
        {
            controller = root.AddComponent<AlgoLabSettingsMenuController>();
        }

        controller.ReconstruirVistaPreviaEnEditor();
        root.SetActive(true);

        Transform canvas = root.transform.Find("AlgoLabSettingsCanvas");
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }

        if (rootCreado)
        {
            ColocarVistaPrevia(root.transform);
        }

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new System.InvalidOperationException(
                "No se pudo guardar el menu de configuracion en " + ScenePath
            );
        }

        Debug.Log(
            "ALGOLAB SETTINGS: menu serializado en la escena con " +
            ContarObjetos(root.transform) +
            " objetos."
        );

        if (!batchMode)
        {
            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }

    private static void ColocarVistaPrevia(Transform root)
    {
        Camera camara = Camera.main;

        if (camara == null)
        {
            Camera[] camaras = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            if (camaras.Length > 0)
            {
                camara = camaras[0];
            }
        }

        if (camara != null)
        {
            Vector3 frente = camara.transform.forward.sqrMagnitude > 0.001f
                ? camara.transform.forward.normalized
                : Vector3.forward;

            root.position = camara.transform.position + frente * 1.2f - Vector3.up * 0.04f;
            root.rotation = Quaternion.LookRotation(frente, Vector3.up);
            return;
        }

        root.position = new Vector3(0f, 1.45f, 1.2f);
        root.rotation = Quaternion.identity;
    }

    private static int ContarObjetos(Transform root)
    {
        int total = 1;

        for (int i = 0; i < root.childCount; i++)
        {
            total += ContarObjetos(root.GetChild(i));
        }

        return total;
    }

}
