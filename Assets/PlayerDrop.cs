using JetBrains.Annotations;
using Platformer.Mechanics;
using UnityEngine;

public class PlayerDrop : MonoBehaviour
{
    private PlayerController player;

    [UsedImplicitly]
    private void Start()
    {
        player = gameObject.GetComponentInParent<PlayerController>();
    }

    [UsedImplicitly]
    private void OnTriggerStay2D(Collider2D other)
    {
        if (player.isDropping == false) return;

        var otherPlayer = GetOtherPlayer(other);
        if (otherPlayer == null)
        {
            if (player.IsGrounded == false) return;
            if (other.gameObject.name == "Level") return;
            if (other.gameObject.name == "GameArea") return;

            player.isDropping = false;

            if (Time.time < player.hrzFlippedUntil || Time.time < player.vrtFlippedUntil) return; // no penatly if flipped

            player.stunnedUntil = Time.time + 0.4f * PlayerController.DropStunPeriod;
            return;
        }

        player.isDropping = false;
        //player.catchableAfter = Time.time + 0.1f; // extra protection against catcher-bug
        if (otherPlayer.stunnedUntil < Time.time)
        {
            otherPlayer.stunnedUntil = Time.time + 1.1f * PlayerController.DropStunPeriod;
        }

        // required for unknown reasons, otherwise catcher can stun someone without catching
        if (player.isCatcher && PlayerCatch.CanBeCaught(otherPlayer))
        {
            otherPlayer.MakeCatcher(player);
        }
    }

    private static PlayerController GetOtherPlayer(Collider2D other)
    {
        if (other.gameObject.name == "Drop") return other.gameObject.GetComponentInParent<PlayerController>();
        if (other.gameObject.name == "Catch") return other.gameObject.GetComponentInParent<PlayerController>();
        return other.gameObject.GetComponent<PlayerController>();
    }
}
