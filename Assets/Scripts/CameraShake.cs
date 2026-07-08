using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("Shake Settings")]
    public float defaultDuration = 0.12f;
    public float defaultStrength = 0.15f;

    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeStrength = 0f;

    void Awake()
    {
        // Sahnede yanlışlıkla ikinci bir CameraShake varsa sessizce ezmesin.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Sahnede birden fazla CameraShake var! Fazlalık yok sayılıyor: " + gameObject.name);
            return;
        }

        Instance = this;
    }

    void LateUpdate()
    {
        if (shakeTimer <= 0f)
        {
            return;
        }

        shakeTimer -= Time.unscaledDeltaTime;

        float progress = 1f;

        if (shakeDuration > 0f)
        {
            progress = shakeTimer / shakeDuration;
        }

        float currentStrength = shakeStrength * Mathf.Clamp01(progress);

        Vector3 shakeOffset = Random.insideUnitSphere * currentStrength;

        // Kamera çok ileri-geri zıplamasın, daha çok X/Y titreşim versin.
        shakeOffset.z = 0f;

        transform.position += shakeOffset;
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        shakeDuration = duration;
        shakeTimer = duration;
        shakeStrength = strength;

        Debug.Log("Camera shake çalıştı. Duration: " + duration + " Strength: " + strength);
    }
}