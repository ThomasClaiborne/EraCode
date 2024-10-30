using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    [Header("Bullet Properties")]
    [SerializeField] private GameObject objectHitEffect;
    [SerializeField] private GameObject explosionEffect;

    [Header("Asset Components")]
    private HS_ProjectileMover projectileMover;
    private ParticleSystem projectilePS;

    public float timeToDestroy { get; set; }
    public int damageAmount { get; set; }
    public bool isPiercing { get; set; }
    public float piercingDamageReduction { get; set; }
    public int pierceLimit { get; set; }
    public bool isExplosive { get; set; }
    public float explosionRadius { get; set; }
    public float explosionForce { get; set; }

    private int pierceCount = 0;

    void Start()
    {
        projectileMover = GetComponent<HS_ProjectileMover>();
        Destroy(gameObject, timeToDestroy);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || other.CompareTag("Player") || other.CompareTag("PlayerWall") || other.CompareTag("Environment"))
            return;

        if (isExplosive)
        {
            Explode();
            projectileMover.HandleCollision(other);
        }
        else
        {
            DealDamage(other);
        }

        if (!isPiercing || pierceCount >= pierceLimit || other.CompareTag("Obstacle")) 
        {
            projectileMover.HandleCollision(other);
        }
    }

    private void DealDamage(Collider other)
    {
        IDamage dmg = other.GetComponent<IDamage>();
        if (dmg != null)
        {
            int currentDamage = Mathf.RoundToInt(damageAmount * Mathf.Pow(1 - piercingDamageReduction, pierceCount));
            dmg.takeDamage(currentDamage, false);

            if (objectHitEffect != null)
            {
                GameObject effect = Instantiate(objectHitEffect, transform.position, Quaternion.identity);
                Destroy(effect, 0.5f);
            }

            pierceCount++;
        }
    }

    private void Explode()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            IDamage dmg = hit.GetComponent<IDamage>();
            if (dmg != null)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float damagePercent = 1 - (distance / explosionRadius);
                int explosionDamage = Mathf.RoundToInt(damageAmount * damagePercent);
                dmg.takeDamage(explosionDamage, false);
            }
        }
    }
}