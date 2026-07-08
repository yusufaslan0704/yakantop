using UnityEngine;

public class ImpactEffect : MonoBehaviour
{
    public float lifeTime = 0.25f;
    public float growSpeed = 7f;

    private Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.localScale += Vector3.one * growSpeed * Time.deltaTime;
    }
}