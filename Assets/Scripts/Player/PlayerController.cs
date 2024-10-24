using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float maxRotationAngle = 90f;
    [SerializeField] GameObject bulletPrefab;    
    [SerializeField] Transform bulletSpawnPoint; 

    private Quaternion initialRotation;
    private Vector3 lastValidDirection;
    void Start()
    {
        initialRotation = transform.rotation;
    }

    void Update()
    {
        RotateTowardsMouse();
    }

    void RotateTowardsMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            Vector3 targetPoint = hit.point;
            Vector3 direction = (targetPoint - transform.position).normalized;
            direction.y = 0;

            float angleToTarget = Vector3.SignedAngle(initialRotation * Vector3.forward, direction, Vector3.up);

            // Clamp the rotation angle
            if (Mathf.Abs(angleToTarget) <= maxRotationAngle)
            {
                lastValidDirection = direction;
            }
            else
            {
                // Clamp the direction to the maximum allowed angle
                float clampedAngle = Mathf.Sign(angleToTarget) * maxRotationAngle;
                lastValidDirection = Quaternion.Euler(0, clampedAngle, 0) * (initialRotation * Vector3.forward);
            }

            // Rotate towards the last valid direction
            Quaternion targetRotation = Quaternion.LookRotation(lastValidDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}

