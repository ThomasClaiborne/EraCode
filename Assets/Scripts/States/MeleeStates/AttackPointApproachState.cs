using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPointApproachState : IEnemyState
{
    private BaseMeleeEnemy enemy;

    public AttackPointApproachState(BaseMeleeEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.isMoving = true;
        enemy.FindAttackPoint();
    }

    public void Update()
    {
        enemy.MoveToAttackPoint();
    }

    public void Exit()
    {
        enemy.isMoving = false;
    }
}
