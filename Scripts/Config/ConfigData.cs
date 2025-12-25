// ============================================================
// ConfigData.cs
// JSON 配置数据结构定义（用于反序列化）
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Config
{
    /// <summary>
    /// 游戏总配置数据
    /// </summary>
    [Serializable]
    public class GameConfigData
    {
        public string version;
        public string lastUpdate;
        
        public Dictionary<string, AttackTypeInfo> attackTypes;
        public Dictionary<string, ArmorTypeInfo> armorTypes;
        public List<UnitConfigData> units;
        public List<BuildingConfigData> buildings;
        public Dictionary<string, Dictionary<string, float>> damageMultipliers;
    }

    /// <summary>
    /// 攻击类型信息
    /// </summary>
    [Serializable]
    public class AttackTypeInfo
    {
        public int id;
        public string displayName;
        public string description;
    }

    /// <summary>
    /// 护甲类型信息
    /// </summary>
    [Serializable]
    public class ArmorTypeInfo
    {
        public int id;
        public string displayName;
    }

    /// <summary>
    /// 单位配置数据
    /// </summary>
    [Serializable]
    public class UnitConfigData
    {
        public string unitId;
        public string displayName;
        public string faction;
        public string category;
        public string description;
        
        public UnitStats stats;
        public ResourceCost cost;
        public float buildTime;
        public int populationCost;
        public UnitAbilities abilities;
        public List<SkillData> skills;
        public string prefabPath;
    }

    /// <summary>
    /// 单位属性数据
    /// </summary>
    [Serializable]
    public class UnitStats
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
    /// 单位能力
    /// </summary>
    [Serializable]
    public class UnitAbilities
    {
        public bool canGather;
        public bool canBuild;
        public bool canHeal;
    }

    /// <summary>
    /// 技能数据
    /// </summary>
    [Serializable]
    public class SkillData
    {
        public string skillId;
        public string displayName;
        public string description;
        public bool passive;
        public float cooldown;
        public float manaCost;
    }

    /// <summary>
    /// 资源消耗
    /// </summary>
    [Serializable]
    public class ResourceCost
    {
        public int wood;
        public int food;
        public int gold;
        
        /// <summary>
        /// 转换为字典格式
        /// </summary>
        public Dictionary<RTS.Core.ResourceType, int> ToDictionary()
        {
            return new Dictionary<RTS.Core.ResourceType, int>
            {
                { RTS.Core.ResourceType.Wood, wood },
                { RTS.Core.ResourceType.Food, food },
                { RTS.Core.ResourceType.Gold, gold }
            };
        }
    }

    /// <summary>
    /// 建筑配置数据
    /// </summary>
    [Serializable]
    public class BuildingConfigData
    {
        public string buildingId;
        public string displayName;
        public string faction;
        public int tier;
        public string description;
        
        public BuildingStats stats;
        public BuildingSize size;
        public ResourceCost cost;
        public float buildTime;
        public BuildingProduction production;
        public BuildingCombat combat;
        public List<string> requirements;
        public string prefabPath;
    }

    /// <summary>
    /// 建筑属性
    /// </summary>
    [Serializable]
    public class BuildingStats
    {
        public int maxHealth;
        public int armor;
        public string armorType;
        public float sightRange;
    }

    /// <summary>
    /// 建筑尺寸
    /// </summary>
    [Serializable]
    public class BuildingSize
    {
        public int gridWidth;
        public int gridHeight;
    }

    /// <summary>
    /// 建筑生产能力
    /// </summary>
    [Serializable]
    public class BuildingProduction
    {
        public int populationProvide;
        public bool isDropOffPoint;
        public List<string> producibleUnits;
        public int queueSize;
    }

    /// <summary>
    /// 建筑战斗属性
    /// </summary>
    [Serializable]
    public class BuildingCombat
    {
        public bool canAttack;
        public int attackDamage;
        public float attackRange;
        public float attackSpeed;
    }
}
