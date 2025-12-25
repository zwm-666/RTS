// ============================================================
// PauseMenuUI.cs
// 暂停菜单UI
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using RTS.Managers;

namespace RTS.UI
{
    /// <summary>
    /// 暂停菜单UI
    /// 挂载位置：Canvas 下的 PauseMenuPanel 对象
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        #region 配置
        
        [Header("UI 元素")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _gameTimeText;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _surrenderButton;    // 认输按钮
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitButton;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            // 绑定按钮事件
            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(OnResumeClicked);
            }
            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (_surrenderButton != null)
            {
                _surrenderButton.onClick.AddListener(OnSurrenderClicked);
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
        
        private void Start()
        {
            // 订阅暂停事件
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused += HandleGamePaused;
            }
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused -= HandleGamePaused;
            }
        }
        
        private void Update()
        {
            // 更新游戏时间显示
            if (_panel != null && _panel.activeSelf && _gameTimeText != null && GameManager.Instance != null)
            {
                _gameTimeText.text = $"游戏时间: {GameManager.Instance.GetFormattedGameTime()}";
            }
        }
        
        #endregion

        #region 事件处理
        
        private void HandleGamePaused(bool isPaused)
        {
            if (isPaused)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        #endregion

        #region 公共方法
        
        public void Show()
        {
            if (_panel != null)
            {
                _panel.SetActive(true);
            }
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
            gameObject.SetActive(false);
        }
        
        #endregion

        #region 按钮回调
        
        private void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }
        
        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
        
        private void OnMainMenuClicked()
        {
            // 恢复时间
            Time.timeScale = 1f;
            
            // 清理对象池
            if (RTS.Core.ObjectPoolManager.Instance != null)
            {
                RTS.Core.ObjectPoolManager.Instance.ClearAllPools();
            }
            
            // 使用SceneController切换场景
            if (RTS.Core.SceneController.Instance != null)
            {
                RTS.Core.SceneController.Instance.LoadMainMenu();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.ExitToMainMenu();
            }
        }
        
        private void OnQuitClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }
        
        private void OnSurrenderClicked()
        {
            // 先关闭暂停菜单，恢复游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            
            // 打开认输确认对话框（不暂停游戏）
            if (SurrenderConfirmUI.Instance != null)
            {
                SurrenderConfirmUI.Instance.Show();
            }
            else
            {
                Debug.LogWarning("[PauseMenuUI] 未找到 SurrenderConfirmUI！");
            }
        }
        
        #endregion
    }
}
