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

        var otherPlayer = GetOtherPlayer(other);
        if (otherPlayer == null) return;

        if (CanBeCaught(otherPlayer))
        {
            otherPlayer.MakeCatcher(player);
        }
    }

    private PlayerController GetOtherPlayer(Collider2D other)
    {
        if (other.gameObject.name == "Drop") return other.gameObject.GetComponentInParent<PlayerController>();
        if (other.gameObject.name == "Catch") return other.gameObject.GetComponentInParent<PlayerController>();
        return other.gameObject.GetComponent<PlayerController>();
    }

    public static bool CanBeCaught(PlayerController otherPlayer, bool ignoreIsDropping = false)
    {
        if (Time.time <= otherPlayer.catchableAfter) return false;
        if (ignoreIsDropping == false && otherPlayer.isDropping) return false;
        return true;
    }
}
