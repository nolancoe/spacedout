using System;
using UnityEngine;

public class LedgeCollisionChecker : MonoBehaviour
{
    [SerializeField] private GameObject handPosition;
    
    // New serialized fields for ledge-specific offsets (for front and back hanging)
    [Header("Collider Settings")]
    [SerializeField] private BoxCollider frontHangCollider;
    [SerializeField] private BoxCollider backHangCollider;

    [SerializeField] private float ledgeOffsetX = -0.09f;
    [SerializeField] private float ledgeOffsetY = -2.42f;
    [SerializeField] private float ledgeOffsetZ = -1.38f;
    [SerializeField] private float ledgeRotationY = 0f;

    // Offsets for back hanging
    [SerializeField] private float ledgeOffsetBackX = -0.09f;
    [SerializeField] private float ledgeOffsetBackY = -2.42f;
    [SerializeField] private float ledgeOffsetBackZ = -1.38f;
    [SerializeField] private float ledgeRotationBackY = 180f;

    // Event that now includes offsets and rotation
    public static Action<Vector3, Transform, float, float, float, float> OnLedgeCollision;

    private void OnTriggerEnter(Collider other)
    {
        
        // Debugs
        Debug.Log($"LedgeCollisionChecker::OnTriggerEnter::{other.transform.name}");

        // Check if the player (with tag "LedgeCollider") is colliding with any of the ledge's colliders
        if (other.CompareTag("LedgeCollider"))
        {
            if (frontHangCollider.bounds.Intersects(other.bounds))
            {
                float currentYRotation = handPosition.transform.eulerAngles.y;
                // Use both dynamic rotation and offset for fine-tuning
                float adjustedRotation = currentYRotation + ledgeRotationY;
                OnLedgeCollision?.Invoke(handPosition.transform.position, handPosition.transform, ledgeOffsetX, ledgeOffsetY, ledgeOffsetZ, adjustedRotation);
            }
            else if (backHangCollider.bounds.Intersects(other.bounds))
            {
                Debug.Log("Back Hang Collider Triggered");
                float currentYRotation = handPosition.transform.eulerAngles.y;
                float adjustedRotation = currentYRotation + 180f + ledgeRotationBackY; // Add back offset
                OnLedgeCollision?.Invoke(handPosition.transform.position, handPosition.transform, ledgeOffsetBackX, ledgeOffsetBackY, ledgeOffsetBackZ, adjustedRotation);
            }
        }
    }
}