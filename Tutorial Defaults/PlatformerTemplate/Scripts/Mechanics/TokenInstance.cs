using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This class contains the data required for implementing token collection mechanics.
    /// It does not perform animation of the token, this is handled in a batch by the 
    /// TokenController in the scene.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TokenInstance : MonoBehaviour
    {
        public AudioClip tokenCollectAudio;
        [Tooltip("If true, animation will start at a random position in the sequence.")]
        public bool randomAnimationStartTime = false;
        [Tooltip("List of frames that make up the animation.")]
        public Sprite[] idleAnimation, collectedAnimation;
        public float respawnInterval;

        internal Sprite[] sprites = new Sprite[0];

        new internal SpriteRenderer renderer;

        //unique index which is assigned by the TokenController in a scene.
        internal int tokenIndex = -1;
        internal TokenController controller;
        //active frame in animation, updated by the controller.
        internal int frame;
        internal bool collected;
        internal float respawnAt;

        public const int flipTime = 5;

        void Awake()
        {
            renderer = GetComponent<SpriteRenderer>();
            if (randomAnimationStartTime)
                frame = Random.Range(0, sprites.Length);
            sprites = idleAnimation;
        }

        void Update()
        {
            if (respawnAt <= Time.time)
            {
                var gameArea = GameObject.Find("GameArea");
                var gameAreaCollider = gameArea.GetComponent<Collider2D>();
                var bounds = gameAreaCollider.bounds;

                var x = Random.Range(bounds.min.x, bounds.max.x);
                var y = Random.Range(bounds.min.y, bounds.max.y);
                var position = new Vector3(x, y, 0);

                renderer.transform.position = position;

                collected = false;
                sprites = idleAnimation;
                respawnAt = Time.time + respawnInterval;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            //only exectue OnPlayerEnter if the player collides with this token.
            var player = other.gameObject.GetComponent<PlayerController>();
            if (player != null) OnPlayerEnter(player);
        }

        void OnPlayerEnter (PlayerController player) {
            if (collected) return;

            //frame = 0;
            sprites = collectedAnimation;
            collected = true;
            renderer.transform.position = new Vector3 (-1000, -1000, 0);

            if (player.isCatcher) {
                var mousePlayers = PlayerController.GetNonChasers ();
                foreach (var mousePlayer in mousePlayers) {
                    mousePlayer.hrzFlippedUntil = Time.time + flipTime;
                    mousePlayer.vrtFlippedUntil = Time.time + flipTime;
                }
            }

            //send an event into the gameplay system to perform some behaviour.
            //var ev = Schedule<PlayerTokenCollision>();
            //ev.token = this;
            //ev.player = player;
        }

        var catherRandomActionChances = new Dictionary<RandomAction, int> { 
            { RandomAction.None, 20 },
            { RandomAction.FlipNonChasers, 40 },
            { RandomAction.FlipChaser, 5 },
            { RandomAction.StunNonChasers, 40 }
        };
        var nonCatherRandomActionChances = new Dictionary<RandomAction, int> {
             { RandomAction.None, 80 },
            { RandomAction.FlipNonChasers, 5 },
            { RandomAction.FlipChaser, 5 },
            { RandomAction.StunNonChasers, 5 }
        };
        void int GetRandomAction (bool isCatcher) {
            var chances = isCatcher?catherRandomActionChances:nonCatherRandomActionChances;
            var max = chances.Sum(_=>_.Value);

            var selected = Random.Range(0, max);

            var pointer = 0;
            foreach (var chance in chances){
                pointer+=chance.Value;

            }

        }

        public enum RandomAction {
            None,
            FlipNonChasers,
            FlipChaser,
            StunNonChasers
        }
    }
}