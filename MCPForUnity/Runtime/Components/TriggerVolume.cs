using System;
using UnityEngine;
using UnityEngine.Events;

namespace MCPForUnity.Runtime.Components
{
    /// <summary>
    /// A trigger volume that fires events when entities enter, stay in, or exit the volume.
    /// Used for level design triggers like cutscenes, doors, traps, etc.
    /// </summary>
    [AddComponentMenu("MCP/Level Design/Trigger Volume")]
    [RequireComponent(typeof(Collider))]
    public class TriggerVolume : MonoBehaviour
    {
        /// <summary>
        /// Shape of the trigger volume.
        /// </summary>
        public enum VolumeShape
        {
            Box,
            Sphere,
            Capsule
        }

        /// <summary>
        /// When the trigger should activate.
        /// </summary>
        public enum TriggerMode
        {
            OnEnter,
            OnExit,
            OnStay,
            OnEnterAndExit
        }

        [Header("Trigger Configuration")]
        [Tooltip("Name/identifier for this trigger event.")]
        public string EventName = "OnTrigger";

        [Tooltip("When the trigger should fire.")]
        public TriggerMode Mode = TriggerMode.OnEnter;

        [Tooltip("Tags that can activate this trigger (empty = any tag).")]
        public string[] ActivatorTags = new string[] { "Player" };

        [Tooltip("Layer mask for objects that can activate this trigger.")]
        public LayerMask ActivatorLayers = ~0;

        [Header("Trigger Settings")]
        [Tooltip("Maximum number of times this trigger can fire (-1 = unlimited).")]
        public int MaxActivations = -1;

        [Tooltip("Cooldown between activations (in seconds).")]
        [Min(0f)]
        public float Cooldown = 0f;

        [Tooltip("Whether this trigger is currently enabled.")]
        public bool IsEnabled = true;

        [Tooltip("Whether to destroy this object after max activations.")]
        public bool DestroyOnComplete = false;

        [Header("Events")]
        [Tooltip("Event fired when trigger is activated.")]
        public UnityEvent<GameObject> OnTriggerActivated;

        [Tooltip("Event fired when entity enters the volume.")]
        public UnityEvent<GameObject> OnEntityEntered;

        [Tooltip("Event fired when entity exits the volume.")]
        public UnityEvent<GameObject> OnEntityExited;

        [Header("Optional Settings")]
        [Tooltip("Unique identifier for this trigger.")]
        public string TriggerId;

        [Tooltip("Group this trigger belongs to.")]
        public string TriggerGroup;

        [Tooltip("Custom metadata as JSON string.")]
        [TextArea(2, 4)]
        public string Metadata;

        private int _activationCount = 0;
        private float _lastActivationTime = float.NegativeInfinity;
        private Collider _collider;

        /// <summary>
        /// Gets the current activation count.
        /// </summary>
        public int ActivationCount => _activationCount;

        /// <summary>
        /// Checks if this trigger can be activated.
        /// </summary>
        public bool CanActivate => IsEnabled &&
            (MaxActivations < 0 || _activationCount < MaxActivations) &&
            (Time.time - _lastActivationTime >= Cooldown);

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanActivate || !IsValidActivator(other.gameObject))
                return;

            OnEntityEntered?.Invoke(other.gameObject);

            if (Mode == TriggerMode.OnEnter || Mode == TriggerMode.OnEnterAndExit)
            {
                Activate(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsValidActivator(other.gameObject))
                return;

            OnEntityExited?.Invoke(other.gameObject);

            if (CanActivate && (Mode == TriggerMode.OnExit || Mode == TriggerMode.OnEnterAndExit))
            {
                Activate(other.gameObject);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!CanActivate || !IsValidActivator(other.gameObject))
                return;

            if (Mode == TriggerMode.OnStay)
            {
                Activate(other.gameObject);
            }
        }

        private bool IsValidActivator(GameObject obj)
        {
            // Check layer
            if ((ActivatorLayers.value & (1 << obj.layer)) == 0)
                return false;

            // Check tag
            if (ActivatorTags != null && ActivatorTags.Length > 0)
            {
                bool tagMatch = false;
                foreach (var tag in ActivatorTags)
                {
                    if (string.IsNullOrEmpty(tag) || obj.CompareTag(tag))
                    {
                        tagMatch = true;
                        break;
                    }
                }
                if (!tagMatch)
                    return false;
            }

            return true;
        }

        private void Activate(GameObject activator)
        {
            _activationCount++;
            _lastActivationTime = Time.time;

            OnTriggerActivated?.Invoke(activator);

            if (MaxActivations > 0 && _activationCount >= MaxActivations)
            {
                IsEnabled = false;
                if (DestroyOnComplete)
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Resets the trigger state.
        /// </summary>
        public void ResetTrigger()
        {
            _activationCount = 0;
            _lastActivationTime = float.NegativeInfinity;
            IsEnabled = true;
        }

        /// <summary>
        /// Manually activates the trigger.
        /// </summary>
        public void ManualActivate(GameObject activator = null)
        {
            if (CanActivate)
            {
                Activate(activator);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = IsEnabled ? new Color(0f, 1f, 1f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);

            var collider = GetComponent<Collider>();
            if (collider is BoxCollider box)
            {
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
            else if (collider is CapsuleCollider capsule)
            {
                // Simple approximation
                Gizmos.DrawWireSphere(transform.position, capsule.radius);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;

            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Trigger: {EventName}\nMode: {Mode}\nActivations: {_activationCount}/{(MaxActivations < 0 ? "∞" : MaxActivations.ToString())}");
        }
#endif
    }
}
