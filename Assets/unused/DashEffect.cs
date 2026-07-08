using UnityEngine;

public class DashEffect : MonoBehaviour
{
    public float lifeTime = 0.4f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}