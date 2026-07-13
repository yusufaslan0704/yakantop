using System.Collections;
using UnityEngine;

// Atis release pipeline: cooldown, delay, spawn coroutine.
// PlayerThrow charge/aim/loadout ile ilgilenir; release buraya delege edilir.
public class ThrowReleaseController : MonoBehaviour
{
    PlayerThrow thrower;
    PlayerHealth playerHealth;
    PlayerAirLift airLift;

    Coroutine pendingThrowRoutine;
    bool isThrowInProgress;
    float nextThrowTime;

    public bool IsThrowInProgress => isThrowInProgress;
    public float NextThrowTime => nextThrowTime;
    public bool IsBusy => isThrowInProgress || Time.time < nextThrowTime;

    public void Bind(PlayerThrow owner)
    {
        thrower = owner;
        playerHealth = owner != null ? owner.GetComponent<PlayerHealth>() : null;
        airLift = owner != null ? owner.GetComponent<PlayerAirLift>() : null;
    }

    public void CancelPending()
    {
        if (pendingThrowRoutine != null)
        {
            StopCoroutine(pendingThrowRoutine);
            pendingThrowRoutine = null;
        }

        isThrowInProgress = false;
    }

    public void BeginThrow(BallData dataToThrow, Vector3 throwDirection, float force, bool rotateToAim)
    {
        if (thrower == null)
        {
            return;
        }

        if (dataToThrow == null || dataToThrow.prefab == null)
        {
            Debug.LogWarning("Ball Data atanmadı veya prefab eksik!");
            thrower.NotifyChargeCancelled();
            return;
        }

        if (thrower.throwPoint == null)
        {
            Debug.LogWarning("Throw Point atanmadı!");
            thrower.NotifyChargeCancelled();
            return;
        }

        if (throwDirection == Vector3.zero)
        {
            thrower.NotifyChargeCancelled();
            return;
        }

        if (rotateToAim)
        {
            thrower.RotateToThrowDirection(throwDirection);
        }

        CancelPending();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(thrower.cooldown, thrower.ballReleaseDelay);
        CameraShake.SetChargeFovPullAll(0f);
        thrower.NotifyThrowStarted();

        pendingThrowRoutine = StartCoroutine(
            ReleaseBallAfterDelay(dataToThrow, throwDirection, force, null, thrower.ballReleaseDelay));
    }

    public bool TryBeginAbilityThrow(
        BallData dataToThrow,
        Vector3 throwDirection,
        float force,
        System.Action<Ball> configureBall,
        float cooldownOverride)
    {
        if (thrower == null || thrower.throwPoint == null) return false;
        if (Time.time < nextThrowTime || isThrowInProgress) return false;
        if (dataToThrow == null || dataToThrow.prefab == null) return false;
        if (throwDirection == Vector3.zero) return false;

        CancelPending();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(thrower.cooldown, cooldownOverride);
        thrower.NotifyThrowStarted();

        pendingThrowRoutine = StartCoroutine(
            ReleaseBallAfterDelay(dataToThrow, throwDirection, force, configureBall, thrower.ballReleaseDelay));
        return true;
    }

    public bool TryBeginSpread(
        BallData dataToThrow,
        Vector3 centerDir,
        float force,
        float totalSpreadDegrees,
        int count,
        float loft)
    {
        if (thrower == null || thrower.throwPoint == null) return false;
        if (Time.time < nextThrowTime || isThrowInProgress) return false;
        if (dataToThrow == null || dataToThrow.prefab == null) return false;
        if (centerDir == Vector3.zero) return false;

        CancelPending();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(thrower.cooldown, 0.35f);
        thrower.NotifyThrowStarted();

        pendingThrowRoutine = StartCoroutine(
            ReleaseSpreadAfterDelay(dataToThrow, centerDir, force, totalSpreadDegrees, count, loft));
        return true;
    }

    public GameObject SpawnBall(
        BallData dataToThrow,
        Vector3 throwDirection,
        float force,
        bool playFeedback,
        Vector3 spawnLateralOffset)
    {
        if (thrower == null || thrower.throwPoint == null || dataToThrow == null)
        {
            return null;
        }

        Vector3[] path = null;
        float pathSpeed = 0f;
        bool followPath = thrower.humanFollowsPlannedPath &&
                          thrower.IsHumanControlled &&
                          playFeedback &&
                          thrower.TryConsumePlannedPath(out path, out pathSpeed);

        GameObject ball = ThrowBallSpawner.Spawn(new ThrowBallSpawner.Request
        {
            Data = dataToThrow,
            ThrowPoint = thrower.throwPoint,
            Direction = throwDirection,
            Force = force,
            LateralOffset = spawnLateralOffset,
            Owner = thrower.gameObject,
            FollowPath = followPath,
            Path = path,
            PathSpeed = pathSpeed,
            PlayFeedback = playFeedback,
            ChargePercent = thrower.LastReleasedChargePercent,
            IsHuman = thrower.IsHumanControlled,
            ShakeDuration = thrower.throwShakeDuration,
            ShakeStrength = thrower.throwShakeStrength,
            FovPunch = thrower.throwFovPunch,
            AirLift = airLift,
            OnBallThrown = thrower.NotifyBallThrown
        });

        if (ball != null)
        {
            Vector3 spawnPosition = thrower.throwPoint.position + throwDirection * 0.6f + spawnLateralOffset;
            thrower.RecordThrowStats(force, spawnPosition + throwDirection.normalized * 32f);
        }

        if (!followPath)
        {
            thrower.ClearPlannedPath();
        }

        return ball;
    }

    IEnumerator ReleaseBallAfterDelay(
        BallData dataToThrow,
        Vector3 throwDirection,
        float force,
        System.Action<Ball> configureBall,
        float delaySeconds)
    {
        float delay = Mathf.Max(0f, delaySeconds);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        pendingThrowRoutine = null;

        if (!GameManager.RoundIsActive ||
            (playerHealth != null && playerHealth.IsEliminated))
        {
            isThrowInProgress = false;
            yield break;
        }

        GameObject ballGo = SpawnBall(
            dataToThrow, throwDirection, force, playFeedback: true, spawnLateralOffset: Vector3.zero);

        if (configureBall != null && ballGo != null)
        {
            Ball ball = ballGo.GetComponent<Ball>();
            if (ball != null)
            {
                configureBall(ball);
            }
        }

        isThrowInProgress = false;
    }

    IEnumerator ReleaseSpreadAfterDelay(
        BallData dataToThrow,
        Vector3 centerDir,
        float force,
        float totalSpreadDegrees,
        int count,
        float loft)
    {
        float delay = Mathf.Min(0.18f, Mathf.Max(0f, thrower.ballReleaseDelay * 0.45f));
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        pendingThrowRoutine = null;

        if (!GameManager.RoundIsActive ||
            (playerHealth != null && playerHealth.IsEliminated))
        {
            isThrowInProgress = false;
            yield break;
        }

        count = Mathf.Max(1, count);
        float half = totalSpreadDegrees * 0.5f;

        Vector3 flat = centerDir;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
        {
            flat = thrower.transform.forward;
            flat.y = 0f;
        }

        flat.Normalize();
        float pitchY = centerDir.y + loft;

        Collider[] spawnedColliders = new Collider[count];
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float yaw = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * flat;
            dir = (dir + Vector3.up * pitchY).normalized;

            Vector3 lateral = Vector3.Cross(Vector3.up, flat).normalized;
            Vector3 spawnOffset = lateral * Mathf.Lerp(-0.55f, 0.55f, t);

            GameObject ball = SpawnBall(
                dataToThrow, dir, force, playFeedback: i == 0, spawnLateralOffset: spawnOffset);

            if (ball != null)
            {
                spawnedColliders[i] = ball.GetComponent<Collider>();
            }
        }

        IgnoreCollisionsBetween(spawnedColliders);
        isThrowInProgress = false;
    }

    static void IgnoreCollisionsBetween(Collider[] colliders)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;

            for (int j = i + 1; j < colliders.Length; j++)
            {
                if (colliders[j] == null) continue;
                Physics.IgnoreCollision(colliders[i], colliders[j], true);
            }
        }
    }
}
