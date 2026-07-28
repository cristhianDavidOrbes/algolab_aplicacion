using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlgoLabStabilitySetup
{
    private const string MenuPath = "Tools/AlgoLab/Aplicar estabilidad integral";
    private const string DefaultScene = "Assets/Scenes/version_estable14.unity";
    private const string PublicBaseUrl = "https://appetite-tuesday-empty.ngrok-free.dev";

    [MenuItem(MenuPath)]
    public static void ApplyFromMenu()
    {
        ApplyInternal();
        EditorUtility.DisplayDialog(
            "AlgoLab",
            "Configuración de estabilidad aplicada y escena guardada.",
            "Aceptar"
        );
    }

    public static void ApplyBatch()
    {
        ApplyInternal();
    }

    private static void ApplyInternal()
    {
        string scenePath = GetEnabledScenePath();
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool changed = false;

        AlgoLabVoiceAssistant voice =
            UnityEngine.Object.FindFirstObjectByType<AlgoLabVoiceAssistant>(FindObjectsInactive.Include);
        if (voice == null)
        {
            throw new InvalidOperationException("No se encontró AlgoLabVoiceAssistant en " + scenePath + ".");
        }

        AlgoLabSpeechToTextClient stt = voice.GetComponent<AlgoLabSpeechToTextClient>();
        if (stt == null)
        {
            stt = voice.gameObject.AddComponent<AlgoLabSpeechToTextClient>();
            changed = true;
        }

        if (voice.speechToTextLocal != stt)
        {
            voice.speechToTextLocal = stt;
            changed = true;
        }

        string sttUrl = PublicBaseUrl + "/api/voz/transcribir";
        if (!string.Equals(stt.apiUrl, sttUrl, StringComparison.Ordinal))
        {
            stt.apiUrl = sttUrl;
            changed = true;
        }

        if (voice.iaClient != null)
        {
            string iaUrl = PublicBaseUrl + "/api/ia/responder";
            if (!string.Equals(voice.iaClient.iaApiUrl, iaUrl, StringComparison.Ordinal))
            {
                voice.iaClient.iaApiUrl = iaUrl;
                changed = true;
            }
        }

        AlgoLabVehicleRoomCommandController[] commands =
            UnityEngine.Object.FindObjectsByType<AlgoLabVehicleRoomCommandController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < commands.Length; i++)
        {
            AlgoLabVehicleRoomCommandController command = commands[i];
            if (command == null)
                continue;

            if (command.rayOrigin == null)
            {
                command.rayOrigin = command.transform;
                changed = true;
            }

            if (!command.desactivarEnTrackingDeManos)
            {
                command.desactivarEnTrackingDeManos = true;
                changed = true;
            }

            if (!command.usarPlanoHorizontalDeRespaldo)
            {
                command.usarPlanoHorizontalDeRespaldo = true;
                changed = true;
            }

            float safeMinimum = Mathf.Max(0.1f, command.distanciaMinimaDestino);
            if (!Mathf.Approximately(command.distanciaMinimaDestino, safeMinimum))
            {
                command.distanciaMinimaDestino = safeMinimum;
                changed = true;
            }
        }

        AlgoLabProgressPanel progress =
            UnityEngine.Object.FindFirstObjectByType<AlgoLabProgressPanel>(FindObjectsInactive.Include);
        if (progress != null && progress.textoBotonCompletarPilar != "Completar práctica")
        {
            progress.textoBotonCompletarPilar = "Completar práctica";
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Unity no pudo guardar " + scenePath + ".");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("ALGOLAB_STABILITY_SETUP scene=" + scenePath + " changed=" + changed);
    }

    private static string GetEnabledScenePath()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled && File.Exists(scenes[i].path))
                return scenes[i].path;
        }

        if (File.Exists(DefaultScene))
            return DefaultScene;

        throw new FileNotFoundException("No hay una escena habilitada de AlgoLab.");
    }
}
