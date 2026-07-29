using UnityEngine;

/// <summary>
/// Indice de autoría visible únicamente en Unity. El objeto que contiene este
/// componente usa la etiqueta EditorOnly, por lo que no se incluye en el APK.
/// Sus instancias enlazan los prefabs reales utilizados durante la ejecución.
/// </summary>
[DisallowMultipleComponent]
public sealed class AlgoLabEditableContentCatalog : MonoBehaviour
{
    [Header("Contenido real por nivel")]
    [Tooltip("Instancias de los prefabs reales. Edite el prefab original para que el cambio llegue al juego.")]
    public GameObject[] contenidoEditable = new GameObject[0];

    [Header("Tutoriales ya compuestos en la escena principal")]
    [Tooltip("Objetos de la escena que contienen controladores, paneles o secuencias de tutorial.")]
    public GameObject[] tutorialesEnEscena = new GameObject[0];

    [Header("Controladores de niveles")]
    [Tooltip("Controlador de los niveles 3 a 6, incluida su configuración serializada.")]
    public AlgoLabPillarLevelController controladorNiveles;

    [Tooltip("Administrador que contiene las referencias de paneles y objetos frontales.")]
    public AlgoLabManualPanelSpawnManager administradorObjetos;

    [TextArea(4, 10)]
    public string instrucciones =
        "Este catálogo existe solo para edición. Expanda sus hijos para inspeccionar " +
        "los niveles 1 a 4. Para modificar contenido, abra el prefab enlazado; " +
        "los tutoriales y los niveles 5 y 6 se inspeccionan mediante las referencias " +
        "serializadas de este componente y del controlador de niveles.";
}
