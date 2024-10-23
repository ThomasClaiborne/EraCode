using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkillTreePath
{
    public string pathName;
    public List<SkillTreeNode> nodes = new List<SkillTreeNode>();
}
