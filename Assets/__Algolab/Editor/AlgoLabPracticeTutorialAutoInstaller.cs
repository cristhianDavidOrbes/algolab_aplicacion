#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[InitializeOnLoad]
public static class AlgoLabPracticeTutorialAutoInstaller
{
    private const string ScenePath = "Assets/Scenes/version_estable14.unity";
    private static int intentos;

    static AlgoLabPracticeTutorialAutoInstaller()
    {
        EditorApplication.delayCall += IntentarInstalar;
    }

    private static void IntentarInstalar()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += IntentarInstalar;
            return;
        }

        Scene escena = SceneManager.GetActiveScene();
        if (!escena.IsValid() || escena.path != ScenePath)
            return;

        var tutorial = Object.FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
        var nivel1 = Object.FindFirstObjectByType<AlgoLabCarPracticeController>(FindObjectsInactive.Include);
        var nivel2 = Object.FindFirstObjectByType<AlgoLabLevel02PracticeController>(FindObjectsInactive.Include);
        var pilares = Object.FindFirstObjectByType<AlgoLabPillarLevelController>(FindObjectsInactive.Include);
        if (tutorial == null || nivel1 == null || nivel2 == null || pilares == null)
            return;

        VideoClip video1 = AssetDatabase.LoadAssetAtPath<VideoClip>(
            "Assets/__Algolab/_TutorialSystem/videos/practicas/practica_nivel_1_tutorial.mp4");
        VideoClip video2 = AssetDatabase.LoadAssetAtPath<VideoClip>(
            "Assets/__Algolab/_TutorialSystem/videos/practicas/practica_nivel_2_tutorial.mp4");
        if (video1 == null || video2 == null)
        {
            if (++intentos < 6) EditorApplication.delayCall += IntentarInstalar;
            return;
        }

        if (nivel1.tutorialMultimedia != null && nivel2.tutorialMultimedia != null &&
            pilares.tutorialPracticaNivel3 != null &&
            nivel1.tutorialMultimedia.videoTutorial != null && nivel2.tutorialMultimedia.videoTutorial != null)
            return;

        AlgoLabPracticeTutorialSequence secuencia1 = ObtenerOCrear(nivel1.gameObject);
        secuencia1.tipoPractica = AlgoLabPracticeTutorialSequence.TipoPractica.Nivel1AtributosYMetodos;
        secuencia1.tutorialPanel = tutorial;
        secuencia1.videoTutorial = video1;
        secuencia1.narraciones = new List<AudioClip> { nivel1.audioInstruccionesPractica };
        nivel1.tutorialMultimedia = secuencia1;

        AlgoLabPracticeTutorialSequence secuencia2 = ObtenerOCrear(nivel2.gameObject);
        secuencia2.tipoPractica = AlgoLabPracticeTutorialSequence.TipoPractica.Nivel2CrearObjetos;
        secuencia2.tutorialPanel = tutorial;
        secuencia2.videoTutorial = video2;
        secuencia2.narraciones = new List<AudioClip>();
        for (int i = 1; i <= 6; i++)
        {
            secuencia2.narraciones.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/__Algolab/Audio/level2-tema/practica/practice" + i + ".mp3"));
        }
        nivel2.tutorialMultimedia = secuencia2;

        AlgoLabPracticeTutorialSequence secuencia3 = ObtenerOCrear(pilares.gameObject);
        secuencia3.tipoPractica = AlgoLabPracticeTutorialSequence.TipoPractica.Nivel3Encapsulamiento;
        secuencia3.tutorialPanel = tutorial;
        if (secuencia3.narraciones == null)
            secuencia3.narraciones = new List<AudioClip>();
        pilares.tutorialPracticaNivel3 = secuencia3;

        EditorUtility.SetDirty(secuencia1);
        EditorUtility.SetDirty(secuencia2);
        EditorUtility.SetDirty(secuencia3);
        EditorUtility.SetDirty(nivel1);
        EditorUtility.SetDirty(nivel2);
        EditorUtility.SetDirty(pilares);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();
        Debug.Log("TUTORIALES PRACTICA: niveles 1 y 2 integrados; nivel 3 preparado.");
    }

    private static AlgoLabPracticeTutorialSequence ObtenerOCrear(GameObject objeto)
    {
        AlgoLabPracticeTutorialSequence secuencia = objeto.GetComponent<AlgoLabPracticeTutorialSequence>();
        return secuencia != null ? secuencia : objeto.AddComponent<AlgoLabPracticeTutorialSequence>();
    }
}
#endif
