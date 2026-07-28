using System.Collections;
using UnityEngine;
using UnityEngine.Android;

public class AlgoLabMicPermissionChecker : MonoBehaviour
{
    [Header("Debug")]
    public bool mostrarDebug = true;

    private IEnumerator Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("Solicitando permiso de micrófono...");
            Permission.RequestUserPermission(Permission.Microphone);

            yield return new WaitForSeconds(1.5f);
        }

        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("Permiso de micrófono CONCEDIDO.");
        }
        else
        {
            Debug.LogError("Permiso de micrófono DENEGADO.");
        }
#else
        Debug.Log("Chequeo de micrófono ejecutado fuera de Android.");
        yield return null;
#endif

        MostrarMicrofonosDisponibles();
    }

    private void MostrarMicrofonosDisponibles()
    {
        string[] dispositivos = Microphone.devices;

        if (dispositivos == null || dispositivos.Length == 0)
        {
            Debug.LogWarning("No se detectaron micrófonos disponibles.");
            return;
        }

        for (int i = 0; i < dispositivos.Length; i++)
        {
            Debug.Log("Micrófono disponible: " + dispositivos[i]);
        }
    }
}