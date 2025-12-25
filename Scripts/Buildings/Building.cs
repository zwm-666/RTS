// ============================================================
// Building.cs
// 建筑脚本 - 使用 Domain 层数据
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.Core;
using RTS.Domain.Entities;
using RTS.Domain.Enums;
using RTS.Domain.Repositories;
using RTS.Managers;
using RTS.Units;
using RTS.Presentation;

namespace RTS.Buildings
{
    /// <summary>
    /// 建筑状态
    /// </summary>
    public enum BuildingState
    {
        UnderConstruction,  // 建造中
        Ready,              // 就绪
        Producing,          // 生产中
        Destroyed           // 已摧毁
    }

    /// <summary>
    /// 生产队列项（使用 unitId 而非 ScriptableObject 引用）
    /// </summary>
    [Serializable]
    public class ProductionQueueItem
    {
        public string unitId;
        public string displayName;
        public float remainingTime;
        public float totalTime;
        
        public ProductionQueueItem(UnitData unitData)
        {
            unitId = unitData.UnitId;
            displayName = unitData.DisplayName;
            remainingTime = unitData.BuildTime;
            totalTime = unitData.BuildTime;
        }
        
        public float Progress => totalTime > 0 ? 1f - (remainingTime / totalTime) : 1f;
    }

    /// <summary>
    /// 建筑脚本 - 挂载在建筑预制体上
    /// 实现 IEntityInitializable 接口
    /// </summary>
    public class Building : MonoBehaviour, IEntityInitializable
    {
        #region 事件
        
        /// <summary>
        /// 建筑摧毁事件
        /// </summary>
        public event Action<Building> OnDestroyed;
        
        /// <summary>
        /// 生命值变化事件
        /// </summary>
        public event Action<int, int> OnHealthChanged;
        
        /// <summary>
        /// 生产完成事件
        /// </summary>
        public event Action<Unit> OnUnitProduced;
        
        /// <summary>
        /// 生产队列变化事件
        /// </summary>
        public event Action<List<ProductionQueueItem>> OnQueueChanged;
        
        #endregion

        #region 序列化字段
        
        [Header("建筑ID（用于从Repository获取数据）")]
        [SerializeField] private string _buildingId;
        
        [Header("所属玩家")]
        [SerializeField] private int _playerId = 0;
        
        [Header("当前状态")]
        [SerializeField] private BuildingState _currentState = BuildingState.Ready;
        
        [Header("集结点")]
        [SerializeField] private Transform _rallyPoint;
        
        [Header("生成点偏移")]
        [SerializeField] private Vector3 _spawnPointOffset = Vector3.forward * 2f;
        
        [Header("集结点偏移")]
        [SerializeField] private Vector3 _rallyPointOffset = Vector3.forward * 3f;
        
        #endregion

        #region 领域数据
        
        private BuildingData _domainData;
        
        #endregion

        #region 运行时属性
        
        private int _currentHealth;
        private List<ProductionQueueItem> _productionQueue = new List<ProductionQueueItem>();
        private float _constructionProgress = 0f;
        
        #endregion

        #region 属性访问器
        
        public string BuildingId => _buildingId;
        public BuildingData DomainData => _domainData;
        public int PlayerId => _playerId;
        public BuildingState CurrentState => _currentState;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _domainData?.MaxHealth ?? 0;
        public bool IsAlive => _currentHealth > 0;
        public List<ProductionQueueItem> ProductionQueue => _productionQueue;
        public int QueueCount => _productionQueue.Count;
        public Vector3 RallyPointPosition => _rallyPoint != null 
            ? _rallyPoint.position 
            : transform.position + _rallyPointOffset;
        
        // 兼容旧代码
        public string DisplayName => _domainData?.DisplayName ?? "Unknown";
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            // 如果有预设的 buildingId，尝试从 Repository 获取数据
            if (!string.IsNullOrEmpty(_buildingId) && _domainData == null)
            {
                TryLoadFromRepository();
            }
        }
        
        private void Update()
        {
            if (_currentState == BuildingState.Destroyed) return;
            
            // 处理生产队列
            if (_currentState == BuildingState.Ready || _currentState == BuildingState.Producing)
            {
                ProcessProductionQueue();
            }
        }
        
        #endregion

        #region IEntityInitializable 实现
        
        public void InitializeWithData(object data, int playerId)
        {
            if (data is BuildingData buildingData)
            {
                _domainData = buildingData;
                _buildingId = buildingData.BuildingId;
                _playerId = playerId;
                InitializeFromDomainData();
            }
            else
            {
                Debug.LogError($"[Building] InitializeWithData 收到错误类型: {data?.GetType().Name}");
            }
        }
        
        #endregion

        #region 初始化
        
        private void TryLoadFromRepository()
        {
            if (ServiceLocator.TryGet<IBuildingRepository>(out var repo))
            {
                _domainData = repo.GetById(_buildingId);
                if (_domainData != null)
                {
                    InitializeFromDomainData();
                }
                else
                {
                    Debug.LogWarning($"[Building] 从 Repository 找不到建筑: {_buildingId}");
                }
            }
        }
        
        /// <summary>
        /// 从领域数据初始化建筑属性
        /// </summary>
        private void InitializeFromDomainData()
        {
            if (_domainData == null)
            {
                Debug.LogError($"[Building] {gameObject.name} 缺少领域数据！");
                return;
            }
            
            _currentHealth = _domainData.MaxHealth;
            _currentState = BuildingState.Ready;
            
            // 创建默认集结点
            if (_rallyPoint == null)
            {
                GameObject rallyObj = new GameObject("RallyPoint");
                rallyObj.transform.SetParent(transform);
                rallyObj.transform.localPosition = _rallyPointOffset;
                _rallyPoint = rallyObj.transform;
            }
            
            Debug.Log($"[Building] {_domainData.DisplayName} 已初始化 - 生命:{_currentHealth}");
        }
        
        /// <summary>
        /// 设置建筑数据（兼容旧代码）
        /// </summary>
        public void SetBuildingData(BuildingData data, int playerId)
        {
            _domainData = data;
            _buildingId = data?.BuildingId;
            _playerId = playerId;
            InitializeFromDomainData();
        }
        
        #endregion

        #region 生产系统
        
        /// <summary>
        /// 生产单位（通过 unitId 添加到生产队列）
        /// </summary>
        public bool ProduceUnit(string unitId)
        {
            if (_domainData == null || string.IsNullOrEmpty(unitId))
            {
                Debug.LogWarning("[Building] 建筑数据或单位ID为空！");
                return false;
            }
            
            // 检查状态
            if (_currentState == BuildingState.Destroyed || _currentState == BuildingState.UnderConstruction)
            {
                Debug.LogWarning($"[Building] 建筑状态不允许生产：{_currentState}");
                return false;
            }
            
            // 检查是否是该建筑可生产的单位
            if (!_domainData.CanProduceUnit(unitId))
            {
                Debug.LogWarning($"[Building] {_domainData.DisplayName} 无法生产 {unitId}");
                return false;
            }
            
            // 检查生产队列是否已满
            if (_productionQueue.Count >= _domainData.QueueSize)
            {
                Debug.LogWarning($"[Building] 生产队列已满！({_productionQueue.Count}/{_domainData.QueueSize})");
                return false;
            }
            
            // 从 Repository 获取单位数据
            if (!ServiceLocator.TryGet<IUnitRepository>(out var unitRepo))
            {
                Debug.LogError("[Building] 无法获取 IUnitRepository");
                return false;
            }
            
            var unitData = unitRepo.GetById(unitId);
            if (unitData == null)
            {
                Debug.LogError($"[Building] 找不到单位数据: {unitId}");
                return false;
            }
            
            // 检查资源是否足够
            var cost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Wood, unitData.WoodCost },
                { ResourceType.Food, unitData.FoodCost },
                { ResourceType.Gold, unitData.GoldCost }
            };
            
            if (!ResourceManager.Instance.CanAfford(_playerId, cost))
            {
                Debug.LogWarning($"[Building] 玩家 {_playerId} 资源不足，无法生产 {unitData.DisplayName}");
                return false;
            }
            
            // 扣除资源
            if (!ResourceManager.Instance.SpendResource(_playerId, cost))
            {
                return false;
            }
            
            // 添加到生产队列
            var queueItem = new ProductionQueueItem(unitData);
            _productionQueue.Add(queueItem);
            
            Debug.Log($"[Building] {unitData.DisplayName} 已加入生产队列（{_productionQueue.Count}/{_domainData.QueueSize}）");
            
            // 触发队列变化事件
            OnQueueChanged?.Invoke(_productionQueue);
            
            return true;
        }
        
        /// <summary>
        /// 取消生产队列中的项目
        /// </summary>
        public bool CancelProduction(int index)
        {
            if (index < 0 || index >= _productionQueue.Count)
            {
                return false;
            }
            
            var item = _productionQueue[index];
            
            // 从 Repository 获取单位数据以返还资源
            if (ServiceLocator.TryGet<IUnitRepository>(out var unitRepo))
            {
                var unitData = unitRepo.GetById(item.unitId);
                if (unitData != null)
                {
                    // 返还资源（75%）
                    float refundRate = 0.75f;
                    ResourceManager.Instance.AddResource(_playerId, ResourceType.Wood, 
                        Mathf.RoundToInt(unitData.WoodCost * refundRate));
                    ResourceManager.Instance.AddResource(_playerId, ResourceType.Food, 
                        Mathf.RoundToInt(unitData.FoodCost * refundRate));
                    ResourceManager.Instance.AddResource(_playerId, ResourceType.Gold, 
                        Mathf.RoundToInt(unitData.GoldCost * refundRate));
                    
                    Debug.Log($"[Building] 取消生产 {item.displayName}，已返还 {refundRate * 100}% 资源");
                }
            }
            
            _productionQueue.RemoveAt(index);
            
            // 更新状态
            if (_productionQueue.Count == 0)
            {
                _currentState = BuildingState.Ready;
            }
            
            OnQueueChanged?.Invoke(_productionQueue);
            
            return true;
        }
        
        /// <summary>
        /// 处理生产队列
        /// </summary>
        private void ProcessProductionQueue()
        {
            if (_productionQueue.Count == 0)
            {
                _currentState = BuildingState.Ready;
                return;
            }
            
            _currentState = BuildingState.Producing;
            
            // 处理队列首项
            var currentItem = _productionQueue[0];
            currentItem.remainingTime -= Time.deltaTime;
            
            // 生产完成
            if (currentItem.remainingTime <= 0)
            {
                SpawnUnit(currentItem.unitId);
                _productionQueue.RemoveAt(0);
                
                OnQueueChanged?.Invoke(_productionQueue);
                
                Debug.Log($"[Building] {currentItem.displayName} 生产完成！剩余队列: {_productionQueue.Count}");
            }
        }
        
        /// <summary>
        /// 生成单位（通过 EntitySpawner）
        /// </summary>
        private void SpawnUnit(string unitId)
        {
            // 计算生成位置
            Vector3 spawnPosition = transform.position + _spawnPointOffset;
            
            // 使用 EntitySpawner 生成单位
            if (EntitySpawner.Instance != null)
            {
                var unitObj = EntitySpawner.Instance.SpawnUnit(unitId, spawnPosition, _playerId);
                
                if (unitObj != null)
                {
                    var unit = unitObj.GetComponent<Unit>();
                    if (unit != null)
                    {
                        OnUnitProduced?.Invoke(unit);
                        
                        // TODO: 让单位移动到集结点
                        // unit.MoveTo(RallyPointPosition);
                    }
                }
            }
            else
            {
                Debug.LogError("[Building] EntitySpawner 未初始化！");
            }
        }
        
        #endregion

        #region 建筑生命系统
        
        /// <summary>
        /// 建筑受到伤害
        /// </summary>
        public int TakeDamage(int damage, AttackType attackType = AttackType.Normal)
        {
            if (!IsAlive) return 0;
            
            // 计算实际伤害（建筑通常是城防护甲）
            float multiplier = attackType == AttackType.Siege ? 2.0f : 1.0f;
            int actualDamage = Mathf.Max(1, Mathf.RoundToInt(damage * multiplier) - (_domainData?.Armor ?? 0));
            
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
            
            Debug.Log($"[Building] {_domainData?.DisplayName} 受到 {actualDamage} 点伤害，剩余生命: {_currentHealth}/{_domainData?.MaxHealth}");
            
            OnHealthChanged?.Invoke(_currentHealth, _domainData?.MaxHealth ?? 0);
            
            if (_currentHealth <= 0)
            {
                DestroyBuilding();
            }
            
            return actualDamage;
        }
        
        /// <summary>
        /// 修复建筑
        /// </summary>
        public void Repair(int amount)
        {
            if (!IsAlive || _domainData == null) return;
            
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_domainData.MaxHealth, _currentHealth + amount);
            
            if (_currentHealth != oldHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, _domainData.MaxHealth);
            }
        }
        
        /// <summary>
        /// 摧毁建筑
        /// </summary>
        private void DestroyBuilding()
        {
            _currentState = BuildingState.Destroyed;
            
            // 取消所有生产并返还资源
            while (_productionQueue.Count > 0)
            {
                CancelProduction(0);
            }
            
            Debug.Log($"[Building] {_domainData?.DisplayName} 已被摧毁！");
            
            OnDestroyed?.Invoke(this);
            
            // 延迟销毁
            Destroy(gameObject, 2f);
        }
        
        #endregion

        #region 设置集结点
        
        /// <summary>
        /// 设置集结点位置
        /// </summary>
        public void SetRallyPoint(Vector3 position)
        {
            if (_rallyPoint != null)
            {
                _rallyPoint.position = position;
            }
            Debug.Log($"[Building] 集结点已设置到 {position}");
        }
        
        #endregion

        #region 调试
        
        private void OnDrawGizmosSelected()
        {
            if (_domainData == null) return;
            
            // 显示攻击范围（如果有攻击能力）
            if (_domainData.CanAttack)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _domainData.AttackRange);
            }
            
            // 显示集结点
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(RallyPointPosition, 0.5f);
            Gizmos.DrawLine(transform.position, RallyPointPosition);
            
            // 显示生成点
            Gizmos.color = Color.cyan;
            Vector3 spawnPos = transform.position + _spawnPointOffset;
            Gizmos.DrawWireSphere(spawnPos, 0.3f);
        }
        
        #endregion
    }
}
