using System.Collections;
using UnityEngine;

public class AlgoLabRuntimeSpawnedObjectMover : MonoBehaviour
{
    [Header("Movimiento runtime")]
    public float pasoMovimiento = 0.05f;
    public float duracionMovimiento = 0.25f;

    [Header("Datos actuales")]
    public Vector3 offsetLocalActual;
    public Vector3 rotacionLocalExtraEuler;

    private AlgoLabFrontSpawnUtility spawnUtility;
    private Coroutine rutinaMover;
    private Vector3 ultimoDestino;
    private Quaternion ultimaRotacionDestino = Quaternion.identity;
    private bool tieneDestinoPendiente;

    private void OnDisable()
    {
        if (rutinaMover != null)
        {
            StopCoroutine(rutinaMover);
            rutinaMover = null;
        }

        if (tieneDestinoPendiente)
            AplicarDestinoInmediato();
    }

    public void Configurar(
        AlgoLabFrontSpawnUtility utility,
        Vector3 offsetInicial,
        Vector3 rotacionExtra)
    {
        spawnUtility = utility;
        offsetLocalActual = offsetInicial;
        rotacionLocalExtraEuler = rotacionExtra;
    }

    public void MoverXMas()
    {
        MoverLocal(new Vector3(pasoMovimiento, 0f, 0f));
    }

    public void MoverXMenos()
    {
        MoverLocal(new Vector3(-pasoMovimiento, 0f, 0f));
    }

    public void MoverYMas()
    {
        MoverLocal(new Vector3(0f, pasoMovimiento, 0f));
    }

    public void MoverYMenos()
    {
        MoverLocal(new Vector3(0f, -pasoMovimiento, 0f));
    }

    public void MoverZMas()
    {
        MoverLocal(new Vector3(0f, 0f, pasoMovimiento));
    }

    public void MoverZMenos()
    {
        MoverLocal(new Vector3(0f, 0f, -pasoMovimiento));
    }

    public void MoverLocal(Vector3 deltaLocal)
    {
        offsetLocalActual += deltaLocal;
        ReubicarConSmooth();
    }

    public void SetOffsetLocal(Vector3 nuevoOffset)
    {
        offsetLocalActual = nuevoOffset;
        ReubicarConSmooth();
    }

    public void ReubicarConSmooth()
    {
        if (spawnUtility == null)
        {
            Debug.LogWarning("No hay SpawnUtility asignado al objeto movible.");
            return;
        }

        Vector3 destino = spawnUtility.ObtenerPosicionMundo(offsetLocalActual);
        Quaternion rotacionDestino = spawnUtility.ObtenerRotacionMundo(rotacionLocalExtraEuler);

        if (!EsVectorFinito(destino) || !EsQuaternionFinito(rotacionDestino))
        {
            Debug.LogError("No se pudo mover el objeto: el destino calculado no es válido.");
            return;
        }

        ultimoDestino = destino;
        ultimaRotacionDestino = Quaternion.Normalize(rotacionDestino);
        tieneDestinoPendiente = true;

        if (rutinaMover != null)
        {
            StopCoroutine(rutinaMover);
        }

        if (!isActiveAndEnabled || duracionMovimiento <= 0f)
        {
            AplicarDestinoInmediato();
            return;
        }

        rutinaMover = StartCoroutine(MoverSmooth(ultimoDestino, ultimaRotacionDestino));
    }

    private IEnumerator MoverSmooth(Vector3 destino, Quaternion rotacionDestino)
    {
        Vector3 inicio = transform.position;
        Quaternion rotacionInicio = transform.rotation;

        float tiempo = 0f;

        float duracion = Mathf.Max(0.01f, duracionMovimiento);

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(inicio, destino, smooth);
            transform.rotation = Quaternion.Slerp(rotacionInicio, rotacionDestino, smooth);

            yield return null;
        }

        transform.position = destino;
        transform.rotation = rotacionDestino;

        rutinaMover = null;
        tieneDestinoPendiente = false;
    }

    private void AplicarDestinoInmediato()
    {
        transform.SetPositionAndRotation(ultimoDestino, ultimaRotacionDestino);
        tieneDestinoPendiente = false;
    }

    private static bool EsVectorFinito(Vector3 valor)
    {
        return float.IsFinite(valor.x) &&
               float.IsFinite(valor.y) &&
               float.IsFinite(valor.z);
    }

    private static bool EsQuaternionFinito(Quaternion valor)
    {
        return float.IsFinite(valor.x) &&
               float.IsFinite(valor.y) &&
               float.IsFinite(valor.z) &&
               float.IsFinite(valor.w) &&
               valor.x * valor.x +
               valor.y * valor.y +
               valor.z * valor.z +
               valor.w * valor.w > 0.000001f;
    }
}
