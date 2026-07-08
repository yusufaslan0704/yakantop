using UnityEngine;

public class HitEffect : MonoBehaviour
{
	public float lifeTime = 0.25f;
	public float growSpeed = 6f;

	void Update()
	{
		transform.localScale += Vector3.one * growSpeed * Time.deltaTime;
	}

	void Start()
	{
		Destroy(gameObject, lifeTime);
	}
}