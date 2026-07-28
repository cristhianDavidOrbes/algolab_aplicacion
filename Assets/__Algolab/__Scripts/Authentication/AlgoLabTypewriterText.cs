using System.Collections;
using TMPro;
using UnityEngine;

public class AlgoLabTypewriterText : MonoBehaviour
{
    [Header("Referencia")]
    public TMP_Text texto;

    [Header("Mensaje")]
    [TextArea(3, 8)]
    public string mensajeInicial =
        "Estás entrando como invitado.\nTu progreso no se guardará.\nInicia sesión para guardar información.";

    [Header("Escritura")]
    public bool reproducirAlActivar = true;
    public float retrasoInicial = 0.25f;
    public float tiempoEntreCaracteres = 0.035f;

    [Header("Cursor")]
    public bool usarCursor = true;
    public string simboloCursor = "|";
    public float velocidadParpadeoCursor = 0.35f;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private Coroutine rutinaEscritura;
    private Coroutine rutinaCursor;

    private string textoBaseActual = "";
    private bool cursorVisible = false;
    private bool cursorActivo = false;

    private void Awake()
    {
        if (texto == null)
        {
            texto = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (reproducirAlActivar)
        {
            ReproducirMensajeInicial();
        }
    }

    private void OnDisable()
    {
        DetenerTodo();
    }

    [ContextMenu("Reproducir mensaje inicial")]
    public void ReproducirMensajeInicial()
    {
        Reproducir(mensajeInicial);
    }

    public void Reproducir(string mensaje)
    {
        if (texto == null)
        {
            texto = GetComponent<TMP_Text>();
        }

        if (texto == null)
        {
            Debug.LogWarning("TYPEWRITER: No hay TMP_Text asignado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(mensaje))
        {
            mensaje = mensajeInicial;
        }

        DetenerTodo();

        rutinaEscritura = StartCoroutine(EscribirRutina(mensaje));
    }

    public void DetenerTodo()
    {
        if (rutinaEscritura != null)
        {
            StopCoroutine(rutinaEscritura);
            rutinaEscritura = null;
        }

        if (rutinaCursor != null)
        {
            StopCoroutine(rutinaCursor);
            rutinaCursor = null;
        }

        cursorActivo = false;
        cursorVisible = false;
    }

    private IEnumerator EscribirRutina(string mensaje)
    {
        textoBaseActual = "";
        cursorVisible = true;

        texto.text = "";

        if (retrasoInicial > 0f)
        {
            yield return new WaitForSeconds(retrasoInicial);
        }

        if (usarCursor)
        {
            IniciarCursor();
        }

        for (int i = 0; i < mensaje.Length; i++)
        {
            textoBaseActual += mensaje[i];
            ActualizarTexto();

            yield return new WaitForSeconds(tiempoEntreCaracteres);
        }

        cursorActivo = false;

        if (rutinaCursor != null)
        {
            StopCoroutine(rutinaCursor);
            rutinaCursor = null;
        }

        cursorVisible = false;
        ActualizarTexto();

        rutinaEscritura = null;

        if (mostrarDebug)
        {
            Debug.Log("TYPEWRITER: mensaje terminado.");
        }
    }

    private void IniciarCursor()
    {
        cursorActivo = true;

        if (rutinaCursor != null)
        {
            StopCoroutine(rutinaCursor);
        }

        rutinaCursor = StartCoroutine(CursorRutina());
    }

    private IEnumerator CursorRutina()
    {
        while (cursorActivo)
        {
            cursorVisible = !cursorVisible;
            ActualizarTexto();

            yield return new WaitForSeconds(velocidadParpadeoCursor);
        }
    }

    private void ActualizarTexto()
    {
        if (texto == null)
        {
            return;
        }

        if (usarCursor && cursorVisible)
        {
            texto.text = textoBaseActual + simboloCursor;
        }
        else
        {
            texto.text = textoBaseActual;
        }
    }
}