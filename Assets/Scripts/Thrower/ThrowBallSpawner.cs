using UnityEngine;

// Top spawn + impulse / path follow. Release pipeline burayi kullanir.
public static class ThrowBallSpawner
{
    public struct Request
    {
        public BallData Data;
        public Transform ThrowPoint;
        public Vector3 Direction;
        public float Force;
        public Vector3 LateralOffset;
        public GameObject Owner;
        public bool FollowPath;
        public Vector3[] Path;
        public float PathSpeed;
        public bool PlayFeedback;
        public float ChargePercent;
        public bool IsHuman;
        public float ShakeDuration;
        public float ShakeStrength;
        public float FovPunch;
        public PlayerAirLift AirLift;
        public System.Action OnBallThrown;
    }

    public static GameObject Spawn(Request request)
    {
        if (request.Data == null || request.Data.prefab == null || request.ThrowPoint == null)
        {
            return null;
        }

        Vector3 spawnPosition = request.ThrowPoint.position +
                                request.Direction * 0.6f +
                                request.LateralOffset;

        GameObject ball = Object.Instantiate(request.Data.prefab, spawnPosition, Quaternion.identity);
        SceneFolders.ParentTo(ball.transform, SceneFolders.RuntimeSpawned);

        // BallData kimlik rengi (Fast sari vb.); textured mesh BaseColor beyaz olabilir.
        Color trailColor = UIColorPalette.ColorForBall(request.Data);
        CombatVfx.TintBallTrail(ball, trailColor);

        Ball ballScript = ball.GetComponent<Ball>();
        if (ballScript != null)
        {
            ballScript.SetOwner(request.Owner);
            ballScript.SetData(request.Data);
        }

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        bool followPath = request.FollowPath &&
                          request.Path != null &&
                          request.Path.Length >= 2 &&
                          request.PlayFeedback;

        if (followPath && ballScript != null)
        {
            Vector3[] pathCopy = (Vector3[])request.Path.Clone();
            pathCopy[0] = spawnPosition;
            float speed = request.PathSpeed > 0.1f
                ? request.PathSpeed
                : ThrowPhysics.PathSpeedFromForce(request.Force, request.Data);
            ballScript.FollowPath(pathCopy, speed);

            if (ballRb != null)
            {
                ballRb.angularVelocity = Vector3.zero;
            }
        }
        else if (ballRb != null)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.AddForce(request.Direction * request.Force, ForceMode.Impulse);
        }

        if (request.PlayFeedback)
        {
            PlayThrowSound(request.Data);
            float charge = Mathf.Clamp01(request.ChargePercent);
            if (charge < 0.01f && !request.IsHuman)
            {
                charge = 0.75f;
            }
            else if (charge < 0.01f)
            {
                charge = 0.55f;
            }

            float shakeMul = Mathf.Lerp(0.45f, 1.2f, charge);
            float fovMul = Mathf.Lerp(0.5f, 1.3f, charge);
            CameraShake.ShakeAll(request.ShakeDuration, request.ShakeStrength * shakeMul);
            CameraShake.PunchFovAll(request.FovPunch * fovMul, 0.12f);
            request.OnBallThrown?.Invoke();
            if (request.AirLift != null)
            {
                request.AirLift.NotifyBallThrown();
            }
        }

        return ball;
    }

    static void PlayThrowSound(BallData dataToThrow)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        if (dataToThrow != null && dataToThrow.throwSfx != null)
        {
            AudioManager.Instance.PlayClip(dataToThrow.throwSfx, AudioManager.Instance.throwVolume);
        }
        else
        {
            AudioManager.Instance.PlayThrow();
        }
    }
}
