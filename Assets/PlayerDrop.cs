﻿using JetBrains.Annotations;
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
            if (other.gameObject.name != "Level") return;

            player.isDropping = false;

            if (player.GetControlManipulation() != PlayerController.ControlManipulation.Normal) return; // no penatly if controls messed up

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
        if (player.isCatcher && PlayerCatch.CanBeCaught(otherPlayer, ignoreIsDropping: true))
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
}
