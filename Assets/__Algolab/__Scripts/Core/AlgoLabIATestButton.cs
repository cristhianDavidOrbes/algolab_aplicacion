using UnityEngine;

public class AlgoLabIATestButton : MonoBehaviour
{
    public AlgoLabIAClient iaClient;

    [TextArea(2, 5)]
    public string pregunta = "Que debo practicar ahora?";

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            ProbarIA();
        }
    }

    public void ProbarIA()
    {
        if (iaClient == null)
        {
            Debug.LogError("No asignaste AlgoLabIAClient.");
            return;
        }

        iaClient.PreguntarDesdeTexto(pregunta);
    }
}