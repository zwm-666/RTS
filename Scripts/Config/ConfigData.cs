// ============================================================
// ConfigData.cs - JSON反序列化DTO结构
// ============================================================

using System;
using System.Collections.Generic;

namespace RTS.Config
{
    /// <summary>
    /// 游戏配置DTO（根对象）
    /// </summary>
    [Serializable]
    public class GameConfigDto
    {
        public string version;
        public string lastUpdate;
        public List<UnitDto> units;
        public List<BuildingDto> buildings;
    }

    /// <summary>
    /// 单位配置DTO
    /// </summary>
    [Serializable]
    public class UnitDto
    {
        public string unitId;
        public string displayName;
        public string faction;
        public string category;
        public string description;
        public UnitStatsDto stats;
        public ResourceCostDto cost;
        public float buildTime;
        public int populationCost;
        public UnitAbilitiesDto abilities;
        public List<SkillDto> skills;
    }

    /// <summary>
    /// 单位属性DTO
    /// </summary>
    [Serializable]
    public class UnitStatsDto
    {
        public int maxHealth;
        public float healthRegen;
        public int attackDamage;
        public string attackType;
        public float attackSpeed;
        public float attackRange;
        public int armor;
        public string armorType;
        public float moveSpeed;
        public float sightRange;
    }

    /// <summary>
    /// 单位能力DTO
    /// </summary>
    [Serializable]
    public class UnitAbilitiesDto
    {
        public bool canGather;
        public bool canBuild;
        public bool canHeal;
    }

    /// <summary>
    /// 技能DTO
    /// </summary>
    [Serializable]
    public class SkillDto
    {
        public string skillId;
        public string displayName;
        public string description;
        public bool passive;
        public float cooldown;
        public float manaCost;
    }

    /// <summary>
    /// 建筑配置DTO
    /// </summary>
    [Serializable]
    public class BuildingDto
    {
        public string buildingId;
        public string displayName;
        public string faction;
        public int tier;
        public string description;
        public BuildingStatsDto stats;
        public BuildingSizeDto size;
        public ResourceCostDto cost;
        public float buildTime;
        public BuildingProductionDto production;
        public BuildingCombatDto combat;
        public List<string> requirements;
    }

    /// <summary>
    /// 建筑属性DTO
    /// </summary>
    [Serializable]
    public class BuildingStatsDto
    {
        public int maxHealth;
        public int armor;
        public string armorType;
        public float sightRange;
    }

    /// <summary>
    /// 建筑尺寸DTO
    /// </summary>
    [Serializable]
    public class BuildingSizeDto
    {
        public int gridWidth;
        public int gridHeight;
    }

    /// <summary>
    /// 建筑生产能力DTO
    /// </summary>
    [Serializable]
    public class BuildingProductionDto
    {
        public int populationProvide;
        public bool isDropOffPoint;
        public List<string> producibleUnits;
        public int queueSize;
    }

    /// <summary>
    /// 建筑战斗属性DTO
    /// </summary>
    [Serializable]
    public class BuildingCombatDto
    {
        public bool canAttack;
        public int attackDamage;
        public float attackRange;
        public float attackSpeed;
    }

    /// <summary>
    /// 资源消耗DTO
    /// </summary>
    [Serializable]
    public class ResourceCostDto
    {
        public int wood;
        public int food;
        public int gold;
    }
}
