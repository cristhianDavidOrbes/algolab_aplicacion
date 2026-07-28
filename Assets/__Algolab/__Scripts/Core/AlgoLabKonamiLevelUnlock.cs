using System.Collections;
using UnityEngine;

/// <summary>
/// Acceso oculto para pruebas en Quest:
/// arriba, arriba, abajo, abajo, izquierda, izquierda, derecha, derecha, B, A.
/// </summary>
public class AlgoLabKonamiLevelUnlock : MonoBehaviour
{
    private enum ActivationContext
    {
        None,
        GuestLevelUnlock,
        ActiveLevelEffect
    }

    private enum InputStep
    {
        Up,
        Down,
        Left,
        Right,
        B,
        A
    }

    private static readonly InputStep[] Sequence =
    {
        InputStep.Up,
        InputStep.Up,
        InputStep.Down,
        InputStep.Down,
        InputStep.Left,
        InputStep.Left,
        InputStep.Right,
        InputStep.Right,
        InputStep.B,
        InputStep.A
    };

    [Header("Destino")]
    public AlgoLabProgressPanel progressPanel;
    public AlgoLabSessionManager sessionManager;
    public AlgoLabTutorialPanelController tutorialController;
    public AlgoLabFlowStateManager flowStateManager;

    [Header("Tiempo")]
    [Tooltip("Tiempo maximo permitido entre una entrada y la siguiente.")]
    public float maximumIntervalBetweenInputs = 2f;

    [Header("Joystick")]
    [Range(0.5f, 0.95f)] public float directionThreshold = 0.72f;
    [Range(0.05f, 0.5f)] public float releaseThreshold = 0.30f;
    public bool acceptEitherThumbstick = true;

    [Header("Confirmacion")]
    public bool vibrateOnSuccess = true;
    public float successVibrationDuration = 0.22f;

    [Header("Debug")]
    public bool showDebug;

    private int currentStep;
    private float lastAcceptedInputTime = -100f;
    private bool stickReady = true;
    private Coroutine vibrationRoutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!CanReceiveSequence())
        {
            if (currentStep > 0)
            {
                ResetSequence("condiciones de prueba no disponibles");
            }
            return;
        }

        if (currentStep > 0 &&
            Time.unscaledTime - lastAcceptedInputTime > maximumIntervalBetweenInputs)
        {
            ResetSequence("intervalo excedido");
        }

        ReadStickDirection();

        if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            RegisterInput(InputStep.B);
        }

        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            RegisterInput(InputStep.A);
        }
    }

    private void ReadStickDirection()
    {
        Vector2 primary = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector2 secondary = acceptEitherThumbstick
            ? OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)
            : Vector2.zero;

        Vector2 stick = primary.sqrMagnitude >= secondary.sqrMagnitude ? primary : secondary;

        if (!stickReady)
        {
            if (stick.magnitude <= releaseThreshold)
            {
                stickReady = true;
            }
            return;
        }

        if (stick.magnitude < directionThreshold)
        {
            return;
        }

        InputStep direction;
        if (Mathf.Abs(stick.y) >= Mathf.Abs(stick.x))
        {
            direction = stick.y >= 0f ? InputStep.Up : InputStep.Down;
        }
        else
        {
            direction = stick.x >= 0f ? InputStep.Right : InputStep.Left;
        }

        stickReady = false;
        RegisterInput(direction);
    }

    private void RegisterInput(InputStep input)
    {
        InputStep expected = Sequence[currentStep];

        if (input == expected)
        {
            currentStep++;
            lastAcceptedInputTime = Time.unscaledTime;

            if (showDebug)
            {
                Debug.Log("KONAMI ALGOLAB: paso correcto " + currentStep + "/" + Sequence.Length);
            }

            if (currentStep >= Sequence.Length)
            {
                CompleteSequence();
            }
            return;
        }

        // Si la entrada incorrecta tambien es el primer paso, comienza de nuevo
        // desde ella. Esto hace natural repetir "arriba" al corregir un intento.
        if (input == Sequence[0])
        {
            currentStep = 1;
            lastAcceptedInputTime = Time.unscaledTime;
        }
        else
        {
            ResetSequence("entrada incorrecta: " + input);
        }
    }

    private void CompleteSequence()
    {
        currentStep = 0;
        lastAcceptedInputTime = -100f;
        ResolveReferences();
        ActivationContext context = GetActivationContext();

        if (progressPanel == null || context == ActivationContext.None)
        {
            Debug.LogWarning(
                "KONAMI ALGOLAB: se ignoro la activacion porque ya no existe un contexto valido."
            );
            return;
        }

        bool activated;
        if (context == ActivationContext.ActiveLevelEffect)
        {
            int activeLevel = progressPanel.ObtenerNivelActivoRealActual();
            activated = TryActivateLevelEffect(activeLevel);
            if (activated)
            {
                Debug.Log(
                    "KONAMI ALGOLAB: efecto secreto activado dentro del nivel " + activeLevel + "."
                );
            }
            else
            {
                Debug.Log(
                    "KONAMI ALGOLAB: el nivel " + activeLevel +
                    " aun no tiene un efecto secreto configurado; no se desbloquearon niveles."
                );
            }
        }
        else
        {
            progressPanel.ActivarDesbloqueoPruebaTodosLosNiveles();
            activated = true;
            Debug.Log(
                "KONAMI ALGOLAB: modo de prueba activado desde el menu invitado; " +
                "todos los niveles estan disponibles."
            );
        }

        if (!activated) return;

        if (vibrateOnSuccess)
        {
            if (vibrationRoutine != null)
            {
                StopCoroutine(vibrationRoutine);
            }
            vibrationRoutine = StartCoroutine(SuccessVibration());
        }

    }

    private bool TryActivateLevelEffect(int levelNumber)
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < behaviours.Length; i++)
        {
            IAlgoLabKonamiLevelEffect effect = behaviours[i] as IAlgoLabKonamiLevelEffect;
            if (effect == null || effect.KonamiLevelNumber != levelNumber) continue;
            if (effect.ActivateKonamiLevelEffect()) return true;
        }

        return false;
    }

    private IEnumerator SuccessVibration()
    {
        OVRInput.SetControllerVibration(1f, 1f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(1f, 1f, OVRInput.Controller.LTouch);

        float endTime = Time.unscaledTime + Mathf.Max(0.05f, successVibrationDuration);
        while (Time.unscaledTime < endTime)
        {
            yield return null;
        }

        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        vibrationRoutine = null;
    }

    private void OnDisable()
    {
        if (vibrationRoutine != null)
        {
            StopCoroutine(vibrationRoutine);
            vibrationRoutine = null;
        }

        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        ResetSequence("componente desactivado");
    }

    private void ResetSequence(string reason)
    {
        if (showDebug && currentStep > 0)
        {
            Debug.Log("KONAMI ALGOLAB: secuencia reiniciada (" + reason + ").");
        }

        currentStep = 0;
        lastAcceptedInputTime = -100f;
    }

    private bool CanReceiveSequence()
    {
        return GetActivationContext() != ActivationContext.None;
    }

    private ActivationContext GetActivationContext()
    {
        ResolveReferences();

        bool tutorialReady = tutorialController != null &&
                             tutorialController.TutorialPrincipalCompletadoUOmitido &&
                             !tutorialController.TutorialEnCurso;
        if (!tutorialReady || progressPanel == null)
        {
            return ActivationContext.None;
        }

        if (progressPanel.ObtenerNivelActivoRealActual() > 0)
        {
            return ActivationContext.ActiveLevelEffect;
        }

        bool isGuest = sessionManager != null &&
                       sessionManager.SesionIniciada &&
                       sessionManager.ModoInvitado;

        bool noLevelSelected = !progressPanel.HayNivelSeleccionadoOEnCurso();

        bool flowIsFree = flowStateManager == null ||
                          flowStateManager.estadoActual ==
                          AlgoLabFlowStateManager.EstadoFlujoAlgolab.Ninguno;

        return isGuest && noLevelSelected && flowIsFree
            ? ActivationContext.GuestLevelUnlock
            : ActivationContext.None;
    }

    private void ResolveReferences()
    {
        if (progressPanel == null)
        {
            progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>(FindObjectsInactive.Include);
        }

        if (sessionManager == null)
        {
            sessionManager = AlgoLabSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            sessionManager = FindFirstObjectByType<AlgoLabSessionManager>(FindObjectsInactive.Include);
        }

        if (tutorialController == null)
        {
            tutorialController = FindFirstObjectByType<AlgoLabTutorialPanelController>(
                FindObjectsInactive.Include
            );
        }

        if (flowStateManager == null)
        {
            flowStateManager = AlgoLabFlowStateManager.Instance;
        }

        if (flowStateManager == null)
        {
            flowStateManager = FindFirstObjectByType<AlgoLabFlowStateManager>(
                FindObjectsInactive.Include
            );
        }
    }
}
