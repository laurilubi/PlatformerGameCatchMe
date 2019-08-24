using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

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

        internal int tokenIndex = -1;
        internal TokenController controller;
        internal int frame;  // active frame in animation, updated by the controller.
        internal bool collected;
        internal float respawnAt;
        internal RandomAction action = RandomAction.None;

        public const float FlipTime = 7;

        [UsedImplicitly]
        private void Awake()
        {
            renderer = GetComponent<SpriteRenderer>();
            if (randomAnimationStartTime)
                frame = Random.Range(0, sprites.Length);
            sprites = idleAnimation;

            action = GetRandomAction();
            AutoSetColor();
        }

        [UsedImplicitly]
        private void Update()
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

                action = GetRandomAction();
                AutoSetColor();
            }
        }

        [UsedImplicitly]
        private void OnTriggerEnter2D(Collider2D other)
        {
            //only exectue OnPlayerEnter if the player collides with this token.
            var player = other.gameObject.GetComponent<PlayerController>();
            if (player != null) OnPlayerEnter(player);
        }

        private void OnPlayerEnter(PlayerController player)
        {
            if (collected) return;

            sprites = collectedAnimation;
            collected = true;
            renderer.transform.position = new Vector3(-1000, -1000, 0);

            var targets = GetTargets(player);
            if (targets.Any() == false) return;

            switch (action)
            {
                case RandomAction.FlipControls:
                    FlipControls(targets);
                    break;
                case RandomAction.Stun:
                    Stun(targets);
                    break;
                case RandomAction.Slippery:
                    MakeSlippery(targets);
                    break;
            }
        }

        private List<PlayerController> GetTargets(PlayerController player)
        {
            if (player.isCatcher) return PlayerController.GetNonChasers().ToList();

            var nonChasersChance = 10 + GameController.Instance.GetCatcherTime();
            var nothingChance = 10 + 0.3f * GameController.Instance.GetCatcherTime();
            var catcherChance = 20;
            var dice = Random.Range(0, nonChasersChance + nothingChance + catcherChance);

            if (dice < nonChasersChance) return PlayerController.GetNonChasers().ToList();
            if (dice < nonChasersChance + nothingChance) return new List<PlayerController>();
            return PlayerController.GetPlayers().Where(_ => _.isCatcher).ToList();
        }

        private bool IsTargetCatcher(List<PlayerController> targets)
        {
            return targets.FirstOrDefault()?.isCatcher ?? false;
        }

        private void FlipControls(List<PlayerController> targets)
        {
            var duration = FlipTime;
            if (IsTargetCatcher(targets) == false)
                duration *= Math.Min(Math.Max(GameController.Instance.GetCatcherTime() / 35f, 1f), 1.5f);

            foreach (var target in targets)
            {
                var overtime = GetOvertime(target);
                target.controlRotatedUntil = Time.time + duration + 0.1f * overtime;

                if (Math.Abs(overtime) < 0.01f)
                {
                    target.controlManipulation = PlayerController.ControlManipulation.Flipped;
                    continue;
                }

                var dice = Random.Range(0f, 60f);
                if (dice < overtime)
                {
                    target.controlManipulation = PlayerController.ControlManipulation.Flipped;
                    continue;
                }

                target.controlManipulation = dice < 30f
                    ? PlayerController.ControlManipulation.RotateLeft
                    : PlayerController.ControlManipulation.RotateRight;
            }
        }

        private float GetOvertime(PlayerController target)
        {
            var overtime = Time.time - target.catcherLastOn - 20f;
            return Math.Max(0, Math.Min(50f, overtime));
        }

        private void Stun(List<PlayerController> targets)
        {
            foreach (var target in targets)
            {
                target.stunnedUntil = Time.time + 0.7f * PlayerController.DropStunPeriod;
            }
        }

        private void MakeSlippery(List<PlayerController> targets)
        {
            var duration = FlipTime;
            if (IsTargetCatcher(targets) == false)
                duration *= Math.Min(Math.Max(GameController.Instance.GetCatcherTime() / 35f, 1f), 2.5f);

            foreach (var target in targets)
            {
                var overtime = GetOvertime(target);
                target.slipperyUntil = Time.time + duration + 0.1f * overtime;
            }
        }

        private void AutoSetColor()
        {
            switch (action)
            {
                case RandomAction.FlipControls:
                    renderer.color = new Color(255f / 255f, 255f / 255f, 255f / 255f);
                    break;
                case RandomAction.Stun:
                    renderer.color = new Color(138f / 255f, 255f / 255f, 149f / 255f);
                    break;
                case RandomAction.Slippery:
                    renderer.color = new Color(255f / 255f, 138f / 255f, 137f / 255f);
                    break;
                default:
                    renderer.color = new Color(255f / 255f, 255f / 255f, 255f / 255f);
                    break;
            }
        }

        readonly Dictionary<RandomAction, int> randomActionChances = new Dictionary<RandomAction, int> {
            { RandomAction.FlipControls, 30 },
            { RandomAction.Stun, 30 },
            { RandomAction.Slippery, 30 }
        };
        RandomAction GetRandomAction()
        {
            var max = randomActionChances.Values.Sum();

            var selected = Random.Range(0, max);

            var pointer = 0;
            foreach (var chance in randomActionChances)
            {
                pointer += chance.Value;
                if (pointer >= selected) return chance.Key;
            }

            return randomActionChances.Last().Key; // fallback
        }

        public enum RandomAction
        {
            None,
            FlipControls,
            Stun,
            Slippery
        }
    }
}