// ============================================================
// HealthBar.cs
// 生命值条辅助脚本 - 控制颜色变化
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
    /// <summary>
    /// 生命值条辅助脚本
    /// 根据血量百分比控制颜色变化
    /// 挂载位置：生命值 Slider 对象上
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class HealthBar : MonoBehaviour
    {
        #region 配置
        
        [Header("颜色配置")]
        [Tooltip("满血时的颜色")]
        [SerializeField] private Color _fullHealthColor = Color.green;
        
        [Tooltip("中等血量颜色")]
        [SerializeField] private Color _mediumHealthColor = Color.yellow;
        
        [Tooltip("低血量颜色")]
        [SerializeField] private Color _lowHealthColor = Color.red;
        
        [Header("阈值配置")]
        [Tooltip("中等血量阈值（低于此值显示黄色）")]
        [SerializeField] [Range(0f, 1f)] private float _mediumThreshold = 0.6f;
        
        [Tooltip("低血量阈值（低于此值显示红色）")]
        [SerializeField] [Range(0f, 1f)] private float _lowThreshold = 0.3f;
        
        [Header("动画配置")]
        [SerializeField] private bool _enablePulse = true;
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _pulseIntensity = 0.3f;
        
        #endregion

        #region 私有字段
        
        private Slider _slider;
        private Image _fillImage;
        private float _currentRatio = 1f;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            _slider = GetComponent<Slider>();
            
            // 查找填充图像
            if (_slider != null && _slider.fillRect != null)
            {
                _fillImage = _slider.fillRect.GetComponent<Image>();
            }
        }
        
        private void Update()
        {
            // 低血量时脉冲闪烁效果
            if (_enablePulse && _currentRatio <= _lowThreshold && _fillImage != null)
            {
                float pulse = Mathf.Sin(Time.time * _pulseSpeed) * _pulseIntensity;
                Color baseColor = GetHealthColor(_currentRatio);
                _fillImage.color = new Color(
                    Mathf.Clamp01(baseColor.r + pulse),
                    baseColor.g,
                    baseColor.b,
                    baseColor.a
                );
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 更新生命值显示
        /// </summary>
        /// <param name="ratio">生命值百分比 (0-1)</param>
        public void UpdateHealth(float ratio)
        {
            _currentRatio = Mathf.Clamp01(ratio);
            
            // 更新 Slider 值
            if (_slider != null)
            {
                _slider.value = _currentRatio;
            }
            
            // 更新颜色
            if (_fillImage != null)
            {
                _fillImage.color = GetHealthColor(_currentRatio);
            }
        }
        
        /// <summary>
        /// 根据生命值百分比获取颜色
        /// </summary>
        public Color GetHealthColor(float ratio)
        {
            if (ratio <= _lowThreshold)
            {
                // 低血量 → 红色
                return _lowHealthColor;
            }
            else if (ratio <= _mediumThreshold)
            {
                // 中等血量 → 黄色到红色的渐变
                float t = (ratio - _lowThreshold) / (_mediumThreshold - _lowThreshold);
                return Color.Lerp(_lowHealthColor, _mediumHealthColor, t);
            }
            else
            {
                // 高血量 → 黄色到绿色的渐变
                float t = (ratio - _mediumThreshold) / (1f - _mediumThreshold);
                return Color.Lerp(_mediumHealthColor, _fullHealthColor, t);
            }
        }
        
        /// <summary>
        /// 设置自定义颜色范围
        /// </summary>
        public void SetColors(Color full, Color medium, Color low)
        {
            _fullHealthColor = full;
            _mediumHealthColor = medium;
            _lowHealthColor = low;
        }
        
        /// <summary>
        /// 设置阈值
        /// </summary>
        public void SetThresholds(float medium, float low)
        {
            _mediumThreshold = Mathf.Clamp01(medium);
            _lowThreshold = Mathf.Clamp01(low);
        }
        
        #endregion

        #region 编辑器预览
        
        private void OnValidate()
        {
            // 确保低阈值小于中阈值
            if (_lowThreshold > _mediumThreshold)
            {
                _lowThreshold = _mediumThreshold;
            }
        }
        
        #endregion
    }
}
