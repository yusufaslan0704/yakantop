using UnityEngine;

// Atıcı özel yeteneklerinin tek kaydı.
// Yeni yetenek: buraya satır ekle + component; PlayerThrow/BallSelectUI otomatik uzar.
public enum ThrowerAbilityId
{
    None = 0,
    AirLift = 1,
    Invis = 2,
    Fake = 3,
    Shadow = 4
}

public readonly struct ThrowerAbilityInfo
{
    public readonly ThrowerAbilityId Id;
    public readonly string DisplayName;
    public readonly string RowName;
    public readonly string ActivateHint;
    public readonly Color Accent;

    public ThrowerAbilityInfo(
        ThrowerAbilityId id,
        string displayName,
        string rowName,
        string activateHint,
        Color accent)
    {
        Id = id;
        DisplayName = displayName;
        RowName = rowName;
        ActivateHint = activateHint;
        Accent = accent;
    }
}

public static class ThrowerAbilityRegistry
{
    static readonly ThrowerAbilityInfo[] Abilities =
    {
        new ThrowerAbilityInfo(
            ThrowerAbilityId.AirLift,
            "Yuksek",
            "AirLift",
            "RM",
            new Color(0.45f, 0.85f, 1f, 1f)),
        new ThrowerAbilityInfo(
            ThrowerAbilityId.Invis,
            "Gorunmez",
            "Invis",
            "V",
            new Color(0.75f, 0.55f, 1f, 1f)),
        new ThrowerAbilityInfo(
            ThrowerAbilityId.Fake,
            "Fake",
            "Fake",
            "Q",
            new Color(1f, 0.55f, 0.25f, 1f)),
        new ThrowerAbilityInfo(
            ThrowerAbilityId.Shadow,
            "Golge",
            "Shadow",
            "X",
            new Color(0.55f, 0.55f, 0.7f, 1f)),
    };

    public static int Count => Abilities.Length;

    public static ThrowerAbilityInfo GetByIndex(int abilityIndex)
    {
        if (abilityIndex < 0 || abilityIndex >= Abilities.Length)
        {
            return default;
        }

        return Abilities[abilityIndex];
    }

    public static int IndexOf(ThrowerAbilityId id)
    {
        for (int i = 0; i < Abilities.Length; i++)
        {
            if (Abilities[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    public static int SlotFor(int ballCount, ThrowerAbilityId id)
    {
        int index = IndexOf(id);
        if (index < 0)
        {
            return -1;
        }

        return ballCount + index;
    }

    public static bool IsAbilitySlot(int ballCount, int slot)
    {
        return slot >= ballCount && slot < ballCount + Abilities.Length;
    }

    public static ThrowerAbilityId AbilityAtSlot(int ballCount, int slot)
    {
        if (!IsAbilitySlot(ballCount, slot))
        {
            return ThrowerAbilityId.None;
        }

        return Abilities[slot - ballCount].Id;
    }

    public static ThrowerAbilityInfo InfoAtSlot(int ballCount, int slot)
    {
        if (!IsAbilitySlot(ballCount, slot))
        {
            return default;
        }

        return Abilities[slot - ballCount];
    }

    // 0 = CD'de (bos), 1 = hazir. Aktif durumlar 1'e yakin tutulur.
    public static float GetCooldownVisual(PlayerThrow thrower, ThrowerAbilityId id)
    {
        if (thrower == null) return 1f;

        switch (id)
        {
            case ThrowerAbilityId.AirLift:
            {
                PlayerAirLift lift = thrower.GetComponent<PlayerAirLift>();
                if (lift != null && lift.IsElevated) return 1f;
                return 1f;
            }
            case ThrowerAbilityId.Invis:
            {
                PlayerThrowerInvis invis = thrower.GetComponent<PlayerThrowerInvis>();
                return invis != null ? invis.GetCooldownPercent() : 1f;
            }
            case ThrowerAbilityId.Fake:
            {
                PlayerThrowFake fake = thrower.GetComponent<PlayerThrowFake>();
                return fake != null ? fake.GetCooldownPercent() : 1f;
            }
            case ThrowerAbilityId.Shadow:
            {
                PlayerThrowerShadow shadow = thrower.GetComponent<PlayerThrowerShadow>();
                return shadow != null ? shadow.GetCooldownPercent() : 1f;
            }
            default:
                return 1f;
        }
    }

    public static bool IsAbilityBusy(PlayerThrow thrower, ThrowerAbilityId id)
    {
        if (thrower == null) return false;

        switch (id)
        {
            case ThrowerAbilityId.AirLift:
            {
                PlayerAirLift lift = thrower.GetComponent<PlayerAirLift>();
                return lift != null && lift.IsElevated;
            }
            case ThrowerAbilityId.Invis:
            {
                PlayerThrowerInvis invis = thrower.GetComponent<PlayerThrowerInvis>();
                return invis != null && invis.IsInvisible;
            }
            case ThrowerAbilityId.Fake:
            {
                PlayerThrowFake fake = thrower.GetComponent<PlayerThrowFake>();
                return fake != null && fake.IsFaking;
            }
            case ThrowerAbilityId.Shadow:
            {
                PlayerThrowerShadow shadow = thrower.GetComponent<PlayerThrowerShadow>();
                return shadow != null && shadow.HasShadow;
            }
            default:
                return false;
        }
    }

    public static string BuildStatusLine(PlayerThrow thrower)
    {
        if (thrower == null) return string.Empty;

        int ballCount = thrower.AvailableBallTypes != null ? thrower.AvailableBallTypes.Length : 0;
        var parts = new System.Collections.Generic.List<string>(3);

        AppendSlotStatus(parts, thrower, ballCount, thrower.PrimarySlotIndex, "Sari");
        AppendSlotStatus(parts, thrower, ballCount, thrower.ComboSlotIndex, "Yesil");

        if (parts.Count == 0)
        {
            return "Kombo sec: 1-0  |  Volley: RM";
        }

        string line = string.Join(" + ", parts);

        if (thrower.AirLiftModeSelected)
        {
            line += "  |  RM tut=yuksel (Volley kapali)";
        }
        else
        {
            line += "  |  Volley: RM";
        }

        return line;
    }

    static void AppendSlotStatus(
        System.Collections.Generic.List<string> parts,
        PlayerThrow thrower,
        int ballCount,
        int slot,
        string colorTag)
    {
        if (slot < 0) return;

        if (IsAbilitySlot(ballCount, slot))
        {
            ThrowerAbilityInfo info = InfoAtSlot(ballCount, slot);
            parts.Add(info.DisplayName + " [" + info.ActivateHint + "]");
            return;
        }

        BallData[] types = thrower.AvailableBallTypes;
        if (types != null && slot < types.Length && types[slot] != null)
        {
            string name = types[slot].ballName;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.EndsWith(" Ball")) name = name.Substring(0, name.Length - 5);
                else if (name.EndsWith(" Top")) name = name.Substring(0, name.Length - 4);
            }
            else
            {
                name = "Top";
            }

            parts.Add(name);
        }
    }

    public static void EnsureComponents(GameObject thrower)
    {
        if (thrower == null) return;

        if (thrower.GetComponent<PlayerAirLift>() == null)
        {
            thrower.AddComponent<PlayerAirLift>();
        }

        if (thrower.GetComponent<PlayerThrowerInvis>() == null)
        {
            thrower.AddComponent<PlayerThrowerInvis>();
        }

        if (thrower.GetComponent<PlayerThrowFake>() == null)
        {
            thrower.AddComponent<PlayerThrowFake>();
        }

        if (thrower.GetComponent<PlayerThrowerShadow>() == null)
        {
            thrower.AddComponent<PlayerThrowerShadow>();
        }

        if (thrower.GetComponent<ThrowAimPreview>() == null)
        {
            thrower.AddComponent<ThrowAimPreview>();
        }

        if (thrower.GetComponent<ThrowReleaseController>() == null)
        {
            thrower.AddComponent<ThrowReleaseController>();
        }
    }
}
