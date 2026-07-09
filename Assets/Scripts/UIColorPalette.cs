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

    public static readonly Color StartButton = new Color(0.15f, 0.55f, 0.35f, 1f);
    public static readonly Color StartButtonHover = new Color(0.2f, 0.7f, 0.45f, 1f);
}
