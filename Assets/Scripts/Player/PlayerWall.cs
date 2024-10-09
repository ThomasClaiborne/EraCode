using System.Collections;
using UnityEngine;

public class PlayerWall : MonoBehaviour, IDamage
{
    public Transform[] wallSegments;
    [SerializeField] Material tempMat;

    [Header("--Stats--")]
    public int HP;


    int HPMax;
    Material tempColor;

    void Start()
    {
        HPMax = HP;
        HUDManager.Instance.WallHPText.text = HP.ToString();
        tempColor = wallSegments[0].GetComponent<MeshRenderer>().material;
    }

    public void takeDamage(int amount, bool headshot)
    {
        HP -= amount;
        HUDManager.Instance.DecreaseHealth(amount, HP, HPMax);
        HUDManager.Instance.WallHPText.text = HP.ToString();
        StartCoroutine(flashMat());
        Debug.Log("Wall took " + amount + " damage. Health left: " + HP);

        if (HP <= 0)
        {
            DestroyWall();
        }
    }

    public void heal(int amount)
    {
        HP += amount;
        if (HP > HPMax)
        {
            HP = HPMax;
        }
        HUDManager.Instance.IncreaseHealth(amount, HP, HPMax);
        HUDManager.Instance.WallHPText.text = HP.ToString();
        Debug.Log("Wall healed for " + amount + " health. Health left: " + HP);
    }

    void UpdateHPBar()
    {
        float fillAmount = (float)HP / HPMax;
        HUDManager.Instance.wallHPBar.fillAmount = fillAmount;
    }

    IEnumerator flashMat()
    {
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
        Destroy(gameObject); 
        GameManager.Instance.LoseGame();
    }
}
