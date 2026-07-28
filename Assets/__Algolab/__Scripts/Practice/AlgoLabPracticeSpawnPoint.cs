using UnityEngine;

public class AlgoLabPracticeSpawnPoint : MonoBehaviour
{
    [Header("Punto de alineación")]
    public Transform spawnAnchor;

    public Transform ObtenerAnchor()
    {
        if (spawnAnchor != null)
        {
            return spawnAnchor;
        }

        return transform;
    }
}