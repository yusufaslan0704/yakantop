using UnityEngine;

// Atış animasyonu ile gameplay arasindaki ince kopru.
// CharacterModelVisual PlayerThrow'a baglanmaz; sadece bu bridge'i dinler.
public class ThrowAnimationBridge : MonoBehaviour
{
    PlayerThrow thrower;

    public event System.Action OnThrowStarted;
    public event System.Action OnChargeStarted;
    public event System.Action OnChargeCancelled;

    // Top spawn gecikmesi — PlayerThrow.ballReleaseDelay tek kaynak.
    public float BallReleaseDelay
    {
        get
        {
            if (thrower != null)
            {
                return Mathf.Max(0.08f, thrower.ballReleaseDelay);
            }

            return 0.4f;
        }
    }

    void Awake()
    {
        thrower = GetComponent<PlayerThrow>();
    }

    public void RaiseThrowStarted()
    {
        OnThrowStarted?.Invoke();
    }

    public void RaiseChargeStarted()
    {
        OnChargeStarted?.Invoke();
    }

    public void RaiseChargeCancelled()
    {
        OnChargeCancelled?.Invoke();
    }
}
