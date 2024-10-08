using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    //rigid body to move bullet in space
    [SerializeField] Rigidbody rb;

    //attributes
    [SerializeField] int speed;
    [SerializeField] private GameObject enemyHitEffect;
    [SerializeField] private GameObject objectHitEffect;

    private float _timeToDestroy;
    public float timeToDestroy
    {
        get { return _timeToDestroy; }
        set
        {
            _timeToDestroy = value;
            Destroy(gameObject, _timeToDestroy);
        }
    }

    public int damageAmount { get; set; }


    void Start()
    {
        rb.velocity = transform.forward * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || other.CompareTag("Player") || other.CompareTag("PlayerWall"))
            return;

        IDamage dmg = other.GetComponent<IDamage>();

        if (dmg != null)
        {
            dmg.takeDamage(damageAmount, false);
            GameObject effect = Instantiate(enemyHitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 0.5f);
        }
        else
        {
            GameObject effect = Instantiate(objectHitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 0.5f);
        }
        Destroy(gameObject);
    }
}