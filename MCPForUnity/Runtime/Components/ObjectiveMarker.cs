using System;
using UnityEngine;
using UnityEngine.Events;

namespace MCPForUnity.Runtime.Components
{
    /// <summary>
    /// Marks a location or object as an objective/goal in the level.
    /// Used for quest markers, goal positions, collectibles, etc.
    /// </summary>
    [AddComponentMenu("MCP/Level Design/Objective Marker")]
    public class ObjectiveMarker : MonoBehaviour
    {
        /// <summary>
        /// Type of objective.
        /// </summary>
        public enum ObjectiveType
        {
            Primary,        // Main mission objective
            Secondary,      // Optional side objective
            Bonus,          // Hidden/bonus objective
            Collectible,    // Collectible item
            Interaction,    // Interact with something
            Escort,         // Escort/protect target
            Elimination,    // Defeat target
            Survival,       // Survive in area
            Custom          // Custom objective type
        }

        /// <summary>
        /// State of the objective.
        /// </summary>
        public enum ObjectiveState
        {
            Hidden,         // Not yet revealed
            Available,      // Can be started
            Active,         // Currently active
            Completed,      // Successfully completed
            Failed          // Failed to complete
        }

        [Header("Objective Configuration")]
        [Tooltip("Type of objective.")]
        public ObjectiveType Type = ObjectiveType.Primary;

        [Tooltip("Current state of this objective.")]
        public ObjectiveState State = ObjectiveState.Available;

        [Tooltip("Display name for UI.")]
        public string DisplayName = "Objective";

        [Tooltip("Description shown in UI.")]
        [TextArea(2, 4)]
        public string Description;

        [Header("Objective Settings")]
        [Tooltip("Order in the objective list/sequence.")]
        public int Order = 0;

        [Tooltip("Whether this objective is optional.")]
        public bool IsOptional = false;

        [Tooltip("Points/score awarded for completing this objective.")]
        public int PointValue = 100;

        [Tooltip("Time limit to complete (0 = no limit).")]
        [Min(0f)]
        public float TimeLimit = 0f;

        [Header("Completion Settings")]
        [Tooltip("Radius for proximity-based completion.")]
        [Min(0f)]
        public float CompletionRadius = 2f;

        [Tooltip("Tags that can complete this objective.")]
        public string[] CompletorTags = new string[] { "Player" };

        [Tooltip("Whether to auto-complete when player enters radius.")]
        public bool AutoComplete = false;

        [Header("Events")]
        [Tooltip("Event fired when objective becomes active.")]
        public UnityEvent OnObjectiveActivated;

        [Tooltip("Event fired when objective is completed.")]
        public UnityEvent<GameObject> OnObjectiveCompleted;

        [Tooltip("Event fired when objective is failed.")]
        public UnityEvent OnObjectiveFailed;

        [Header("UI/Visual Settings")]
        [Tooltip("Icon to display on minimap/HUD.")]
        public Sprite Icon;

        [Tooltip("Color for the objective marker.")]
        public Color MarkerColor = Color.yellow;

        [Tooltip("Whether to show distance to objective.")]
        public bool ShowDistance = true;

        [Header("Optional Settings")]
        [Tooltip("Unique identifier for this objective.")]
        public string ObjectiveId;

        [Tooltip("Group this objective belongs to.")]
        public string ObjectiveGroup;

        [Tooltip("Prerequisites (ObjectiveIds that must be completed first).")]
        public string[] Prerequisites;

        [Tooltip("Custom metadata as JSON string.")]
        [TextArea(2, 4)]
        public string Metadata;

        private float _startTime;
        private float _completionTime;

        /// <summary>
        /// Time elapsed since objective became active.
        /// </summary>
        public float ElapsedTime => State == ObjectiveState.Active ? Time.time - _startTime : _completionTime - _startTime;

        /// <summary>
        /// Time remaining if there's a time limit.
        /// </summary>
        public float TimeRemaining => TimeLimit > 0 ? Mathf.Max(0, TimeLimit - ElapsedTime) : float.PositiveInfinity;

        /// <summary>
        /// Whether the time limit has expired.
        /// </summary>
        public bool IsExpired => TimeLimit > 0 && ElapsedTime >= TimeLimit;

        /// <summary>
        /// Activates this objective.
        /// </summary>
        public void Activate()
        {
            if (State != ObjectiveState.Available && State != ObjectiveState.Hidden)
                return;

            State = ObjectiveState.Active;
            _startTime = Time.time;

            OnObjectiveActivated?.Invoke();
        }

        /// <summary>
        /// Completes this objective.
        /// </summary>
        public void Complete(GameObject completor = null)
        {
            if (State != ObjectiveState.Active)
                return;

            State = ObjectiveState.Completed;
            _completionTime = Time.time;

            OnObjectiveCompleted?.Invoke(completor);
        }

        /// <summary>
        /// Fails this objective.
        /// </summary>
        public void Fail()
        {
            if (State != ObjectiveState.Active)
                return;

            State = ObjectiveState.Failed;
            _completionTime = Time.time;

            OnObjectiveFailed?.Invoke();
        }

        /// <summary>
        /// Resets this objective to available state.
        /// </summary>
        public void Reset()
        {
            State = ObjectiveState.Available;
            _startTime = 0;
            _completionTime = 0;
        }

        /// <summary>
        /// Reveals a hidden objective.
        /// </summary>
        public void Reveal()
        {
            if (State == ObjectiveState.Hidden)
            {
                State = ObjectiveState.Available;
            }
        }

        private void Update()
        {
            if (State == ObjectiveState.Active && TimeLimit > 0 && IsExpired)
            {
                Fail();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!AutoComplete || State != ObjectiveState.Active)
                return;

            if (IsValidCompletor(other.gameObject))
            {
                Complete(other.gameObject);
            }
        }

        private bool IsValidCompletor(GameObject obj)
        {
            if (CompletorTags == null || CompletorTags.Length == 0)
                return true;

            foreach (var tag in CompletorTags)
            {
                if (string.IsNullOrEmpty(tag) || obj.CompareTag(tag))
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Color based on type and state
            Color color = Type switch
            {
                ObjectiveType.Primary => Color.yellow,
                ObjectiveType.Secondary => Color.cyan,
                ObjectiveType.Bonus => Color.magenta,
                ObjectiveType.Collectible => new Color(1f, 0.5f, 0f),
                ObjectiveType.Interaction => Color.green,
                ObjectiveType.Escort => Color.blue,
                ObjectiveType.Elimination => Color.red,
                ObjectiveType.Survival => new Color(0.5f, 0f, 0.5f),
                _ => MarkerColor
            };

            // Modify alpha based on state
            color.a = State switch
            {
                ObjectiveState.Hidden => 0.2f,
                ObjectiveState.Completed => 0.4f,
                ObjectiveState.Failed => 0.3f,
                _ => 1f
            };

            Gizmos.color = color;

            // Draw marker
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw completion radius
            if (CompletionRadius > 0.5f)
            {
                Gizmos.color = new Color(color.r, color.g, color.b, 0.2f);
                Gizmos.DrawWireSphere(transform.position, CompletionRadius);
            }

            // Draw diamond shape for objectives
            Vector3 pos = transform.position;
            Vector3 top = pos + Vector3.up * 1f;
            Vector3 bottom = pos + Vector3.up * 0.2f;
            Vector3 offset = 0.3f * Vector3.one;

            Gizmos.color = color;
            Gizmos.DrawLine(top, pos + new Vector3(offset.x, 0.6f, 0));
            Gizmos.DrawLine(top, pos + new Vector3(-offset.x, 0.6f, 0));
            Gizmos.DrawLine(top, pos + new Vector3(0, 0.6f, offset.z));
            Gizmos.DrawLine(top, pos + new Vector3(0, 0.6f, -offset.z));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;

            string stateStr = State.ToString();
            if (State == ObjectiveState.Active && TimeLimit > 0)
            {
                stateStr += $" ({TimeRemaining:F1}s)";
            }

            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"{Type}: {DisplayName}\nState: {stateStr}\nPoints: {PointValue}");
        }
#endif
    }
}
