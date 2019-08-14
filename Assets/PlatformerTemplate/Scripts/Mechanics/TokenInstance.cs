using Platformer.Gameplay;
using System.Collections.Generic;
using System.Linq;
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
        public const float teleportRangeMin = 2;
        public const float teleportRangeMax = 4;

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

        void OnPlayerEnter(PlayerController player)
        {
            if (collected) return;

            sprites = collectedAnimation;
            collected = true;
            renderer.transform.position = new Vector3(-1000, -1000, 0);

            var randomAction = GetRandomAction(player.isCatcher);
            switch (randomAction)
            {
                case RandomAction.FlipNonChasers:
                    FlipNonChasers();
                    break;
                case RandomAction.FlipChaser:
                    FlipCatcher();
                    break;
                case RandomAction.StunNonChasers:
                    StunNonChasers();
                    break;
                case RandomAction.TeleportClose:
                    TeleportCloseToChaser();
                    break;
            }
        }

        void FlipNonChasers()
        {
            var nonChasers = PlayerController.GetNonChasers();
            foreach (var player in nonChasers)
            {
                player.hrzFlippedUntil = Time.time + 1.8f * flipTime;
                player.vrtFlippedUntil = Time.time + 1.8f * flipTime;
            }
        }

        void FlipCatcher()
        {
            var chaser = PlayerController.GetPlayers().FirstOrDefault(_ => _.isCatcher);
            if (chaser == null) return;

            chaser.hrzFlippedUntil = Time.time + 1.8f * flipTime;
            chaser.vrtFlippedUntil = Time.time + 1.8f * flipTime;
        }

        void StunNonChasers()
        {
            var nonChasers = PlayerController.GetNonChasers();
            foreach (var player in nonChasers)
            {
                player.stunnedUntil = Time.time + 0.7f * PlayerController.dropStunPeriod;
            }
        }

        void TeleportCloseToChaser()
        {
            var catcher = PlayerController.GetPlayers().FirstOrDefault(_ => _.isCatcher);
            if (catcher == null) return;

            var nonChasers = PlayerController.GetNonChasers();
            foreach (var player in nonChasers)
            {
                var direction = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), 0).normalized;
                var distance = Random.Range(teleportRangeMin, teleportRangeMax);

                var pos = catcher.gameObject.transform.position + direction * distance;
                player.Teleport(pos, new Vector2(1, 1));
            }
        }

        readonly Dictionary<RandomAction, int> catherRandomActionChances = new Dictionary<RandomAction, int> {
            { RandomAction.None, 20 },
            { RandomAction.FlipNonChasers, 35 },
            { RandomAction.FlipChaser, 5 },
            { RandomAction.StunNonChasers, 35 },
            //{ RandomAction.TeleportClose, 35 }
        };
        readonly Dictionary<RandomAction, int> nonCatherRandomActionChances = new Dictionary<RandomAction, int> {
            { RandomAction.None, 60 },
            { RandomAction.FlipNonChasers, 5 },
            { RandomAction.FlipChaser, 15 },
            { RandomAction.StunNonChasers, 5 },
            //{ RandomAction.TeleportClose, 15 }
        };
        RandomAction GetRandomAction(bool isCatcher)
        {
            var chances = isCatcher ? catherRandomActionChances : nonCatherRandomActionChances;
            var max = chances.Values.Sum();

            var selected = Random.Range(0, max);

            var pointer = 0;
            foreach (var chance in chances)
            {
                pointer += chance.Value;
                if (pointer >= selected) return chance.Key;
            }

            return chances.Last().Key; // fallback
        }

        public enum RandomAction
        {
            None,
            FlipNonChasers,
            FlipChaser,
            StunNonChasers,
            TeleportClose
        }
    }
}