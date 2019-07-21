using Platformer.Mechanics;
using UnityEngine;

public class PlayerCatch : MonoBehaviour
{
    PlayerController player;

    // Start is called before the first frame update
    void Start()
    {
        player = gameObject.GetComponentInParent<PlayerController>();
    }

    //void OnCollisionStay2D(Collision2D other)
    void OnTriggerStay2D(Collider2D other)
    {
        if (player.isCatcher == false) return;
        if (Time.time < player.stunnedUntil) return;

        var otherPlayer = other.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null) return;

        if (otherPlayer.catchableAfter < Time.time && otherPlayer.isDropping == false)
        {
            otherPlayer.MakeCatcher(player);
        }
    }
}
