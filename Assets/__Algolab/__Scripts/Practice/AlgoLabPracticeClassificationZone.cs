using UnityEngine;
using UnityEngine.UI;

public class AlgoLabPracticeClassificationZone : MonoBehaviour
{
    public enum TipoZona
    {
        Atributo,
        Metodo
    }

    [Header("Tipo de zona")]
    public TipoZona tipoZona = TipoZona.Atributo;

    [Header("Controller")]
    public AlgoLabCarPracticeController controller;

    [Header("Referencias UI")]
    public Button boton;
    public Image imagenZona;

    [Header("Colores opcionales")]
    public Color colorNormal = new Color(1f, 1f, 1f, 0.25f);
    public Color colorHover = new Color(1f, 0.85f, 0.2f, 0.45f);

    [Header("Debug")]
    public bool mostrarDebug = true;

    private bool configurado;

    private void Awake()
    {
        PrepararReferencias();
        ConfigurarBoton();
    }

    private void OnEnable()
    {
        PrepararReferencias();
        ConfigurarBoton();

        if (controller == null)
        {
            controller = FindFirstObjectByType<AlgoLabCarPracticeController>();
        }

        if (imagenZona != null)
        {
            imagenZona.color = colorNormal;
        }
    }

    private void OnDisable()
    {
        if (boton != null)
            boton.onClick.RemoveListener(OnClickZona);

        configurado = false;
    }

    private void PrepararReferencias()
    {
        if (boton == null)
        {
            boton = GetComponent<Button>();

            if (boton == null)
            {
                boton = GetComponentInChildren<Button>(true);
            }
        }

        if (imagenZona == null)
        {
            imagenZona = GetComponent<Image>();

            if (imagenZona == null && boton != null)
            {
                imagenZona = boton.GetComponent<Image>();
            }

            if (imagenZona == null)
            {
                imagenZona = GetComponentInChildren<Image>(true);
            }
        }

        if (boton != null)
        {
            boton.interactable = true;
        }

        if (imagenZona != null)
        {
            imagenZona.raycastTarget = true;
        }
    }

    private void ConfigurarBoton()
    {
        if (configurado)
        {
            return;
        }

        if (boton == null)
        {
            Debug.LogWarning("La zona " + name + " no tiene Button.");
            return;
        }

        boton.onClick.RemoveListener(OnClickZona);
        boton.onClick.AddListener(OnClickZona);
        boton.interactable = true;

        configurado = true;
    }

    public void SetController(AlgoLabCarPracticeController nuevoController)
    {
        controller = nuevoController;
    }

    public void OnClickZona()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<AlgoLabCarPracticeController>();
        }

        if (mostrarDebug)
        {
            Debug.Log("CLICK EN ZONA DE CLASIFICACIÓN: " + name + " | Tipo: " + tipoZona);
        }

        if (controller == null)
        {
            Debug.LogError("No se encontró AlgoLabCarPracticeController desde la zona: " + name);
            return;
        }

        if (tipoZona == TipoZona.Atributo)
        {
            controller.ClasificarComoAtributo();
        }
        else
        {
            controller.ClasificarComoMetodo();
        }
    }
}
