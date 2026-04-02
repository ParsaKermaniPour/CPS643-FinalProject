using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Waypoint route for a patrol bot.
/// Assign waypoints manually in the inspector so order, repeats, and backtracking are fully controlled.
/// </summary>
public class PatrolRoute : MonoBehaviour
{
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private bool drawLoopConnection = true;
    [SerializeField, Min(0.01f)] private float waypointGizmoRadius = 0.12f;

    public int Count => waypoints.Count;

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Count == 0)
            return null;

        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index];
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform current = waypoints[i];
            if (current == null)
                continue;

            Gizmos.DrawSphere(current.position, waypointGizmoRadius);

            int nextIndex = i + 1;
            if (nextIndex >= waypoints.Count)
            {
                if (!drawLoopConnection)
                    continue;

                nextIndex = 0;
            }

            Transform next = waypoints[nextIndex];
            if (next == null)
                continue;

            Gizmos.DrawLine(current.position, next.position);
        }
    }
}
