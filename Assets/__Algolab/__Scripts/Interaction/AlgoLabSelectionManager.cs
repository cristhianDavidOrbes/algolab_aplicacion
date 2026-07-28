using UnityEngine;

public class AlgoLabSelectionManager : MonoBehaviour
{
    public static AlgoLabSelectionManager Instance { get; private set; }

    [Header("Objeto actualmente seleccionado")]
    public AlgoLabObjetoEducativo objetoSeleccionado;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (objetoSeleccionado != null)
        {
            objetoSeleccionado.Deseleccionar();
            objetoSeleccionado = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ToggleSeleccion(AlgoLabObjetoEducativo nuevoObjeto)
    {
        if (nuevoObjeto == null)
        {
            return;
        }

        // Si selecciono el mismo objeto otra vez, lo deselecciono
        if (objetoSeleccionado == nuevoObjeto)
        {
            objetoSeleccionado.Deseleccionar();
            objetoSeleccionado = null;

            Debug.Log("Objeto quitado del contexto de IA.");
            return;
        }

        // Si había otro objeto seleccionado, se quita primero
        if (objetoSeleccionado != null)
        {
            objetoSeleccionado.Deseleccionar();
        }

        objetoSeleccionado = nuevoObjeto;
        objetoSeleccionado.Seleccionar();

        Debug.Log("Objeto agregado al contexto de IA: " + objetoSeleccionado.nombreObjeto);
    }

    public bool HayObjetoSeleccionado()
    {
        return objetoSeleccionado != null && objetoSeleccionado.seleccionado;
    }

    public string ObtenerContextoObjetoSeleccionado()
    {
        if (!HayObjetoSeleccionado())
        {
            return "";
        }

        return objetoSeleccionado.CrearContextoParaIA();
    }
}
