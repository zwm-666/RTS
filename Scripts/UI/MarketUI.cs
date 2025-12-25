// ============================================================
// MarketUI.cs
// 市场交易界面
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using RTS.Core;
using RTS.Managers;

namespace RTS.UI
{
    /// <summary>
    /// 市场交易界面
    /// 挂载位置：市场面板 Panel 上
    /// </summary>
    public class MarketUI : MonoBehaviour
    {
        #region UI 引用
        
        [Header("面板")]
        [SerializeField] private GameObject _marketPanel;
        
        [Header("资源选择")]
        [SerializeField] private Dropdown _fromResourceDropdown;
        [SerializeField] private Dropdown _toResourceDropdown;
        
        [Header("数量输入")]
        [SerializeField] private InputField _amountInput;
        [SerializeField] private Slider _amountSlider;
        [SerializeField] private Text _maxAmountText;
        
        [Header("预览")]
        [SerializeField] private Text _previewText;
        [SerializeField] private Text _rateText;
        
        [Header("按钮")]
        [SerializeField] private Button _exchangeButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _maxButton;
        
        [Header("快捷按钮")]
        [SerializeField] private Button _btn10;
        [SerializeField] private Button _btn50;
        [SerializeField] private Button _btn100;
        [SerializeField] private Button _btn500;
        
        [Header("消息")]
        [SerializeField] private Text _messageText;
        [SerializeField] private float _messageDuration = 3f;
        
        #endregion

        #region 配置
        
        [Header("配置")]
        [SerializeField] private int _localPlayerId = 0;
        
        #endregion

        #region 私有字段
        
        private ResourceType _fromType = ResourceType.Wood;
        private ResourceType _toType = ResourceType.Gold;
        private int _currentAmount = 100;
        private float _messageTimer = 0f;
        
        #endregion

        #region Unity 生命周期
        
        private void Start()
        {
            InitializeUI();
            BindEvents();
            
            // 初始隐藏面板
            if (_marketPanel != null)
            {
                _marketPanel.SetActive(false);
            }
        }
        
        private void Update()
        {
            // 消息淡出
            if (_messageTimer > 0)
            {
                _messageTimer -= Time.deltaTime;
                if (_messageTimer <= 0 && _messageText != null)
                {
                    _messageText.text = "";
                }
            }
        }
        
        private void OnDestroy()
        {
            UnbindEvents();
        }
        
        #endregion

        #region 初始化
        
        private void InitializeUI()
        {
            // 初始化下拉框
            if (_fromResourceDropdown != null)
            {
                _fromResourceDropdown.ClearOptions();
                _fromResourceDropdown.AddOptions(new System.Collections.Generic.List<string> 
                    { "木材", "粮食", "金币" });
                _fromResourceDropdown.value = 0;
            }
            
            if (_toResourceDropdown != null)
            {
                _toResourceDropdown.ClearOptions();
                _toResourceDropdown.AddOptions(new System.Collections.Generic.List<string> 
                    { "金币", "木材", "粮食" });
                _toResourceDropdown.value = 0;
            }
            
            // 初始化数量
            if (_amountInput != null)
            {
                _amountInput.text = "100";
            }
            
            if (_amountSlider != null)
            {
                _amountSlider.minValue = RTS.Resources.MarketManager.Instance?.MinTradeAmount ?? 10;
                _amountSlider.maxValue = 1000;
                _amountSlider.value = 100;
            }
            
            UpdatePreview();
        }
        
        private void BindEvents()
        {
            if (_fromResourceDropdown != null)
            {
                _fromResourceDropdown.onValueChanged.AddListener(OnFromResourceChanged);
            }
            
            if (_toResourceDropdown != null)
            {
                _toResourceDropdown.onValueChanged.AddListener(OnToResourceChanged);
            }
            
            if (_amountInput != null)
            {
                _amountInput.onValueChanged.AddListener(OnAmountInputChanged);
            }
            
            if (_amountSlider != null)
            {
                _amountSlider.onValueChanged.AddListener(OnAmountSliderChanged);
            }
            
            if (_exchangeButton != null)
            {
                _exchangeButton.onClick.AddListener(OnExchangeClicked);
            }
            
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(CloseMarket);
            }
            
            if (_maxButton != null)
            {
                _maxButton.onClick.AddListener(OnMaxClicked);
            }
            
            // 快捷按钮
            if (_btn10 != null) _btn10.onClick.AddListener(() => SetAmount(10));
            if (_btn50 != null) _btn50.onClick.AddListener(() => SetAmount(50));
            if (_btn100 != null) _btn100.onClick.AddListener(() => SetAmount(100));
            if (_btn500 != null) _btn500.onClick.AddListener(() => SetAmount(500));
            
            // 订阅市场事件
            if (RTS.Resources.MarketManager.Instance != null)
            {
                RTS.Resources.MarketManager.Instance.OnTradeCompleted += OnTradeCompleted;
                RTS.Resources.MarketManager.Instance.OnTradeFailed += OnTradeFailed;
            }
        }
        
        private void UnbindEvents()
        {
            if (RTS.Resources.MarketManager.Instance != null)
            {
                RTS.Resources.MarketManager.Instance.OnTradeCompleted -= OnTradeCompleted;
                RTS.Resources.MarketManager.Instance.OnTradeFailed -= OnTradeFailed;
            }
        }
        
        #endregion

        #region UI 事件
        
        private void OnFromResourceChanged(int index)
        {
            _fromType = IndexToResourceType(index);
            UpdatePreview();
        }
        
        private void OnToResourceChanged(int index)
        {
            // 目标下拉框顺序: 金币、木材、粮食
            _toType = index switch
            {
                0 => ResourceType.Gold,
                1 => ResourceType.Wood,
                2 => ResourceType.Food,
                _ => ResourceType.Gold
            };
            UpdatePreview();
        }
        
        private void OnAmountInputChanged(string value)
        {
            if (int.TryParse(value, out int amount))
            {
                _currentAmount = Mathf.Clamp(amount, 1, 10000);
                
                if (_amountSlider != null)
                {
                    _amountSlider.value = _currentAmount;
                }
                
                UpdatePreview();
            }
        }
        
        private void OnAmountSliderChanged(float value)
        {
            _currentAmount = Mathf.RoundToInt(value);
            
            if (_amountInput != null)
            {
                _amountInput.text = _currentAmount.ToString();
            }
            
            UpdatePreview();
        }
        
        private void OnMaxClicked()
        {
            if (ResourceManager.Instance != null)
            {
                int maxAvailable = ResourceManager.Instance.GetResource(_localPlayerId, _fromType);
                int maxTrade = RTS.Resources.MarketManager.Instance?.MaxTradeAmount ?? 1000;
                SetAmount(Mathf.Min(maxAvailable, maxTrade));
            }
        }
        
        private void SetAmount(int amount)
        {
            _currentAmount = amount;
            
            if (_amountInput != null)
            {
                _amountInput.text = amount.ToString();
            }
            
            if (_amountSlider != null)
            {
                _amountSlider.value = amount;
            }
            
            UpdatePreview();
        }
        
        private void OnExchangeClicked()
        {
            if (_fromType == _toType)
            {
                ShowMessage("不能兑换相同类型的资源", Color.red);
                return;
            }
            
            if (RTS.Resources.MarketManager.Instance != null)
            {
                RTS.Resources.MarketManager.Instance.ExchangeResource(
                    _localPlayerId, _fromType, _toType, _currentAmount);
            }
        }
        
        #endregion

        #region 市场事件回调
        
        private void OnTradeCompleted(RTS.Resources.TradeRecord record)
        {
            ShowMessage($"交易成功！获得 {record.toAmount} {GetResourceName(record.toType)}", Color.green);
            UpdatePreview();
            UpdateMaxAmount();
        }
        
        private void OnTradeFailed(string reason)
        {
            ShowMessage($"交易失败：{reason}", Color.red);
        }
        
        #endregion

        #region 预览更新
        
        private void UpdatePreview()
        {
            if (RTS.Resources.MarketManager.Instance == null) return;
            
            // 更新汇率显示
            float rate = RTS.Resources.MarketManager.Instance.GetExchangeRate(_fromType, _toType);
            if (_rateText != null)
            {
                _rateText.text = $"汇率: 100 {GetResourceName(_fromType)} = {rate} {GetResourceName(_toType)}";
            }
            
            // 更新预览
            int result = RTS.Resources.MarketManager.Instance.PreviewExchange(_fromType, _toType, _currentAmount);
            if (_previewText != null)
            {
                _previewText.text = $"{_currentAmount} {GetResourceName(_fromType)} → {result} {GetResourceName(_toType)}";
            }
            
            UpdateMaxAmount();
        }
        
        private void UpdateMaxAmount()
        {
            if (_maxAmountText != null && ResourceManager.Instance != null)
            {
                int available = ResourceManager.Instance.GetResource(_localPlayerId, _fromType);
                _maxAmountText.text = $"可用: {available}";
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 打开市场面板
        /// </summary>
        public void OpenMarket()
        {
            if (_marketPanel != null)
            {
                _marketPanel.SetActive(true);
                UpdatePreview();
            }
        }
        
        /// <summary>
        /// 关闭市场面板
        /// </summary>
        public void CloseMarket()
        {
            if (_marketPanel != null)
            {
                _marketPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 切换市场面板
        /// </summary>
        public void ToggleMarket()
        {
            if (_marketPanel != null)
            {
                if (_marketPanel.activeSelf)
                {
                    CloseMarket();
                }
                else
                {
                    OpenMarket();
                }
            }
        }
        
        #endregion

        #region 辅助方法
        
        private ResourceType IndexToResourceType(int index)
        {
            return index switch
            {
                0 => ResourceType.Wood,
                1 => ResourceType.Food,
                2 => ResourceType.Gold,
                _ => ResourceType.Wood
            };
        }
        
        private string GetResourceName(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => "金币",
                ResourceType.Wood => "木材",
                ResourceType.Food => "粮食",
                _ => type.ToString()
            };
        }
        
        private void ShowMessage(string message, Color color)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
                _messageText.color = color;
                _messageTimer = _messageDuration;
            }
            
            Debug.Log($"[MarketUI] {message}");
        }
        
        #endregion
    }
}
