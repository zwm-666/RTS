// ============================================================
// SurrenderConfirmUI.cs
// 认输确认对话框 - 不暂停游戏
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using RTS.Managers;

namespace RTS.UI
{
    /// <summary>
    /// 认输确认对话框
    /// 特点：显示时不暂停游戏，游戏继续进行
    /// 挂载位置：Canvas 下的 SurrenderConfirmPanel 对象
    /// </summary>
    public class SurrenderConfirmUI : MonoBehaviour
    {
        #region 单例
        
        private static SurrenderConfirmUI _instance;
        public static SurrenderConfirmUI Instance => _instance;
        
        #endregion

        #region 配置
        
        [Header("UI 元素")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        
        [Header("显示文本")]
        [SerializeField] private string _title = "确认认输";
        [SerializeField] private string _message = "你确定要认输吗？\n认输后将立即判定为失败。";
        
        [Header("快捷键")]
        [SerializeField] private KeyCode _surrenderHotkey = KeyCode.F10;
        
        #endregion

        #region 私有字段
        
        private bool _isShowing = false;
        
        #endregion

        #region 属性
        
        public bool IsShowing => _isShowing;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            _instance = this;
            
            // 绑定按钮事件
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }
            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
            
            // 初始隐藏
            Hide();
        }
        
        private void Update()
        {
            // 快捷键打开认输确认
            if (Input.GetKeyDown(_surrenderHotkey))
            {
                if (_isShowing)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }
            
            // ESC 关闭对话框
            if (_isShowing && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
            
            // Enter 确认
            if (_isShowing && Input.GetKeyDown(KeyCode.Return))
            {
                OnConfirmClicked();
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 显示认输确认对话框（不暂停游戏）
        /// </summary>
        public void Show()
        {
            // 检查游戏是否还在进行中
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                Debug.LogWarning("[SurrenderConfirmUI] 游戏未在进行中，无法认输");
                return;
            }
            
            if (_panel != null)
            {
                _panel.SetActive(true);
            }
            gameObject.SetActive(true);
            _isShowing = true;
            
            // 设置文本
            if (_titleText != null)
            {
                _titleText.text = _title;
            }
            if (_messageText != null)
            {
                _messageText.text = _message;
            }
            
            Debug.Log("[SurrenderConfirmUI] 显示认输确认对话框");
        }
        
        /// <summary>
        /// 隐藏认输确认对话框
        /// </summary>
        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
            gameObject.SetActive(false);
            _isShowing = false;
        }
        
        #endregion

        #region 按钮回调
        
        /// <summary>
        /// 确认认输
        /// </summary>
        private void OnConfirmClicked()
        {
            Hide();
            
            if (GameManager.Instance != null)
            {
                // 本地玩家认输
                GameManager.Instance.LocalPlayerSurrender();
            }
        }
        
        /// <summary>
        /// 取消认输
        /// </summary>
        private void OnCancelClicked()
        {
            Hide();
            Debug.Log("[SurrenderConfirmUI] 取消认输");
        }
        
        #endregion
    }
}
