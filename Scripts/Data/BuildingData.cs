// ============================================================
// BuildingData.cs
// 建筑数据 ScriptableObject
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace RTS.Data
{
    /// <summary>
    /// 建筑类型
    /// </summary>
    public enum BuildingType
    {
        Base,           // 主城/基地
        Resource,       // 资源建筑（仓库、采矿场等）
        Military,       // 军事建筑（兵营、马厩等）
        Defense,        // 防御建筑（箭塔、城墙等）
        Tech,           // 科技建筑（铁匠铺、研究所等）
        Support         // 辅助建筑（市场、农场等）
    }

    /// <summary>
    /// 建筑数据 ScriptableObject
    /// 在 Unity 中创建: 右键 -> Create -> RTS -> Building Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuildingData", menuName = "RTS/Building Data", order = 2)]
    public class BuildingData : EntityData
    {
        [Header("====== 建筑属性 ======")]
        
        [Header("类型")]
        public BuildingType buildingType = BuildingType.Military;
        
        [Header("生命值")]
        public int maxHealth = 500;
        
        [Header("防御")]
        public int armor = 5;
        public ArmorType armorType = ArmorType.Fortified;
        
        [Header("尺寸（网格）")]
        public int gridWidth = 2;           // 占用网格宽度
        public int gridHeight = 2;          // 占用网格高度
        
        [Header("人口")]
        public int populationProvide = 0;   // 提供的人口上限
        
        [Header("生产能力")]
        public List<UnitData> producibleUnits = new List<UnitData>();   // 可生产的单位
        public int productionQueueSize = 5; // 生产队列大小
        
        [Header("资源收集（仓库类建筑）")]
        public bool isDropOffPoint = false; // 是否为资源交付点
        public float dropOffRadius = 5f;    // 交付范围
        
        [Header("攻击能力（防御塔等）")]
        public bool canAttack = false;
        public int attackDamage = 20;
        public float attackSpeed = 1.5f;
        public float attackRange = 8f;
        
        [Header("科技解锁（预留）")]
        public List<string> unlockedTechs = new List<string>();     // 解锁的科技ID
        public List<string> unlockedUnits = new List<string>();     // 解锁的单位ID
        public List<string> unlockedBuildings = new List<string>(); // 解锁的建筑ID
        
        [Header("预制体")]
        public GameObject buildingPrefab;           // 建筑预制体
        public GameObject constructionPrefab;       // 建造中的预制体（可选）
        
        [Header("视觉效果")]
        public Vector3 rallyPointOffset = Vector3.forward * 3f;     // 集结点偏移
        public Vector3 spawnPointOffset = Vector3.forward * 2f;     // 单位生成点偏移
    }
}
