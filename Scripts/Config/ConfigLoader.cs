// ============================================================
// ConfigLoader.cs
// 配置加载器 - 从 JSON 加载单位和建筑配置
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RTS.Data;
using RTS.Core;

namespace RTS.Config
{
    /// <summary>
    /// 配置加载器（单例模式）
    /// 挂载位置：GameManager 空对象上
    /// </summary>
    public class ConfigLoader : MonoBehaviour
    {
        #region 单例
        
        private static ConfigLoader _instance;
        public static ConfigLoader Instance => _instance;
        
        #endregion

        #region 配置
        
        [Header("配置文件路径")]
        [SerializeField] private string _configFileName = "Config/GameConfig";
        [SerializeField] private bool _useStreamingAssets = false;
        
        [Header("预制体基础路径")]
        [SerializeField] private string _prefabBasePath = "";
        
        [Header("调试")]
        [SerializeField] private bool _logLoading = true;
        
        #endregion

        #region 私有字段
        
        private GameConfigData _gameConfig;
        private Dictionary<string, UnitConfigData> _unitConfigs = new Dictionary<string, UnitConfigData>();
        private Dictionary<string, BuildingConfigData> _buildingConfigs = new Dictionary<string, BuildingConfigData>();
        private Dictionary<string, Dictionary<string, float>> _damageTable = new Dictionary<string, Dictionary<string, float>>();
        
        private bool _isLoaded = false;
        
        #endregion

        #region 属性
        
        public bool IsLoaded => _isLoaded;
        public GameConfigData GameConfig => _gameConfig;
        public int UnitCount => _unitConfigs.Count;
        public int BuildingCount => _buildingConfigs.Count;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            LoadConfig();
        }
        
        #endregion

        #region 加载配置
        
        /// <summary>
        /// 加载配置文件
        /// </summary>
        public void LoadConfig()
        {
            string jsonContent = null;
            
            if (_useStreamingAssets)
            {
                // 从 StreamingAssets 加载
                string path = Path.Combine(Application.streamingAssetsPath, _configFileName + ".json");
                if (File.Exists(path))
                {
                    jsonContent = File.ReadAllText(path);
                }
                else
                {
                    Debug.LogError($"[ConfigLoader] 配置文件不存在: {path}");
                    return;
                }
            }
            else
            {
                // 从 Resources 加载
                TextAsset textAsset = Resources.Load<TextAsset>(_configFileName);
                if (textAsset != null)
                {
                    jsonContent = textAsset.text;
                }
                else
                {
                    Debug.LogError($"[ConfigLoader] 无法从 Resources 加载配置: {_configFileName}");
                    return;
                }
            }
            
            // 解析 JSON
            try
            {
                _gameConfig = JsonUtility.FromJson<GameConfigData>(jsonContent);
                
                // 由于 JsonUtility 不支持 Dictionary，需要手动处理
                // 这里我们使用简化版的解析
                ParseConfigManually(jsonContent);
                
                _isLoaded = true;
                
                if (_logLoading)
                {
                    Debug.Log($"[ConfigLoader] 配置加载成功！版本: {_gameConfig?.version ?? "未知"}");
                    Debug.Log($"[ConfigLoader] 加载了 {_unitConfigs.Count} 个单位，{_buildingConfigs.Count} 个建筑");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigLoader] JSON 解析失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 手动解析配置（因为 JsonUtility 不支持 Dictionary）
        /// </summary>
        private void ParseConfigManually(string jsonContent)
        {
            // 使用 SimpleJSON 或手动解析
            // 这里我们使用一个简化的方法：利用包装类
            var wrapper = JsonUtility.FromJson<ConfigWrapper>(jsonContent);
            
            if (wrapper != null)
            {
                // 解析单位
                if (wrapper.units != null)
                {
                    foreach (var unit in wrapper.units)
                    {
                        _unitConfigs[unit.unitId] = unit;
                    }
                }
                
                // 解析建筑
                if (wrapper.buildings != null)
                {
                    foreach (var building in wrapper.buildings)
                    {
                        _buildingConfigs[building.buildingId] = building;
                    }
                }
            }
        }
        
        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfig()
        {
            _unitConfigs.Clear();
            _buildingConfigs.Clear();
            _isLoaded = false;
            LoadConfig();
        }
        
        #endregion

        #region 获取配置
        
        /// <summary>
        /// 获取单位配置
        /// </summary>
        public UnitConfigData GetUnitConfig(string unitId)
        {
            if (_unitConfigs.TryGetValue(unitId, out UnitConfigData config))
            {
                return config;
            }
            Debug.LogWarning($"[ConfigLoader] 未找到单位配置: {unitId}");
            return null;
        }
        
        /// <summary>
        /// 获取建筑配置
        /// </summary>
        public BuildingConfigData GetBuildingConfig(string buildingId)
        {
            if (_buildingConfigs.TryGetValue(buildingId, out BuildingConfigData config))
            {
                return config;
            }
            Debug.LogWarning($"[ConfigLoader] 未找到建筑配置: {buildingId}");
            return null;
        }
        
        /// <summary>
        /// 获取所有单位配置
        /// </summary>
        public List<UnitConfigData> GetAllUnitConfigs()
        {
            return new List<UnitConfigData>(_unitConfigs.Values);
        }
        
        /// <summary>
        /// 获取所有建筑配置
        /// </summary>
        public List<BuildingConfigData> GetAllBuildingConfigs()
        {
            return new List<BuildingConfigData>(_buildingConfigs.Values);
        }
        
        /// <summary>
        /// 获取指定阵营的单位
        /// </summary>
        public List<UnitConfigData> GetUnitsByFaction(string faction)
        {
            List<UnitConfigData> result = new List<UnitConfigData>();
            foreach (var unit in _unitConfigs.Values)
            {
                if (unit.faction == faction)
                {
                    result.Add(unit);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 获取伤害倍率
        /// </summary>
        public float GetDamageMultiplier(string attackType, string armorType)
        {
            if (_damageTable.TryGetValue(attackType, out var armorDict))
            {
                if (armorDict.TryGetValue(armorType, out float multiplier))
                {
                    return multiplier;
                }
            }
            return 1.0f;
        }
        
        #endregion

        #region 创建运行时数据
        
        /// <summary>
        /// 从配置创建 UnitData ScriptableObject（运行时）
        /// </summary>
        public UnitData CreateUnitDataFromConfig(string unitId)
        {
            var config = GetUnitConfig(unitId);
            if (config == null) return null;
            
            UnitData unitData = ScriptableObject.CreateInstance<UnitData>();
            
            // 基础信息
            unitData.entityId = config.unitId;
            unitData.displayName = config.displayName;
            unitData.description = config.description;
            
            // 属性
            unitData.maxHealth = config.stats.maxHealth;
            unitData.healthRegen = config.stats.healthRegen;
            unitData.attackDamage = config.stats.attackDamage;
            unitData.attackSpeed = config.stats.attackSpeed;
            unitData.attackRange = config.stats.attackRange;
            unitData.armor = config.stats.armor;
            unitData.moveSpeed = config.stats.moveSpeed;
            unitData.sightRange = config.stats.sightRange;
            
            // 攻击类型
            unitData.attackType = ParseAttackType(config.stats.attackType);
            unitData.armorType = ParseArmorType(config.stats.armorType);
            
            // 造价
            unitData.costs = new List<RTS.Data.ResourceCost>
            {
                new RTS.Data.ResourceCost(ResourceType.Wood, config.cost.wood),
                new RTS.Data.ResourceCost(ResourceType.Food, config.cost.food),
                new RTS.Data.ResourceCost(ResourceType.Gold, config.cost.gold)
            };
            
            unitData.buildTime = config.buildTime;
            unitData.populationCost = config.populationCost;
            
            // 能力
            unitData.canGather = config.abilities.canGather;
            unitData.canBuild = config.abilities.canBuild;
            unitData.canHeal = config.abilities.canHeal;
            
            // 加载预制体
            if (!string.IsNullOrEmpty(config.prefabPath))
            {
                unitData.unitPrefab = Resources.Load<GameObject>(config.prefabPath);
            }
            
            return unitData;
        }
        
        /// <summary>
        /// 从配置创建 BuildingData ScriptableObject（运行时）
        /// </summary>
        public BuildingData CreateBuildingDataFromConfig(string buildingId)
        {
            var config = GetBuildingConfig(buildingId);
            if (config == null) return null;
            
            BuildingData buildingData = ScriptableObject.CreateInstance<BuildingData>();
            
            // 基础信息
            buildingData.entityId = config.buildingId;
            buildingData.displayName = config.displayName;
            buildingData.description = config.description;
            
            // 属性
            buildingData.maxHealth = config.stats.maxHealth;
            buildingData.armor = config.stats.armor;
            buildingData.armorType = ParseArmorType(config.stats.armorType);
            
            // 尺寸
            buildingData.gridWidth = config.size.gridWidth;
            buildingData.gridHeight = config.size.gridHeight;
            
            // 造价
            buildingData.costs = new List<RTS.Data.ResourceCost>
            {
                new RTS.Data.ResourceCost(ResourceType.Wood, config.cost.wood),
                new RTS.Data.ResourceCost(ResourceType.Food, config.cost.food),
                new RTS.Data.ResourceCost(ResourceType.Gold, config.cost.gold)
            };
            
            buildingData.buildTime = config.buildTime;
            
            // 生产
            buildingData.populationProvide = config.production.populationProvide;
            buildingData.isDropOffPoint = config.production.isDropOffPoint;
            buildingData.productionQueueSize = config.production.queueSize;
            
            // 战斗
            buildingData.canAttack = config.combat.canAttack;
            buildingData.attackDamage = config.combat.attackDamage;
            buildingData.attackRange = config.combat.attackRange;
            buildingData.attackSpeed = config.combat.attackSpeed;
            
            // 加载预制体
            if (!string.IsNullOrEmpty(config.prefabPath))
            {
                buildingData.buildingPrefab = Resources.Load<GameObject>(config.prefabPath);
            }
            
            return buildingData;
        }
        
        #endregion

        #region 辅助方法
        
        private AttackType ParseAttackType(string typeStr)
        {
            switch (typeStr)
            {
                case "Normal": return AttackType.Normal;
                case "Piercing": return AttackType.Pierce;
                case "Magic": return AttackType.Magic;
                case "Siege": return AttackType.Siege;
                default: return AttackType.Normal;
            }
        }
        
        private ArmorType ParseArmorType(string typeStr)
        {
            switch (typeStr)
            {
                case "None": return ArmorType.None;
                case "Light": return ArmorType.Light;
                case "Medium": return ArmorType.Medium;
                case "Heavy": return ArmorType.Heavy;
                case "Fortified": return ArmorType.Fortified;
                default: return ArmorType.None;
            }
        }
        
        #endregion
    }

    /// <summary>
    /// JSON 包装类（用于 JsonUtility 解析数组）
    /// </summary>
    [Serializable]
    public class ConfigWrapper
    {
        public string version;
        public string lastUpdate;
        public List<UnitConfigData> units;
        public List<BuildingConfigData> buildings;
    }
}
