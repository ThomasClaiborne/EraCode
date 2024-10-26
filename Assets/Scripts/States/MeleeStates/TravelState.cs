using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelState : IEnemyState
{
    private BaseMeleeEnemy enemy;

    public TravelState(BaseMeleeEnemy enemy)
    {
        this.enemy = enemy;
        Debug.Log("Travel State Constructed");
    }

    public void Enter()
    {
        enemy.isMoving = true;
        Debug.Log("Travel State Entered");
    }

    public void Update()
    {
        enemy.MoveAlongPath();
        Debug.Log("Travel State Updating");
    }

    public void Exit()
    {
        enemy.isMoving = false;
        Debug.Log("Travel State Exited");
    }
}
