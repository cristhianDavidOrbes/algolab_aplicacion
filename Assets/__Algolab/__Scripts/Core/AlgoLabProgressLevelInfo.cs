using UnityEngine;

public class AlgoLabProgressLevelInfo : MonoBehaviour
{
    [Header("Tema")]
    public string nombreNivel = "POO";

    [TextArea(2, 5)]
    public string descripcionNivel = "Aprende cómo una clase define atributos y métodos para crear objetos.";

    [Header("Práctica")]
    [TextArea(2, 5)]
    public string tareaPractica = "Selecciona los atributos y métodos del carro.";

    public string tiempoPractica = "01:20";

    [Header("Escena opcional")]
    [Tooltip("Si está vacío, no carga escena. Si tiene nombre, debe existir en Build Settings.")]
    public string nombreEscena = "";
}