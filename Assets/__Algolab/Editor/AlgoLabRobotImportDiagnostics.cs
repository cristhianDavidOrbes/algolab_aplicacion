using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlgoLabRobotImportDiagnostics
{
    public static void RunFromCommandLine()
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();
        StringBuilder report = new StringBuilder();

        GameObject prefab = Resources.Load<GameObject>(
            "Level3/RobotWorkshop/Models/Robot/AlgoLabRobot"
        );
        if (prefab == null)
        {
            Debug.LogError("ROBOT_DIAGNOSTIC: no se encontro el FBX.");
            return;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = "RobotImportDiagnostic";
        SceneManager.MoveGameObjectToScene(instance, previewScene);
        AppendRenderers(report, "FBX DIRECTO", instance.transform);
        AppendBones(report, instance.transform);

        GameObject practicePrefab = Resources.Load<GameObject>(
            "Level3/AlgoLabRobotPractice"
        );
        if (practicePrefab == null)
        {
            Debug.LogError(
                "ROBOT_DIAGNOSTIC: no se encontro el prefab editable de la practica."
            );
            Object.DestroyImmediate(instance);
            EditorSceneManager.ClosePreviewScene(previewScene);
            return;
        }

        GameObject practiceRoot = Object.Instantiate(practicePrefab);
        practiceRoot.name = "PracticeDiagnostic";
        SceneManager.MoveGameObjectToScene(practiceRoot, previewScene);
        AlgoLabEncapsulationRobotPractice practice =
            practiceRoot.GetComponent<AlgoLabEncapsulationRobotPractice>();
        practice.mostrarDebug = false;
        practice.completarNivelAutomaticamente = false;
        practice.IniciarPractica();

        Transform robot = practiceRoot.transform.Find("RobotPracticeVisual/Robot");
        Transform model = robot != null ? robot.Find("ModeloRobotRigged") : null;
        report.AppendLine("PRACTICA");
        AppendTransform(report, "Robot", robot);
        AppendTransform(report, "ModeloRobotRigged", model);
        if (model != null)
            AppendRenderers(report, "MODELO NORMALIZADO", model);

        AppendTransform(
            report,
            "VidrioTemperatura",
            practiceRoot.transform.Find(
                "RobotPracticeVisual/Robot/CompartimientoTemperatura/VidrioTemperatura"
            )
        );
        AppendTransform(
            report,
            "ModuloTemperatura",
            practiceRoot.transform.Find(
                "RobotPracticeVisual/Robot/CompartimientoTemperatura/ModuloTemperaturaExtraible"
            )
        );
        AppendTransform(
            report,
            "VidrioBateria",
            practiceRoot.transform.Find(
                "RobotPracticeVisual/Robot/CompartimientoBateriaTrasero/VidrioBateria"
            )
        );
        AppendTransform(
            report,
            "Bateria",
            practiceRoot.transform.Find(
                "RobotPracticeVisual/Robot/CompartimientoBateriaTrasero/BateriaExtraible"
            )
        );

        Debug.Log("ROBOT_DIAGNOSTIC_BEGIN\n" + report + "ROBOT_DIAGNOSTIC_END");
        Object.DestroyImmediate(practiceRoot);
        Object.DestroyImmediate(instance);
        EditorSceneManager.ClosePreviewScene(previewScene);
    }

    private static void AppendRenderers(
        StringBuilder report,
        string title,
        Transform root)
    {
        report.AppendLine(title);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds combined = default;
        bool initialized = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!initialized)
            {
                combined = renderer.bounds;
                initialized = true;
            }
            else
            {
                combined.Encapsulate(renderer.bounds);
            }

            Material material = renderer.sharedMaterial;
            Texture texture = null;
            if (material != null)
            {
                if (material.HasProperty("_BaseMap"))
                    texture = material.GetTexture("_BaseMap");
                if (texture == null && material.HasProperty("_MainTex"))
                    texture = material.GetTexture("_MainTex");
            }

            report.AppendLine(
                renderer.name +
                " | type=" + renderer.GetType().Name +
                " | center=" + Format(renderer.bounds.center) +
                " | size=" + Format(renderer.bounds.size) +
                " | material=" + (material != null ? material.name : "null") +
                " | shader=" + (material != null && material.shader != null
                    ? material.shader.name
                    : "null") +
                " | texture=" + (texture != null ? texture.name : "null")
            );
        }

        report.AppendLine(
            "COMBINED | center=" + Format(combined.center) +
            " | size=" + Format(combined.size)
        );
    }

    private static void AppendBones(StringBuilder report, Transform root)
    {
        string[] names = { "Arm.L", "Arm.R", "Leg.L", "Leg.R", "head", "Torso" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform found = FindRecursive(root, names[i]);
            AppendTransform(report, "Bone " + names[i], found);
        }
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == name)
                return children[i];
        }
        return null;
    }

    private static void AppendTransform(
        StringBuilder report,
        string label,
        Transform transform)
    {
        if (transform == null)
        {
            report.AppendLine(label + " | null");
            return;
        }

        Collider collider = transform.GetComponent<Collider>();
        report.AppendLine(
            label +
            " | worldPos=" + Format(transform.position) +
            " | localPos=" + Format(transform.localPosition) +
            " | localEuler=" + Format(transform.localEulerAngles) +
            " | localScale=" + Format(transform.localScale) +
            (collider != null
                ? " | colliderCenter=" + Format(collider.bounds.center) +
                  " | colliderSize=" + Format(collider.bounds.size)
                : string.Empty)
        );
    }

    private static string Format(Vector3 value)
    {
        return string.Format("({0:F4},{1:F4},{2:F4})", value.x, value.y, value.z);
    }
}
