using UnityEngine;

// Atıcı çift seçim (sarı primary + yeşil combo) durumu.
// PlayerThrow bu sinifi sahiplenir; UI / yetenekler public API üzerinden okur.
public sealed class ThrowerLoadout
{
    public const int EmptySlot = -1;

    int primarySlot = EmptySlot;
    int comboSlot = EmptySlot;

    public int PrimarySlotIndex => primarySlot;
    public int ComboSlotIndex => comboSlot;
    public int SelectedSlotIndex => comboSlot >= 0 ? comboSlot : primarySlot;

    public void Clear()
    {
        primarySlot = EmptySlot;
        comboSlot = EmptySlot;
    }

    public bool IsAbilitySelected(int ballCount, ThrowerAbilityId id)
    {
        if (id == ThrowerAbilityId.None)
        {
            return false;
        }

        return ThrowerAbilityRegistry.AbilityAtSlot(ballCount, primarySlot) == id ||
               ThrowerAbilityRegistry.AbilityAtSlot(ballCount, comboSlot) == id;
    }

    // Combo'daki top oncelikli, yoksa primary top (ozel yetenek slotlari haric).
    public int GetActiveBallSlot(int ballCount)
    {
        if (comboSlot >= 0 && !ThrowerAbilityRegistry.IsAbilitySlot(ballCount, comboSlot))
        {
            return comboSlot;
        }

        if (primarySlot >= 0 && !ThrowerAbilityRegistry.IsAbilitySlot(ballCount, primarySlot))
        {
            return primarySlot;
        }

        return EmptySlot;
    }

    // Cift secim: 1. slot sari (primary), 2. slot yesil (combo).
    // Ayni slota tekrar bas = kaldir / promote.
    // returns true if selection changed enough to notify UI.
    public bool SelectSlot(int slot, int ballCount, int selectableCount, BallData[] ballTypes)
    {
        if (slot < 0 || slot >= selectableCount)
        {
            return false;
        }

        if (slot < ballCount && (ballTypes == null || ballTypes[slot] == null))
        {
            return false;
        }

        if (slot == primarySlot && comboSlot < 0)
        {
            primarySlot = EmptySlot;
        }
        else if (slot == primarySlot && comboSlot >= 0)
        {
            primarySlot = comboSlot;
            comboSlot = EmptySlot;
        }
        else if (slot == comboSlot)
        {
            comboSlot = EmptySlot;
        }
        else if (primarySlot < 0)
        {
            primarySlot = slot;
        }
        else if (comboSlot < 0)
        {
            comboSlot = slot;
        }
        else
        {
            comboSlot = slot;
        }

        return true;
    }

    // Scroll: sadece top slotlari arasinda gezer; yetenekleri atlar.
    public bool CycleBallSlots(int delta, int ballCount, BallData[] ballTypes)
    {
        if (ballCount <= 0 || delta == 0 || ballTypes == null)
        {
            return false;
        }

        int current = primarySlot >= 0 ? primarySlot : 0;
        for (int step = 0; step < ballCount; step++)
        {
            current = (current + delta) % ballCount;
            if (current < 0)
            {
                current += ballCount;
            }

            if (current >= ballCount)
            {
                continue;
            }

            if (ballTypes[current] == null)
            {
                continue;
            }

            primarySlot = current;
            comboSlot = EmptySlot;
            return true;
        }

        return false;
    }
}
