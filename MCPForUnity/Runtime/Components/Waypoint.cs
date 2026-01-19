using System.Collections.Generic;
using UnityEngine;

namespace MCPForUnity.Runtime.Components
{
    /// <summary>
    /// Marks a location as a waypoint for AI navigation and path following.
    /// Waypoints can be connected to form navigation paths.
    /// </summary>
    [AddComponentMenu("MCP/Level Design/Waypoint")]
    public class Waypoint : MonoBehaviour
    {
        /// <summary>
        /// Actions that can be performed at this waypoint.
        /// </summary>
        public enum WaypointAction
        {
            None,
            Patrol,
            Guard,
            Investigate,
            Wait,
            LookAround,
            Crouch,
            Sprint,
            Custom
        }

        [Header("Waypoint Configuration")]
        [Tooltip("Connected waypoints for path following.")]
        public List<Waypoint> Connections = new List<Waypoint>();

        [Tooltip("Time to wait at this waypoint (in seconds).")]
        [Min(0f)]
        public float WaitTime = 0f;

        [Tooltip("Action to perform at this waypoint.")]
        public WaypointAction Action = WaypointAction.None;

        [Header("Path Settings")]
        [Tooltip("Movement speed modifier at this waypoint (1 = normal).")]
        [Range(0.1f, 3f)]
        public float SpeedModifier = 1f;

        [Tooltip("Radius for reaching this waypoint.")]
        [Min(0.1f)]
        public float ReachRadius = 0.5f;

        [Tooltip("Whether this waypoint is bidirectional.")]
        public bool Bidirectional = true;

        [Header("Optional Settings")]
        [Tooltip("Unique identifier for this waypoint.")]
        public string WaypointId;

        [Tooltip("Group this waypoint belongs to.")]
        public string WaypointGroup;

        [Tooltip("Custom action name when Action is Custom.")]
        public string CustomActionName;

        [Tooltip("Custom metadata as JSON string.")]
        [TextArea(2, 4)]
        public string Metadata;

        /// <summary>
        /// Gets the next waypoint in the path.
        /// </summary>
        /// <param name="previous">The previous waypoint (to avoid backtracking).</param>
        /// <returns>The next waypoint, or null if none available.</returns>
        public Waypoint GetNextWaypoint(Waypoint previous = null)
        {
            if (Connections == null || Connections.Count == 0)
                return null;

            // If only one connection, return it
            if (Connections.Count == 1)
                return Connections[0];

            // Try to find a connection that isn't the previous waypoint
            foreach (var connection in Connections)
            {
                if (connection != null && connection != previous)
                    return connection;
            }

            // If all connections are the previous waypoint, return the first one
            return Connections[0];
        }

        /// <summary>
        /// Gets a random connected waypoint.
        /// </summary>
        public Waypoint GetRandomConnection()
        {
            if (Connections == null || Connections.Count == 0)
                return null;

            int index = Random.Range(0, Connections.Count);
            return Connections[index];
        }

        /// <summary>
        /// Adds a connection to another waypoint.
        /// </summary>
        public void AddConnection(Waypoint other)
        {
            if (other == null || other == this)
                return;

            if (!Connections.Contains(other))
                Connections.Add(other);

            // Add reverse connection if bidirectional
            if (Bidirectional && !other.Connections.Contains(this))
                other.Connections.Add(this);
        }

        /// <summary>
        /// Removes a connection to another waypoint.
        /// </summary>
        public void RemoveConnection(Waypoint other)
        {
            if (other == null)
                return;

            Connections.Remove(other);

            // Remove reverse connection if bidirectional
            if (Bidirectional)
                other.Connections.Remove(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw waypoint marker
            Gizmos.color = Action switch
            {
                WaypointAction.Patrol => Color.yellow,
                WaypointAction.Guard => Color.red,
                WaypointAction.Investigate => Color.cyan,
                WaypointAction.Wait => Color.gray,
                WaypointAction.LookAround => Color.magenta,
                WaypointAction.Crouch => new Color(0.5f, 0.5f, 0f),
                WaypointAction.Sprint => Color.green,
                WaypointAction.Custom => Color.white,
                _ => Color.yellow
            };

            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw connections
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            foreach (var conn in Connections)
            {
                if (conn != null)
                {
                    Gizmos.DrawLine(transform.position, conn.transform.position);

                    // Draw arrow direction
                    Vector3 direction = (conn.transform.position - transform.position).normalized;
                    Vector3 midpoint = (transform.position + conn.transform.position) / 2f;
                    DrawArrow(midpoint, direction * 0.5f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Highlight selected waypoint
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.4f);

            // Draw reach radius
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, ReachRadius);

            // Draw label
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
                $"{Action} (Wait: {WaitTime}s, Speed: {SpeedModifier}x)");
        }

        private void DrawArrow(Vector3 position, Vector3 direction)
        {
            if (direction == Vector3.zero) return;

            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward * 0.2f;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward * 0.2f;

            Gizmos.DrawRay(position + direction, right);
            Gizmos.DrawRay(position + direction, left);
        }
#endif
    }
}
