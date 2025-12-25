// ============================================================
// GameBootstrap.cs - 游戏启动引导器
// ============================================================

using UnityEngine;
using RTS.Config;
using RTS.Domain.Repositories;
using RTS.Presentation;

namespace RTS.Core
{
    /// <summary>
    /// 游戏启动引导器
    /// 负责按顺序初始化各层，建立依赖关系
    /// 挂载位置：场景中的 GameBootstrap 对象（执行顺序设为 -100）
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        #region 场景引用
        
        [Header("场景组件引用")]
        [SerializeField] private PrefabBinder _prefabBinder;
        [SerializeField] private EntitySpawner _entitySpawner;
        
        [Header("配置")]
        [SerializeField] private string _configPath = "Config/GameConfig";
        [SerializeField] private bool _useStreamingAssets = false;
        
        [Header("调试")]
        [SerializeField] private bool _logInitialization = true;
        
        #endregion

        #region 私有字段
        
        private UnitRepository _unitRepository;
        private BuildingRepository _buildingRepository;
        private bool _isInitialized = false;
        
        #endregion

        #region 属性
        
        public bool IsInitialized => _isInitialized;
        public IUnitRepository UnitRepository => _unitRepository;
        public IBuildingRepository BuildingRepository => _buildingRepository;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            Initialize();
        }
        
        #endregion

        #region 初始化流程
        
        /// <summary>
        /// 执行完整的初始化流程
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[GameBootstrap] 已经初始化过");
                return;
            }
            
            Log("========== 开始初始化 ==========");
            
            // 1. 初始化配置层
            if (!InitializeConfigLayer())
            {
                Debug.LogError("[GameBootstrap] 配置层初始化失败！");
                return;
            }
            
            // 2. 初始化领域层
            InitializeDomainLayer();
            
            // 3. 初始化表现层
            InitializePresentationLayer();
            
            _isInitialized = true;
            Log("========== 初始化完成 ==========");
        }
        
        /// <summary>
        /// 1. 初始化配置层：加载JSON，创建领域对象
        /// </summary>
        private bool InitializeConfigLayer()
        {
            Log("初始化配置层...");
            
            // 加载配置
            var loader = new ConfigLoader();
            GameConfigDto configData;
            
            if (_useStreamingAssets)
            {
                configData = loader.LoadFromStreamingAssets(_configPath + ".json");
            }
            else
            {
                configData = loader.LoadFromResources(_configPath);
            }
            
            if (configData == null)
            {
                return false;
            }
            
            // 校验配置
            if (!loader.ValidateConfig(configData))
            {
                Debug.LogWarning("[GameBootstrap] 配置校验有警告，但继续初始化");
            }
            
            // 创建工厂
            var factory = new ConfigToDomainFactory();
            
            // 创建 Repository
            _unitRepository = new UnitRepository();
            _buildingRepository = new BuildingRepository();
            
            // 填充 Repository
            if (configData.units != null)
            {
                foreach (var dto in configData.units)
                {
                    var unitData = factory.CreateUnitData(dto);
                    if (unitData != null)
                    {
                        _unitRepository.Register(unitData);
                    }
                }
            }
            
            if (configData.buildings != null)
            {
                foreach (var dto in configData.buildings)
                {
                    var buildingData = factory.CreateBuildingData(dto);
                    if (buildingData != null)
                    {
                        _buildingRepository.Register(buildingData);
                    }
                }
            }
            
            Log($"配置层初始化完成: {_unitRepository.Count} 个单位, {_buildingRepository.Count} 个建筑");
            return true;
        }
        
        /// <summary>
        /// 2. 初始化领域层：注册 Repository 到 ServiceLocator
        /// </summary>
        private void InitializeDomainLayer()
        {
            Log("初始化领域层...");
            
            // 注册 Repository 为全局服务
            ServiceLocator.Register<IUnitRepository>(_unitRepository);
            ServiceLocator.Register<IBuildingRepository>(_buildingRepository);
            
            Log("领域层初始化完成");
        }
        
        /// <summary>
        /// 3. 初始化表现层：连接 PrefabBinder 和 EntitySpawner
        /// </summary>
        private void InitializePresentationLayer()
        {
            Log("初始化表现层...");
            
            // 自动查找组件（如果未手动指定）
            if (_prefabBinder == null)
            {
                _prefabBinder = FindObjectOfType<PrefabBinder>();
            }
            
            if (_entitySpawner == null)
            {
                _entitySpawner = FindObjectOfType<EntitySpawner>();
            }
            
            // 注册 PrefabProvider
            if (_prefabBinder != null)
            {
                ServiceLocator.Register<IPrefabProvider>(_prefabBinder);
                Log($"PrefabBinder 已注册: {_prefabBinder.UnitPrefabCount} 单位, {_prefabBinder.BuildingPrefabCount} 建筑");
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] 未找到 PrefabBinder");
            }
            
            // 初始化 EntitySpawner
            if (_entitySpawner != null)
            {
                _entitySpawner.Initialize(_unitRepository, _buildingRepository, _prefabBinder);
                ServiceLocator.Register<EntitySpawner>(_entitySpawner);
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] 未找到 EntitySpawner");
            }
            
            Log("表现层初始化完成");
        }
        
        #endregion

        #region 辅助方法
        
        private void Log(string message)
        {
            if (_logInitialization)
            {
                Debug.Log($"[GameBootstrap] {message}");
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 重新加载配置（用于热更新）
        /// </summary>
        public void ReloadConfig()
        {
            Debug.Log("[GameBootstrap] 重新加载配置...");
            
            // 清空现有数据
            _unitRepository?.Clear();
            _buildingRepository?.Clear();
            ServiceLocator.Clear();
            
            // 重置状态
            _isInitialized = false;
            
            // 重新初始化
            Initialize();
            
            // 通知 PrefabBinder 重新初始化
            _prefabBinder?.Reinitialize();
        }
        
        #endregion
    }
}
