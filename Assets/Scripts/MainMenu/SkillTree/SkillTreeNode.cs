using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkillTreeNode
{
    public string nodeID;
    public Ability ability;
    public int skillPointCost;
    public List<string> requiredNodeIDs = new List<string>();
    //public bool isUnlocked = false;
}
