using System.Collections.Generic;
using UnityEngine;

// Kamera sarsintisi + kisa FOV punch (dash / hit / dodge hissi).
[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private static readonly List<CameraShake> allInstances = new List<CameraShake>();

    [Header("Shake Settings")]
    public float defaultDuration = 0.12f;
    public float defaultStrength = 0.15f;

    [Header("FOV Punch")]
    [Tooltip("Dash/hit aninda FOV genisleyip geri doner.")]
    public float fovPunchDegrees = 4.5f;
    public float fovPunchDuration = 0.18f;

    private float shakeTimer;
    private float shakeDuration;
    private float shakeStrength;

    private Camera cam;
    private float baseFov = 60f;
    private float fovPunchTimer;
    private float fovPunchAmount;
    private float chargeFovPull;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (!allInstances.Contains(this))
        {
            allInstances.Add(this);
        }

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            baseFov = cam.fieldOfView;
        }
    }

    void OnDestroy()
    {
        allInstances.Remove(this);

        if (Instance == this)
        {
            Instance = allInstances.Count > 0 ? allInstances[0] : null;
        }
    }

    void LateUpdate()
    {
        UpdateShake();
        UpdateFovPunch();
    }

    void UpdateShake()
    {
        if (shakeTimer <= 0f)
        {
            return;
        }

        shakeTimer -= Time.unscaledDeltaTime;

        float progress = shakeDuration > 0f ? shakeTimer / shakeDuration : 0f;
        // Ease-out: baslangicta guclu, sonda yumusak.
        float falloff = Mathf.Clamp01(progress);
        falloff *= falloff;

        float currentStrength = shakeStrength * falloff;
        Vector3 shakeOffset = Random.insideUnitSphere * currentStrength;
        shakeOffset.z *= 0.25f;

        transform.position += shakeOffset;
    }

    void UpdateFovPunch()
    {
        if (cam == null)
        {
            return;
        }

        float targetBase = baseFov - chargeFovPull;

        if (fovPunchTimer <= 0f)
        {
            if (!Mathf.Approximately(cam.fieldOfView, targetBase))
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetBase, 14f * Time.unscaledDeltaTime);
            }

            return;
        }

        fovPunchTimer -= Time.unscaledDeltaTime;
        float t = fovPunchDuration > 0f ? 1f - Mathf.Clamp01(fovPunchTimer / fovPunchDuration) : 1f;
        // Hizli acilip yavas kapanan punch.
        float punch = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * fovPunchAmount;
        cam.fieldOfView = targetBase + punch;
    }

    public void SetChargeFovPull(float degrees)
    {
        chargeFovPull = Mathf.Max(0f, degrees);
    }

    public static void SetChargeFovPullAll(float degrees)
    {
        foreach (CameraShake shake in allInstances)
        {
            if (shake != null)
            {
                shake.SetChargeFovPull(degrees);
            }
        }
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        // Daha guclu gelen sarsinti oncekini ezer; zayif olan birikir gibi uzatmaz.
        if (strength >= shakeStrength || shakeTimer <= 0f)
        {
            shakeDuration = duration;
            shakeTimer = duration;
            shakeStrength = strength;
        }
        else
        {
            shakeTimer = Mathf.Max(shakeTimer, duration * 0.5f);
        }
    }

    public void PunchFov(float degrees, float duration)
    {
        if (cam == null)
        {
            return;
        }

        // Punch bitmemisse once mevcut baz FOV'a yaklasik don.
        if (fovPunchTimer <= 0f)
        {
            baseFov = cam.fieldOfView;
        }

        fovPunchAmount = degrees;
        fovPunchDuration = Mathf.Max(0.05f, duration);
        fovPunchTimer = fovPunchDuration;
    }

    public static void ShakeAll(float duration, float strength)
    {
        foreach (CameraShake shake in allInstances)
        {
            if (shake != null)
            {
                shake.Shake(duration, strength);
            }
        }
    }

    public static void PunchFovAll(float degrees, float duration)
    {
        foreach (CameraShake shake in allInstances)
        {
            if (shake != null)
            {
                shake.PunchFov(degrees, duration);
            }
        }
    }
}
