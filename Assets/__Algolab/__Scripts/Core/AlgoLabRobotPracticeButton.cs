using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conserva los colores semanticos (+ publico / - privado) de los botones de
/// la practica del robot aunque el clicker VR global actualice su hover.
/// </summary>
public class AlgoLabRobotPracticeButton : MonoBehaviour
{
    public Image background;
    public Color normalColor = new Color(0.08f, 0.20f, 0.25f, 0.98f);
    public Color hoverColor = new Color(0.05f, 0.78f, 0.64f, 1f);

    private void Awake()
    {
        if (background == null)
            background = GetComponent<Image>();

        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        if (background != null)
            background.color = hovered ? hoverColor : normalColor;
    }
}
