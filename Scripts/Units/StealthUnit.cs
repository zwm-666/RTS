// ============================================================
// StealthUnit.cs
// 隐身单位 - 脱战后自动隐身
// ============================================================

using UnityEngine;

namespace RTS.Units
{
    /// <summary>
    /// 隐身单位
    /// 特性：脱战 N 秒后自动进入隐身状态
    /// </summary>
    public class StealthUnit : Unit
    {
        #region 隐身配置
        
        [Header("===== 隐身系统 =====")]
        [Tooltip("脱战后多少秒进入隐身")]
        [SerializeField] private float _stealthDelay = 3f;
        
        [Tooltip("隐身状态下的透明度（对友军）")]
        [SerializeField] [Range(0f, 1f)] private float _stealthAlpha = 0.3f;
        
        [Tooltip("隐身状态视觉指示器（可选）")]
        [SerializeField] private GameObject _stealthEffectObject;
        
        [Tooltip("隐身粒子效果（可选）")]
        [SerializeField] private ParticleSystem _stealthParticles;
        
        [Tooltip("隐身图标（显示在单位头上）")]
        [SerializeField] private GameObject _stealthIcon;
        
        #endregion

        #region 运行时状态
        
        private bool _isStealthed = false;
        private float _outOfCombatTimer = 0f;
        private float _lastCombatTime = 0f;
        private Material[] _originalMaterials;
        private Color[] _originalColors;
        private bool _materialsInitialized = false;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 是否处于隐身状态
        /// </summary>
        public override bool IsStealthed => _isStealthed;
        
        /// <summary>
        /// 脱战时间
        /// </summary>
        public float OutOfCombatTime => Time.time - _lastCombatTime;
        
        #endregion

        #region Unity 生命周期
        
        protected override void Awake()
        {
            base.Awake();
            
            // 初始化材质缓存
            CacheMaterials();
            
            // 初始隐藏隐身图标
            if (_stealthIcon != null)
            {
                _stealthIcon.SetActive(false);
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (!IsAlive) return;
            
            // 更新隐身状态
            UpdateStealthState();
        }
        
        #endregion

        #region 隐身机制
        
        /// <summary>
        /// 更新隐身状态
        /// </summary>
        protected override void UpdateStealthState()
        {
            // 检查是否被侦测
            if (_isStealthed && IsDetectedByEnemy())
            {
                BreakStealth("被侦测");
                return;
            }
            
            // 如果已隐身，不需要更新计时器
            if (_isStealthed) return;
            
            // 检查是否脱战足够长时间
            if (OutOfCombatTime >= _stealthDelay)
            {
                EnterStealth();
            }
        }
        
        /// <summary>
        /// 进入隐身状态
        /// </summary>
        private void EnterStealth()
        {
            if (_isStealthed) return;
            
            _isStealthed = true;
            
            Debug.Log($"[StealthUnit] {DisplayName} 进入隐身状态");
            
            // 更新视觉效果
            UpdateStealthVisuals(true);
            
            // 显示隐身图标
            if (_stealthIcon != null)
            {
                _stealthIcon.SetActive(true);
            }
            
            // 播放隐身特效
            if (_stealthEffectObject != null)
            {
                _stealthEffectObject.SetActive(true);
            }
            
            if (_stealthParticles != null)
            {
                _stealthParticles.Play();
            }
        }
        
        /// <summary>
        /// 打破隐身状态
        /// </summary>
        public void BreakStealth(string reason = "")
        {
            if (!_isStealthed) return;
            
            _isStealthed = false;
            _lastCombatTime = Time.time; // 重置脱战计时器
            
            Debug.Log($"[StealthUnit] {DisplayName} 隐身被打破 - {reason}");
            
            // 恢复视觉效果
            UpdateStealthVisuals(false);
            
            // 隐藏隐身图标
            if (_stealthIcon != null)
            {
                _stealthIcon.SetActive(false);
            }
            
            // 停止隐身特效
            if (_stealthEffectObject != null)
            {
                _stealthEffectObject.SetActive(false);
            }
            
            if (_stealthParticles != null)
            {
                _stealthParticles.Stop();
            }
        }
        
        /// <summary>
        /// 标记进入战斗（攻击或受到伤害）
        /// </summary>
        public void MarkInCombat()
        {
            _lastCombatTime = Time.time;
            
            if (_isStealthed)
            {
                BreakStealth("进入战斗");
            }
        }
        
        /// <summary>
        /// 检查是否被敌方侦测单位侦测到
        /// </summary>
        private bool IsDetectedByEnemy()
        {
            // 查找所有敌方侦测单位
            Unit[] allUnits = FindObjectsOfType<Unit>();
            
            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                if (unit.PlayerId == PlayerId) continue; // 忽略友军
                if (!unit.IsDetector) continue; // 不是侦测单位
                
                // 检查距离是否在侦测范围内
                float distance = Vector3.Distance(transform.position, unit.transform.position);
                float detectionRange = unit.DomainData?.SightRange ?? 10f;
                
                if (distance <= detectionRange)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion

        #region 视觉效果
        
        /// <summary>
        /// 缓存原始材质
        /// </summary>
        private void CacheMaterials()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            _originalMaterials = new Material[renderers.Length];
            _originalColors = new Color[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material != null)
                {
                    _originalMaterials[i] = renderers[i].material;
                    _originalColors[i] = renderers[i].material.color;
                }
            }
            
            _materialsInitialized = true;
        }
        
        /// <summary>
        /// 更新隐身视觉效果
        /// </summary>
        private void UpdateStealthVisuals(bool stealthed)
        {
            if (!_materialsInitialized) return;
            
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            // 判断是否是本地玩家的单位
            bool isLocalPlayer = RTS.Map.FogOfWarManager.Instance != null && 
                                 PlayerId == RTS.Map.FogOfWarManager.Instance.LocalPlayerId;
            
            for (int i = 0; i < renderers.Length && i < _originalColors.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null) continue;
                
                if (stealthed)
                {
                    if (isLocalPlayer)
                    {
                        // 友军：半透明
                        Color color = _originalColors[i];
                        color.a = _stealthAlpha;
                        renderers[i].material.color = color;
                        
                        // 设置材质为透明模式
                        SetMaterialTransparent(renderers[i].material, true);
                    }
                    else
                    {
                        // 敌军：完全隐藏
                        renderers[i].enabled = false;
                    }
                }
                else
                {
                    // 恢复原始状态
                    renderers[i].enabled = true;
                    renderers[i].material.color = _originalColors[i];
                    SetMaterialTransparent(renderers[i].material, false);
                }
            }
        }
        
        /// <summary>
        /// 设置材质透明模式
        /// </summary>
        private void SetMaterialTransparent(Material mat, bool transparent)
        {
            if (transparent)
            {
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            else
            {
                mat.SetFloat("_Mode", 0); // Opaque mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
            }
        }
        
        #endregion

        #region 重写战斗方法
        
        /// <summary>
        /// 重写攻击方法 - 攻击时打破隐身
        /// </summary>
        public new bool Attack(Unit target)
        {
            // 攻击时打破隐身
            MarkInCombat();
            
            return base.Attack(target);
        }
        
        /// <summary>
        /// 重写受伤方法 - 受伤时打破隐身
        /// </summary>
        public new int TakeDamage(int damage, RTS.Domain.Enums.AttackType attackType)
        {
            // 受伤时打破隐身
            MarkInCombat();
            
            return base.TakeDamage(damage, attackType);
        }
        
        #endregion

        #region 死亡处理
        
        protected override void Die()
        {
            // 清理隐身效果
            if (_stealthEffectObject != null)
            {
                _stealthEffectObject.SetActive(false);
            }
            
            if (_stealthParticles != null)
            {
                _stealthParticles.Stop();
            }
            
            if (_stealthIcon != null)
            {
                _stealthIcon.SetActive(false);
            }
            
            base.Die();
        }
        
        #endregion
    }
}
