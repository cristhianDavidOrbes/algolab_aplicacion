using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlgoLabKonamiUnlockSetup
{
    [MenuItem("Tools/AlgoLab/Pruebas/Instalar codigo Konami")]
    private static void ConfigureFromMenu()
    {
        ConfigureBatch();
    }

    public static void ConfigureBatch()
    {
        int configuredScenes = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrWhiteSpace(buildScene.path))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            AlgoLabProgressPanel progressPanel =
                Object.FindFirstObjectByType<AlgoLabProgressPanel>(FindObjectsInactive.Include);

            if (progressPanel == null)
            {
                continue;
            }

            AlgoLabKonamiLevelUnlock detector =
                Object.FindFirstObjectByType<AlgoLabKonamiLevelUnlock>(FindObjectsInactive.Include);

            if (detector == null)
            {
                detector = progressPanel.gameObject.AddComponent<AlgoLabKonamiLevelUnlock>();
            }

            detector.progressPanel = progressPanel;
            detector.sessionManager =
                Object.FindFirstObjectByType<AlgoLabSessionManager>(FindObjectsInactive.Include);
            detector.tutorialController =
                Object.FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
            detector.flowStateManager =
                Object.FindFirstObjectByType<AlgoLabFlowStateManager>(FindObjectsInactive.Include);
            detector.maximumIntervalBetweenInputs = 2f;
            detector.directionThreshold = 0.72f;
            detector.releaseThreshold = 0.30f;
            detector.acceptEitherThumbstick = true;
            detector.vibrateOnSuccess = true;
            detector.successVibrationDuration = 0.22f;
            detector.showDebug = false;

            EditorUtility.SetDirty(detector);
            EditorUtility.SetDirty(progressPanel);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, buildScene.path))
            {
                throw new System.InvalidOperationException(
                    "No se pudo guardar el codigo Konami en " + buildScene.path
                );
            }

            configuredScenes++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("ALGOLAB KONAMI: configurado en " + configuredScenes + " escena(s).");
    }
}
