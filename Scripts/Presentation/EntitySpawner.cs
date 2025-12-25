// ============================================================
// EntitySpawner.cs - 实体生成器（唯一实例化入口）
// ============================================================

using UnityEngine;
using RTS.Domain.Entities;
using RTS.Domain.Repositories;

namespace RTS.Presentation
{
    /// <summary>
    /// 实体生成器
    /// 唯一的实例化入口，遵循流程：领域数据 → 取出ID → 查找Prefab → 实例化
    /// </summary>
    public class EntitySpawner : MonoBehaviour
    {
        #region 单例
        
        private static EntitySpawner _instance;
        public static EntitySpawner Instance => _instance;
        
        #endregion

        #region 依赖
        
        private IUnitRepository _unitRepository;
        private IBuildingRepository _buildingRepository;
        private IPrefabProvider _prefabProvider;
        
        #endregion

        #region 配置
        
        [Header("调试")]
        [SerializeField] private bool _logSpawn = true;
        
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
        /// 注入依赖（由 GameBootstrap 调用）
        /// </summary>
        public void Initialize(
            IUnitRepository unitRepository,
            IBuildingRepository buildingRepository,
            IPrefabProvider prefabProvider)
        {
            _unitRepository = unitRepository;
            _buildingRepository = buildingRepository;
            _prefabProvider = prefabProvider;
            
            Debug.Log("[EntitySpawner] 初始化完成");
        }
        
        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public bool IsInitialized => _unitRepository != null && _buildingRepository != null && _prefabProvider != null;
        
        #endregion

        #region 单位生成
        
        /// <summary>
        /// 生成单位（主要入口）
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">朝向</param>
        /// <param name="playerId">所属玩家ID</param>
        /// <returns>生成的单位GameObject，失败返回null</returns>
        public GameObject SpawnUnit(string unitId, Vector3 position, Quaternion rotation, int playerId)
        {
            // 1. 验证依赖
            if (!IsInitialized)
            {
                Debug.LogError("[EntitySpawner] 未初始化");
                return null;
            }
            
            // 2. 从 Repository 获取领域数据
            var unitData = _unitRepository.GetById(unitId);
            if (unitData == null)
            {
                Debug.LogError($"[EntitySpawner] 单位数据不存在: {unitId}");
                return null;
            }
            
            // 3. 从 PrefabProvider 获取预制体（使用领域对象的ID）
            var prefab = _prefabProvider.GetUnitPrefab(unitData.UnitId);
            if (prefab == null)
            {
                Debug.LogError($"[EntitySpawner] 单位预制体不存在: {unitId}");
                return null;
            }
            
            // 4. 实例化
            GameObject obj = Instantiate(prefab, position, rotation);
            obj.name = $"{unitData.DisplayName}_{playerId}";
            
            // 5. 初始化组件（查找并调用初始化方法）
            var initializable = obj.GetComponent<IEntityInitializable>();
            if (initializable != null)
            {
                initializable.InitializeWithData(unitData, playerId);
            }
            
            if (_logSpawn)
            {
                Debug.Log($"[EntitySpawner] 生成单位: {unitData.DisplayName} @ {position}");
            }
            
            return obj;
        }
        
        /// <summary>
        /// 生成单位（简化版，使用默认朝向）
        /// </summary>
        public GameObject SpawnUnit(string unitId, Vector3 position, int playerId)
        {
            return SpawnUnit(unitId, position, Quaternion.identity, playerId);
        }
        
        #endregion

        #region 建筑生成
        
        /// <summary>
        /// 生成建筑（主要入口）
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">朝向</param>
        /// <param name="playerId">所属玩家ID</param>
        /// <returns>生成的建筑GameObject，失败返回null</returns>
        public GameObject SpawnBuilding(string buildingId, Vector3 position, Quaternion rotation, int playerId)
        {
            // 1. 验证依赖
            if (!IsInitialized)
            {
                Debug.LogError("[EntitySpawner] 未初始化");
                return null;
            }
            
            // 2. 从 Repository 获取领域数据
            var buildingData = _buildingRepository.GetById(buildingId);
            if (buildingData == null)
            {
                Debug.LogError($"[EntitySpawner] 建筑数据不存在: {buildingId}");
                return null;
            }
            
            // 3. 从 PrefabProvider 获取预制体
            var prefab = _prefabProvider.GetBuildingPrefab(buildingData.BuildingId);
            if (prefab == null)
            {
                Debug.LogError($"[EntitySpawner] 建筑预制体不存在: {buildingId}");
                return null;
            }
            
            // 4. 实例化
            GameObject obj = Instantiate(prefab, position, rotation);
            obj.name = $"{buildingData.DisplayName}_{playerId}";
            
            // 5. 初始化组件
            var initializable = obj.GetComponent<IEntityInitializable>();
            if (initializable != null)
            {
                initializable.InitializeWithData(buildingData, playerId);
            }
            
            if (_logSpawn)
            {
                Debug.Log($"[EntitySpawner] 生成建筑: {buildingData.DisplayName} @ {position}");
            }
            
            return obj;
        }
        
        /// <summary>
        /// 生成建筑（简化版，使用默认朝向）
        /// </summary>
        public GameObject SpawnBuilding(string buildingId, Vector3 position, int playerId)
        {
            return SpawnBuilding(buildingId, position, Quaternion.identity, playerId);
        }
        
        #endregion
    }
    
    /// <summary>
    /// 实体初始化接口
    /// 预制体上的组件可以实现此接口来接收领域数据
    /// </summary>
    public interface IEntityInitializable
    {
        void InitializeWithData(object data, int playerId);
    }
}
