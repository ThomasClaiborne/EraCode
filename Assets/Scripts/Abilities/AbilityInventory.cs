using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityInventory : MonoBehaviour
{
    public List<Ability> allAbilities = new List<Ability>();

    public Ability GetAbilityByID(string abilityID)
    {
        return allAbilities.Find(a => a.abilityID == abilityID);
    }

    public List<Ability> GetAllAbilities()
    {
        return new List<Ability>(allAbilities);
    }
}
