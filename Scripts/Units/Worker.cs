// ============================================================
// Worker.cs
// 工人单位 - 资源采集示例
// ============================================================

using System.Collections;
using UnityEngine;
using RTS.Core;
using RTS.Managers;

namespace RTS.Units
{
    /// <summary>
    /// 工人单位状态
    /// </summary>
    public enum WorkerState
    {
        Idle,           // 空闲
        MovingToResource,   // 前往资源点
        Gathering,      // 采集中
        MovingToDropOff,    // 前往交付点
        DroppingOff     // 交付中
    }

    /// <summary>
    /// 工人单位 - 负责采集资源
    /// </summary>
    public class Worker : MonoBehaviour
    {
        #region 配置字段
        
        [Header("所属玩家")]
        [SerializeField] private int _playerId = 0;
        
        [Header("采集配置")]
        [SerializeField] private ResourceType _currentResourceType = ResourceType.Gold;
        [SerializeField] private int _gatherAmountPerTrip = 10;     // 每次采集数量
        [SerializeField] private float _gatherTime = 2.0f;          // 采集耗时（秒）
        [SerializeField] private float _dropOffTime = 0.5f;         // 交付耗时（秒）
        [SerializeField] private float _moveSpeed = 5.0f;           // 移动速度
        
        [Header("目标点")]
        [SerializeField] private Transform _resourcePoint;          // 资源点
        [SerializeField] private Transform _dropOffPoint;           // 交付点（主城/仓库）
        
        #endregion

        #region 私有字段
        
        private WorkerState _currentState = WorkerState.Idle;
        private int _carriedAmount = 0;                             // 当前携带的资源量
        private int _maxCarryCapacity = 10;                         // 最大携带量
        
        #endregion

        #region 属性
        
        public WorkerState CurrentState => _currentState;
        public int CarriedAmount => _carriedAmount;
        public int PlayerId => _playerId;
        
        #endregion

        #region Unity 生命周期
        
        private void Start()
        {
            // 演示：自动开始采集
            if (_resourcePoint != null && _dropOffPoint != null)
            {
                StartGathering(_currentResourceType);
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 开始采集指定类型的资源
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        public void StartGathering(ResourceType resourceType)
        {
            _currentResourceType = resourceType;
            _currentState = WorkerState.MovingToResource;
            StartCoroutine(GatheringLoop());
        }
        
        /// <summary>
        /// 停止采集
        /// </summary>
        public void StopGathering()
        {
            StopAllCoroutines();
            _currentState = WorkerState.Idle;
        }
        
        /// <summary>
        /// 设置资源点和交付点
        /// </summary>
        public void SetTargets(Transform resourcePoint, Transform dropOffPoint)
        {
            _resourcePoint = resourcePoint;
            _dropOffPoint = dropOffPoint;
        }
        
        #endregion

        #region 采集逻辑
        
        /// <summary>
        /// 采集循环协程
        /// </summary>
        private IEnumerator GatheringLoop()
        {
            while (true)
            {
                // 1. 前往资源点
                _currentState = WorkerState.MovingToResource;
                yield return StartCoroutine(MoveToTarget(_resourcePoint.position));
                
                // 2. 采集资源
                _currentState = WorkerState.Gathering;
                yield return StartCoroutine(GatherResource());
                
                // 3. 前往交付点
                _currentState = WorkerState.MovingToDropOff;
                yield return StartCoroutine(MoveToTarget(_dropOffPoint.position));
                
                // 4. 交付资源
                _currentState = WorkerState.DroppingOff;
                yield return StartCoroutine(DropOffResource());
            }
        }
        
        /// <summary>
        /// 移动到目标位置
        /// </summary>
        private IEnumerator MoveToTarget(Vector3 targetPosition)
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * _moveSpeed * Time.deltaTime;
                
                // 朝向目标
                if (direction != Vector3.zero)
                {
                    transform.forward = direction;
                }
                
                yield return null;
            }
            
            transform.position = targetPosition;
        }
        
        /// <summary>
        /// 采集资源
        /// </summary>
        private IEnumerator GatherResource()
        {
            Debug.Log($"工人开始采集 {_currentResourceType}...");
            
            // 模拟采集时间
            yield return new WaitForSeconds(_gatherTime);
            
            // 采集完成
            _carriedAmount = Mathf.Min(_gatherAmountPerTrip, _maxCarryCapacity);
            Debug.Log($"工人采集完成，携带 {_carriedAmount} {_currentResourceType}");
        }
        
        /// <summary>
        /// 交付资源到主城/仓库
        /// </summary>
        private IEnumerator DropOffResource()
        {
            Debug.Log($"工人正在交付 {_carriedAmount} {_currentResourceType}...");
            
            // 模拟交付时间
            yield return new WaitForSeconds(_dropOffTime);
            
            // 将资源添加到玩家资源池
            if (_carriedAmount > 0)
            {
                ResourceManager.Instance.AddResource(_playerId, _currentResourceType, _carriedAmount);
                _carriedAmount = 0;
            }
            
            Debug.Log("工人交付完成，返回采集点...");
        }
        
        #endregion

        #region 调试显示
        
        private void OnGUI()
        {
            // 在游戏视图中显示工人状态（仅用于调试）
            if (Camera.main == null) return;
            
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2);
            if (screenPos.z > 0)
            {
                GUI.Label(
                    new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 40),
                    $"状态: {_currentState}\n携带: {_carriedAmount}"
                );
            }
        }
        
        #endregion
    }
}
