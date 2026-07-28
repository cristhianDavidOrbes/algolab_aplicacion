using System.Collections;
using UnityEngine;

/// <summary>
/// Controla una caja fuerte modular. La puerta gira desde una bisagra real y
/// el mecanismo mueve el disco, la manija y los cerrojos antes de abrir.
/// </summary>
public class AlgoLabAnimatedSafe : MonoBehaviour
{
    [Header("Partes moviles")]
    public Transform doorPivot;
    public Transform dialPivot;
    public Transform handlePivot;
    public Transform boltRoot;

    [Header("Puerta")]
    public Vector3 closedDoorEuler = Vector3.zero;
    public Vector3 openDoorEuler = new Vector3(0f, 108f, 0f);
    public float animationDuration = 0.85f;

    [Header("Cerradura")]
    public float dialTurnsDegrees = 210f;
    public float handleTurnDegrees = -75f;
    public Vector3 boltsRetractedOffset = new Vector3(-0.055f, 0f, 0f);

    private Vector3 dialClosedEuler;
    private Vector3 handleClosedEuler;
    private Vector3 boltsExtendedPosition;
    private Coroutine activeRoutine;
    private bool initialized;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        EnsureInitialized();
        SetOpenInstantly(false);
    }

    private void OnDisable()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
    }

    [ContextMenu("Abrir")]
    public void Open()
    {
        StartAnimation(true);
    }

    [ContextMenu("Cerrar")]
    public void Close()
    {
        StartAnimation(false);
    }

    public void Toggle()
    {
        StartAnimation(!isOpen);
    }

    public IEnumerator OpenSequence(float duration = -1f)
    {
        yield return AnimateState(true, duration);
    }

    public IEnumerator CloseSequence(float duration = -1f)
    {
        yield return AnimateState(false, duration);
    }

    public void SetOpenInstantly(bool open)
    {
        EnsureInitialized();
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        ApplyDoor(open ? 1f : 0f);
        ApplyLock(open ? 1f : 0f);
        isOpen = open;
    }

    private void StartAnimation(bool open)
    {
        EnsureInitialized();
        if (!isActiveAndEnabled)
        {
            SetOpenInstantly(open);
            return;
        }

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(AnimateAndClear(open));
    }

    private IEnumerator AnimateAndClear(bool open)
    {
        yield return AnimateState(open, animationDuration);
        activeRoutine = null;
    }

    private IEnumerator AnimateState(bool open, float duration)
    {
        EnsureInitialized();
        duration = duration > 0f ? duration : animationDuration;
        duration = Mathf.Max(0.05f, duration);

        Quaternion doorStart = doorPivot != null ? doorPivot.localRotation : Quaternion.identity;
        Quaternion doorTarget = Quaternion.Euler(open ? openDoorEuler : closedDoorEuler);
        float startLock = EstimateLockProgress();
        float targetLock = open ? 1f : 0f;

        if (open)
        {
            yield return AnimateLock(startLock, 1f, duration * 0.34f);
            yield return AnimateDoor(doorStart, doorTarget, duration * 0.66f);
        }
        else
        {
            yield return AnimateDoor(doorStart, doorTarget, duration * 0.66f);
            yield return AnimateLock(startLock, 0f, duration * 0.34f);
        }

        ApplyDoor(open ? 1f : 0f);
        ApplyLock(targetLock);
        isOpen = open;
    }

    private IEnumerator AnimateDoor(Quaternion start, Quaternion target, float duration)
    {
        if (doorPivot == null) yield break;
        float elapsed = 0f;
        duration = Mathf.Max(0.02f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            doorPivot.localRotation = Quaternion.SlerpUnclamped(start, target, Smooth01(elapsed / duration));
            yield return null;
        }
        doorPivot.localRotation = target;
    }

    private IEnumerator AnimateLock(float start, float target, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.02f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyLock(Mathf.Lerp(start, target, Smooth01(elapsed / duration)));
            yield return null;
        }
        ApplyLock(target);
    }

    private void ApplyDoor(float progress)
    {
        if (doorPivot == null) return;
        doorPivot.localRotation = Quaternion.Slerp(
            Quaternion.Euler(closedDoorEuler),
            Quaternion.Euler(openDoorEuler),
            Mathf.Clamp01(progress)
        );
    }

    private void ApplyLock(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (dialPivot != null)
        {
            dialPivot.localRotation = Quaternion.Euler(
                dialClosedEuler + Vector3.forward * (dialTurnsDegrees * progress)
            );
        }
        if (handlePivot != null)
        {
            handlePivot.localRotation = Quaternion.Euler(
                handleClosedEuler + Vector3.forward * (handleTurnDegrees * progress)
            );
        }
        if (boltRoot != null)
        {
            boltRoot.localPosition = Vector3.Lerp(
                boltsExtendedPosition,
                boltsExtendedPosition + boltsRetractedOffset,
                progress
            );
        }
    }

    private float EstimateLockProgress()
    {
        if (boltRoot == null || boltsRetractedOffset.sqrMagnitude < 0.000001f)
        {
            return isOpen ? 1f : 0f;
        }
        return Mathf.Clamp01(
            Vector3.Dot(boltRoot.localPosition - boltsExtendedPosition, boltsRetractedOffset) /
            boltsRetractedOffset.sqrMagnitude
        );
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        if (dialPivot != null) dialClosedEuler = dialPivot.localEulerAngles;
        if (handlePivot != null) handleClosedEuler = handlePivot.localEulerAngles;
        if (boltRoot != null) boltsExtendedPosition = boltRoot.localPosition;
        initialized = true;
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
