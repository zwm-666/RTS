// ============================================================
// Projectile.cs
// 投射物脚本 - 追踪目标并造成伤害
// ============================================================

using UnityEngine;
using RTS.Domain.Enums;

namespace RTS.Units
{
    /// <summary>
    /// 投射物类型
    /// </summary>
    public enum ProjectileType
    {
        Tracking,       // 追踪型（自动追踪目标）
        Linear,         // 直线型（发射后直线飞行）
        Parabolic       // 抛物线型（预留）
    }

    /// <summary>
    /// 投射物脚本
    /// 挂载位置：投射物预制体上
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        #region 序列化字段
        
        [Header("投射物配置")]
        [SerializeField] private ProjectileType _projectileType = ProjectileType.Tracking;
        [SerializeField] private float _speed = 15f;
        [SerializeField] private float _hitThreshold = 0.5f;
        [SerializeField] private float _maxLifetime = 10f;
        
        [Header("视觉效果")]
        [SerializeField] private GameObject _hitEffectPrefab;
        [SerializeField] private float _hitEffectDuration = 2f;
        [SerializeField] private TrailRenderer _trail;
        
        [Header("音效（可选）")]
        [SerializeField] private AudioClip _launchSound;
        [SerializeField] private AudioClip _hitSound;
        
        #endregion

        #region 运行时属性
        
        private Unit _target;
        private Unit _owner;
        private int _damage;
        private AttackType _attackType;
        private Vector3 _lastTargetPosition;
        private float _lifetime;
        private bool _hasHit = false;
        
        #endregion

        #region 属性访问器
        
        public float Speed => _speed;
        public Unit Target => _target;
        public Unit Owner => _owner;
        public int Damage => _damage;
        public AttackType AttackType => _attackType;
        
        #endregion

        #region 初始化
        
        /// <summary>
        /// 初始化投射物
        /// </summary>
        /// <param name="owner">发射者</param>
        /// <param name="target">目标单位</param>
        /// <param name="damage">伤害值</param>
        /// <param name="attackType">攻击类型（用于克制计算）</param>
        public void Initialize(Unit owner, Unit target, int damage, AttackType attackType)
        {
            _owner = owner;
            _target = target;
            _damage = damage;
            _attackType = attackType;
            _lifetime = 0f;
            _hasHit = false;
            
            // 记录初始目标位置（用于目标死亡后继续飞行）
            if (_target != null)
            {
                _lastTargetPosition = _target.transform.position;
            }
            else
            {
                _lastTargetPosition = transform.position + transform.forward * 10f;
            }
            
            // 播放发射音效
            if (_launchSound != null)
            {
                AudioSource.PlayClipAtPoint(_launchSound, transform.position);
            }
            
            // 初始朝向目标
            LookAtTarget();
        }
        
        /// <summary>
        /// 简化初始化（不传递 owner）
        /// </summary>
        public void Initialize(Unit target, int damage, AttackType attackType)
        {
            Initialize(null, target, damage, attackType);
        }
        
        #endregion

        #region Unity 生命周期
        
        private void Update()
        {
            if (_hasHit) return;
            
            _lifetime += Time.deltaTime;
            
            // 超时销毁
            if (_lifetime >= _maxLifetime)
            {
                DestroySelf();
                return;
            }
            
            // 根据类型执行不同的飞行逻辑
            switch (_projectileType)
            {
                case ProjectileType.Tracking:
                    UpdateTracking();
                    break;
                case ProjectileType.Linear:
                    UpdateLinear();
                    break;
                case ProjectileType.Parabolic:
                    UpdateParabolic();
                    break;
            }
        }
        
        #endregion

        #region 飞行逻辑
        
        /// <summary>
        /// 追踪型飞行 - 持续追踪目标
        /// </summary>
        private void UpdateTracking()
        {
            Vector3 targetPos = GetTargetPosition();
            
            // 计算方向
            Vector3 direction = (targetPos - transform.position).normalized;
            
            // 平滑转向
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
            
            // 移动
            transform.position += transform.forward * _speed * Time.deltaTime;
            
            // 检测命中
            float distance = Vector3.Distance(transform.position, targetPos);
            if (distance <= _hitThreshold)
            {
                OnHitTarget();
            }
        }
        
        /// <summary>
        /// 直线型飞行 - 朝发射方向直线飞行
        /// </summary>
        private void UpdateLinear()
        {
            // 直线飞行
            transform.position += transform.forward * _speed * Time.deltaTime;
            
            // 检测命中（与目标距离）
            if (_target != null && _target.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, _target.transform.position);
                if (distance <= _hitThreshold)
                {
                    OnHitTarget();
                }
            }
            else
            {
                // 目标已死亡，检测是否到达原目标位置
                float distance = Vector3.Distance(transform.position, _lastTargetPosition);
                if (distance <= _hitThreshold)
                {
                    // 到达目标位置但目标已死亡，直接销毁
                    DestroySelf();
                }
            }
        }
        
        /// <summary>
        /// 抛物线型飞行（预留）
        /// </summary>
        private void UpdateParabolic()
        {
            // TODO: 实现抛物线飞行
            UpdateTracking(); // 暂时使用追踪
        }
        
        /// <summary>
        /// 获取目标位置
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            if (_target != null && _target.IsAlive)
            {
                _lastTargetPosition = _target.transform.position;
                
                // 瞄准目标中心偏上（更自然）
                return _lastTargetPosition + Vector3.up * 0.5f;
            }
            
            // 目标死亡/丢失，飞向最后位置
            return _lastTargetPosition + Vector3.up * 0.5f;
        }
        
        /// <summary>
        /// 朝向目标
        /// </summary>
        private void LookAtTarget()
        {
            Vector3 targetPos = GetTargetPosition();
            Vector3 direction = (targetPos - transform.position).normalized;
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        #endregion

        #region 命中处理
        
        /// <summary>
        /// 命中目标（虚方法，子类可重写）
        /// </summary>
        protected virtual void OnHitTarget()
        {
            if (_hasHit) return;
            _hasHit = true;
            
            // 对目标造成伤害
            if (_target != null && _target.IsAlive)
            {
                _target.TakeDamage(_damage, _attackType);
                
                Debug.Log($"[Projectile] 命中目标 {_target.DisplayName}，造成 {_damage} 点伤害");
            }
            
            // 播放命中音效
            if (_hitSound != null)
            {
                AudioSource.PlayClipAtPoint(_hitSound, transform.position);
            }
            
            // 生成命中特效
            SpawnHitEffect();
            
            // 销毁自己
            DestroySelf();
        }
        
        /// <summary>
        /// 生成命中特效
        /// </summary>
        private void SpawnHitEffect()
        {
            if (_hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(_hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, _hitEffectDuration);
            }
        }
        
        /// <summary>
        /// 销毁自己（虚方法，子类可重写）
        /// </summary>
        protected virtual void DestroySelf()
        {
            // 如果有拖尾，先断开再销毁
            if (_trail != null)
            {
                _trail.transform.SetParent(null);
                Destroy(_trail.gameObject, _trail.time);
            }
            
            Destroy(gameObject);
        }
        
        #endregion

        #region 调试
        
        private void OnDrawGizmosSelected()
        {
            // 显示命中范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _hitThreshold);
            
            // 显示飞行方向
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
        
        #endregion
    }
}
