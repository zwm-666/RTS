// ============================================================
// ResourceNode.cs
// 资源点 - 可采集、可再生的资源节点
// ============================================================

using UnityEngine;
using RTS.Core;

namespace RTS.Resources
{
    /// <summary>
    /// 资源点状态
    /// </summary>
    public enum ResourceNodeState
    {
        Full,       // 满资源
        Partial,    // 部分资源
        Depleted,   // 枯竭
        Regenerating // 再生中
    }

    /// <summary>
    /// 资源点脚本
    /// 挂载位置：矿点、树林、农田等资源节点
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        #region 配置
        
        [Header("资源类型")]
        [SerializeField] private ResourceType _resourceType = ResourceType.Gold;
        
        [Header("资源容量")]
        [SerializeField] private int _maxAmount = 1000;
        [SerializeField] private int _currentAmount = 1000;
        
        [Header("再生配置")]
        [Tooltip("每秒恢复量（金矿应设为较低值）")]
        [SerializeField] private float _regenRate = 1f;
        
        [Tooltip("枯竭后多久开始再生（秒）")]
        [SerializeField] private float _regenDelay = 30f;
        
        [Tooltip("是否允许再生")]
        [SerializeField] private bool _canRegenerate = true;
        
        [Header("采集配置")]
        [Tooltip("每次采集量")]
        [SerializeField] private int _harvestAmountPerTrip = 10;
        
        [Header("视觉效果")]
        [SerializeField] private GameObject _fullModel;
        [SerializeField] private GameObject _partialModel;
        [SerializeField] private GameObject _depletedModel;
        
        [Header("交互配置")]
        [SerializeField] private float _interactionRadius = 2f;
        
        #endregion

        #region 私有字段
        
        private ResourceNodeState _currentState = ResourceNodeState.Full;
        private int _workerCount = 0;
        private float _depletedTime = 0f;
        private float _regenAccumulator = 0f;
        
        #endregion

        #region 属性
        
        public ResourceType ResourceType => _resourceType;
        public int CurrentAmount => _currentAmount;
        public int MaxAmount => _maxAmount;
        public float RegenRate => _regenRate;
        public bool IsDepleted => _currentState == ResourceNodeState.Depleted;
        public bool IsFull => _currentAmount >= _maxAmount;
        public ResourceNodeState State => _currentState;
        public int WorkerCount => _workerCount;
        public int HarvestAmountPerTrip => _harvestAmountPerTrip;
        public float InteractionRadius => _interactionRadius;
        
        /// <summary>
        /// 资源百分比
        /// </summary>
        public float ResourcePercent => _maxAmount > 0 ? (float)_currentAmount / _maxAmount : 0f;
        
        #endregion

        #region Unity 生命周期
        
        private void Start()
        {
            // 初始化状态
            UpdateState();
            UpdateVisuals();
        }
        
        private void Update()
        {
            // 再生逻辑
            if (_canRegenerate && _currentAmount < _maxAmount)
            {
                ProcessRegeneration();
            }
        }
        
        #endregion

        #region 采集系统
        
        /// <summary>
        /// 工人开始采集
        /// </summary>
        public void RegisterWorker()
        {
            _workerCount++;
            Debug.Log($"[ResourceNode] 工人开始采集 {_resourceType}，当前工人数: {_workerCount}");
        }
        
        /// <summary>
        /// 工人停止采集
        /// </summary>
        public void UnregisterWorker()
        {
            _workerCount = Mathf.Max(0, _workerCount - 1);
            Debug.Log($"[ResourceNode] 工人停止采集 {_resourceType}，当前工人数: {_workerCount}");
        }
        
        /// <summary>
        /// 采集资源
        /// </summary>
        /// <param name="amount">请求采集量</param>
        /// <returns>实际采集到的数量</returns>
        public int Harvest(int amount)
        {
            if (IsDepleted)
            {
                return 0;
            }
            
            // 实际能采集的数量
            int actualAmount = Mathf.Min(amount, _currentAmount);
            _currentAmount -= actualAmount;
            
            Debug.Log($"[ResourceNode] 采集 {actualAmount} {_resourceType}，剩余: {_currentAmount}/{_maxAmount}");
            
            // 更新状态
            UpdateState();
            UpdateVisuals();
            
            return actualAmount;
        }
        
        /// <summary>
        /// 采集一次（使用默认采集量）
        /// </summary>
        public int HarvestOnce()
        {
            return Harvest(_harvestAmountPerTrip);
        }
        
        #endregion

        #region 再生系统
        
        /// <summary>
        /// 处理资源再生
        /// </summary>
        private void ProcessRegeneration()
        {
            // 有工人在采集时不再生
            if (_workerCount > 0)
            {
                return;
            }
            
            // 枯竭后需要等待一段时间才开始再生
            if (_currentState == ResourceNodeState.Depleted)
            {
                _depletedTime += Time.deltaTime;
                
                if (_depletedTime < _regenDelay)
                {
                    return;
                }
                
                // 开始再生
                _currentState = ResourceNodeState.Regenerating;
            }
            
            // 根据资源类型调整再生速度
            float effectiveRegenRate = GetEffectiveRegenRate();
            
            // 累积再生
            _regenAccumulator += effectiveRegenRate * Time.deltaTime;
            
            if (_regenAccumulator >= 1f)
            {
                int regenAmount = Mathf.FloorToInt(_regenAccumulator);
                _currentAmount = Mathf.Min(_maxAmount, _currentAmount + regenAmount);
                _regenAccumulator -= regenAmount;
                
                // 更新状态
                UpdateState();
                UpdateVisuals();
            }
        }
        
        /// <summary>
        /// 获取有效再生速率（不同资源类型不同）
        /// </summary>
        private float GetEffectiveRegenRate()
        {
            // 金矿再生最慢
            switch (_resourceType)
            {
                case ResourceType.Gold:
                    return _regenRate * 0.2f; // 金矿再生速度降低80%
                case ResourceType.Wood:
                    return _regenRate * 0.8f; // 木材较快
                case ResourceType.Food:
                    return _regenRate * 1.0f; // 食物最快
                default:
                    return _regenRate;
            }
        }
        
        #endregion

        #region 状态管理
        
        /// <summary>
        /// 更新资源点状态
        /// </summary>
        private void UpdateState()
        {
            ResourceNodeState oldState = _currentState;
            
            if (_currentAmount <= 0)
            {
                _currentAmount = 0;
                if (_currentState != ResourceNodeState.Depleted && _currentState != ResourceNodeState.Regenerating)
                {
                    _currentState = ResourceNodeState.Depleted;
                    _depletedTime = 0f;
                    Debug.Log($"[ResourceNode] {_resourceType} 资源点已枯竭！");
                }
            }
            else if (_currentAmount >= _maxAmount)
            {
                _currentAmount = _maxAmount;
                _currentState = ResourceNodeState.Full;
            }
            else
            {
                if (_currentState == ResourceNodeState.Depleted || _currentState == ResourceNodeState.Regenerating)
                {
                    _currentState = ResourceNodeState.Regenerating;
                }
                else
                {
                    _currentState = ResourceNodeState.Partial;
                }
            }
            
            // 状态变化通知
            if (oldState != _currentState)
            {
                OnStateChanged(oldState, _currentState);
            }
        }
        
        /// <summary>
        /// 状态变化回调
        /// </summary>
        private void OnStateChanged(ResourceNodeState oldState, ResourceNodeState newState)
        {
            Debug.Log($"[ResourceNode] {_resourceType} 状态变化: {oldState} -> {newState}");
        }
        
        /// <summary>
        /// 更新视觉效果
        /// </summary>
        private void UpdateVisuals()
        {
            // 根据状态显示不同模型
            if (_fullModel != null)
            {
                _fullModel.SetActive(_currentState == ResourceNodeState.Full);
            }
            
            if (_partialModel != null)
            {
                _partialModel.SetActive(_currentState == ResourceNodeState.Partial || 
                                        _currentState == ResourceNodeState.Regenerating);
            }
            
            if (_depletedModel != null)
            {
                _depletedModel.SetActive(_currentState == ResourceNodeState.Depleted);
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 设置资源量
        /// </summary>
        public void SetAmount(int amount)
        {
            _currentAmount = Mathf.Clamp(amount, 0, _maxAmount);
            UpdateState();
            UpdateVisuals();
        }
        
        /// <summary>
        /// 添加资源（用于特殊效果）
        /// </summary>
        public void AddResource(int amount)
        {
            _currentAmount = Mathf.Min(_maxAmount, _currentAmount + amount);
            UpdateState();
            UpdateVisuals();
        }
        
        /// <summary>
        /// 立即补满资源
        /// </summary>
        [ContextMenu("Refill Resource")]
        public void Refill()
        {
            _currentAmount = _maxAmount;
            _currentState = ResourceNodeState.Full;
            UpdateVisuals();
            Debug.Log($"[ResourceNode] {_resourceType} 资源已补满");
        }
        
        #endregion

        #region 调试
        
        private void OnDrawGizmosSelected()
        {
            // 显示交互范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
            
            // 显示资源类型颜色
            switch (_resourceType)
            {
                case ResourceType.Gold:
                    Gizmos.color = Color.yellow;
                    break;
                case ResourceType.Wood:
                    Gizmos.color = new Color(0.6f, 0.3f, 0f);
                    break;
                case ResourceType.Food:
                    Gizmos.color = Color.green;
                    break;
            }
            
            // 显示资源量指示器
            float fillPercent = ResourcePercent;
            Gizmos.DrawCube(transform.position + Vector3.up * 2f, 
                new Vector3(fillPercent * 2f, 0.2f, 0.2f));
        }
        
        #endregion
    }
}
