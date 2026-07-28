using UnityEngine;

public class AlgoLabFrontSpawnUtility : MonoBehaviour
{
    [Header("Referencia principal")]
    public AlgoLabManualPanelSpawnManager spawnManager;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private void Awake()
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }
    }

    public Vector3 ObtenerPosicionMundo(Vector3 offsetLocalExtra)
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager == null)
        {
            Debug.LogError("No se encontró AlgoLabManualPanelSpawnManager.");
            return transform.position;
        }

        Transform referencia = spawnManager.referenciaManual != null
            ? spawnManager.referenciaManual
            : spawnManager.transform;

        Vector3 posicionLocalFinal =
            spawnManager.posicionLocalObjetoFrontal + offsetLocalExtra;

        return referencia.TransformPoint(posicionLocalFinal);
    }

    public Quaternion ObtenerRotacionMundo(Vector3 rotacionLocalExtraEuler)
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager == null)
        {
            return transform.rotation * Quaternion.Euler(rotacionLocalExtraEuler);
        }

        Transform referencia = spawnManager.referenciaManual != null
            ? spawnManager.referenciaManual
            : spawnManager.transform;

        return referencia.rotation * Quaternion.Euler(rotacionLocalExtraEuler);
    }

    public GameObject SpawnearObjeto(
        GameObject prefab,
        string nombre,
        Transform parent,
        Vector3 escala,
        Vector3 offsetLocalExtra,
        Vector3 rotacionLocalExtraEuler,
        bool agregarMoverRuntime)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab vacío. No se puede spawnear.");
            return null;
        }

        Vector3 posicion = ObtenerPosicionMundo(offsetLocalExtra);
        Quaternion rotacion = ObtenerRotacionMundo(rotacionLocalExtraEuler);

        GameObject objeto = Instantiate(prefab, posicion, rotacion, parent);
        objeto.name = string.IsNullOrWhiteSpace(nombre) ? prefab.name : nombre;
        objeto.transform.localScale = escala;
        objeto.SetActive(true);

        if (agregarMoverRuntime)
        {
            AlgoLabRuntimeSpawnedObjectMover mover =
                objeto.GetComponent<AlgoLabRuntimeSpawnedObjectMover>();

            if (mover == null)
            {
                mover = objeto.AddComponent<AlgoLabRuntimeSpawnedObjectMover>();
            }

            mover.Configurar(this, offsetLocalExtra, rotacionLocalExtraEuler);
        }

        if (mostrarDebug)
        {
            Debug.Log(
                "Spawn creado: " + objeto.name +
                " | Posición: " + posicion +
                " | Offset local extra: " + offsetLocalExtra
            );
        }

        return objeto;
    }
}