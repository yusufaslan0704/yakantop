using System.Collections.Generic;
using UnityEngine;

// Sahnedeki tum oyuncularin merkezi kaydi.
// PlayerRole kendini buraya kaydeder; GameManager, botlar ve UI
// oyunculara sahne referansi yerine buradan erisir.
// Boylece yeni oyuncu eklemek icin hicbir scripti elle baglamak gerekmez.
public static class PlayerManager
{
    private static readonly List<PlayerRole> players = new List<PlayerRole>();

    public static IReadOnlyList<PlayerRole> All
    {
        get { return players; }
    }

    public static void Register(PlayerRole player)
    {
        if (player != null && !players.Contains(player))
        {
            players.Add(player);
        }
    }

    public static void Unregister(PlayerRole player)
    {
        players.Remove(player);
    }

    public static PlayerRole GetFirst(RoleType role)
    {
        foreach (PlayerRole player in players)
        {
            if (player.roleType == role)
            {
                return player;
            }
        }

        return null;
    }

    // Bu rolde en az bir oyuncu var mi (elenmis olsa bile)?
    public static bool HasAny(RoleType role)
    {
        foreach (PlayerRole player in players)
        {
            if (player.roleType == role)
            {
                return true;
            }
        }

        return false;
    }

    // Bu rolde hayatta (elenmemis) en az bir oyuncu var mi?
    public static bool AnyAlive(RoleType role)
    {
        foreach (PlayerRole player in players)
        {
            if (player.roleType != role) continue;

            if (player.Health == null || !player.Health.IsEliminated)
            {
                return true;
            }
        }

        return false;
    }

    // Verilen noktaya en yakin, hayatta olan oyuncu (bot hedeflemesi icin).
    public static PlayerRole GetClosestAlive(RoleType role, Vector3 from)
    {
        PlayerRole closest = null;
        float closestSqrDistance = float.MaxValue;

        foreach (PlayerRole player in players)
        {
            if (player.roleType != role) continue;

            if (player.Health != null && player.Health.IsEliminated) continue;

            float sqrDistance = (player.transform.position - from).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = player;
            }
        }

        return closest;
    }

    // Editor'de domain reload kapaliysa bile liste her oyun basinda temiz baslasin.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        players.Clear();
    }
}
