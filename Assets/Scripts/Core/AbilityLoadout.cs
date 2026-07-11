using System.Collections.Generic;
using UnityEngine;

public enum LoadoutAbility
{
    Flash,
    Shield,
    Invisibility,
    Decoy
}

// Kacan takim: mac basinda en fazla 2 yetenek secilir.
// Secim lobide yapilir; StartMatch'te tum Runner/Saver'lara uygulanir.
public static class AbilityLoadout
{
    public const int MaxPicks = 2;
    public const int MinPicks = 1;

    static readonly List<LoadoutAbility> picks = new List<LoadoutAbility>
    {
        LoadoutAbility.Flash,
        LoadoutAbility.Decoy
    };

    public static event System.Action OnChanged;

    public static IReadOnlyList<LoadoutAbility> Picks => picks;
    public static int Count => picks.Count;

    public static bool IsSelected(LoadoutAbility ability)
    {
        return picks.Contains(ability);
    }

    // true = degisti. false = min/max engeli.
    public static bool TryToggle(LoadoutAbility ability)
    {
        if (picks.Contains(ability))
        {
            if (picks.Count <= MinPicks)
            {
                return false;
            }

            picks.Remove(ability);
            OnChanged?.Invoke();
            return true;
        }

        if (picks.Count >= MaxPicks)
        {
            return false;
        }

        picks.Add(ability);
        OnChanged?.Invoke();
        return true;
    }

    // HUD sirasi: 0 / 1, secili degilse -1.
    public static int HudSlot(LoadoutAbility ability)
    {
        return picks.IndexOf(ability);
    }

    public static int HudSlot(AbilityBarType barType)
    {
        if (!TryMap(barType, out LoadoutAbility ability))
        {
            return -1;
        }

        return HudSlot(ability);
    }

    public static bool TryMap(AbilityBarType barType, out LoadoutAbility ability)
    {
        switch (barType)
        {
            case AbilityBarType.Flash:
                ability = LoadoutAbility.Flash;
                return true;
            case AbilityBarType.Shield:
                ability = LoadoutAbility.Shield;
                return true;
            case AbilityBarType.Invisibility:
                ability = LoadoutAbility.Invisibility;
                return true;
            case AbilityBarType.Decoy:
                ability = LoadoutAbility.Decoy;
                return true;
            default:
                ability = default;
                return false;
        }
    }

    public static void ApplyToAllPlayers()
    {
        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null) continue;
            if (player.roleType != RoleType.Runner && player.roleType != RoleType.Saver)
            {
                continue;
            }

            ApplyToEscapePlayer(player.gameObject);
        }
    }

    public static void ApplyToEscapePlayer(GameObject player)
    {
        if (player == null) return;

        SetEnabled(player.GetComponent<PlayerFlash>(), IsSelected(LoadoutAbility.Flash));
        SetEnabled(player.GetComponent<PlayerShield>(), IsSelected(LoadoutAbility.Shield));
        SetEnabled(player.GetComponent<PlayerInvisibility>(), IsSelected(LoadoutAbility.Invisibility));

        PlayerDecoy decoy = player.GetComponent<PlayerDecoy>();
        if (decoy != null)
        {
            // Decoy sadece Runner'da var; Saver'da component yok.
            SetEnabled(decoy, IsSelected(LoadoutAbility.Decoy));
        }
    }

    static void SetEnabled(Behaviour behaviour, bool enabled)
    {
        if (behaviour != null)
        {
            behaviour.enabled = enabled;
        }
    }

    public static string DisplayName(LoadoutAbility ability)
    {
        switch (ability)
        {
            case LoadoutAbility.Flash: return "FLASH";
            case LoadoutAbility.Shield: return "SHIELD";
            case LoadoutAbility.Invisibility: return "INVIS";
            case LoadoutAbility.Decoy: return "DECOY";
            default: return ability.ToString();
        }
    }

    public static string KeyHint(LoadoutAbility ability)
    {
        switch (ability)
        {
            case LoadoutAbility.Flash: return "E";
            case LoadoutAbility.Shield: return "G";
            case LoadoutAbility.Invisibility: return "V";
            case LoadoutAbility.Decoy: return "X";
            default: return "?";
        }
    }

    public static string ShortDesc(LoadoutAbility ability)
    {
        switch (ability)
        {
            case LoadoutAbility.Flash: return "Flashbang";
            case LoadoutAbility.Shield: return "Kalkan";
            case LoadoutAbility.Invisibility: return "Gorunmez";
            case LoadoutAbility.Decoy: return "Sahte kopya";
            default: return "";
        }
    }
}
