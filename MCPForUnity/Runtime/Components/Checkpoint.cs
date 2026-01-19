using System;
using UnityEngine;
using UnityEngine.Events;

namespace MCPForUnity.Runtime.Components
{
    /// <summary>
    /// Marks a location as a checkpoint for save/respawn functionality.
    /// Players respawn at the last activated checkpoint.
    /// </summary>
    [AddComponentMenu("MCP/Level Design/Checkpoint")]
    public class Checkpoint : MonoBehaviour
    {
        /// <summary>
        /// Type of checkpoint.
        /// </summary>
        public enum CheckpointType
        {
            Auto,           // Activates automatically when player enters
            Manual,         // Requires manual activation (e.g., interact button)
            OneTime,        // Can only be activated once
            Persistent      // Persists across game sessions
        }

        [Header("Checkpoint Configuration")]
        [Tooltip("Type of checkpoint activation.")]
        public CheckpointType Type = CheckpointType.Auto;

        [Tooltip("Order in the level sequence (for linear progression).")]
        public int SequenceOrder = 0;

        [Tooltip("Whether this checkpoint is currently active/unlocked.")]
        public bool IsUnlocked = true;

        [Tooltip("Whether this is the currently active respawn point.")]
        [SerializeField]
        private bool _isActive = false;

        [Header("Respawn Settings")]
        [Tooltip("Custom respawn position offset from checkpoint position.")]
        public Vector3 RespawnOffset = Vector3.zero;

        [Tooltip("Custom respawn rotation (uses checkpoint rotation if not set).")]
        public bool UseCustomRotation = false;
        public Vector3 CustomRespawnRotation = Vector3.zero;

        [Tooltip("Delay before respawning (in seconds).")]
        [Min(0f)]
        public float RespawnDelay = 0f;

        [Header("Events")]
        [Tooltip("Event fired when this checkpoint is activated.")]
        public UnityEvent<GameObject> OnCheckpointActivated;

        [Tooltip("Event fired when player respawns at this checkpoint.")]
        public UnityEvent<GameObject> OnPlayerRespawned;

        [Header("Detection")]
        [Tooltip("Tags that can activate this checkpoint.")]
        public string[] ActivatorTags = new string[] { "Player" };

        [Tooltip("Radius for auto-activation (0 = use collider).")]
        [Min(0f)]
        public float ActivationRadius = 0f;

        [Header("Optional Settings")]
        [Tooltip("Unique identifier for this checkpoint.")]
        public string CheckpointId;

        [Tooltip("Display name for UI.")]
        public string DisplayName;

        [Tooltip("Custom metadata as JSON string.")]
        [TextArea(2, 4)]
        public string Metadata;

        /// <summary>
        /// Whether this checkpoint is currently the active respawn point.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            private set => _isActive = value;
        }

        /// <summary>
        /// Static reference to the currently active checkpoint.
        /// </summary>
        public static Checkpoint CurrentCheckpoint { get; private set; }

        /// <summary>
        /// Gets the respawn position for this checkpoint.
        /// </summary>
        public Vector3 GetRespawnPosition()
        {
            return transform.position + transform.TransformDirection(RespawnOffset);
        }

        /// <summary>
        /// Gets the respawn rotation for this checkpoint.
        /// </summary>
        public Quaternion GetRespawnRotation()
        {
            if (UseCustomRotation)
            {
                return Quaternion.Euler(CustomRespawnRotation);
            }
            return transform.rotation;
        }

        /// <summary>
        /// Activates this checkpoint as the current respawn point.
        /// </summary>
        public void Activate(GameObject activator = null)
        {
            if (!IsUnlocked)
                return;

            // Deactivate previous checkpoint
            if (CurrentCheckpoint != null && CurrentCheckpoint != this)
            {
                CurrentCheckpoint.IsActive = false;
            }

            IsActive = true;
            CurrentCheckpoint = this;

            OnCheckpointActivated?.Invoke(activator);

            // Lock one-time checkpoints
            if (Type == CheckpointType.OneTime)
            {
                IsUnlocked = false;
            }
        }

        /// <summary>
        /// Respawns an entity at this checkpoint.
        /// </summary>
        public void Respawn(GameObject entity)
        {
            if (entity == null)
                return;

            entity.transform.position = GetRespawnPosition();
            entity.transform.rotation = GetRespawnRotation();

            OnPlayerRespawned?.Invoke(entity);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Type != CheckpointType.Auto && Type != CheckpointType.OneTime)
                return;

            if (!IsUnlocked || IsActive)
                return;

            if (IsValidActivator(other.gameObject))
            {
                Activate(other.gameObject);
            }
        }

        private bool IsValidActivator(GameObject obj)
        {
            if (ActivatorTags == null || ActivatorTags.Length == 0)
                return true;

            foreach (var tag in ActivatorTags)
            {
                if (string.IsNullOrEmpty(tag) || obj.CompareTag(tag))
                    return true;
            }

            return false;
        }

        private void OnEnable()
        {
            // If this checkpoint was previously active, restore it
            if (_isActive && CurrentCheckpoint == null)
            {
                CurrentCheckpoint = this;
            }
        }

        private void OnDisable()
        {
            if (CurrentCheckpoint == this)
            {
                CurrentCheckpoint = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Color based on state
            if (!IsUnlocked)
            {
                Gizmos.color = Color.gray;
            }
            else if (IsActive)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.blue;
            }

            // Draw checkpoint marker
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 1f));

            // Draw respawn position
            Vector3 respawnPos = GetRespawnPosition();
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(respawnPos, 0.3f);
            Gizmos.DrawRay(respawnPos, GetRespawnRotation() * Vector3.forward * 1.5f);

            // Draw activation radius
            if (ActivationRadius > 0)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, ActivationRadius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;

            string status = IsActive ? "ACTIVE" : (IsUnlocked ? "Ready" : "Locked");
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
                $"Checkpoint #{SequenceOrder}\n{DisplayName ?? CheckpointId}\nStatus: {status}");
        }
#endif
    }
}
