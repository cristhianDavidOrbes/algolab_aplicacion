#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public static class AlgoLabPracticeTutorialSceneInstaller
{
    private const string ScenePath = "Assets/Scenes/version_estable14.unity";
    private const string VideoNivel1 = "Assets/__Algolab/_TutorialSystem/videos/practicas/practica_nivel_1_tutorial.mp4";
    private const string VideoNivel2 = "Assets/__Algolab/_TutorialSystem/videos/practicas/practica_nivel_2_tutorial.mp4";

    [MenuItem("AlgoLab/Instalar tutoriales de practicas")]
    public static void Install()
    {
        Scene escena = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var tutorial = Object.FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
        var nivel1 = Object.FindFirstObjectByType<AlgoLabCarPracticeController>(FindObjectsInactive.Include);
        var nivel2 = Object.FindFirstObjectByType<AlgoLabLevel02PracticeController>(FindObjectsInactive.Include);
        var pilares = Object.FindFirstObjectByType<AlgoLabPillarLevelController>(FindObjectsInactive.Include);

        if (tutorial == null || nivel1 == null || nivel2 == null || pilares == null)
            throw new System.InvalidOperationException("Faltan controladores para instalar los tutoriales de practica.");

        AlgoLabPracticeTutorialSequence secuencia1 = ObtenerOCrear(nivel1.gameObject);
        secuencia1.tipoPractica = AlgoLabPracticeTutorialSequence.TipoPractica.Nivel1AtributosYMetodos;
        secuencia1.tutorialPanel = tutorial;
        secuencia1.videoTutorial = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoNivel1);
        secuencia1.narraciones = new List<AudioClip> { nivel1.audioInstruccionesPractica };
        nivel1.tutorialMultimedia = secuencia1;

        AlgoLabPracticeTutorialSequence secuencia2 = ObtenerOCrear(nivel2.gameObject);
        secuencia2.tipoPractica = AlgoLabPracticeTutorialSequence.TipoPractica.Nivel2CrearObjetos;
        secuencia2.tutorialPanel = tutorial;
        secuencia2.videoTutorial = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoNivel2);
        secuencia2.narraciones = new List<AudioClip>
        {
            CargarAudio("practice1.mp3"),
            CargarAudio("practice2.mp3"),
            CargarAudio("practice3.mp3"),
            CargarAudio("practice4.mp3"),
            CargarAudio("practice5.mp3"),
            CargarAudio("practice6.mp3")
        };
        nivel2.tutorialMultimedia = secuencia2;

        AlgoLabPracticeTutorialSequence secuencia3 = ObtenerOCrear(pilares.gameObject);
        secuencia3.tipoPractica = AlgoLabPracticeTutorialSequence.TipoPractica.Nivel3Encapsulamiento;
        secuencia3.tutorialPanel = tutorial;
        if (secuencia3.narraciones == null)
            secuencia3.narraciones = new List<AudioClip>();
        pilares.tutorialPracticaNivel3 = secuencia3;

        ValidarAsignaciones(secuencia1, 1, 1);
        ValidarAsignaciones(secuencia2, 2, 6);
        EditorUtility.SetDirty(secuencia1);
        EditorUtility.SetDirty(secuencia2);
        EditorUtility.SetDirty(secuencia3);
        EditorUtility.SetDirty(nivel1);
        EditorUtility.SetDirty(nivel2);
        EditorUtility.SetDirty(pilares);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        ActualizarBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("TUTORIALES PRACTICA: niveles 1 y 2 instalados; nivel 3 preparado.");
    }

    public static void InstallBatch()
    {
        Install();
    }

    private static AlgoLabPracticeTutorialSequence ObtenerOCrear(GameObject objeto)
    {
        AlgoLabPracticeTutorialSequence secuencia = objeto.GetComponent<AlgoLabPracticeTutorialSequence>();
        return secuencia != null ? secuencia : objeto.AddComponent<AlgoLabPracticeTutorialSequence>();
    }

    private static AudioClip CargarAudio(string nombre)
    {
        string ruta = "Assets/__Algolab/Audio/level2-tema/practica/" + nombre;
        return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
    }

    private static void ValidarAsignaciones(AlgoLabPracticeTutorialSequence secuencia, int nivel, int audiosEsperados)
    {
        if (secuencia.videoTutorial == null)
            throw new System.InvalidOperationException("No se importo el video del nivel " + nivel + ".");
        if (secuencia.narraciones == null || secuencia.narraciones.Count != audiosEsperados)
            throw new System.InvalidOperationException("Cantidad de audios incorrecta en nivel " + nivel + ".");
        for (int i = 0; i < secuencia.narraciones.Count; i++)
            if (secuencia.narraciones[i] == null)
                throw new System.InvalidOperationException("Falta audio " + (i + 1) + " del nivel " + nivel + ".");
    }

    private static void ActualizarBuildSettings()
    {
        EditorBuildSettingsScene[] escenas = EditorBuildSettings.scenes;
        bool reemplazada = false;
        for (int i = 0; i < escenas.Length; i++)
        {
            if (escenas[i].path.Contains("version_estable13") || escenas[i].path.Contains("version_estable14"))
            {
                escenas[i] = new EditorBuildSettingsScene(ScenePath, true);
                reemplazada = true;
            }
        }
        if (!reemplazada)
        {
            var lista = new List<EditorBuildSettingsScene>(escenas)
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            escenas = lista.ToArray();
        }
        EditorBuildSettings.scenes = escenas;
    }
}
#endif
