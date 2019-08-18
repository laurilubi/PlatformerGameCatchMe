using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;
        public string playerId = "none";
        public float defaultMaxSpeed = 3;
        public float defaultHrzAcc = 4;


        internal new Collider2D collider;
        internal new AudioSource audio;
        internal Animator animator;
        internal JumpState jumpState = JumpState.Grounded;
        internal bool stopJump;
        internal float stunnedUntil;
        internal int jumpStepCount;
        internal bool isCatcher;
        internal float catchableAfter;
        internal float jumpableAfter;
        internal float hrzAcc;
        internal float maxSpeed;
        internal float jumpTakeOffSpeed;
        internal float hrzFlippedUntil;
        internal float vrtFlippedUntil;
        internal float slipperyUntil;
        internal float teleportableAfter;
        internal bool isDropping;
        internal float droppableAfter;


        private bool jump;
        private Vector2 move;
        private SpriteRenderer spriteRenderer;
        private PlatformerModel model;

        public const float minimum = 0.01f;
        public const float dropStunPeriod = 2.5f;
        public const float DefaultJumpTakeOffSpeed = 6.2f;
        public const float CatcherJumpTakeOffSpeed = DefaultJumpTakeOffSpeed;

        public Bounds Bounds => collider.bounds;

        [UsedImplicitly]
        void Awake()
        {
            audio = GetComponent<AudioSource>();
            collider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            model = GetModel<PlatformerModel>();

            hrzAcc = defaultHrzAcc;
            maxSpeed = defaultMaxSpeed;
            jumpTakeOffSpeed = DefaultJumpTakeOffSpeed;
        }

        protected override void Update()
        {
            var xAxis = GetXAxis();
            var relHrzAcc = Time.time < slipperyUntil
                ? hrzAcc * 0.6f
                : hrzAcc;
            if (Mathf.Abs(xAxis) < minimum)
            {
                // is stopping
                if (move.x > minimum)
                {
                    // still going right
                    move.x = Math.Max(0, move.x - relHrzAcc * 3 * Time.deltaTime);
                }
                else if (move.x < -minimum)
                {
                    // still going left
                    move.x = Math.Min(0, move.x + relHrzAcc * 3 * Time.deltaTime);
                }
                else
                {
                    move.x = 0;
                }
            }
            else
            {
                // is moving
                move.x += xAxis * relHrzAcc * Time.deltaTime;
                move.x = Cap(move.x);
            }

            var yAxis = GetYAxis();
            if (yAxis > 0.2f && CanJump())
            {
                jumpState = JumpState.PrepareToJump;
            }
            else if (yAxis < -0.2f && CanDrop())
            {
                isDropping = true;
                droppableAfter = Time.time + 2;
                velocity.x *= 2;
                velocity.y = -2.5f * jumpTakeOffSpeed * model.jumpModifier;
            }

            UpdateJumpState();

            base.Update();
        }

        private float GetXAxis()
        {
            if (Time.time < stunnedUntil)
            {
                return 0;
            }

            var xAxis = Input.GetAxis($"{playerId}-Horizontal");
            if (xAxis < -minimum) xAxis = -1;
            else if (xAxis > minimum) xAxis = 1;
            else xAxis = 0;

            if (Time.time < hrzFlippedUntil) xAxis *= -1;
            return xAxis;
        }

        private float GetYAxis()
        {
            if (Time.time < stunnedUntil)
            {
                return 0;
            }

            var yAxis = Input.GetAxis($"{playerId}-Vertical");

            if (Time.time < vrtFlippedUntil)
            {
                yAxis *= -1;
            }
            return yAxis;
        }

        private float Cap(float value, float minValue = -1, float maxValue = 1)
        {
            if (value < minValue) return minValue;
            if (value > maxValue) return maxValue;
            return value;
        }

        bool CanJump()
        {
            if (Time.time < jumpableAfter) return false;
            if (isDropping) return false;

            if (isCatcher && jumpStepCount < GameController.Instance.model.catcherMaxJumps) return true;
            if (IsGrounded) return true;

            return false;
        }

        bool CanDrop()
        {
            return Time.time >= droppableAfter;
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    if (IsGrounded) jumpStepCount = 0;
                    jumpStepCount++;
                    jumpableAfter = Time.time + 0.25f;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    jumpStepCount = 0;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > minimum)
                spriteRenderer.flipX = false;
            else if (move.x < -minimum)
                spriteRenderer.flipX = true;

            spriteRenderer.flipY = Time.time < vrtFlippedUntil;
            var scaleY = Time.time < stunnedUntil
                ? 0.2f
                : isCatcher
                    ? 0.55f
                    : 0.4f;
            spriteRenderer.transform.localScale = new Vector2(spriteRenderer.transform.localScale.x, scaleY);

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public void MakeCatcher(PlayerController previousCatcher)
        {
            isCatcher = true;
            maxSpeed = defaultMaxSpeed + 0.5f;
            jumpTakeOffSpeed = CatcherJumpTakeOffSpeed;
            if (spriteRenderer?.transform != null) spriteRenderer.transform.localScale = new Vector2(0.55f, 0.55f);

            GameController.Instance.ChaserSince = Time.time;

            previousCatcher?.UnmakeCatcher(true);
        }

        public void UnmakeCatcher(bool teleport)
        {
            isCatcher = false;
            catchableAfter = Time.time + 0.1f;

            transform.localScale = new Vector2(0.4f, 0.4f);
            maxSpeed = defaultMaxSpeed;
            jumpTakeOffSpeed = DefaultJumpTakeOffSpeed;

            if (teleport)
                TeleportRandom();
        }

        public static PlayerController[] GetPlayers()
        {
            return GameController.Instance.model.activePlayers.ToArray();
        }

        public static PlayerController[] GetNonChasers()
        {
            return GetPlayers().Where(_ => _.isCatcher == false).ToArray();
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}