#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlgoLabFinalizeRobotAuthoring
{
    private const string EditableScenePath =
        "Assets/Scenes/Nivel3_Robot_Editable.unity";
    private const string MainScenePath =
        "Assets/Scenes/version_estable14.unity";
    private const string RobotPrefabFolder =
        "Assets/__Algolab/Resources/Level3/RobotWorkshop/Prefabs";
    private const string RobotPrefabPath =
        RobotPrefabFolder + "/RobotNivel3Editado.prefab";
    private const string SessionKey =
        "AlgoLabFinalizeRobotAuthoring_20260726_v1";

    [InitializeOnLoadMethod]
    private static void ScheduleOnce()
    {
        if (Application.isBatchMode ||
            SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EditableScenePath) == null)
                return;

            SessionState.SetBool(SessionKey, true);
            FinalizeNow();
        };
    }

    [MenuItem("AlgoLab/Nivel 3/Robot/Finalizar autoria del robot")]
    public static void FinalizeNow()
    {
        Scene scene = SceneManager.GetSceneByPath(EditableScenePath);
        bool openedAdditively = !scene.IsValid() || !scene.isLoaded;
        if (openedAdditively)
        {
            scene = EditorSceneManager.OpenScene(
                EditableScenePath,
                OpenSceneMode.Additive
            );
        }

        GameObject practiceRoot = FindPracticeRoot(scene);
        if (practiceRoot == null)
        {
            throw new System.InvalidOperationException(
                "No se encontro la practica editable del robot."
            );
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AlgoLabLevel3RobotPracticeSetup.ApplyEditableSceneToPrefab();

        Transform robot = practiceRoot.transform.Find(
            "RobotPracticeVisual/Robot"
        );
        if (robot == null)
        {
            throw new System.InvalidOperationException(
                "No se encontro RobotPracticeVisual/Robot."
            );
        }

        EnsureFolder(RobotPrefabFolder);
        PrefabUtility.SaveAsPrefabAsset(robot.gameObject, RobotPrefabPath);
        AssetDatabase.SaveAssets();

        if (scene == SceneManager.GetActiveScene())
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }
        else if (openedAdditively)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        if (!AssetDatabase.DeleteAsset(EditableScenePath))
        {
            throw new System.InvalidOperationException(
                "No se pudo retirar la escena temporal del robot."
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "NIVEL 3 ROBOT: prefab definitivo guardado en " + RobotPrefabPath +
            " y escena temporal retirada."
        );
    }

    private static GameObject FindPracticeRoot(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponent<AlgoLabEncapsulationRobotPractice>() != null)
                return roots[i];
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent =
            System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) &&
            !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
