using UnityEngine;

// Lobby + HUD + barlar icin ortak renk dili (karanlik lacivert + cyan).
public static class UIColorPalette
{
    public static readonly Color Backdrop = new Color(0.02f, 0.04f, 0.08f, 0.96f);
    public static readonly Color Card = new Color(0.08f, 0.11f, 0.16f, 0.98f);
    public static readonly Color CardSoft = new Color(0.08f, 0.11f, 0.16f, 0.88f);

    public static readonly Color Title = new Color(0.85f, 0.95f, 1f, 1f);
    public static readonly Color Body = new Color(0.75f, 0.82f, 0.9f, 1f);
    public static readonly Color Muted = new Color(0.55f, 0.65f, 0.75f, 1f);
    public static readonly Color Accent = new Color(0.45f, 0.9f, 1f, 1f);
    public static readonly Color Warning = new Color(0.9f, 0.75f, 0.35f, 1f);
    public static readonly Color Danger = new Color(1f, 0.35f, 0.28f, 1f);
    public static readonly Color Success = new Color(0.35f, 0.85f, 0.55f, 1f);

    public static readonly Color BarBackground = new Color(0.04f, 0.06f, 0.1f, 0.78f);
    public static readonly Color BarTrack = new Color(0.12f, 0.16f, 0.22f, 0.95f);
    public static readonly Color ChargeFill = new Color(0.4f, 0.95f, 0.55f, 0.95f);
    public static readonly Color ChargeFillHot = new Color(0.95f, 0.95f, 0.45f, 1f);
    public static readonly Color ReviveFill = new Color(0.45f, 0.9f, 1f, 0.95f);

    public static readonly Color Outline = new Color(0.02f, 0.04f, 0.08f, 0.9f);
    public static readonly Color Crosshair = new Color(1f, 1f, 1f, 0.72f);
    public static readonly Color AimArc = new Color(0.45f, 0.9f, 1f, 0.55f);
    public static readonly Color AimArcHot = new Color(0.95f, 0.85f, 0.35f, 0.75f);
    public static readonly Color AimImpact = new Color(0.95f, 0.95f, 0.55f, 0.85f);
    public static readonly Color AimTarget = new Color(0.45f, 0.95f, 1f, 0.55f);
    public static readonly Color AimTargetHot = new Color(1f, 0.55f, 0.35f, 0.85f);

    public static readonly Color ComboPrimary = new Color(1f, 0.82f, 0.2f, 1f);
    public static readonly Color ComboSecondary = new Color(0.35f, 0.95f, 0.45f, 1f);

    public static readonly Color StartButton = new Color(0.15f, 0.55f, 0.35f, 1f);
    public static readonly Color StartButtonHover = new Color(0.2f, 0.7f, 0.45f, 1f);

    // Top gorsel kimlik — UI slot + dokuman tek kaynak (fizik degismez).
    public static readonly Color BallNormal = new Color(0.88f, 0.94f, 0.98f, 1f);
    public static readonly Color BallFast = new Color(1f, 0.88f, 0.18f, 1f);
    public static readonly Color BallHeavy = new Color(0.42f, 0.18f, 0.72f, 1f);
    public static readonly Color BallBouncy = new Color(0.28f, 0.92f, 0.4f, 1f);
    public static readonly Color BallCurve = new Color(1f, 0.52f, 0.14f, 1f);
    public static readonly Color BallMirror = new Color(0.62f, 0.9f, 0.98f, 1f);

    public static Color ColorForBall(BallData data)
    {
        if (data == null)
        {
            return Muted;
        }

        string n = data.ballName != null ? data.ballName.ToLowerInvariant() : "";
        if (n.Contains("fast")) return BallFast;
        if (n.Contains("heavy")) return BallHeavy;
        if (n.Contains("sek") || n.Contains("bounc")) return BallBouncy;
        if (n.Contains("egri") || n.Contains("curve")) return BallCurve;
        if (n.Contains("ayna") || n.Contains("mirror")) return BallMirror;
        if (n.Contains("normal")) return BallNormal;
        return Accent;
    }
}
