using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelState : IEnemyState
{
    private BaseMeleeEnemy enemy;

    public TravelState(BaseMeleeEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.isMoving = true;
    }

    public void Update()
    {
        enemy.MoveAlongPath();
    }

    public void Exit()
    {
        enemy.isMoving = false;
    }
}
