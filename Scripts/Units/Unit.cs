// ============================================================
// Unit.cs
// 单位基类脚本 - 实现 ICommandable 接口
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.Data;
using RTS.Map;
using RTS.Interfaces;

namespace RTS.Units
{
    /// <summary>
    /// 单位状态
    /// </summary>
    public enum UnitState
    {
        Idle,           // 空闲
        Moving,         // 移动中（按路径）
        Attacking,      // 攻击中
        Following,      // 跟随中
        Dead            // 死亡
    }

    /// <summary>
    /// 单位基类 - 挂载在单位预制体上
    /// 实现 ICommandable 接口，可被选择和命令
    /// </summary>
    public class Unit : MonoBehaviour, ICommandable
    {
        #region 事件
        
        /// <summary>
        /// 单位死亡事件
        /// </summary>
        public event Action<Unit> OnDeath;
        
        /// <summary>
        /// 生命值变化事件：当前生命值，最大生命值
        /// </summary>
        public event Action<int, int> OnHealthChanged;
        
        /// <summary>
        /// 到达目的地事件
        /// </summary>
        public event Action<Unit> OnReachedDestination;
        
        #endregion

        #region 序列化字段
        
        [Header("单位数据")]
        [SerializeField] private UnitData _unitData;
        
        [Header("所属玩家")]
        [SerializeField] private int _playerId = 0;
        
        [Header("当前状态（调试用）")]
        [SerializeField] private UnitState _currentState = UnitState.Idle;
        
        [Header("移动配置")]
        [SerializeField] private float _stoppingDistance = 0.2f;
        [SerializeField] private float _rotationSpeed = 10f;
        
        [Header("选择指示器（可选）")]
        [SerializeField] private GameObject _selectionIndicator;
        
        #endregion

        #region 运行时属性
        
        private int _currentHealth;
        private float _attackCooldown;
        private Unit _currentTarget;
        private ISelectable _targetSelectable;
        
        // 寻路相关
        private List<Vector3> _currentPath;
        private int _currentPathIndex;
        private Vector3 _moveDestination;
        private bool _hasDestination;
        
        // 选择状态
        private bool _isSelected;
        
        // 单位能力
        private bool _canSwim = false;
        private bool _canFly = false;
        
        #endregion

        #region ISelectable 实现
        
        public int PlayerId => _playerId;
        public bool IsAlive => _currentHealth > 0;
        public Transform Transform => transform;
        
        public void OnSelected()
        {
            _isSelected = true;
            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(true);
            }
        }
        
        public void OnDeselected()
        {
            _isSelected = false;
            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(false);
            }
        }
        
        #endregion

        #region 属性访问器
        
        public UnitData UnitData => _unitData;
        public UnitState CurrentState => _currentState;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _unitData != null ? _unitData.maxHealth : 0;
        public ArmorType ArmorType => _unitData != null ? _unitData.armorType : ArmorType.None;
        public int Armor => _unitData != null ? _unitData.armor : 0;
        public bool IsMoving => _currentState == UnitState.Moving && _currentPath != null;
        public bool IsSelected => _isSelected;
        public bool CanSwim => _canSwim;
        public bool CanFly => _canFly;
        
        #endregion

        #region Unity 生命周期
        
        protected virtual void Awake()
        {
            InitializeFromData();
            
            // 初始隐藏选择指示器
            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(false);
            }
        }
        
        protected virtual void Update()
        {
            if (!IsAlive) return;
            
            // 攻击冷却
            if (_attackCooldown > 0)
            {
                _attackCooldown -= Time.deltaTime;
            }
            
            // 生命恢复
            if (_unitData != null && _unitData.healthRegen > 0)
            {
                Heal(Mathf.RoundToInt(_unitData.healthRegen * Time.deltaTime));
            }
            
            // 状态机更新
            UpdateState();
        }
        
        #endregion

        #region 初始化
        
        public void InitializeFromData()
        {
            if (_unitData == null)
            {
                Debug.LogError($"[Unit] {gameObject.name} 缺少 UnitData！");
                return;
            }
            
            _currentHealth = _unitData.maxHealth;
            _currentState = UnitState.Idle;
            _attackCooldown = 0f;
            _currentPath = null;
            _currentPathIndex = 0;
            _hasDestination = false;
            
            Debug.Log($"[Unit] {_unitData.displayName} 已初始化 - 生命:{_currentHealth}, 攻击:{_unitData.attackDamage}");
        }
        
        public void SetUnitData(UnitData data, int playerId)
        {
            _unitData = data;
            _playerId = playerId;
            InitializeFromData();
        }
        
        #endregion

        #region ICommandable 实现 - 移动
        
        /// <summary>
        /// 移动到目标位置（使用A*寻路）
        /// </summary>
        public bool MoveTo(Vector3 destination)
        {
            if (!IsAlive || _unitData == null) return false;
            
            // 清除当前攻击目标
            _currentTarget = null;
            _targetSelectable = null;
            
            // 调用寻路系统
            if (Pathfinding.Instance == null)
            {
                Debug.LogWarning("[Unit] Pathfinding 系统未初始化！使用直线移动。");
                _currentPath = new List<Vector3> { destination };
                _currentPathIndex = 0;
                _moveDestination = destination;
                _hasDestination = true;
                _currentState = UnitState.Moving;
                return true;
            }
            
            List<Vector3> path = Pathfinding.Instance.FindPath(
                transform.position, destination, _canSwim, _canFly
            );
            
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[Unit] {_unitData.displayName} 无法到达目标位置！");
                return false;
            }
            
            _currentPath = path;
            _currentPathIndex = 0;
            _moveDestination = destination;
            _hasDestination = true;
            _currentState = UnitState.Moving;
            
            Debug.Log($"[Unit] {_unitData.displayName} 开始移动，路径点数: {path.Count}");
            return true;
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            StopMoving();
            _currentTarget = null;
            _targetSelectable = null;
            _currentState = UnitState.Idle;
        }
        
        /// <summary>
        /// 停止移动（内部方法）
        /// </summary>
        public void StopMoving()
        {
            _currentPath = null;
            _currentPathIndex = 0;
            _hasDestination = false;
            
            if (_currentState == UnitState.Moving)
            {
                _currentState = UnitState.Idle;
            }
        }
        
        /// <summary>
        /// 攻击目标（ICommandable 接口）
        /// </summary>
        public void AttackTarget(ISelectable target)
        {
            if (target == null) return;
            
            _targetSelectable = target;
            
            // 如果目标是 Unit，使用内部 SetTarget
            if (target is Unit unit)
            {
                SetTarget(unit);
            }
            else
            {
                // 其他可攻击目标（如建筑）
                _currentState = UnitState.Attacking;
                MoveTo(target.Transform.position);
            }
        }
        
        private void UpdatePathMovement()
        {
            if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
            {
                OnPathComplete();
                return;
            }
            
            Vector3 targetPoint = _currentPath[_currentPathIndex];
            Vector3 direction = (targetPoint - transform.position);
            direction.y = 0;
            float distance = direction.magnitude;
            
            if (distance < _stoppingDistance)
            {
                _currentPathIndex++;
                
                if (_currentPathIndex >= _currentPath.Count)
                {
                    OnPathComplete();
                }
                return;
            }
            
            direction.Normalize();
            Vector3 movement = direction * _unitData.moveSpeed * Time.deltaTime;
            transform.position += movement;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime
                );
            }
        }
        
        private void OnPathComplete()
        {
            _currentPath = null;
            _currentPathIndex = 0;
            _hasDestination = false;
            _currentState = UnitState.Idle;
            
            Debug.Log($"[Unit] {_unitData?.displayName} 到达目的地");
            OnReachedDestination?.Invoke(this);
        }
        
        #endregion

        #region 战斗系统
        
        public int TakeDamage(int damage, AttackType attackType = AttackType.Normal)
        {
            if (!IsAlive) return 0;
            
            int actualDamage = CalculateReceivedDamage(damage, attackType);
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
            
            Debug.Log($"[Unit] {_unitData.displayName} 受到 {actualDamage} 点伤害，剩余生命: {_currentHealth}/{_unitData.maxHealth}");
            
            OnHealthChanged?.Invoke(_currentHealth, _unitData.maxHealth);
            
            if (_currentHealth <= 0)
            {
                Die();
            }
            
            return actualDamage;
        }
        
        public int TakeDamage(int damage)
        {
            return TakeDamage(damage, AttackType.Normal);
        }
        
        private int CalculateReceivedDamage(int rawDamage, AttackType attackType)
        {
            float multiplier = GetDamageMultiplier(attackType);
            int modifiedDamage = Mathf.RoundToInt(rawDamage * multiplier);
            int finalDamage = Mathf.Max(1, modifiedDamage - (_unitData?.armor ?? 0));
            return finalDamage;
        }
        
        private float GetDamageMultiplier(AttackType attackType)
        {
            if (_unitData == null) return 1f;
            
            switch (attackType)
            {
                case AttackType.Pierce:
                    if (_unitData.armorType == ArmorType.Light) return 1.5f;
                    if (_unitData.armorType == ArmorType.Heavy) return 0.5f;
                    break;
                case AttackType.Siege:
                    if (_unitData.armorType == ArmorType.Fortified) return 2.0f;
                    if (_unitData.armorType == ArmorType.Light) return 0.5f;
                    break;
                case AttackType.Magic:
                    return 1.0f;
            }
            return 1.0f;
        }
        
        public bool Attack(Unit target)
        {
            if (!IsAlive || target == null || !target.IsAlive || _unitData == null)
            {
                return false;
            }
            
            if (_attackCooldown > 0)
            {
                return false;
            }
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > _unitData.attackRange)
            {
                _currentTarget = target;
                MoveTo(target.transform.position);
                return false;
            }
            
            _currentState = UnitState.Attacking;
            _attackCooldown = _unitData.attackSpeed;
            
            int damage = _unitData.CalculateDamage(target.ArmorType, target.Armor);
            target.TakeDamage(damage, _unitData.attackType);
            
            Debug.Log($"[Unit] {_unitData.displayName} 攻击 {target.UnitData.displayName}，造成 {damage} 点伤害");
            return true;
        }
        
        public void SetTarget(Unit target)
        {
            _currentTarget = target;
            if (target != null && target.IsAlive)
            {
                _currentState = UnitState.Attacking;
            }
        }
        
        public void Heal(int amount)
        {
            if (!IsAlive || _unitData == null) return;
            
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_unitData.maxHealth, _currentHealth + amount);
            
            if (_currentHealth != oldHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, _unitData.maxHealth);
            }
        }
        
        protected virtual void Die()
        {
            _currentState = UnitState.Dead;
            StopMoving();
            
            Debug.Log($"[Unit] {_unitData.displayName} 已死亡！");
            OnDeath?.Invoke(this);
            
            Destroy(gameObject, 2f);
        }
        
        #endregion

        #region 状态机
        
        protected virtual void UpdateState()
        {
            switch (_currentState)
            {
                case UnitState.Idle:
                    break;
                    
                case UnitState.Moving:
                    UpdatePathMovement();
                    break;
                    
                case UnitState.Attacking:
                    UpdateAttacking();
                    break;
                    
                case UnitState.Following:
                    UpdateFollowing();
                    break;
            }
        }
        
        private void UpdateAttacking()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                _currentTarget = null;
                _currentState = UnitState.Idle;
                return;
            }
            
            Attack(_currentTarget);
        }
        
        private void UpdateFollowing()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                _currentTarget = null;
                _currentState = UnitState.Idle;
                return;
            }
            
            float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);
            if (distance > _unitData.attackRange * 1.5f)
            {
                MoveTo(_currentTarget.transform.position);
            }
        }
        
        #endregion

        #region 调试
        
        private void OnDrawGizmosSelected()
        {
            if (_unitData == null) return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _unitData.attackRange);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _unitData.sightRange);
            
            if (_currentPath != null && _currentPath.Count > 0)
            {
                Gizmos.color = Color.green;
                Vector3 prev = transform.position;
                for (int i = _currentPathIndex; i < _currentPath.Count; i++)
                {
                    Gizmos.DrawLine(prev + Vector3.up * 0.5f, _currentPath[i] + Vector3.up * 0.5f);
                    Gizmos.DrawSphere(_currentPath[i] + Vector3.up * 0.5f, 0.2f);
                    prev = _currentPath[i];
                }
            }
        }
        
        #endregion
    }
}
