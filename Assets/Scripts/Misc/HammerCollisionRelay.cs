using UnityEngine;

namespace HapticsDemo
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class HammerCollisionRelay : MonoBehaviour
    {
        [Tooltip("Reference to the HammerStation that will handle the collision.")]
        public HammerStation station;

        private Rigidbody rb;
        private Vector3 lastPos;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            lastPos = rb ? rb.position : Vector3.zero;
            Debug.Log(
                $"[{nameof(HammerCollisionRelay)}] Awake: rb={(rb ? rb.name : "null")}, station={(station ? station.name : "null")}",
                this
            );
        }

        void FixedUpdate()
        {
            if (rb)
                lastPos = rb.position;
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[{nameof(HammerCollisionRelay)}] OnTriggerEnter with '{other.name}'", this);

            if (!station)
            {
                Debug.LogWarning(
                    $"[{nameof(HammerCollisionRelay)}] No HammerStation assigned.",
                    this
                );
                return;
            }

            if (!rb)
            {
                Debug.LogWarning(
                    $"[{nameof(HammerCollisionRelay)}] Missing Rigidbody on hammer object.",
                    this
                );
                return;
            }

            float velocity;
            if (rb.isKinematic)
            {
                velocity = (rb.position - lastPos).magnitude / Time.fixedDeltaTime;
                Debug.Log(
                    $"[{nameof(HammerCollisionRelay)}] Hammer is KINEMATIC. Estimated velocity={velocity:F3} m/s",
                    this
                );
            }
            else
            {
                velocity = rb.linearVelocity.magnitude;
                Debug.Log(
                    $"[{nameof(HammerCollisionRelay)}] Hammer is DYNAMIC. Rigidbody.velocity={velocity:F3} m/s",
                    this
                );
            }

            Debug.Log(
                $"[{nameof(HammerCollisionRelay)}] Relaying impact to station '{station.name}' | Velocity={velocity:F3} | Other={other.name}",
                this
            );
            station.HandleImpact(velocity, other);
        }
    }
}
