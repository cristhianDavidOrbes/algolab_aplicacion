using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AlgoLabRobotPracticePreviewCapture
{
    public static void CaptureFromCommandLine()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("RobotPracticePreview");
        AlgoLabEncapsulationRobotPractice practice =
            root.AddComponent<AlgoLabEncapsulationRobotPractice>();
        practice.mostrarDebug = false;
        practice.completarNivelAutomaticamente = false;
        practice.IniciarPractica();

        Disable(root.transform, "RobotPracticeVisual/PanelHerramientasPublicas");
        Disable(root.transform, "RobotPracticeVisual/PantallaEstadoRobot");

        CreateLight("Key", new Vector3(-2f, 2.8f, -2.2f), 1.35f);
        CreateLight("Fill", new Vector3(2.2f, 1.6f, -1.3f), 0.75f);
        CreateLight("Back", new Vector3(0.6f, 2.2f, 2.3f), 0.85f);

        Camera camera = CreateCamera();
        string outputDirectory =
            Path.Combine(Application.dataPath, "..", "Logs", "RobotLevel3Preview");
        Directory.CreateDirectory(outputDirectory);

        Vector3 target = new Vector3(0f, 0.15f, 0.22f);
        Render(
            camera,
            new Vector3(0f, 0.15f, -2.35f),
            target,
            Path.Combine(outputDirectory, "robot_front_glass.png")
        );

        SetGlassVisible(root, AlgoLabRobotBreakableGlass.Compartimiento.Temperatura, false);
        Render(
            camera,
            new Vector3(0f, 0.15f, -2.35f),
            target,
            Path.Combine(outputDirectory, "robot_front_module.png")
        );

        SetGlassVisible(root, AlgoLabRobotBreakableGlass.Compartimiento.Bateria, false);
        Render(
            camera,
            new Vector3(0f, 0.15f, 2.80f),
            target,
            Path.Combine(outputDirectory, "robot_back_battery.png")
        );

        Debug.Log("ALGOLAB_ROBOT_PREVIEW=" + outputDirectory);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(camera.gameObject);
    }

    private static void Disable(Transform root, string path)
    {
        Transform child = root.Find(path);
        if (child != null)
            child.gameObject.SetActive(false);
    }

    private static void SetGlassVisible(
        GameObject root,
        AlgoLabRobotBreakableGlass.Compartimiento compartment,
        bool visible)
    {
        AlgoLabRobotBreakableGlass[] glasses =
            root.GetComponentsInChildren<AlgoLabRobotBreakableGlass>(true);
        for (int i = 0; i < glasses.Length; i++)
        {
            if (glasses[i].compartimiento != compartment)
                continue;
            if (glasses[i].vidrioRenderer != null)
                glasses[i].vidrioRenderer.enabled = visible;
            if (glasses[i].vidrioCollider != null)
                glasses[i].vidrioCollider.enabled = visible;
        }
    }

    private static Camera CreateCamera()
    {
        GameObject go = new GameObject("PreviewCamera");
        Camera camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
        camera.fieldOfView = 42f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 20f;
        return camera;
    }

    private static void CreateLight(
        string name,
        Vector3 position,
        float intensity)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 7f;
        light.intensity = intensity;
    }

    private static void Render(
        Camera camera,
        Vector3 position,
        Vector3 target,
        string path)
    {
        camera.transform.position = position;
        camera.transform.rotation =
            Quaternion.LookRotation((target - position).normalized, Vector3.up);

        RenderTexture texture = new RenderTexture(1024, 1024, 24);
        camera.targetTexture = texture;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        Texture2D image = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());

        RenderTexture.active = previous;
        camera.targetTexture = null;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(texture);
    }
}
