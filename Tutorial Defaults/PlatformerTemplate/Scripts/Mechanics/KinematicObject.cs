using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Implements game physics for some in game entity.
    /// </summary>
    public abstract class KinematicObject : MonoBehaviour
    {
        /// <summary>
        /// The minimum normal (dot product) considered suitable for the entity sit on.
        /// </summary>
        public float minGroundNormalY = .65f;

        /// <summary>
        /// A custom gravity coefficient applied to this entity.
        /// </summary>
        public float gravityModifier = 1f;

        /// <summary>
        /// The current velocity of the entity.
        /// </summary>
        public Vector2 velocity;

        /// <summary>
        /// Is the entity currently sitting on a surface?
        /// </summary>
        /// <value></value>
        public bool IsGrounded { get; private set; }

        protected Vector2 targetVelocity;
        protected Vector2 groundNormal;
        protected Rigidbody2D body;
        protected ContactFilter2D contactFilter;
        protected Collider2D[] colliders;


        protected const float minMoveDistance = 0.001f;
        protected const float shellRadius = 0.01f;


        /// <summary>
        /// Bounce the object's vertical velocity.
        /// </summary>
        /// <param name="value"></param>
        public void Bounce(float value)
        {
            velocity.y = value;
        }

        /// <summary>
        /// Bounce the objects velocity in a direction.
        /// </summary>
        /// <param name="dir"></param>
        public void Bounce(Vector2 dir)
        {
            velocity.y = dir.y;
            velocity.x = dir.x;
        }

        /// <summary>
        /// Teleport to some position.
        /// </summary>
        /// <param name="position"></param>
        public void Teleport(Vector3 position, Vector2? multiplier = null)
        {
            multiplier = multiplier ?? new Vector2(0, 0);

            transform.localPosition = position;
            //body.position = position;

            velocity *= multiplier.Value;
            if (body != null) body.velocity *= multiplier.Value;
        }

        public void TeleportRandom(Bounds? bounds = null, Vector2? multiplier = null)
        {
            if (bounds == null)
            {
                var gameArea = GameObject.Find("GameArea");
                var gameAreaCollider = gameArea.GetComponent<Collider2D>();
                bounds = gameAreaCollider.bounds;
            }

            var x = UnityEngine.Random.Range(bounds.Value.min.x, bounds.Value.max.x);
            var y = UnityEngine.Random.Range(bounds.Value.min.y, bounds.Value.max.y);
            var position = new Vector3(x, y, 0);

            Teleport(position, multiplier);
        }

        protected virtual void OnEnable()
        {
            body = GetComponent<Rigidbody2D>();
            body.isKinematic = true;
        }

        protected virtual void OnDisable()
        {
            body.isKinematic = false;
        }

        protected virtual void Start()
        {
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            contactFilter.useLayerMask = true;

            List<Collider2D> collidersTmp = new List<Collider2D>();
            var colliderCount = body.GetAttachedColliders(collidersTmp);
            colliders = collidersTmp.Take(colliderCount).Where(_ => _.isTrigger == false).ToArray();
        }

        protected virtual void Update()
        {
            targetVelocity = Vector2.zero;
            ComputeVelocity();
        }

        protected virtual void ComputeVelocity()
        {

        }

        protected virtual void FixedUpdate()
        {
            //if already falling, fall faster than the jump speed, otherwise use normal gravity.
            if (velocity.y < 0)
                velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
            else
                velocity += Physics2D.gravity * Time.deltaTime;

            velocity.x = targetVelocity.x;

            IsGrounded = false;

            var deltaPosition = velocity * Time.deltaTime;

            var moveAlongGround = new Vector2(groundNormal.y, -groundNormal.x);

            var move = moveAlongGround * deltaPosition.x;

            PerformMovement(move, false);

            move = Vector2.up * deltaPosition.y;

            PerformMovement(move, true);
        }

        void PerformMovement(Vector2 move, bool yMovement)
        {
            var distance = move.magnitude;

            if (distance > minMoveDistance)
            {
                // check if we hit anything in current direction of travel
                var hits = GetHits(move, distance + shellRadius);

                foreach (var hit in hits)
                {
                    var currentNormal = hit.normal;

                    // is this surface flat enough to land on?
                    if (currentNormal.y > minGroundNormalY)
                    {
                        IsGrounded = true;
                        // if moving up, change the groundNormal to new surface normal.
                        if (yMovement)
                        {
                            groundNormal = currentNormal;
                            currentNormal.x = 0;
                        }
                    }
                    if (IsGrounded)
                    {
                        // how much of our velocity aligns with surface normal?
                        var projection = Vector2.Dot(velocity, currentNormal);
                        if (projection < 0)
                        {
                            // slower velocity if moving against the normal (up a hill).
                            velocity = velocity - projection * currentNormal;
                        }
                    }
                    else
                    {
                        // We are airborne, but hit something, so cancel vertical up and horizontal velocity.
                        velocity.x = 0;
                        // velocity.y = Mathf.Min(velocity.y, 0);
                    }

                    // remove shellDistance from actual move distance.
                    var modifiedDistance = hit.distance - shellRadius;
                    distance = modifiedDistance < distance ? modifiedDistance : distance;
                }
            }

            body.position = body.position + move.normalized * distance;
        }

        List<RaycastHit2D> GetHits(Vector2 direction, float distance)
        {
            var hits = new List<RaycastHit2D>();
            foreach (var collider in colliders)
            {
                var results = new List<RaycastHit2D>();
                var count = collider.Cast(direction, contactFilter, results, distance);
                hits.AddRange(results.Take(count));
            }
            return hits;
        }
    }
}