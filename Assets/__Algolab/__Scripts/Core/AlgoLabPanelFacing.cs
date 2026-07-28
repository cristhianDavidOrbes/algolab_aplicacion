using UnityEngine;

public static class AlgoLabPanelFacing
{
    public static bool TryGetStableRotation(
        Vector3 direction,
        bool yawOnly,
        Quaternion localOffset,
        bool invertForward,
        out Quaternion rotation,
        float maxPitchDegrees = 82f)
    {
        rotation = Quaternion.identity;

        if (!IsFinite(direction) || direction.sqrMagnitude < 0.000001f)
        {
            return false;
        }

        if (invertForward)
        {
            direction = -direction;
        }

        if (yawOnly)
        {
            direction.y = 0f;
        }
        else
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude < 0.000001f)
            {
                return false;
            }

            float maxPitch = Mathf.Clamp(maxPitchDegrees, 1f, 88f) * Mathf.Deg2Rad;
            float maxVertical = horizontal.magnitude * Mathf.Tan(maxPitch);
            direction.y = Mathf.Clamp(direction.y, -maxVertical, maxVertical);
        }

        if (direction.sqrMagnitude < 0.000001f)
        {
            return false;
        }

        rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * localOffset;
        return IsFinite(rotation);
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) &&
               float.IsFinite(value.y) &&
               float.IsFinite(value.z);
    }

    private static bool IsFinite(Quaternion value)
    {
        return float.IsFinite(value.x) &&
               float.IsFinite(value.y) &&
               float.IsFinite(value.z) &&
               float.IsFinite(value.w);
    }
}
