using UnityEngine;

public enum RoleType
{
    Runner,
    Thrower,
    Saver
}

public class PlayerRole : MonoBehaviour
{
    public RoleType roleType = RoleType.Runner;
}