using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabPracticeLabel : MonoBehaviour
{
    public enum TipoElemento
    {
        Atributo,
        Metodo
    }

    [Header("Datos de la etiqueta")]
    public string nombreElemento = "color";
    public TipoElemento tipoCorrecto = TipoElemento.Atributo;

    [Header("Referencias UI")]
    public Button boton;
    public Image imagenFondo;
    public TMP_Text textoEtiqueta;

    [Header("Colores")]
    public Color colorNormal = Color.white;
    public Color colorSeleccionado = new Color(1f, 0.85f, 0.2f, 1f);
    public Color colorCorrecto = new Color(0.1f, 0.85f, 0.25f, 1f);
    public Color colorIncorrecto = new Color(1f, 0.15f, 0.15f, 1f);
    public Color colorTextoNormal = Color.black;

    [Header("Estado")]
    [SerializeField] private bool clasificadaCorrectamente = false;
    [SerializeField] private bool seleccionada = false;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private AlgoLabCarPracticeController controller;
    private bool configurado;

    public bool ClasificadaCorrectamente
    {
        get { return clasificadaCorrectamente; }
    }

    private void Awake()
    {
        PrepararReferencias();
        ConfigurarBoton();
        AplicarEstadoNormal();
    }

    private void OnEnable()
    {
        PrepararReferencias();
        ConfigurarBoton();

        if (controller == null)
        {
            controller = FindFirstObjectByType<AlgoLabCarPracticeController>();
        }
    }

    private void OnDisable()
    {
        if (boton != null)
            boton.onClick.RemoveListener(OnClickEtiqueta);

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

        if (imagenFondo == null)
        {
            imagenFondo = GetComponent<Image>();

            if (imagenFondo == null && boton != null)
            {
                imagenFondo = boton.GetComponent<Image>();
            }

            if (imagenFondo == null)
            {
                imagenFondo = GetComponentInChildren<Image>(true);
            }
        }

        if (textoEtiqueta == null)
        {
            textoEtiqueta = GetComponentInChildren<TMP_Text>(true);
        }

        if (textoEtiqueta != null)
        {
            textoEtiqueta.text = nombreElemento;
            textoEtiqueta.color = colorTextoNormal;
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
            Debug.LogWarning("La etiqueta " + name + " no tiene Button.");
            return;
        }

        boton.onClick.RemoveListener(OnClickEtiqueta);
        boton.onClick.AddListener(OnClickEtiqueta);
        boton.interactable = true;

        configurado = true;
    }

    public void Inicializar(AlgoLabCarPracticeController nuevoController)
    {
        controller = nuevoController;

        PrepararReferencias();
        ConfigurarBoton();

        clasificadaCorrectamente = false;
        seleccionada = false;

        AplicarEstadoNormal();

        if (mostrarDebug)
        {
            Debug.Log("Etiqueta inicializada: " + nombreElemento + " | Tipo: " + tipoCorrecto);
        }
    }

    public void OnClickEtiqueta()
    {
        if (clasificadaCorrectamente)
        {
            if (mostrarDebug)
            {
                Debug.Log("La etiqueta ya está clasificada correctamente: " + nombreElemento);
            }

            return;
        }

        if (controller == null)
        {
            controller = FindFirstObjectByType<AlgoLabCarPracticeController>();
        }

        if (controller == null)
        {
            Debug.LogError("No se encontró AlgoLabCarPracticeController para la etiqueta: " + nombreElemento);
            return;
        }

        if (mostrarDebug)
        {
            Debug.Log("CLICK EN ETIQUETA: " + nombreElemento + " | Tipo correcto: " + tipoCorrecto);
        }

        controller.SeleccionarEtiqueta(this);
    }

    public void SetSeleccionada(bool valor)
    {
        if (clasificadaCorrectamente)
        {
            return;
        }

        seleccionada = valor;

        if (seleccionada)
        {
            AplicarColor(colorSeleccionado);
        }
        else
        {
            AplicarEstadoNormal();
        }
    }

    public void AplicarEstadoNormal()
    {
        clasificadaCorrectamente = false;
        seleccionada = false;

        AplicarColor(colorNormal);

        if (boton != null)
        {
            boton.interactable = true;
        }

        if (textoEtiqueta != null)
        {
            textoEtiqueta.text = nombreElemento;
            textoEtiqueta.color = colorTextoNormal;
        }
    }

    public void MarcarCorrecto()
    {
        clasificadaCorrectamente = true;
        seleccionada = false;

        AplicarColor(colorCorrecto);

        if (boton != null)
        {
            boton.interactable = false;
        }

        if (mostrarDebug)
        {
            Debug.Log("Etiqueta correcta: " + nombreElemento);
        }
    }

    public void MarcarIncorrectoTemporal()
    {
        if (clasificadaCorrectamente)
        {
            return;
        }

        AplicarColor(colorIncorrecto);

        if (mostrarDebug)
        {
            Debug.Log("Etiqueta incorrecta: " + nombreElemento);
        }
    }

    private void AplicarColor(Color color)
    {
        if (imagenFondo != null)
        {
            imagenFondo.color = color;
        }

        if (boton != null && boton.targetGraphic != null)
        {
            boton.targetGraphic.color = color;
        }
    }
}
