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
    void Start()
    {
        initialRotation = transform.rotation;
    }

    void Update()
    {
        RotateTowardsMouse();

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
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

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            float angleDifference = Quaternion.Angle(initialRotation, targetRotation);

            if (angleDifference <= maxRotationAngle)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
    }
}

