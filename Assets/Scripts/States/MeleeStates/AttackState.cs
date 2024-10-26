using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : IEnemyState
{
    private BaseMeleeEnemy enemy;

    public AttackState(BaseMeleeEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.isMoving = false;
        enemy.SetupForAttack();
    }

    public void Update()
    {
        enemy.AttackWall();
    }

    public void Exit()
    {
    }
}
