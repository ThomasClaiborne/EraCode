using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    public List<SkillTreePath> skillPaths = new List<SkillTreePath>();
    public AbilityInventory abilityInventory;

    //private PlayerInventory playerInventory;

    private void Start()
    {
        //InitializeSkillTree();
    }


    public bool UnlockNode(string nodeID)
    {
        SkillTreeNode node = FindNode(nodeID);
        if (node == null) return false;
        if (PlayerInventory.Instance.IsAbilityUnlocked(nodeID)) return false;

        if (CanUnlockNode(node) && PlayerInventory.Instance.LevelSystem.SpendSkillPoints(node.skillPointCost))
        {
            PlayerInventory.Instance.UnlockAbility(nodeID, node.ability);
            return true;
        }

        return false;
    }

    public bool SpendSkillPoints(int amount)
    {
        if (PlayerInventory.Instance.LevelSystem.SkillPoints >= amount)
        {
            PlayerInventory.Instance.LevelSystem.SpendSkillPoints(amount);
            return true;
        }
        return false;
    }

    public bool CanUnlockNode(SkillTreeNode node)
    {
        if (node.requiredNodeIDs.Count == 0) return true;

        foreach (string requiredNodeID in node.requiredNodeIDs)
        {
            if (!PlayerInventory.Instance.IsAbilityUnlocked(requiredNodeID))
            {
                return false;
            }
        }

        return true;
    }

    private SkillTreeNode FindNode(string nodeID)
    {
        foreach (var path in skillPaths)
        {
            foreach (var node in path.nodes)
            {
                if (node.nodeID == nodeID) return node;
            }
        }
        return null;
    }
}
