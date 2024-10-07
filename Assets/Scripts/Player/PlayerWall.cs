using System.Collections;
using UnityEngine;

public class PlayerWall : MonoBehaviour, IDamage
{
    public int maxHealth = 10;
    public Transform[] wallSegments;
    [SerializeField] Material tempMat;

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void takeDamage(int amount, bool headshot)
    {
        currentHealth -= amount;
        StartCoroutine(flashMat());
        Debug.Log("Wall took " + amount + " damage. Health left: " + currentHealth);

        if (currentHealth <= 0)
        {
            DestroyWall();
        }
    }

    IEnumerator flashMat()
    {
        Material tempColor = wallSegments[0].GetComponent<MeshRenderer>().material;
        foreach (Transform segment in wallSegments)
        {
            segment.GetComponent<MeshRenderer>().material = tempMat;
        }
        yield return new WaitForSeconds(0.1f);
        foreach (Transform segment in wallSegments)
        {
            segment.GetComponent<MeshRenderer>().material = tempColor;
        }
    }

    void DestroyWall()
    {
        Debug.Log("Wall has been destroyed!");
        GameManager.Instance.isWallDestroyed = true;
        GameManager.Instance.LoseGame();
        Destroy(gameObject); 
    }
}
