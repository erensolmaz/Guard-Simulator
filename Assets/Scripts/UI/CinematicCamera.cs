using UnityEngine;
using System.Collections.Generic;

public class CinematicCamera : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public List<Transform> waypoints = new List<Transform>();
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float waypointReachThreshold = 1f;
    
    [Header("Camera Settings")]
    [SerializeField] private bool lookAtNextWaypoint = true;
    [SerializeField] private int lookAheadSteps = 1;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private float rotationLockThreshold = 5f;
    
    [Header("Rotation Constraints")]
    [SerializeField] private bool constrainXRotation = false;
    [SerializeField] private bool constrainYRotation = false;
    [SerializeField] private bool constrainZRotation = true;
    [SerializeField] private float lockedXRotation = 0f;
    [SerializeField] private float lockedYRotation = 0f;
    [SerializeField] private float lockedZRotation = 0f;
    
    [Header("Loop Settings")]
    [SerializeField] private bool loop = true;
    [SerializeField] private float waitTimeAtEnd = 2f;
    
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isRotationLocked = false;
    private Quaternion targetRotation;

    private void Start()
    {
        if (waypoints.Count > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
            SetInitialRotation();
            Debug.Log($"Cinematic Camera started with {waypoints.Count} waypoints");
        }
        else
        {
            Debug.LogWarning("Cinematic Camera has no waypoints assigned!");
        }
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning("No waypoints assigned to CinematicCamera!");
            return;
        }

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtEnd)
            {
                isWaiting = false;
                waitTimer = 0f;
                currentWaypointIndex = 0;
                isRotationLocked = false;
            }
            return;
        }

        MoveTowardsWaypoint();
        
        if (!isRotationLocked)
        {
            RotateTowardsTarget();
        }
        else
        {
            ApplyRotationConstraints();
        }
    }

    private void SetInitialRotation()
    {
        if (waypoints.Count > 1 && waypoints[1] != null)
        {
            Vector3 direction = (waypoints[1].position - waypoints[0].position).normalized;
            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction);
                targetRotation *= Quaternion.Euler(rotationOffset);
                transform.rotation = targetRotation;
            }
        }
    }

    private void MoveTowardsWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Count) return;
        
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
        {
            Debug.LogWarning($"Waypoint {currentWaypointIndex} is null!");
            currentWaypointIndex++;
            return;
        }

        Vector3 targetPosition = targetWaypoint.position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        float distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);
        
        if (distanceToWaypoint < waypointReachThreshold)
        {
            currentWaypointIndex++;
            isRotationLocked = false;
            
            if (currentWaypointIndex >= waypoints.Count)
            {
                if (loop)
                {
                    isWaiting = true;
                }
                else
                {
                    currentWaypointIndex = waypoints.Count - 1;
                }
            }
        }
    }

    private void RotateTowardsTarget()
    {
        if (!lookAtNextWaypoint || waypoints.Count == 0) return;

        int lookAtIndex = Mathf.Min(currentWaypointIndex + lookAheadSteps, waypoints.Count - 1);
        Transform lookTarget = waypoints[lookAtIndex];
        
        if (lookTarget == null) return;

        Vector3 direction = (lookTarget.position - transform.position).normalized;
        
        if (direction != Vector3.zero)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction);
            desiredRotation *= Quaternion.Euler(rotationOffset);
            
            targetRotation = desiredRotation;
            
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
            
            ApplyRotationConstraints();
            
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            if (angleDifference < rotationLockThreshold)
            {
                isRotationLocked = true;
                transform.rotation = targetRotation;
                ApplyRotationConstraints();
            }
        }
    }

    private void ApplyRotationConstraints()
    {
        Vector3 currentEuler = transform.eulerAngles;
        Vector3 newEuler = currentEuler;
        
        if (constrainXRotation)
        {
            newEuler.x = lockedXRotation;
        }
        
        if (constrainYRotation)
        {
            newEuler.y = lockedYRotation;
        }
        
        if (constrainZRotation)
        {
            newEuler.z = lockedZRotation;
        }
        
        transform.eulerAngles = newEuler;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
            }
        }
        
        if (waypoints.Count > 0 && waypoints[waypoints.Count - 1] != null)
        {
            Gizmos.DrawWireSphere(waypoints[waypoints.Count - 1].position, 0.5f);
        }

        Gizmos.color = Color.green;
        if (Application.isPlaying && currentWaypointIndex < waypoints.Count && waypoints[currentWaypointIndex] != null)
        {
            Gizmos.DrawWireSphere(waypoints[currentWaypointIndex].position, 0.8f);
            
            if (isRotationLocked)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5f);
            }
        }
    }
}
