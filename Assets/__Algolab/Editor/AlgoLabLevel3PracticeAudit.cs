#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlgoLabLevel3PracticeAudit
{
    private const string MonitorScene =
        "Assets/Scenes/Nivel3_Monitor_Editable.unity";
    private const string MonitorPrefab =
        "Assets/__Algolab/Resources/Level3/RobotWorkshop/Prefabs/MonitorNivel3.prefab";
    private const string RobotPrefab =
        "Assets/__Algolab/Resources/Level3/RobotWorkshop/Prefabs/RobotNivel3Editado.prefab";
    private const string PracticePrefab =
        "Assets/__Algolab/Resources/Level3/AlgoLabRobotPractice.prefab";

    [MenuItem("AlgoLab/Nivel 3/Practica/Auditar monitor y robot")]
    public static void Run()
    {
        var report = new StringBuilder(32768);
        report.AppendLine("ALGOLAB NIVEL 3 - AUDITORIA DE AUTORIA");

        Scene scene = EditorSceneManager.OpenScene(MonitorScene, OpenSceneMode.Single);
        report.AppendLine();
        report.AppendLine("=== ESCENA MONITOR ===");
        foreach (GameObject root in scene.GetRootGameObjects())
            AppendTransform(report, root.transform, 0);

        AppendPrefab(report, "PREFAB MONITOR", MonitorPrefab);
        AppendPrefab(report, "PREFAB ROBOT", RobotPrefab);
        AppendPrefab(report, "PREFAB PRACTICA FINAL", PracticePrefab);
        GameObject resource =
            Resources.Load<GameObject>("Level3/AlgoLabRobotPractice");
        report.AppendLine();
        report.AppendLine("=== VALIDACION RESOURCE ===");
        report.AppendLine("resource=" + (resource != null));
        report.AppendLine(
            "runtime=" +
            (resource != null &&
             resource.GetComponent<AlgoLabEncapsulationRobotPractice>() != null)
        );
        report.AppendLine(
            "modelo=" +
            (resource != null &&
             resource.transform.Find(
                 "RobotPracticeVisual/Robot/ModeloRobotRigged"
             ) != null)
        );
        report.AppendLine(
            "vidrioTemperatura=" +
            (resource != null &&
             resource.transform.Find(
                 "RobotPracticeVisual/Robot/ModeloRobotRigged/Torso/CompartimientoTemperatura/VidrioTemperatura"
             ) != null)
        );
        report.AppendLine(
            "vidrioBateria=" +
            (resource != null &&
             resource.transform.Find(
                 "RobotPracticeVisual/Robot/ModeloRobotRigged/Torso/CompartimientoBateriaTrasero/VidrioBateria"
             ) != null)
        );

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string logFolder = Path.Combine(projectRoot, "Logs");
        Directory.CreateDirectory(logFolder);
        string output = Path.Combine(logFolder, "level3-practice-audit.txt");
        File.WriteAllText(output, report.ToString());
        Debug.Log("NIVEL 3 AUDIT: " + output);

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    private static void AppendPrefab(
        StringBuilder report,
        string title,
        string assetPath)
    {
        report.AppendLine();
        report.AppendLine("=== " + title + " ===");
        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            AppendTransform(report, root.transform, 0);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AppendTransform(
        StringBuilder report,
        Transform transform,
        int depth)
    {
        string indent = new string(' ', depth * 2);
        Component[] components = transform.GetComponents<Component>();
        var componentNames = new StringBuilder();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
                continue;
            if (componentNames.Length > 0)
                componentNames.Append(", ");
            componentNames.Append(components[i].GetType().Name);
        }

        report.Append(indent)
            .Append(transform.name)
            .Append(" | active=").Append(transform.gameObject.activeSelf)
            .Append(" | localPosition=").Append(Format(transform.localPosition))
            .Append(" | localEuler=").Append(Format(transform.localEulerAngles))
            .Append(" | localScale=").Append(Format(transform.localScale))
            .Append(" | components=[").Append(componentNames).Append(']');

        if (TryGetLocalBounds(transform, out Bounds bounds))
        {
            report.Append(" | rendererBoundsCenter=")
                .Append(Format(bounds.center))
                .Append(" | rendererBoundsSize=")
                .Append(Format(bounds.size));
        }
        report.AppendLine();

        for (int i = 0; i < transform.childCount; i++)
            AppendTransform(report, transform.GetChild(i), depth + 1);
    }

    private static bool TryGetLocalBounds(Transform root, out Bounds bounds)
    {
        Renderer ownRenderer = root.GetComponent<Renderer>();
        if (ownRenderer == null)
        {
            bounds = default;
            return false;
        }

        bounds = ownRenderer.bounds;
        return true;
    }

    private static string Format(Vector3 value)
    {
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "({0:0.####},{1:0.####},{2:0.####})",
            value.x,
            value.y,
            value.z
        );
    }
}
#endif
