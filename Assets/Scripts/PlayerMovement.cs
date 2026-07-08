using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	[Header("Movement")]
	public float moveSpeed = 8f;
	public float rotationSpeed = 12f;

	[Header("Camera")]
	public Transform cameraTransform;

	private Vector3 moveInput;
	private PlayerHealth playerHealth;

	void Awake()
	{
		playerHealth = GetComponent<PlayerHealth>();
	}

	void Update()
	{
		if (playerHealth != null && playerHealth.isEliminated)
		{
			return;
		}

		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");

		Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

		if (inputDirection.magnitude >= 0.1f)
		{
			Vector3 cameraForward = cameraTransform.forward;
			Vector3 cameraRight = cameraTransform.right;

			cameraForward.y = 0f;
			cameraRight.y = 0f;

			cameraForward.Normalize();
			cameraRight.Normalize();

			moveInput = cameraForward * vertical + cameraRight * horizontal;
			moveInput.Normalize();

			transform.position += moveInput * moveSpeed * Time.deltaTime;

			Quaternion targetRotation = Quaternion.LookRotation(moveInput);

			transform.rotation = Quaternion.Slerp(
				transform.rotation,
				targetRotation,
				rotationSpeed * Time.deltaTime
			);
		}
	}
}