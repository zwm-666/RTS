// ============================================================
// PrefabBinder.cs
// 预制体绑定器 - 将 JSON 配置绑定到实际预制体
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using RTS.Data;
using RTS.Units;
using RTS.Buildings;

namespace RTS.Config
{
    /// <summary>
    /// 预制体绑定器
    /// 负责在编辑器中将 JSON 配置数据绑定到预制体
    /// 挂载位置：场景中的配置管理对象上
    /// </summary>
    public class PrefabBinder : MonoBehaviour
    {
        #region 配置
        
        [Header("预制体映射")]
        [SerializeField] private List<UnitPrefabEntry> _unitPrefabs = new List<UnitPrefabEntry>();
        [SerializeField] private List<BuildingPrefabEntry> _buildingPrefabs = new List<BuildingPrefabEntry>();
        
        [Header("自动绑定")]
        [SerializeField] private bool _autoBindOnStart = true;
        
        #endregion

        #region Unity 生命周期
        
        private void Start()
        {
            if (_autoBindOnStart)
            {
                BindAllPrefabs();
            }
        }
        
        #endregion

        #region 绑定方法
        
        /// <summary>
        /// 绑定所有预制体
        /// </summary>
        public void BindAllPrefabs()
        {
            if (ConfigLoader.Instance == null || !ConfigLoader.Instance.IsLoaded)
            {
                Debug.LogWarning("[PrefabBinder] ConfigLoader 未加载！");
                return;
            }
            
            BindUnitPrefabs();
            BindBuildingPrefabs();
            
            Debug.Log($"[PrefabBinder] 绑定完成！单位: {_unitPrefabs.Count}, 建筑: {_buildingPrefabs.Count}");
        }
        
        /// <summary>
        /// 绑定单位预制体
        /// </summary>
        private void BindUnitPrefabs()
        {
            foreach (var entry in _unitPrefabs)
            {
                if (string.IsNullOrEmpty(entry.unitId) || entry.prefab == null)
                {
                    continue;
                }
                
                var config = ConfigLoader.Instance.GetUnitConfig(entry.unitId);
                if (config == null)
                {
                    Debug.LogWarning($"[PrefabBinder] 未找到单位配置: {entry.unitId}");
                    continue;
                }
                
                // 检查预制体上是否有 Unit 组件
                Unit unitComponent = entry.prefab.GetComponent<Unit>();
                if (unitComponent == null)
                {
                    Debug.LogWarning($"[PrefabBinder] 预制体 {entry.prefab.name} 缺少 Unit 组件！");
                    continue;
                }
                
                // 创建运行时 UnitData 并赋值
                UnitData unitData = ConfigLoader.Instance.CreateUnitDataFromConfig(entry.unitId);
                if (unitData != null)
                {
                    unitData.unitPrefab = entry.prefab;
                    entry.runtimeData = unitData;
                    
                    Debug.Log($"[PrefabBinder] 绑定单位: {entry.unitId} -> {entry.prefab.name}");
                }
            }
        }
        
        /// <summary>
        /// 绑定建筑预制体
        /// </summary>
        private void BindBuildingPrefabs()
        {
            foreach (var entry in _buildingPrefabs)
            {
                if (string.IsNullOrEmpty(entry.buildingId) || entry.prefab == null)
                {
                    continue;
                }
                
                var config = ConfigLoader.Instance.GetBuildingConfig(entry.buildingId);
                if (config == null)
                {
                    Debug.LogWarning($"[PrefabBinder] 未找到建筑配置: {entry.buildingId}");
                    continue;
                }
                
                // 检查预制体上是否有 Building 组件
                Building buildingComponent = entry.prefab.GetComponent<Building>();
                if (buildingComponent == null)
                {
                    Debug.LogWarning($"[PrefabBinder] 预制体 {entry.prefab.name} 缺少 Building 组件！");
                    continue;
                }
                
                // 创建运行时 BuildingData 并赋值
                BuildingData buildingData = ConfigLoader.Instance.CreateBuildingDataFromConfig(entry.buildingId);
                if (buildingData != null)
                {
                    buildingData.buildingPrefab = entry.prefab;
                    entry.runtimeData = buildingData;
                    
                    Debug.Log($"[PrefabBinder] 绑定建筑: {entry.buildingId} -> {entry.prefab.name}");
                }
            }
        }
        
        #endregion

        #region 获取绑定数据
        
        /// <summary>
        /// 获取单位的运行时数据
        /// </summary>
        public UnitData GetUnitData(string unitId)
        {
            foreach (var entry in _unitPrefabs)
            {
                if (entry.unitId == unitId)
                {
                    return entry.runtimeData;
                }
            }
            
            // 尝试动态创建
            return ConfigLoader.Instance?.CreateUnitDataFromConfig(unitId);
        }
        
        /// <summary>
        /// 获取建筑的运行时数据
        /// </summary>
        public BuildingData GetBuildingData(string buildingId)
        {
            foreach (var entry in _buildingPrefabs)
            {
                if (entry.buildingId == buildingId)
                {
                    return entry.runtimeData;
                }
            }
            
            // 尝试动态创建
            return ConfigLoader.Instance?.CreateBuildingDataFromConfig(buildingId);
        }
        
        /// <summary>
        /// 获取单位预制体
        /// </summary>
        public GameObject GetUnitPrefab(string unitId)
        {
            foreach (var entry in _unitPrefabs)
            {
                if (entry.unitId == unitId)
                {
                    return entry.prefab;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 获取建筑预制体
        /// </summary>
        public GameObject GetBuildingPrefab(string buildingId)
        {
            foreach (var entry in _buildingPrefabs)
            {
                if (entry.buildingId == buildingId)
                {
                    return entry.prefab;
                }
            }
            return null;
        }
        
        #endregion

        #region 实例化方法
        
        /// <summary>
        /// 从配置实例化单位
        /// </summary>
        public Unit SpawnUnit(string unitId, Vector3 position, int playerId)
        {
            var unitData = GetUnitData(unitId);
            var prefab = GetUnitPrefab(unitId);
            
            if (prefab == null)
            {
                Debug.LogError($"[PrefabBinder] 无法生成单位 {unitId}：预制体不存在");
                return null;
            }
            
            GameObject unitObj = Instantiate(prefab, position, Quaternion.identity);
            Unit unit = unitObj.GetComponent<Unit>();
            
            if (unit != null && unitData != null)
            {
                unit.SetUnitData(unitData, playerId);
            }
            
            return unit;
        }
        
        /// <summary>
        /// 从配置实例化建筑
        /// </summary>
        public Building SpawnBuilding(string buildingId, Vector3 position, int playerId)
        {
            var buildingData = GetBuildingData(buildingId);
            var prefab = GetBuildingPrefab(buildingId);
            
            if (prefab == null)
            {
                Debug.LogError($"[PrefabBinder] 无法生成建筑 {buildingId}：预制体不存在");
                return null;
            }
            
            GameObject buildingObj = Instantiate(prefab, position, Quaternion.identity);
            Building building = buildingObj.GetComponent<Building>();
            
            if (building != null && buildingData != null)
            {
                building.SetBuildingData(buildingData, playerId);
            }
            
            return building;
        }
        
        #endregion
    }

    /// <summary>
    /// 单位预制体映射条目
    /// </summary>
    [System.Serializable]
    public class UnitPrefabEntry
    {
        public string unitId;
        public GameObject prefab;
        [HideInInspector] public UnitData runtimeData;
    }

    /// <summary>
    /// 建筑预制体映射条目
    /// </summary>
    [System.Serializable]
    public class BuildingPrefabEntry
    {
        public string buildingId;
        public GameObject prefab;
        [HideInInspector] public BuildingData runtimeData;
    }
}
