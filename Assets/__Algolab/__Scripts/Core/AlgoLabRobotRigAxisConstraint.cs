using UnityEngine;

/// <summary>
/// Conserva en Unity las restricciones mecánicas del robot. Las restricciones
/// de Blender sirven al animar, pero FBX no siempre las convierte en
/// componentes activos dentro de Unity.
/// </summary>
[DisallowMultipleComponent]
public class AlgoLabRobotRigAxisConstraint : MonoBehaviour
{
    [Range(1f, 90f)]
    public float limitePiernas = 40f;

    private BoneState brazoIzquierdo;
    private BoneState brazoDerecho;
    private BoneState piernaIzquierda;
    private BoneState piernaDerecha;
    private BoneState cabeza;
    private bool configurado;

    private struct BoneState
    {
        public Transform transform;
        public Quaternion rotacionInicial;
    }

    public bool ConfigurarAutomaticamente(Transform raizModelo)
    {
        Transform raiz = raizModelo != null ? raizModelo : transform;
        brazoIzquierdo = CrearEstado(BuscarRecursivo(raiz, "Arm.L"));
        brazoDerecho = CrearEstado(BuscarRecursivo(raiz, "Arm.R"));
        piernaIzquierda = CrearEstado(BuscarRecursivo(raiz, "Leg.L"));
        piernaDerecha = CrearEstado(BuscarRecursivo(raiz, "Leg.R"));
        cabeza = CrearEstado(BuscarRecursivo(raiz, "head"));

        configurado =
            brazoIzquierdo.transform != null &&
            brazoDerecho.transform != null &&
            piernaIzquierda.transform != null &&
            piernaDerecha.transform != null &&
            cabeza.transform != null;
        return configurado;
    }

    private void LateUpdate()
    {
        if (!configurado && !ConfigurarAutomaticamente(transform))
            return;

        RestringirAUnEje(brazoIzquierdo, EjePermitido.X, false, 0f);
        RestringirAUnEje(brazoDerecho, EjePermitido.X, false, 0f);
        RestringirAUnEje(cabeza, EjePermitido.Y, false, 0f);
        RestringirAUnEje(piernaIzquierda, EjePermitido.X, true, limitePiernas);
        RestringirAUnEje(piernaDerecha, EjePermitido.X, true, limitePiernas);
    }

    private enum EjePermitido
    {
        X,
        Y
    }

    private static void RestringirAUnEje(
        BoneState estado,
        EjePermitido eje,
        bool limitarAngulo,
        float limite)
    {
        if (estado.transform == null)
            return;

        // Se mide en el espacio del padre (el torso). Los ejes locales
        // importados de estos huesos estan inclinados y producian movimiento
        // lateral aun cuando se conservaba un solo eje local.
        Quaternion relativa =
            estado.transform.localRotation * Quaternion.Inverse(estado.rotacionInicial);
        Vector3 angulos = relativa.eulerAngles;
        angulos.x = NormalizarAngulo(angulos.x);
        angulos.y = NormalizarAngulo(angulos.y);

        if (eje == EjePermitido.X)
        {
            float x = limitarAngulo
                ? Mathf.Clamp(angulos.x, -Mathf.Abs(limite), Mathf.Abs(limite))
                : angulos.x;
            estado.transform.localRotation =
                Quaternion.Euler(x, 0f, 0f) * estado.rotacionInicial;
        }
        else
        {
            estado.transform.localRotation =
                Quaternion.Euler(0f, angulos.y, 0f) * estado.rotacionInicial;
        }
    }

    private static float NormalizarAngulo(float angulo)
    {
        return Mathf.DeltaAngle(0f, angulo);
    }

    private static BoneState CrearEstado(Transform bone)
    {
        return new BoneState
        {
            transform = bone,
            rotacionInicial = bone != null ? bone.localRotation : Quaternion.identity
        };
    }

    private static Transform BuscarRecursivo(Transform raiz, string nombre)
    {
        if (raiz == null)
            return null;
        if (raiz.name == nombre)
            return raiz;

        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform encontrado = BuscarRecursivo(raiz.GetChild(i), nombre);
            if (encontrado != null)
                return encontrado;
        }
        return null;
    }
}
