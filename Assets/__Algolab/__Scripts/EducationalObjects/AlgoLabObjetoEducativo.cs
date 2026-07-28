using System.Text;
using UnityEngine;

public class AlgoLabObjetoEducativo : MonoBehaviour
{
    [Header("Información del objeto")]
    public string nombreObjeto = "Vehículo";

    [TextArea(3, 8)]
    public string descripcionObjeto =
        "Objeto educativo usado para explicar Programación Orientada a Objetos.";

    [Header("Representación como clase")]
    public string nombreClase = "Vehiculo";

    public string[] atributos =
    {
        "matricula : texto",
        "marca : texto",
        "modelo : texto"
    };

    public string[] metodos =
    {
        "encender()",
        "acelerar()",
        "frenar()"
    };

    [Tooltip("Permite mostrar esta clase en el panel de diagramas aunque no tenga atributos ni metodos. Util para clases conceptuales como Usuario.")]
    public bool forzarVisibleEnDiagramaTema;

    [Header("Estado")]
    public bool seleccionado;

    private AlgoLabOutlineController outlineController;

    private void Awake()
    {
        outlineController = GetComponent<AlgoLabOutlineController>();
    }

    public void Seleccionar()
    {
        seleccionado = true;

        if (outlineController != null)
        {
            outlineController.SetOutline(true);
        }

        Debug.Log("Objeto seleccionado: " + nombreObjeto);
    }

    public void Deseleccionar()
    {
        seleccionado = false;

        if (outlineController != null)
        {
            outlineController.SetOutline(false);
        }

        Debug.Log("Objeto deseleccionado: " + nombreObjeto);
    }

    public string CrearContextoParaIA()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Contexto del objeto seleccionado:");
        sb.AppendLine("Nombre del objeto: " + nombreObjeto);
        sb.AppendLine("Descripción: " + descripcionObjeto);
        sb.AppendLine("Clase representada: " + nombreClase);

        sb.AppendLine("Atributos de la clase:");
        foreach (string atributo in atributos)
        {
            sb.AppendLine("- " + atributo);
        }

        sb.AppendLine("Métodos de la clase:");
        foreach (string metodo in metodos)
        {
            sb.AppendLine("- " + metodo);
        }

        sb.AppendLine("Responde como tutor educativo de programación orientada a objetos.");
        sb.AppendLine("Relaciona la explicación con este objeto seleccionado.");

        return sb.ToString();
    }
}
