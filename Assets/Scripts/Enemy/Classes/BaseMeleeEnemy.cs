using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseMeleeEnemy : BaseEnemy
{
    protected Transform targetAttackPoint;

    protected override void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }
    }

    protected override void InitializeStateMachine()
    {
        Debug.Log("Initializing Enemy State Machine");
        stateMachine = new EnemyStateMachine();

        // Add all possible states
        stateMachine.AddState(new TravelState(this));
        stateMachine.AddState(new AttackPointApproachState(this));
        stateMachine.AddState(new AttackState(this));

        // Set initial state
        Debug.Log("Setting initial state to TravelState");
        stateMachine.SetState<TravelState>();
    }

    public virtual void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;

        rb.MovePosition(rb.position + direction * stats.speed * Time.deltaTime);
        Debug.Log("Moving along path");

        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z), Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, toRotation, 720 * Time.deltaTime));
        }

        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                stateMachine.SetState<AttackPointApproachState>();
            }
        }
    }

    public virtual void FindAttackPoint()
    {
        PlayerWall playerWall = GameManager.Instance.playerWall;
        Transform closestPoint = null;
        float closestDistance = float.MaxValue;

        // Check front row first
        foreach (Transform point in playerWall.frontRowAttackPoints)
        {
            if (point.gameObject.activeSelf)
            {
                float distance = Vector3.Distance(transform.position, point.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = point;
                }
            }
        }

        // If no point found in front row, check back row
        if (closestPoint == null)
        {
            foreach (Transform point in playerWall.backRowAttackPoints)
            {
                if (point.gameObject.activeSelf)
                {
                    float distance = Vector3.Distance(transform.position, point.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPoint = point;
                    }
                }
            }
        }

        if (closestPoint != null)
        {
            targetAttackPoint = closestPoint;
            closestPoint.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("No available attack points found!");
            LevelManager.Instance.AddEnemyToWaitingList(this);
        }
    }

    public virtual void MoveToAttackPoint()
    {
        if (targetAttackPoint == null) return;

        Vector3 direction = (targetAttackPoint.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * stats.speed * Time.deltaTime);

        float distanceToTarget = Vector3.Distance(transform.position, targetAttackPoint.position);

        if (distanceToTarget < 0.1f)
        {
            stateMachine.SetState<AttackState>();
        }
    }

    public virtual void SetupForAttack()
    {
        rb.isKinematic = true;
    }

    public virtual void AttackWall()
    {
        if (attackTimer <= 0)
        {
            TriggerAttackAnimation();
            attackTimer = stats.attackInterval;
        }
        FacePlayer();
    }

    public override void OnAnimationDamageEvent()
    {
        if (!GameManager.Instance.isWallDestroyed)
        {
            IDamage wall = GameManager.Instance.playerWall.GetComponent<IDamage>();
            if (wall != null)
            {
                wall.takeDamage(stats.damage, false);
            }
        }
    }

    protected override void Die()
    {
        if (targetAttackPoint != null)
        {
            targetAttackPoint.gameObject.SetActive(true);
            LevelManager.Instance.NotifyAttackPointAvailable();
        }
        base.Die();
    }

    protected virtual void FacePlayer()
    {
        if (GameManager.Instance.player != null)
        {
            Vector3 directionToPlayer = GameManager.Instance.player.transform.position - transform.position;
            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
            }
        }
    }
}
