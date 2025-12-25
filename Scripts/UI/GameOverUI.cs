// ============================================================
// GameOverUI.cs
// 游戏结束UI - 显示胜负结果和重新开始按钮
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using RTS.Managers;

namespace RTS.UI
{
    /// <summary>
    /// 游戏结束UI
    /// 挂载位置：Canvas 下的 GameOverPanel 对象
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        #region 配置
        
        [Header("UI 元素")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _gameTimeText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitButton;
        
        [Header("显示文本")]
        [SerializeField] private string _victoryTitle = "胜 利 ！";
        [SerializeField] private string _defeatTitle = "失 败";
        [SerializeField] private Color _victoryColor = Color.green;
        [SerializeField] private Color _defeatColor = Color.red;
        
        [Header("动画")]
        [SerializeField] private float _fadeInDuration = 0.5f;
        
        #endregion

        #region 私有字段
        
        private CanvasGroup _canvasGroup;
        private float _fadeTimer;
        private bool _isFading;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // 绑定按钮事件
            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
            
            // 初始隐藏
            Hide();
        }
        
        private void Update()
        {
            // 淡入动画
            if (_isFading)
            {
                _fadeTimer += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(_fadeTimer / _fadeInDuration);
                _canvasGroup.alpha = progress;
                
                if (progress >= 1f)
                {
                    _isFading = false;
                }
            }
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 显示游戏结束UI
        /// </summary>
        /// <param name="isVictory">是否胜利</param>
        /// <param name="winnerName">胜利者名称</param>
        public void Show(bool isVictory, string winnerName = "")
        {
            if (_panel != null)
            {
                _panel.SetActive(true);
            }
            gameObject.SetActive(true);
            
            // 设置标题
            if (_titleText != null)
            {
                _titleText.text = isVictory ? _victoryTitle : _defeatTitle;
                _titleText.color = isVictory ? _victoryColor : _defeatColor;
            }
            
            // 设置消息
            if (_messageText != null)
            {
                if (isVictory)
                {
                    _messageText.text = "恭喜你取得了胜利！";
                }
                else
                {
                    _messageText.text = string.IsNullOrEmpty(winnerName) 
                        ? "你的主基地被摧毁了..." 
                        : $"玩家 {winnerName} 获得了胜利";
                }
            }
            
            // 显示游戏时间
            if (_gameTimeText != null && GameManager.Instance != null)
            {
                _gameTimeText.text = $"游戏时间: {GameManager.Instance.GetFormattedGameTime()}";
            }
            
            // 开始淡入
            _canvasGroup.alpha = 0f;
            _fadeTimer = 0f;
            _isFading = true;
            
            // 暂停游戏但允许UI交互
            Time.timeScale = 0f;
            
            Debug.Log($"[GameOverUI] 显示游戏结束UI - {(isVictory ? "胜利" : "失败")}");
        }
        
        /// <summary>
        /// 隐藏游戏结束UI
        /// </summary>
        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
            gameObject.SetActive(false);
            _isFading = false;
        }
        
        #endregion

        #region 按钮回调
        
        private void OnRestartClicked()
        {
            Debug.Log("[GameOverUI] 点击重新开始");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
        
        private void OnMainMenuClicked()
        {
            Debug.Log("[GameOverUI] 点击返回主菜单");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ExitToMainMenu();
            }
        }
        
        private void OnQuitClicked()
        {
            Debug.Log("[GameOverUI] 点击退出游戏");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }
        
        #endregion
    }
}
