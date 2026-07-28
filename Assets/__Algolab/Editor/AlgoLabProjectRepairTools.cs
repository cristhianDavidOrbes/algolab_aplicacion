using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AlgoLabProjectRepairTools
{
    [MenuItem("Tools/AlgoLab/Configurar pilares de POO")]
    private static void ConfigurePillarLevelsFromMenu()
    {
        ConfigurePillarLevelsBatch();
    }

    public static void ConfigurePillarLevelsBatch()
    {
        int escenasActualizadas = 0;
        int nivelesActualizados = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrEmpty(buildScene.path))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            AlgoLabProgressLevelInfo[] infos = Object.FindObjectsByType<AlgoLabProgressLevelInfo>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            bool sceneChanged = false;
            for (int i = 0; i < infos.Length; i++)
            {
                AlgoLabProgressLevelInfo info = infos[i];
                if (info == null || info.gameObject == null)
                {
                    continue;
                }

                string nombreObjeto = info.gameObject.name.Trim().ToLowerInvariant();
                int nivel = ObtenerNumeroNivelPilar(nombreObjeto);
                if (nivel < 3 || nivel > 6)
                {
                    continue;
                }

                // Level5 tenía dos componentes de información superpuestos. Conservamos uno.
                AlgoLabProgressLevelInfo[] duplicados =
                    info.gameObject.GetComponents<AlgoLabProgressLevelInfo>();
                if (duplicados.Length > 1 && duplicados[0] != info)
                {
                    continue;
                }

                ConfigurarInfoPilar(info, nivel);
                EditorUtility.SetDirty(info);
                sceneChanged = true;
                nivelesActualizados++;

                for (int duplicadoIndex = 1; duplicadoIndex < duplicados.Length; duplicadoIndex++)
                {
                    if (duplicados[duplicadoIndex] != null)
                    {
                        Object.DestroyImmediate(duplicados[duplicadoIndex], true);
                        sceneChanged = true;
                    }
                }
            }

            if (!sceneChanged)
            {
                continue;
            }

            escenasActualizadas++;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, buildScene.path))
            {
                throw new System.InvalidOperationException(
                    "No se pudo guardar la escena con los pilares: " + buildScene.path
                );
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            "ALGOLAB POO: configurados " + nivelesActualizados +
            " niveles pilar en " + escenasActualizadas + " escenas."
        );
    }

    private static int ObtenerNumeroNivelPilar(string nombreObjeto)
    {
        if (nombreObjeto == "level3") return 3;
        if (nombreObjeto == "level4") return 4;
        if (nombreObjeto == "level5") return 5;
        if (nombreObjeto == "level6") return 6;
        return -1;
    }

    private static void ConfigurarInfoPilar(AlgoLabProgressLevelInfo info, int nivel)
    {
        switch (nivel)
        {
            case 3:
                info.nombreNivel = "Encapsulamiento";
                info.descripcionNivel =
                    "Aprende a proteger el estado interno de un objeto. Identifica qué datos deben mantenerse privados y qué métodos públicos forman una interfaz segura para usar una clase sin romper sus reglas.";
                info.tareaPractica =
                    "Clasifica los datos privados y los métodos públicos de una CuentaSegura. Decide qué operaciones pueden modificar el saldo.";
                break;

            case 4:
                info.nombreNivel = "Abstracción";
                info.descripcionNivel =
                    "Aprende a mostrar solo lo esencial de un objeto y a ocultar los detalles que no necesitas para utilizarlo. Construye una interfaz clara para encender un vehículo sin exponer su complejidad interna.";
                info.tareaPractica =
                    "Separa la interfaz pública de un Vehículo de los detalles internos que deben quedar ocultos. Conserva solo las operaciones esenciales.";
                break;

            case 5:
                info.nombreNivel = "Herencia";
                info.descripcionNivel =
                    "Aprende cómo una clase hija reutiliza y especializa atributos y métodos de una clase padre. Compara Vehículo con Carro, Moto y Camión para reconocer qué comportamiento se hereda y qué comportamiento cambia.";
                info.tareaPractica =
                    "Clasifica los elementos que pertenecen a la clase padre Vehículo y los que son específicos de Carro, Moto o Camión.";
                break;

            case 6:
                info.nombreNivel = "Polimorfismo";
                info.descripcionNivel =
                    "Aprende cómo una misma operación puede producir comportamientos diferentes según el objeto que la ejecuta. Usa una referencia Vehículo para observar respuestas especializadas de Carro, Moto y Camión.";
                info.tareaPractica =
                    "Relaciona cada llamada a acelerar() con la implementación correcta de Carro, Moto o Camión y explica por qué cambia el resultado.";
                break;
        }

        info.tiempoPractica = nivel == 3 ? "05:00" : "02:00";
        info.nombreEscena = "";
    }

    [MenuItem("Tools/AlgoLab/Diagnosticar paneles XR")]
    private static void DumpPanelDiagnosticsFromMenu()
    {
        DumpPanelDiagnosticsBatch();
    }

    public static void DumpPanelDiagnosticsBatch()
    {
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrEmpty(buildScene.path))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            StringBuilder report = new StringBuilder();
            report.AppendLine("ALGOLAB PANEL DIAGNOSTICS: " + buildScene.path);

            AlgoLabPanelGrabHandle[] handles =
                Object.FindObjectsByType<AlgoLabPanelGrabHandle>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            report.AppendLine("Grab handles: " + handles.Length);
            for (int i = 0; i < handles.Length; i++)
            {
                AlgoLabPanelGrabHandle handle = handles[i];
                report.AppendLine(
                    "HANDLE " + GetHierarchyPath(handle.transform) +
                    " | root=" + GetHierarchyPath(handle.panelRoot) +
                    " | billboard=" + GetHierarchyPath(handle.billboard != null ? handle.billboard.transform : null) +
                    " | mantenerBillboard=" + handle.mantenerBillboardActivoDuranteAgarre +
                    " | tutorialForzado=" + handle.forzarEsteHandleComoTutorial +
                    " | diagramaForzado=" + handle.forzarEsteHandleComoPanelDiagrama
                );
            }

            AlgoLabPocketPanelItem[] items =
                Object.FindObjectsByType<AlgoLabPocketPanelItem>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            report.AppendLine("Pocket items: " + items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                AlgoLabPocketPanelItem item = items[i];
                report.AppendLine(
                    "ITEM " + GetHierarchyPath(item.transform) +
                    " | nombre=" + item.nombreCorto +
                    " | root=" + GetHierarchyPath(item.panelRoot) +
                    " | ancla=" + GetHierarchyPath(item.puntoMedicionGuardado) +
                    " | escala=" + (item.panelRoot != null ? item.panelRoot.localScale.ToString("F4") : "null")
                );
            }

            AlgoLabTutorialPanelController tutorial =
                Object.FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
            if (tutorial != null)
            {
                report.AppendLine(
                    "TUTORIAL " + GetHierarchyPath(tutorial.transform) +
                    " | panelRoot=" + GetHierarchyPath(tutorial.panelRoot) +
                    " | rootParaUbicar=" + GetHierarchyPath(tutorial.rootParaUbicar) +
                    " | cabeza=" + GetHierarchyPath(tutorial.cabezaJugador) +
                    " | posPropia=" + tutorial.posicionLocalTutorialPropia.ToString("F3") +
                    " | soloY=" + tutorial.soloRotacionYTutorial +
                    " | primeraVez=" + tutorial.ubicarTutorialSoloLaPrimeraVez +
                    " | noReubicarPocket=" + tutorial.noReubicarDespuesDeSalirDelPocket
                );
            }

            AlgoLabPanelPocketManager pocket =
                Object.FindFirstObjectByType<AlgoLabPanelPocketManager>(FindObjectsInactive.Include);
            if (pocket != null)
            {
                report.AppendLine(
                    "POCKET " + GetHierarchyPath(pocket.transform) +
                    " | visual=" + GetHierarchyPath(pocket.pocketVisualRoot != null ? pocket.pocketVisualRoot.transform : null) +
                    " | cards=" + GetHierarchyPath(pocket.miniCardsParent) +
                    " | camara=" + GetHierarchyPath(pocket.camaraJugador != null ? pocket.camaraJugador.transform : null)
                );
            }

            Debug.Log(report.ToString());
        }
    }

    [MenuItem("Tools/AlgoLab/Reparar orden de eventos del tutorial")]
    private static void NormalizeTutorialTimelineFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        NormalizeTutorialTimeline(false);
    }

    public static void NormalizeTutorialTimelineBatch()
    {
        NormalizeTutorialTimeline(true);
    }

    private static void NormalizeTutorialTimeline(bool batchMode)
    {
        int changedEvents = 0;
        int changedScenes = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrEmpty(buildScene.path))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            AlgoLabTutorialPanelController[] tutorials =
                Object.FindObjectsByType<AlgoLabTutorialPanelController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            bool sceneChanged = false;
            for (int tutorialIndex = 0; tutorialIndex < tutorials.Length; tutorialIndex++)
            {
                AlgoLabTutorialPanelController tutorial = tutorials[tutorialIndex];
                int changes = NormalizeTimeline(tutorial);
                if (changes <= 0)
                {
                    continue;
                }

                changedEvents += changes;
                sceneChanged = true;
                EditorUtility.SetDirty(tutorial);
            }

            if (!sceneChanged)
            {
                continue;
            }

            changedScenes++;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, buildScene.path))
            {
                throw new System.InvalidOperationException(
                    "No se pudo guardar la escena reparada " + buildScene.path + "."
                );
            }
        }

        Debug.Log(
            "ALGOLAB REPAIR: " + changedEvents +
            " ordenes de eventos normalizados en " + changedScenes + " escenas."
        );

        if (batchMode)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private static int NormalizeTimeline(AlgoLabTutorialPanelController tutorial)
    {
        if (tutorial == null || tutorial.eventos == null || tutorial.eventos.Count < 2)
        {
            return 0;
        }

        Dictionary<float, List<EventEntry>> groups = new Dictionary<float, List<EventEntry>>();

        for (int i = 0; i < tutorial.eventos.Count; i++)
        {
            AlgoLabTutorialPanelController.EventoTutorial tutorialEvent = tutorial.eventos[i];
            if (tutorialEvent == null || float.IsNaN(tutorialEvent.tiempo) ||
                float.IsInfinity(tutorialEvent.tiempo))
            {
                continue;
            }

            if (!groups.TryGetValue(tutorialEvent.tiempo, out List<EventEntry> entries))
            {
                entries = new List<EventEntry>();
                groups.Add(tutorialEvent.tiempo, entries);
            }

            entries.Add(new EventEntry(tutorialEvent, i));
        }

        int changes = 0;
        foreach (KeyValuePair<float, List<EventEntry>> group in groups)
        {
            List<EventEntry> entries = group.Value;
            entries.Sort((left, right) =>
            {
                int byOrder = left.tutorialEvent.orden.CompareTo(right.tutorialEvent.orden);
                return byOrder != 0 ? byOrder : left.originalIndex.CompareTo(right.originalIndex);
            });

            int previousOrder = int.MinValue;
            for (int i = 0; i < entries.Count; i++)
            {
                int normalizedOrder = entries[i].tutorialEvent.orden;
                if (normalizedOrder <= previousOrder)
                {
                    normalizedOrder = previousOrder + 1;
                }

                if (entries[i].tutorialEvent.orden != normalizedOrder)
                {
                    entries[i].tutorialEvent.orden = normalizedOrder;
                    changes++;
                }

                previousOrder = normalizedOrder;
            }
        }

        return changes;
    }

    private readonly struct EventEntry
    {
        public readonly AlgoLabTutorialPanelController.EventoTutorial tutorialEvent;
        public readonly int originalIndex;

        public EventEntry(
            AlgoLabTutorialPanelController.EventoTutorial tutorialEvent,
            int originalIndex)
        {
            this.tutorialEvent = tutorialEvent;
            this.originalIndex = originalIndex;
        }
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return "null";
        }

        StringBuilder path = new StringBuilder(target.name);
        Transform parent = target.parent;
        while (parent != null)
        {
            path.Insert(0, parent.name + "/");
            parent = parent.parent;
        }

        return path.ToString();
    }
}
