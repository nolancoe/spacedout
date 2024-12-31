using System;
using UnityEngine;

public class LedgeCollisionChecker : MonoBehaviour
{
    [SerializeField] private GameObject handPosition;
    
    // New serialized fields for ledge-specific offsets (for front and back hanging)
    [Header("Collider Settings")]
    [SerializeField] private BoxCollider frontHangCollider;

    [SerializeField] private float ledgeOffsetX = -0.09f;
    [SerializeField] private float ledgeOffsetY = -2.42f;
    [SerializeField] private float ledgeOffsetZ = -1.38f;
    [SerializeField] private float ledgeRotationY;

    public static Action<Vector3, Transform, float, float, float, float> onLedgeCollision;

    private void OnTriggerEnter(Collider other)
    {
        
        // Debugs
        Debug.Log($"LedgeCollisionChecker::OnTriggerEnter::{other.transform.name}");

        // Check if the object with tag "LedgeFinder" (Should be child of Player object) is colliding with any of the ledge's colliders
        if (!other.CompareTag("LedgeFinder")) return;
        if (!frontHangCollider.bounds.Intersects(other.bounds)) return;
        var currentYRotation = handPosition.transform.eulerAngles.y;
        // Use both dynamic rotation and offset for fine-tuning
        var adjustedRotation = currentYRotation + ledgeRotationY;
        onLedgeCollision?.Invoke(handPosition.transform.position, handPosition.transform, ledgeOffsetX, ledgeOffsetY, ledgeOffsetZ, adjustedRotation);
    }
}