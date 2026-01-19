using System;
using UnityEngine;

namespace MCPForUnity.Runtime.Components
{
    /// <summary>
    /// Marks a location as a spawn point for players, enemies, items, or vehicles.
    /// Used by level design tools to define where entities should spawn.
    /// </summary>
    [AddComponentMenu("MCP/Level Design/Spawn Point")]
    public class SpawnPoint : MonoBehaviour
    {
        /// <summary>
        /// The type of entity that spawns at this point.
        /// </summary>
        public enum SpawnType
        {
            Player,
            Enemy,
            Item,
            Vehicle,
            NPC,
            Projectile
        }

        [Header("Spawn Configuration")]
        [Tooltip("The type of entity that spawns at this point.")]
        public SpawnType Type = SpawnType.Player;

        [Tooltip("Team identifier (0 = neutral, 1+ = team numbers).")]
        [Range(0, 16)]
        public int Team = 0;

        [Tooltip("Priority for spawn selection (higher = more likely to be chosen).")]
        [Range(0, 100)]
        public int Priority = 0;

        [Header("Respawn Settings")]
        [Tooltip("Delay before respawning (in seconds).")]
        [Min(0f)]
        public float RespawnDelay = 0f;

        [Tooltip("Maximum number of spawns (-1 = unlimited).")]
        public int MaxSpawns = -1;

        [Tooltip("Radius to check for nearby entities before spawning.")]
        [Min(0f)]
        public float ClearanceRadius = 1f;

        [Header("Optional Settings")]
        [Tooltip("Unique identifier for this spawn point.")]
        public string SpawnId;

        [Tooltip("Group this spawn point belongs to.")]
        public string SpawnGroup;

        [Tooltip("Custom metadata as JSON string.")]
        [TextArea(2, 4)]
        public string Metadata;

        private int _spawnCount = 0;

        /// <summary>
        /// Gets the current spawn count.
        /// </summary>
        public int SpawnCount => _spawnCount;

        /// <summary>
        /// Checks if this spawn point can spawn another entity.
        /// </summary>
        public bool CanSpawn => MaxSpawns < 0 || _spawnCount < MaxSpawns;

        /// <summary>
        /// Records a spawn event.
        /// </summary>
        public void RecordSpawn()
        {
            _spawnCount++;
        }

        /// <summary>
        /// Resets the spawn count.
        /// </summary>
        public void ResetSpawnCount()
        {
            _spawnCount = 0;
        }

        /// <summary>
        /// Gets the spawn position (transform.position).
        /// </summary>
        public Vector3 GetSpawnPosition() => transform.position;

        /// <summary>
        /// Gets the spawn rotation (transform.rotation).
        /// </summary>
        public Quaternion GetSpawnRotation() => transform.rotation;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Color based on spawn type
            Gizmos.color = Type switch
            {
                SpawnType.Player => Color.green,
                SpawnType.Enemy => Color.red,
                SpawnType.Item => Color.yellow,
                SpawnType.Vehicle => Color.cyan,
                SpawnType.NPC => Color.magenta,
                SpawnType.Projectile => new Color(1f, 0.5f, 0f), // Orange
                _ => Color.white
            };

            // Draw spawn point marker
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw forward direction
            Gizmos.DrawRay(transform.position, transform.forward * 2f);

            // Draw clearance radius if set
            if (ClearanceRadius > 0.5f)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(transform.position, ClearanceRadius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Highlight selected spawn point
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.6f);

            // Draw team indicator
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
                $"{Type} (Team {Team}, Priority {Priority})");
        }
#endif
    }
}
