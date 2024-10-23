using UnityEngine;

public class LevelSystem
{
    public int Level { get; private set; }
    public int Experience { get; private set; }
    public int ExperienceToNextLevel { get; private set; }
    public int SkillPoints { get; private set; }

    private const int BaseExperience = 100;
    private const float ExperienceMultiplier = 1.5f;

    public LevelSystem(int level = 1, int experience = 0, int statPoints = 0)
    {
        Level = level;
        Experience = experience;
        SkillPoints = statPoints;
        CalculateNextLevelExperience();
    }

    public void AddExperience(int amount)
    {
        Experience += amount;
        while (Experience >= ExperienceToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Experience -= ExperienceToNextLevel;
        Level++;
        SkillPoints++;
        CalculateNextLevelExperience();
    }

    public bool SpendSkillPoints(int amount)
    {
        if (SkillPoints >= amount)
        {
            SkillPoints -= amount;
            return true;
        }
        return false;
    }

    private void CalculateNextLevelExperience()
    {
        ExperienceToNextLevel = Mathf.RoundToInt(BaseExperience * Mathf.Pow(ExperienceMultiplier, Level - 1));
    }
}