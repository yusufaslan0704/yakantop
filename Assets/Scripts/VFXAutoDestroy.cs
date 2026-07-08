using UnityEngine;

// Tüm kısa ömürlü efektler için tek script:
// Belirli süre sonra kendini yok eder, istenirse o sırada büyür.
public class VFXAutoDestroy : MonoBehaviour
{
    public float lifeTime = 0.6f;

    [Tooltip("0 ise efekt büyümez, sadece süresi dolunca yok olur.")]
    public float growSpeed = 0f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (growSpeed > 0f)
        {
            transform.localScale += Vector3.one * growSpeed * Time.deltaTime;
        }
    }
}
