// ============================================================
// Building.cs
// 建筑脚本
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS.Core;
using RTS.Data;
using RTS.Managers;
using RTS.Units;

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
    /// 生产队列项
    /// </summary>
    [Serializable]
    public class ProductionQueueItem
    {
        public UnitData unitData;
        public float remainingTime;
        
        public ProductionQueueItem(UnitData data)
        {
            unitData = data;
            remainingTime = data.buildTime;
        }
    }

    /// <summary>
    /// 建筑脚本 - 挂载在建筑预制体上
    /// </summary>
    public class Building : MonoBehaviour
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
        
        [Header("建筑数据")]
        [SerializeField] private BuildingData _buildingData;
        
        [Header("所属玩家")]
        [SerializeField] private int _playerId = 0;
        
        [Header("当前状态")]
        [SerializeField] private BuildingState _currentState = BuildingState.Ready;
        
        [Header("集结点")]
        [SerializeField] private Transform _rallyPoint;
        
        #endregion

        #region 运行时属性
        
        private int _currentHealth;
        private List<ProductionQueueItem> _productionQueue = new List<ProductionQueueItem>();
        private float _constructionProgress = 0f;
        
        #endregion

        #region 属性访问器
        
        public BuildingData BuildingData => _buildingData;
        public int PlayerId => _playerId;
        public BuildingState CurrentState => _currentState;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _buildingData != null ? _buildingData.maxHealth : 0;
        public bool IsAlive => _currentHealth > 0;
        public List<ProductionQueueItem> ProductionQueue => _productionQueue;
        public int QueueCount => _productionQueue.Count;
        public Vector3 RallyPointPosition => _rallyPoint != null 
            ? _rallyPoint.position 
            : transform.position + (_buildingData?.rallyPointOffset ?? Vector3.forward * 3f);
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            InitializeFromData();
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

        #region 初始化
        
        /// <summary>
        /// 从 BuildingData 初始化建筑属性
        /// </summary>
        public void InitializeFromData()
        {
            if (_buildingData == null)
            {
                Debug.LogError($"[Building] {gameObject.name} 缺少 BuildingData！");
                return;
            }
            
            _currentHealth = _buildingData.maxHealth;
            _currentState = BuildingState.Ready;
            
            // 创建默认集结点
            if (_rallyPoint == null)
            {
                GameObject rallyObj = new GameObject("RallyPoint");
                rallyObj.transform.SetParent(transform);
                rallyObj.transform.localPosition = _buildingData.rallyPointOffset;
                _rallyPoint = rallyObj.transform;
            }
            
            Debug.Log($"[Building] {_buildingData.displayName} 已初始化 - 生命:{_currentHealth}");
        }
        
        /// <summary>
        /// 设置建筑数据（用于动态创建建筑）
        /// </summary>
        public void SetBuildingData(BuildingData data, int playerId)
        {
            _buildingData = data;
            _playerId = playerId;
            InitializeFromData();
        }
        
        #endregion

        #region 生产系统
        
        /// <summary>
        /// 生产单位（添加到生产队列）
        /// </summary>
        /// <param name="unitData">要生产的单位数据</param>
        /// <returns>是否成功添加到队列</returns>
        public bool ProduceUnit(UnitData unitData)
        {
            if (_buildingData == null || unitData == null)
            {
                Debug.LogWarning("[Building] 建筑数据或单位数据为空！");
                return false;
            }
            
            // 检查状态
            if (_currentState == BuildingState.Destroyed || _currentState == BuildingState.UnderConstruction)
            {
                Debug.LogWarning($"[Building] 建筑状态不允许生产：{_currentState}");
                return false;
            }
            
            // 检查是否是该建筑可生产的单位
            if (!_buildingData.producibleUnits.Contains(unitData))
            {
                Debug.LogWarning($"[Building] {_buildingData.displayName} 无法生产 {unitData.displayName}");
                return false;
            }
            
            // 检查生产队列是否已满
            if (_productionQueue.Count >= _buildingData.productionQueueSize)
            {
                Debug.LogWarning($"[Building] 生产队列已满！({_productionQueue.Count}/{_buildingData.productionQueueSize})");
                return false;
            }
            
            // 检查资源是否足够
            var cost = unitData.GetCostDictionary();
            if (!ResourceManager.Instance.CanAfford(_playerId, cost))
            {
                Debug.LogWarning($"[Building] 玩家 {_playerId} 资源不足，无法生产 {unitData.displayName}");
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
            
            Debug.Log($"[Building] {unitData.displayName} 已加入生产队列（{_productionQueue.Count}/{_buildingData.productionQueueSize}）");
            
            // 触发队列变化事件
            OnQueueChanged?.Invoke(_productionQueue);
            
            return true;
        }
        
        /// <summary>
        /// 取消生产队列中的项目
        /// </summary>
        /// <param name="index">队列索引</param>
        /// <returns>是否成功取消</returns>
        public bool CancelProduction(int index)
        {
            if (index < 0 || index >= _productionQueue.Count)
            {
                return false;
            }
            
            var item = _productionQueue[index];
            
            // 返还资源（可设置返还比例）
            float refundRate = 0.75f; // 返还75%资源
            var cost = item.unitData.GetCostDictionary();
            foreach (var kvp in cost)
            {
                int refundAmount = Mathf.RoundToInt(kvp.Value * refundRate);
                ResourceManager.Instance.AddResource(_playerId, kvp.Key, refundAmount);
            }
            
            _productionQueue.RemoveAt(index);
            
            Debug.Log($"[Building] 取消生产 {item.unitData.displayName}，已返还 {refundRate * 100}% 资源");
            
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
                SpawnUnit(currentItem.unitData);
                _productionQueue.RemoveAt(0);
                
                OnQueueChanged?.Invoke(_productionQueue);
                
                Debug.Log($"[Building] {currentItem.unitData.displayName} 生产完成！剩余队列: {_productionQueue.Count}");
            }
        }
        
        /// <summary>
        /// 生成单位
        /// </summary>
        private void SpawnUnit(UnitData unitData)
        {
            if (unitData.unitPrefab == null)
            {
                Debug.LogError($"[Building] {unitData.displayName} 没有配置预制体！");
                return;
            }
            
            // 计算生成位置
            Vector3 spawnPosition = transform.position + (_buildingData?.spawnPointOffset ?? Vector3.forward * 2f);
            
            // 实例化单位
            GameObject unitObj = Instantiate(unitData.unitPrefab, spawnPosition, Quaternion.identity);
            
            // 设置单位数据
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit != null)
            {
                unit.SetUnitData(unitData, _playerId);
                
                // 触发生产完成事件
                OnUnitProduced?.Invoke(unit);
                
                // TODO: 让单位移动到集结点
                // unit.MoveTo(RallyPointPosition);
            }
            
            Debug.Log($"[Building] 单位 {unitData.displayName} 已生成在 {spawnPosition}");
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
            int actualDamage = Mathf.Max(1, Mathf.RoundToInt(damage * multiplier) - (_buildingData?.armor ?? 0));
            
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
            
            Debug.Log($"[Building] {_buildingData.displayName} 受到 {actualDamage} 点伤害，剩余生命: {_currentHealth}/{_buildingData.maxHealth}");
            
            OnHealthChanged?.Invoke(_currentHealth, _buildingData.maxHealth);
            
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
            if (!IsAlive || _buildingData == null) return;
            
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_buildingData.maxHealth, _currentHealth + amount);
            
            if (_currentHealth != oldHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, _buildingData.maxHealth);
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
            
            Debug.Log($"[Building] {_buildingData.displayName} 已被摧毁！");
            
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
            if (_buildingData == null) return;
            
            // 显示攻击范围（如果有攻击能力）
            if (_buildingData.canAttack)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _buildingData.attackRange);
            }
            
            // 显示集结点
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(RallyPointPosition, 0.5f);
            Gizmos.DrawLine(transform.position, RallyPointPosition);
            
            // 显示生成点
            Gizmos.color = Color.cyan;
            Vector3 spawnPos = transform.position + (_buildingData?.spawnPointOffset ?? Vector3.forward * 2f);
            Gizmos.DrawWireSphere(spawnPos, 0.3f);
        }
        
        #endregion
    }
}
