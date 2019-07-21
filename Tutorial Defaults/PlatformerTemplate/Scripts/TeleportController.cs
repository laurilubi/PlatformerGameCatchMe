using Platformer.Mechanics;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TeleportController : MonoBehaviour
{
    public GameObject destination;
    public float xMultiplier = 1;
    public float yMultiplier = 1;

    private Vector2 multiplier;

    void Awake()
    {
        multiplier = new Vector2(xMultiplier, yMultiplier);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (destination == null) return;

        var player = other.gameObject.GetComponent<PlayerController>();
        if (player == null) return;
        if (Time.time < player.teleportableAfter) return;

        var position = destination.transform.position;
        player.Teleport(position, multiplier);

        player.teleportableAfter = Time.time + 1;
    }
}
