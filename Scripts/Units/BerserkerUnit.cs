// ============================================================
// BerserkerUnit.cs
// 狂战士单位 - 星火族狂暴被动技能
// ============================================================

using UnityEngine;
using RTS.Domain.Entities;

namespace RTS.Units
{
    /// <summary>
    /// 狂战士单位
    /// 特性：生命值越低，攻击速度越快（狂暴被动）
    /// </summary>
    public class BerserkerUnit : Unit
    {
        #region 狂暴配置
        
        [Header("===== 狂暴被动 =====")]
        [Tooltip("最大攻速加成倍率（血量为0时的额外攻速加成）")]
        [SerializeField] private float _maxBonusMultiplier = 1.0f;
        
        [Tooltip("狂暴触发血量阈值（低于此百分比开始生效）")]
        [SerializeField] [Range(0f, 1f)] private float _berserkThreshold = 1.0f;
        
        [Tooltip("狂暴状态视觉指示器（可选）")]
        [SerializeField] private GameObject _berserkEffectObject;
        
        [Tooltip("狂暴粒子效果（可选）")]
        [SerializeField] private ParticleSystem _berserkParticles;
        
        #endregion

        #region 运行时状态
        
        private bool _isBerserkActive = false;
        private float _currentBerserkMultiplier = 1f;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 当前生命值百分比 (0-1)
        /// </summary>
        public float HealthPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
        
        /// <summary>
        /// 是否处于狂暴状态
        /// </summary>
        public bool IsBerserkActive => _isBerserkActive;
        
        /// <summary>
        /// 当前狂暴攻速加成倍率
        /// </summary>
        public float CurrentBerserkMultiplier => _currentBerserkMultiplier;
        
        #endregion

        #region Unity 生命周期
        
        protected override void Update()
        {
            // 更新狂暴状态
            UpdateBerserkState();
            
            // 调用基类 Update
            base.Update();
        }
        
        #endregion

        #region 狂暴机制
        
        /// <summary>
        /// 更新狂暴状态
        /// </summary>
        private void UpdateBerserkState()
        {
            if (!IsAlive || DomainData == null) return;
            
            // 计算狂暴加成
            _currentBerserkMultiplier = CalculateBerserkMultiplier();
            
            // 判断是否进入狂暴状态
            bool shouldBeBerserk = HealthPercent < _berserkThreshold && _currentBerserkMultiplier > 1f;
            
            if (shouldBeBerserk != _isBerserkActive)
            {
                _isBerserkActive = shouldBeBerserk;
                OnBerserkStateChanged(_isBerserkActive);
            }
        }
        
        /// <summary>
        /// 计算狂暴倍率
        /// </summary>
        /// <returns>攻速倍率 (1.0 = 正常, 2.0 = 两倍速度)</returns>
        private float CalculateBerserkMultiplier()
        {
            if (HealthPercent >= _berserkThreshold)
            {
                return 1f;
            }
            
            // 公式: 1 + (1 - HealthPercent) * MaxBonusMultiplier
            // 例如: 血量50%, MaxBonus=1.0 → 1 + 0.5 * 1.0 = 1.5 (攻速提升50%)
            // 例如: 血量20%, MaxBonus=1.0 → 1 + 0.8 * 1.0 = 1.8 (攻速提升80%)
            float healthFactor = 1f - HealthPercent;
            return 1f + (healthFactor * _maxBonusMultiplier);
        }
        
        /// <summary>
        /// 计算实际攻击速度（重写虚方法）
        /// </summary>
        /// <returns>实际攻击间隔（秒）</returns>
        public override float CalculateAttackSpeed()
        {
            if (DomainData == null) return 1f;
            
            // 基础攻击间隔 / 狂暴倍率 = 实际间隔
            // 倍率越高，间隔越短，攻速越快
            float baseSpeed = DomainData.AttackSpeed;
            return baseSpeed / _currentBerserkMultiplier;
        }
        
        /// <summary>
        /// 狂暴状态变化回调
        /// </summary>
        private void OnBerserkStateChanged(bool isActive)
        {
            if (isActive)
            {
                Debug.Log($"[BerserkerUnit] {DisplayName} 进入狂暴状态！攻速加成: {_currentBerserkMultiplier:F2}x");
                
                // 显示狂暴视觉效果
                if (_berserkEffectObject != null)
                {
                    _berserkEffectObject.SetActive(true);
                }
                
                if (_berserkParticles != null)
                {
                    _berserkParticles.Play();
                }
            }
            else
            {
                Debug.Log($"[BerserkerUnit] {DisplayName} 退出狂暴状态");
                
                // 隐藏狂暴视觉效果
                if (_berserkEffectObject != null)
                {
                    _berserkEffectObject.SetActive(false);
                }
                
                if (_berserkParticles != null)
                {
                    _berserkParticles.Stop();
                }
            }
        }
        
        #endregion

        #region 重写死亡
        
        protected override void Die()
        {
            // 清理狂暴效果
            if (_berserkEffectObject != null)
            {
                _berserkEffectObject.SetActive(false);
            }
            
            if (_berserkParticles != null)
            {
                _berserkParticles.Stop();
            }
            
            base.Die();
        }
        
        #endregion

        #region 调试
        
        private void OnGUI()
        {
            if (!IsAlive || DomainData == null) return;
            
            #if UNITY_EDITOR
            // 仅在编辑器中显示狂暴信息
            if (_isBerserkActive && Camera.main != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
                if (screenPos.z > 0)
                {
                    GUI.color = Color.red;
                    GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y, 100, 20), 
                        $"狂暴 x{_currentBerserkMultiplier:F1}");
                }
            }
            #endif
        }
        
        #endregion
    }
}
