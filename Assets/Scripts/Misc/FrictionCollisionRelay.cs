using UnityEngine;

namespace HapticsDemo
{
    [RequireComponent(typeof(Collider))]
    public class FrictionCollisionRelay : MonoBehaviour
    {
        [Tooltip("Reference to the FrictionStation that will handle the trigger.")]
        public FrictionStation station;

        private Collider _col;

        private void Awake()
        {
            _col = GetComponent<Collider>();
            if (_col != null)
                _col.isTrigger = true;
        }

        private void OnValidate()
        {
            var c = GetComponent<Collider>();
            if (c != null)
                c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (station != null)
                station.HandleTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (station != null)
                station.HandleTriggerExit(other);
        }
    }
}
