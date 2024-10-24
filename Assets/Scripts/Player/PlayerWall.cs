using System.Collections;
using UnityEngine;

public class PlayerWall : MonoBehaviour, IDamage
{
    public Transform[] wallSegments;
    [SerializeField] Material tempMat;

    [Header("--Stats--")]
    public int HP;

    [Header("--AttackPoints--")]
    public Transform[] frontRowAttackPoints;
    public Transform[] backRowAttackPoints;
    [SerializeField] private float gizmoSize = 0.5f;
    [SerializeField] private Color frontRowColor = Color.blue;
    [SerializeField] private Color backRowColor = Color.red;


    int HPMax;
    Material tempColor;

    void Start()
    {
        HPMax = HP;
        tempColor = wallSegments[0].GetComponent<MeshRenderer>().material;

        HUDManager.Instance.InitializeWallHealth(HPMax);
    }

    public void takeDamage(int amount, bool headshot)
    {
        HP -= amount;
        HUDManager.Instance.UpdateWallHealth(HP,HPMax);
        StartCoroutine(flashMat());

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
        HUDManager.Instance.UpdateWallHealth(HP,HPMax);

        Debug.Log("Wall healed for " + amount + " health. Health left: " + HP);
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
        GameManager.Instance.isWallDestroyed = true;
        Destroy(gameObject); 
        GameManager.Instance.LoseGame();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = frontRowColor;
        DrawAttackPointGizmos(frontRowAttackPoints);

        Gizmos.color = backRowColor;
        DrawAttackPointGizmos(backRowAttackPoints);
    }

    private void DrawAttackPointGizmos(Transform[] attackPoints)
    {
        if (attackPoints == null) return;

        foreach (Transform point in attackPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, gizmoSize);
            }
        }
    }
}
