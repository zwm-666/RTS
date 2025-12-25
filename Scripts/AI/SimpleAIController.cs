// ============================================================
// SimpleAIController.cs
// 简单敌人AI控制器 - 发育/集结/进攻状态机
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using RTS.Units;
using RTS.Buildings;
using RTS.Core;
using RTS.Managers;
using RTS.Presentation;

namespace RTS.AI
{
    /// <summary>
    /// AI 状态
    /// </summary>
    public enum AIState
    {
        Developing,  // 发育：生产单位
        Rallying,    // 集结：集合部队
        Attacking    // 进攻：攻击敌人
    }

    /// <summary>
    /// 简单敌人AI控制器
    /// 挂载位置：空对象或 GameManager
    /// </summary>
    public class SimpleAIController : MonoBehaviour
    {
        #region 配置
        
        [Header("AI 玩家配置")]
        [SerializeField] private int _aiPlayerId = 1;
        [SerializeField] private int _enemyPlayerId = 0; // 玩家ID
        
        [Header("发育阶段")]
        [Tooltip("单位数量阈值，超过后进入集结")]
        [SerializeField] private int _unitThreshold = 10;
        
        [Tooltip("检查是否该生产单位的间隔")]
        [SerializeField] private float _productionCheckInterval = 5f;
        
        [Header("集结阶段")]
        [Tooltip("集结点（地图中心）")]
        [SerializeField] private Vector3 _rallyPoint = new Vector3(64, 0, 64);
        
        [Tooltip("集结距离阈值")]
        [SerializeField] private float _rallyDistance = 10f;
        
        [Header("进攻阶段")]
        [Tooltip("发现敌人的检测范围")]
        [SerializeField] private float _detectionRange = 30f;
        
        [Header("决策间隔")]
        [SerializeField] private float _decisionInterval = 2f;
        
        [Header("调试")]
        [SerializeField] private bool _logDecisions = true;
        
        #endregion

        #region 私有字段
        
        private AIState _currentState = AIState.Developing;
        private float _decisionTimer = 0f;
        private float _productionTimer = 0f;
        
        // 缓存
        private List<Unit> _myUnits = new List<Unit>();
        private List<Building> _myBuildings = new List<Building>();
        private List<Unit> _enemyUnits = new List<Unit>();
        
        #endregion

        #region 属性
        
        public AIState CurrentState => _currentState;
        public int UnitCount => _myUnits.Count;
        
        #endregion

        #region Unity 生命周期
        
        private void Update()
        {
            _decisionTimer += Time.deltaTime;
            _productionTimer += Time.deltaTime;
            
            // 刷新单位列表
            RefreshUnitLists();
            
            // 定期决策
            if (_decisionTimer >= _decisionInterval)
            {
                _decisionTimer = 0f;
                MakeDecision();
            }
            
            // 定期检查生产
            if (_productionTimer >= _productionCheckInterval)
            {
                _productionTimer = 0f;
                TryProduceUnits();
            }
            
            // 执行当前状态逻辑
            ExecuteCurrentState();
        }
        
        #endregion

        #region 单位列表管理
        
        private void RefreshUnitLists()
        {
            _myUnits.Clear();
            _myBuildings.Clear();
            _enemyUnits.Clear();
            
            // 获取所有单位
            Unit[] allUnits = FindObjectsOfType<Unit>();
            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                
                if (unit.PlayerId == _aiPlayerId)
                {
                    _myUnits.Add(unit);
                }
                else if (unit.PlayerId == _enemyPlayerId)
                {
                    _enemyUnits.Add(unit);
                }
            }
            
            // 获取所有建筑
            Building[] allBuildings = FindObjectsOfType<Building>();
            foreach (var building in allBuildings)
            {
                if (building == null || !building.IsAlive) continue;
                
                if (building.PlayerId == _aiPlayerId)
                {
                    _myBuildings.Add(building);
                }
            }
        }
        
        #endregion

        #region 决策系统
        
        private void MakeDecision()
        {
            AIState previousState = _currentState;
            
            // 检查是否发现敌人
            Unit nearestEnemy = FindNearestEnemy();
            
            if (nearestEnemy != null)
            {
                // 发现敌人，进入进攻状态
                _currentState = AIState.Attacking;
            }
            else if (_myUnits.Count >= _unitThreshold)
            {
                // 单位足够，进入集结状态
                _currentState = AIState.Rallying;
            }
            else
            {
                // 继续发育
                _currentState = AIState.Developing;
            }
            
            // 状态变化日志
            if (previousState != _currentState && _logDecisions)
            {
                Debug.Log($"[SimpleAI] 状态变化: {previousState} -> {_currentState} (单位数:{_myUnits.Count})");
            }
        }
        
        private Unit FindNearestEnemy()
        {
            if (_myUnits.Count == 0 || _enemyUnits.Count == 0)
                return null;
            
            // 从我方单位的平均位置检测敌人
            Vector3 averagePos = GetAveragePosition(_myUnits);
            
            Unit nearest = null;
            float minDistance = float.MaxValue;
            
            foreach (var enemy in _enemyUnits)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                
                float distance = Vector3.Distance(averagePos, enemy.transform.position);
                if (distance < _detectionRange && distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }
            
            return nearest;
        }
        
        private Vector3 GetAveragePosition(List<Unit> units)
        {
            if (units.Count == 0) return _rallyPoint;
            
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    sum += unit.transform.position;
                    count++;
                }
            }
            
            return count > 0 ? sum / count : _rallyPoint;
        }
        
        #endregion

        #region 状态执行
        
        private void ExecuteCurrentState()
        {
            switch (_currentState)
            {
                case AIState.Developing:
                    ExecuteDeveloping();
                    break;
                case AIState.Rallying:
                    ExecuteRallying();
                    break;
                case AIState.Attacking:
                    ExecuteAttacking();
                    break;
            }
        }
        
        /// <summary>
        /// 发育阶段：专注生产
        /// </summary>
        private void ExecuteDeveloping()
        {
            // 生产逻辑在 TryProduceUnits 中处理
        }
        
        /// <summary>
        /// 集结阶段：命令所有单位移动到集结点
        /// </summary>
        private void ExecuteRallying()
        {
            foreach (var unit in _myUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                
                // 检查是否已经在集结点附近
                float distance = Vector3.Distance(unit.transform.position, _rallyPoint);
                if (distance > _rallyDistance)
                {
                    // 只有空闲单位才移动
                    if (unit.CurrentState == UnitState.Idle)
                    {
                        unit.MoveTo(_rallyPoint);
                    }
                }
            }
        }
        
        /// <summary>
        /// 进攻阶段：攻击最近的敌人
        /// </summary>
        private void ExecuteAttacking()
        {
            Unit target = FindNearestEnemy();
            
            if (target == null)
            {
                // 没有敌人，返回集结状态
                _currentState = AIState.Rallying;
                return;
            }
            
            foreach (var unit in _myUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                
                // 命令攻击
                if (unit.CurrentState == UnitState.Idle || unit.CurrentState == UnitState.Moving)
                {
                    unit.Attack(target);
                }
            }
        }
        
        #endregion

        #region 生产系统
        
        private void TryProduceUnits()
        {
            // 查找可以生产单位的建筑（兵营）
            foreach (var building in _myBuildings)
            {
                if (building == null || !building.IsAlive) continue;
                if (building.DomainData == null) continue;
                
                var producibleUnits = building.DomainData.ProducibleUnitIds;
                if (producibleUnits == null || producibleUnits.Count == 0) continue;
                
                // 尝试生产第一个可生产的单位
                string unitId = producibleUnits[0];
                
                bool success = building.ProduceUnit(unitId);
                if (success && _logDecisions)
                {
                    Debug.Log($"[SimpleAI] 开始生产: {unitId}");
                }
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 强制设置集结点
        /// </summary>
        public void SetRallyPoint(Vector3 point)
        {
            _rallyPoint = point;
        }
        
        /// <summary>
        /// 强制进入进攻状态
        /// </summary>
        public void ForceAttack()
        {
            _currentState = AIState.Attacking;
        }
        
        /// <summary>
        /// 强制撤退（返回发育状态）
        /// </summary>
        public void ForceRetreat()
        {
            _currentState = AIState.Developing;
        }
        
        #endregion

        #region 调试
        
        private void OnDrawGizmosSelected()
        {
            // 显示集结点
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_rallyPoint, _rallyDistance);
            
            // 显示检测范围
            if (_myUnits.Count > 0)
            {
                Vector3 averagePos = GetAveragePosition(_myUnits);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(averagePos, _detectionRange);
            }
        }
        
        #endregion
    }
}
