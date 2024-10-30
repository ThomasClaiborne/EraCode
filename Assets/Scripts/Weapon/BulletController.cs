using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    [Header("Bullet Properties")]
    //[SerializeField] Rigidbody rb;
    //[SerializeField] int speed;
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
        //rb.velocity = transform.forward * speed;
        Destroy(gameObject, timeToDestroy);

        //projectileMover = GetComponent<HS_ProjectileMover>();
        //projectilePS = GetComponentInChildren<ParticleSystem>();

        //if (projectileMover != null)
        //{
        //    projectileMover.speed = speed;
        //}
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger || other.CompareTag("Player") || other.CompareTag("PlayerWall") || other.CompareTag("Environment"))
            return;

        if (isExplosive)
        {
            Explode();
        }
        else
        {
            DealDamage(other);
        }

        if (!isPiercing || pierceCount >= pierceLimit || other.CompareTag("Obstacle")) // Limit piercing to 3 enemies
        {
            //Destroy(gameObject);
        }
    }

    private void DealDamage(Collider other)
    {
        IDamage dmg = other.GetComponent<IDamage>();
        if (dmg != null)
        {
            int currentDamage = Mathf.RoundToInt(damageAmount * Mathf.Pow(1 - piercingDamageReduction, pierceCount));
            dmg.takeDamage(currentDamage, false);
            pierceCount++;
        }
        else
        {
            GameObject effect = Instantiate(objectHitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 0.5f);
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

        //GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        //float scaleFactor = explosionRadius / 1f;
        //effect.transform.localScale = Vector3.one * scaleFactor;

        //ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
        //foreach (ParticleSystem ps in particleSystems)
        //{
        //    var main = ps.main;
        //    main.startSizeMultiplier *= scaleFactor;
        //}

        //Destroy(effect, scaleFactor);
        //Destroy(gameObject);
    }

}