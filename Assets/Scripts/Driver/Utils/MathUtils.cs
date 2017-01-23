using UnityEngine;

static class MathUtils
{
    public static float GetSignedAngleDiff(float a, float b)
    {
        float phi = Mathf.Abs(b - a) % 360;
        float dif = phi > 180 ? 360 - phi : phi;

        if ((a - b >= 0 && a - b <= 180) || (a - b <= -180 && a - b >= -360))
            dif *= -1;

        return dif;
    }

    public static float NfMod(float a, float b)
    {
        return a - b * Mathf.Floor(a / b);
    }

    public static Quaternion NormQuat(Quaternion q)
    {
        float sq = 1 / (Mathf.Sqrt(q.w * q.w + q.x * q.x + q.y * q.y + q.z * q.z));
        return new Quaternion(q.x * sq, q.y * sq, q.z * sq, q.w * sq);
    }
}


