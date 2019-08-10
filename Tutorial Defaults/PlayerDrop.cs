using Platformer.Mechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDrop : MonoBehaviour
{
    PlayerController player;

    // Start is called before the first frame update
    void Start()
    {
        player = gameObject.GetComponentInParent<PlayerController>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (player.isDropping == false) return;

        var otherPlayer = other.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null)
        {
            var isTouchingLevel = other.gameObject.name == "Level";
            if (isTouchingLevel == false) return;

            player.isDropping = false;
            player.stunnedUntil = Time.time + 0.75f * PlayerController.dropStunPeriod;
            return;
        }

        player.isDropping = false;
        if (otherPlayer.stunnedUntil < Time.time)
        {
            otherPlayer.stunnedUntil = Time.time + PlayerController.dropStunPeriod;
        }
    }
}
