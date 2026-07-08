using UnityEngine;

public class VFXAutoDestroy : MonoBehaviour
{
    public float lifeTime = 0.6f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}