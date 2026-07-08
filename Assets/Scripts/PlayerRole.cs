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

    // Siklikla erisilen saglik bileseni; herkes tekrar tekrar GetComponent yapmasin.
    public PlayerHealth Health { get; private set; }

    void Awake()
    {
        Health = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        PlayerManager.Register(this);
    }

    void OnDisable()
    {
        PlayerManager.Unregister(this);
    }
}
