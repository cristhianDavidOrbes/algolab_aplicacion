using UnityEngine;

public class AlgoLabTemaDiagramHighlighter : MonoBehaviour
{
    [Header("Referencias")]
    public AlgoLabClassDiagramController diagramController;

    [Header("Clase del tema")]
    public string nombreClaseTema = "Puerta";

    [Header("Duraciones")]
    public float duracionPulsoCampo = 1f;
    public float duracionElemento = 3f;

    private void Start()
    {
        if (diagramController == null)
        {
            diagramController = FindFirstObjectByType<AlgoLabClassDiagramController>();
        }
    }

    public void PulsoClase()
    {
        if (diagramController == null)
        {
            return;
        }

        diagramController.ResaltarClase(nombreClaseTema, duracionPulsoCampo);
    }

    public void PulsoAtributos()
    {
        if (diagramController == null)
        {
            return;
        }

        diagramController.ResaltarAtributos(nombreClaseTema, duracionPulsoCampo);
    }

    public void PulsoMetodos()
    {
        if (diagramController == null)
        {
            return;
        }

        diagramController.ResaltarMetodos(nombreClaseTema, duracionPulsoCampo);
    }

    public void MantenerAtributo(string atributo)
    {
        if (diagramController == null)
        {
            return;
        }

        diagramController.MantenerAtributo(nombreClaseTema, atributo);
    }

    public void MantenerMetodo(string metodo)
    {
        if (diagramController == null)
        {
            return;
        }

        diagramController.MantenerMetodo(nombreClaseTema, metodo);
    }

    public void ResaltarAtributoPorTiempo(string atributo)
    {
        if (diagramController == null)
        {
            return;
        }

        AlgoLabClassDiagramCardUI tarjeta =
            diagramController.ObtenerTarjetaPorNombreClase(nombreClaseTema);

        if (tarjeta != null)
        {
            tarjeta.ResaltarAtributoPorTiempo(atributo, duracionElemento);
        }
    }

    public void ResaltarMetodoPorTiempo(string metodo)
    {
        if (diagramController == null)
        {
            return;
        }

        AlgoLabClassDiagramCardUI tarjeta =
            diagramController.ObtenerTarjetaPorNombreClase(nombreClaseTema);

        if (tarjeta != null)
        {
            tarjeta.ResaltarMetodoPorTiempo(metodo, duracionElemento);
        }
    }

    public void Limpiar()
    {
        if (diagramController == null)
        {
            return;
        }

        diagramController.LimpiarResaltado(nombreClaseTema);
    }

    public void LimpiarTodos()
    {
        if (diagramController == null)
        {
            return;
        }

        diagramController.LimpiarTodosLosResaltados();
    }
}