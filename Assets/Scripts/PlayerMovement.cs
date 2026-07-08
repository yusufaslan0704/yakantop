using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
	[Header("Movement")]
	public float moveSpeed = 8f;
	public float rotationSpeed = 12f;

	[Header("Camera")]
	public Transform cameraTransform;

	private Vector3 moveDirection;

	private Rigidbody rb;
	private PlayerHealth playerHealth;
	private PlayerDash playerDash;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		playerHealth = GetComponent<PlayerHealth>();
		playerDash = GetComponent<PlayerDash>();
	}

	void Update()
	{
		// Input'u Update'te okuyoruz, fiziksel hareketi FixedUpdate'te uyguluyoruz.
		moveDirection = Vector3.zero;

		if (playerHealth != null && playerHealth.isEliminated)
		{
			return;
		}

		if (cameraTransform == null)
		{
			return;
		}

		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");

		Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);

		if (inputDirection.sqrMagnitude < 0.01f)
		{
			return;
		}

		Vector3 cameraForward = cameraTransform.forward;
		Vector3 cameraRight = cameraTransform.right;

		cameraForward.y = 0f;
		cameraRight.y = 0f;

		cameraForward.Normalize();
		cameraRight.Normalize();

		moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
	}

	void FixedUpdate()
	{
		// Elenen oyuncunun rigidbody'si kinematik olur, hız verilemez.
		if (rb.isKinematic)
		{
			return;
		}

		// Dash sırasında hızı PlayerDash yönetiyor.
		if (playerDash != null && playerDash.IsDashing())
		{
			return;
		}

		Vector3 velocity = moveDirection * moveSpeed;
		velocity.y = rb.linearVelocity.y; // Yerçekimini koru.

		rb.linearVelocity = velocity;

		if (moveDirection != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

			rb.MoveRotation(Quaternion.Slerp(
				rb.rotation,
				targetRotation,
				rotationSpeed * Time.fixedDeltaTime
			));
		}
	}

	void OnDisable()
	{
		// Kontrol başka karaktere geçtiğinde bu karakter kayarak gitmesin.
		moveDirection = Vector3.zero;

		if (rb != null && !rb.isKinematic)
		{
			Vector3 velocity = rb.linearVelocity;
			velocity.x = 0f;
			velocity.z = 0f;
			rb.linearVelocity = velocity;
		}
	}
}
