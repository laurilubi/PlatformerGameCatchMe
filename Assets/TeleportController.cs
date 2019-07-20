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

        var playerVelocity = player.velocity * multiplier;
        //var diff = playerVelocity.normalized * destination.transform.renderer.
        var position = destination.transform.position;
        //position += new Vector3(multiplier.x * 0.1f, multiplier.y * 0.1f, 0);

        player.Teleport(position, multiplier);
        //player.velocity.x = player.velocity.x * xMultiplier;
        //player.velocity.y = player.velocity.y * yMultiplier;
        //player.gameObject.transform.position = destination.transform.position + new Vector3(player.velocity.x, player.velocity.y, 0);

        player.teleportableAfter = Time.time + 1;
    }
}
