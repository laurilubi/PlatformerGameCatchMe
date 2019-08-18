using JetBrains.Annotations;
using Platformer.Mechanics;
using UnityEngine;

public class PlayerDrop : MonoBehaviour
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
        if (player.isDropping == false) return;

        var otherPlayer = other.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null)
        {

            var isTouchingLevel = other.gameObject.name == "Level";
            if (isTouchingLevel == false) return;

            player.isDropping = false;

            if (Time.time < player.hrzFlippedUntil || Time.time < player.vrtFlippedUntil) return; // no penatly if flipped

            player.stunnedUntil = Time.time + 0.4f * PlayerController.dropStunPeriod;
            return;
        }

        player.isDropping = false;
        if (otherPlayer.stunnedUntil < Time.time)
        {
            otherPlayer.stunnedUntil = Time.time + 1.1f * PlayerController.dropStunPeriod;
        }
    }
}
