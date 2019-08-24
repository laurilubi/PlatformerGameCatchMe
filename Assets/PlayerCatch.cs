using JetBrains.Annotations;
using Platformer.Mechanics;
using UnityEngine;

public class PlayerCatch : MonoBehaviour
{
    PlayerController player;

    [UsedImplicitly]
    private void Start()
    {
        player = gameObject.GetComponentInParent<PlayerController>();
    }

    [UsedImplicitly]
    private void OnTriggerStay2D(Collider2D other)
    {
        if (player.isCatcher == false) return;
        if (Time.time < player.stunnedUntil) return;

        var otherPlayer = other.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null) return;

        if (CanBeCaught(otherPlayer))
        {
            otherPlayer.MakeCatcher(player);
        }
    }

    public static bool CanBeCaught(PlayerController otherPlayer)
    {
        return otherPlayer.catchableAfter < Time.time && otherPlayer.isDropping == false;
    }
}
