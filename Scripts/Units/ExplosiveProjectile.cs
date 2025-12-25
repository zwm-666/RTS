// ============================================================
// ExplosiveProjectile.cs
// 爆炸投射物 - 范围伤害/溅射攻击
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using RTS.Domain.Enums;
using RTS.Buildings;

namespace RTS.Units
{
    /// <summary>
    /// 爆炸投射物
    /// 命中时对范围内所有单位造成伤害
    /// </summary>
    public class ExplosiveProjectile : Projectile
    {
        #region 爆炸配置
        
        [Header("===== 爆炸配置 =====")]
        [Tooltip("爆炸半径")]
        [SerializeField] private float _explosionRadius = 5f;
        
        [Tooltip("是否伤害友军")]
        [SerializeField] private bool _friendlyFire = false;
        
        [Tooltip("是否对建筑造成伤害")]
        [SerializeField] private bool _damageBuildings = true;
        
        [Header("伤害衰减")]
        [Tooltip("启用距离衰减（中心高，边缘低）")]
        [SerializeField] private bool _enableFalloff = true;
        
        [Tooltip("边缘伤害百分比（0.5 = 边缘50%伤害）")]
        [SerializeField] [Range(0f, 1f)] private float _edgeDamagePercent = 0.3f;
        
        [Header("爆炸特效")]
        [SerializeField] private GameObject _explosionEffectPrefab;
        [SerializeField] private float _explosionEffectDuration = 2f;
        [SerializeField] private AudioClip _explosionSound;
        
        [Header("检测配置")]
        [SerializeField] private LayerMask _targetLayerMask = -1;
        
        #endregion

        #region 属性
        
        public float ExplosionRadius => _explosionRadius;
        public bool FriendlyFire => _friendlyFire;
        
        #endregion

        #region 扩展初始化
        
        /// <summary>
        /// 初始化爆炸投射物
        /// </summary>
        public void InitializeExplosive(Unit owner, Unit target, int damage, AttackType attackType, 
            float explosionRadius, bool friendlyFire = false)
        {
            Initialize(owner, target, damage, attackType);
            _explosionRadius = explosionRadius;
            _friendlyFire = friendlyFire;
        }
        
        #endregion

        #region 重写命中处理
        
        /// <summary>
        /// 命中目标时触发爆炸
        /// </summary>
        protected override void OnHitTarget()
        {
            // 获取命中位置
            Vector3 explosionCenter = transform.position;
            
            // 播放爆炸特效
            SpawnExplosionEffect(explosionCenter);
            
            // 播放爆炸音效
            if (_explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(_explosionSound, explosionCenter);
            }
            
            // 对范围内所有目标造成伤害
            DealAreaDamage(explosionCenter);
            
            // 销毁投射物
            DestroySelf();
        }
        
        /// <summary>
        /// 生成爆炸特效
        /// </summary>
        private void SpawnExplosionEffect(Vector3 position)
        {
            if (_explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(_explosionEffectPrefab, position, Quaternion.identity);
                
                // 缩放特效以匹配爆炸半径
                effect.transform.localScale = Vector3.one * (_explosionRadius / 2.5f);
                
                Destroy(effect, _explosionEffectDuration);
            }
        }
        
        /// <summary>
        /// 对范围内目标造成伤害
        /// </summary>
        private void DealAreaDamage(Vector3 center)
        {
            // 获取范围内的所有碰撞体
            Collider[] hits = Physics.OverlapSphere(center, _explosionRadius, _targetLayerMask);
            
            // 用于防止重复伤害同一目标
            HashSet<GameObject> damagedObjects = new HashSet<GameObject>();
            
            int hitCount = 0;
            
            foreach (var hit in hits)
            {
                GameObject rootObject = hit.gameObject;
                
                // 防止重复伤害
                if (damagedObjects.Contains(rootObject)) continue;
                damagedObjects.Add(rootObject);
                
                // 尝试获取单位组件
                Unit unit = hit.GetComponent<Unit>();
                if (unit == null) unit = hit.GetComponentInParent<Unit>();
                
                if (unit != null && unit.IsAlive)
                {
                    if (TryDamageUnit(unit, center))
                    {
                        hitCount++;
                    }
                    continue;
                }
                
                // 尝试获取建筑组件
                if (_damageBuildings)
                {
                    Building building = hit.GetComponent<Building>();
                    if (building == null) building = hit.GetComponentInParent<Building>();
                    
                    if (building != null && building.IsAlive)
                    {
                        if (TryDamageBuilding(building, center))
                        {
                            hitCount++;
                        }
                    }
                }
            }
            
            Debug.Log($"[ExplosiveProjectile] 爆炸命中 {hitCount} 个目标，半径: {_explosionRadius}");
        }
        
        /// <summary>
        /// 尝试对单位造成伤害
        /// </summary>
        private bool TryDamageUnit(Unit unit, Vector3 center)
        {
            // 检查友军伤害
            if (!_friendlyFire && Owner != null && unit.PlayerId == Owner.PlayerId)
            {
                return false;
            }
            
            // 计算伤害
            int damage = CalculateAreaDamage(unit.transform.position, center);
            
            if (damage > 0)
            {
                unit.TakeDamage(damage, AttackType);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 尝试对建筑造成伤害
        /// </summary>
        private bool TryDamageBuilding(Building building, Vector3 center)
        {
            // 检查友军伤害
            if (!_friendlyFire && Owner != null && building.PlayerId == Owner.PlayerId)
            {
                return false;
            }
            
            // 计算伤害
            int damage = CalculateAreaDamage(building.transform.position, center);
            
            if (damage > 0)
            {
                building.TakeDamage(damage, AttackType);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 计算范围伤害（考虑距离衰减）
        /// </summary>
        private int CalculateAreaDamage(Vector3 targetPos, Vector3 center)
        {
            float distance = Vector3.Distance(targetPos, center);
            
            // 超出范围
            if (distance > _explosionRadius)
            {
                return 0;
            }
            
            int baseDamage = Damage;
            
            if (_enableFalloff)
            {
                // 距离衰减公式: 中心100%伤害，边缘 _edgeDamagePercent 伤害
                // 线性插值: damage = baseDamage * lerp(1, edgePercent, distance/radius)
                float falloffFactor = 1f - (distance / _explosionRadius);
                float damagePercent = Mathf.Lerp(_edgeDamagePercent, 1f, falloffFactor);
                
                return Mathf.RoundToInt(baseDamage * damagePercent);
            }
            
            return baseDamage;
        }
        
        #endregion

        #region 调试
        
        private void OnDrawGizmosSelected()
        {
            // 显示爆炸范围
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, _explosionRadius);
            
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
            
            // 显示衰减区域
            if (_enableFalloff)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, _explosionRadius * 0.5f);
            }
        }
        
        #endregion
    }
}
