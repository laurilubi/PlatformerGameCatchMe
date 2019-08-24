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
        internal ControlManipulation controlManipulation;
        internal float controlRotatedUntil;
        internal float slipperyUntil;
        internal float teleportableAfter;
        internal bool isDropping;
        internal float droppableAfter;
        internal float catcherLastOn;


        private bool jump;
        private Vector2 move;
        private SpriteRenderer spriteRenderer;
        private PlatformerModel model;
        private SpriteRenderer catcherSymbol;

        public const float minimum = 0.01f;
        public const float DropStunPeriod = 2.5f;
        public const float DefaultJumpTakeOffSpeed = 6.2f;
        public const float CatcherJumpTakeOffSpeed = DefaultJumpTakeOffSpeed;
        public const float ScaleNormal = 0.38f;
        public const float ScaleCatcher = 0.46f;
        public const float ScaleSlipperyX = 0.28f;
        public const float ScaleStunnedY = 0.18f;

        public Bounds Bounds => collider.bounds;

        [UsedImplicitly]
        private void Awake()
        {
            audio = GetComponent<AudioSource>();
            collider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            catcherSymbol = transform.Find("CatcherSymbol").GetComponent<SpriteRenderer>();
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
                ? hrzAcc * 0.5f
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
                velocity.x *= 3;
                velocity.y = -5f * jumpTakeOffSpeed * model.jumpModifier;
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

            var controlRotation = GetControlManipulation();
            var axisName = controlRotation == ControlManipulation.Normal || controlRotation == ControlManipulation.Flipped
                ? $"{playerId}-Horizontal"
                : $"{playerId}-Vertical";

            var x = Input.GetAxis(axisName);
            if (x < -minimum) x = -1;
            else if (x > minimum) x = 1;
            else x = 0;

            if (controlRotation == ControlManipulation.Flipped || controlRotation == ControlManipulation.RotateRight)
                x *= -1;

            return x;
        }

        private float GetYAxis()
        {
            if (Time.time < stunnedUntil)
            {
                return 0;
            }

            var controlRotation = GetControlManipulation();
            var axisName = controlRotation == ControlManipulation.Normal || controlRotation == ControlManipulation.Flipped
                ? $"{playerId}-Vertical"
                : $"{playerId}-Horizontal";

            var y = Input.GetAxis(axisName);

            if (controlRotation == ControlManipulation.Flipped || controlRotation == ControlManipulation.RotateRight)
                y *= -1;

            return y;
        }

        private float Cap(float value, float minValue = -1, float maxValue = 1)
        {
            if (value < minValue) return minValue;
            if (value > maxValue) return maxValue;
            return value;
        }

        private bool CanJump()
        {
            if (Time.time < jumpableAfter) return false;
            if (isDropping) return false;

            if (isCatcher && jumpStepCount < GameController.Instance.model.catcherMaxJumps) return true;
            if (IsGrounded) return true;

            return false;
        }

        private bool CanDrop()
        {
            return Time.time >= droppableAfter;
        }

        private void UpdateJumpState()
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
                    jumpableAfter = Time.time + 0.3f;
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
            {
                spriteRenderer.flipX = false;
                catcherSymbol.transform.localPosition = new Vector3(-0.163f, catcherSymbol.transform.localPosition.y, catcherSymbol.transform.localPosition.z);
                catcherSymbol.flipX = false;
            }
            else if (move.x < -minimum)
            {
                spriteRenderer.flipX = true;
                catcherSymbol.transform.localPosition = new Vector3(0.163f, catcherSymbol.transform.localPosition.y, catcherSymbol.transform.localPosition.z);
                catcherSymbol.flipX = true;
            }

            spriteRenderer.flipY = GetControlManipulation() == ControlManipulation.Flipped;
            var rotZ = GetControlManipulation() == ControlManipulation.RotateLeft
                ? -45
                : GetControlManipulation() == ControlManipulation.RotateRight
                    ? 45
                    : 0;
            spriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, rotZ);

            var scaleX = Time.time < slipperyUntil
                ? ScaleSlipperyX
                : isCatcher
                    ? ScaleCatcher
                    : ScaleNormal;
            var scaleY = Time.time < stunnedUntil
                ? ScaleStunnedY
                : isCatcher
                    ? ScaleCatcher
                    : ScaleNormal;
            spriteRenderer.transform.localScale = new Vector2(scaleX, scaleY);

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public void MakeCatcher(PlayerController previousCatcher)
        {
            isCatcher = true;
            maxSpeed = defaultMaxSpeed + 0.5f;
            jumpTakeOffSpeed = CatcherJumpTakeOffSpeed;
            if (spriteRenderer?.transform != null) spriteRenderer.transform.localScale = new Vector2(ScaleCatcher, ScaleCatcher);
            if (catcherSymbol != null) catcherSymbol.enabled = true;

            GameController.Instance.CatcherSince = Time.time;

            previousCatcher?.UnmakeCatcher(true);
        }

        public void UnmakeCatcher(bool teleport)
        {
            isCatcher = false;
            maxSpeed = defaultMaxSpeed;
            jumpTakeOffSpeed = DefaultJumpTakeOffSpeed;
            catchableAfter = Time.time + 1f;

            transform.localScale = new Vector2(ScaleNormal, ScaleNormal);
            if (catcherSymbol != null) catcherSymbol.enabled = false;

            if (teleport)
                TeleportRandom();
        }

        public override void Teleport(Vector3 position, Vector2? multiplier = null)
        {
            base.Teleport(position, multiplier);
            isDropping = false;
            jump = false;
        }

        public ControlManipulation GetControlManipulation()
        {
            return Time.time < controlRotatedUntil
                ? controlManipulation
                : ControlManipulation.Normal;
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

        public enum ControlManipulation
        {
            Normal = 0,
            Flipped = 1,
            RotateLeft = 2,
            RotateRight = 3
        }
    }
}