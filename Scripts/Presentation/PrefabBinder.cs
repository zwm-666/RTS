// ============================================================
// PrefabBinder.cs - IPrefabProvider 的标准实现
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace RTS.Presentation
{
    /// <summary>
    /// 预制体绑定器
    /// 实现 IPrefabProvider 接口
    /// 在 Awake 时将 PrefabRegistry 转换为字典，提供快速查找
    /// </summary>
    public class PrefabBinder : MonoBehaviour, IPrefabProvider
    {
        #region 单例
        
        private static PrefabBinder _instance;
        
        /// <summary>
        /// 单例实例（类型为接口）
        /// </summary>
        public static IPrefabProvider Instance => _instance;
        
        /// <summary>
        /// 具体实例
        /// </summary>
        public static PrefabBinder InstanceConcrete => _instance;
        
        #endregion

        #region 配置
        
        [Header("预制体注册表")]
        [SerializeField] private PrefabRegistry _registry;
        
        [Header("调试")]
        [SerializeField] private bool _logOnBuild = true;
        
        #endregion

        #region 私有字段
        
        private Dictionary<string, GameObject> _unitPrefabs;
        private Dictionary<string, GameObject> _buildingPrefabs;
        private bool _isInitialized = false;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[PrefabBinder] 已存在实例，销毁重复对象");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            Initialize();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region 初始化
        
        /// <summary>
        /// 初始化（构建查找字典）
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            _unitPrefabs = new Dictionary<string, GameObject>();
            _buildingPrefabs = new Dictionary<string, GameObject>();
            
            if (_registry == null)
            {
                Debug.LogError("[PrefabBinder] PrefabRegistry 未配置！");
                return;
            }
            
            // 构建单位字典
            foreach (var entry in _registry.UnitPrefabs)
            {
                if (entry.IsValid)
                {
                    if (_unitPrefabs.ContainsKey(entry.id))
                    {
                        Debug.LogWarning($"[PrefabBinder] 单位ID重复: {entry.id}");
                    }
                    _unitPrefabs[entry.id] = entry.prefab;
                }
            }
            
            // 构建建筑字典
            foreach (var entry in _registry.BuildingPrefabs)
            {
                if (entry.IsValid)
                {
                    if (_buildingPrefabs.ContainsKey(entry.id))
                    {
                        Debug.LogWarning($"[PrefabBinder] 建筑ID重复: {entry.id}");
                    }
                    _buildingPrefabs[entry.id] = entry.prefab;
                }
            }
            
            _isInitialized = true;
            
            if (_logOnBuild)
            {
                Debug.Log($"[PrefabBinder] 初始化完成: {_unitPrefabs.Count} 个单位, {_buildingPrefabs.Count} 个建筑");
            }
        }
        
        #endregion

        #region IPrefabProvider 实现
        
        public GameObject GetUnitPrefab(string unitId)
        {
            if (!_isInitialized) Initialize();
            
            if (string.IsNullOrEmpty(unitId))
            {
                Debug.LogWarning("[PrefabBinder] unitId 为空");
                return null;
            }
            
            return _unitPrefabs.TryGetValue(unitId, out var prefab) ? prefab : null;
        }
        
        public GameObject GetBuildingPrefab(string buildingId)
        {
            if (!_isInitialized) Initialize();
            
            if (string.IsNullOrEmpty(buildingId))
            {
                Debug.LogWarning("[PrefabBinder] buildingId 为空");
                return null;
            }
            
            return _buildingPrefabs.TryGetValue(buildingId, out var prefab) ? prefab : null;
        }
        
        public bool HasUnitPrefab(string unitId)
        {
            if (!_isInitialized) Initialize();
            return !string.IsNullOrEmpty(unitId) && _unitPrefabs.ContainsKey(unitId);
        }
        
        public bool HasBuildingPrefab(string buildingId)
        {
            if (!_isInitialized) Initialize();
            return !string.IsNullOrEmpty(buildingId) && _buildingPrefabs.ContainsKey(buildingId);
        }
        
        public int UnitPrefabCount => _unitPrefabs?.Count ?? 0;
        
        public int BuildingPrefabCount => _buildingPrefabs?.Count ?? 0;
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 获取注册表引用
        /// </summary>
        public PrefabRegistry GetRegistry() => _registry;
        
        /// <summary>
        /// 重新初始化（用于热重载）
        /// </summary>
        public void Reinitialize()
        {
            _isInitialized = false;
            _unitPrefabs?.Clear();
            _buildingPrefabs?.Clear();
            Initialize();
        }
        
        #endregion
    }
}
