// ============================================================
// MainMenuUI.cs
// 主菜单界面
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using RTS.Core;
using RTS.Managers;

namespace RTS.UI
{
    /// <summary>
    /// 主菜单界面
    /// 挂载位置：主菜单场景的 Canvas 上
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        #region UI 引用
        
        [Header("主菜单按钮")]
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;
        
        [Header("设置面板")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Button _settingsCloseButton;
        
        [Header("音量文本")]
        [SerializeField] private Text _masterVolumeText;
        [SerializeField] private Text _musicVolumeText;
        [SerializeField] private Text _sfxVolumeText;
        
        [Header("游戏标题")]
        [SerializeField] private Text _titleText;
        [SerializeField] private string _gameTitle = "裂星纪元";
        
        #endregion

        #region Unity 生命周期
        
        private void Start()
        {
            InitializeUI();
            BindEvents();
            
            // 初始隐藏设置面板
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
            
            // 设置标题
            if (_titleText != null)
            {
                _titleText.text = _gameTitle;
            }
            
            // 确保时间恢复正常
            Time.timeScale = 1f;
        }
        
        private void OnDestroy()
        {
            UnbindEvents();
        }
        
        #endregion

        #region 初始化
        
        private void InitializeUI()
        {
            // 初始化音量滑条
            if (AudioManager.Instance != null)
            {
                if (_masterVolumeSlider != null)
                {
                    _masterVolumeSlider.value = AudioManager.Instance.MasterVolume;
                }
                if (_musicVolumeSlider != null)
                {
                    _musicVolumeSlider.value = AudioManager.Instance.MusicVolume;
                }
                if (_sfxVolumeSlider != null)
                {
                    _sfxVolumeSlider.value = AudioManager.Instance.SFXVolume;
                }
            }
            
            UpdateVolumeTexts();
        }
        
        private void BindEvents()
        {
            // 主菜单按钮
            if (_startGameButton != null)
            {
                _startGameButton.onClick.AddListener(OnStartGameClicked);
            }
            
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }
            
            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(OnExitClicked);
            }
            
            // 设置面板
            if (_settingsCloseButton != null)
            {
                _settingsCloseButton.onClick.AddListener(OnSettingsCloseClicked);
            }
            
            // 音量滑条
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }
        
        private void UnbindEvents()
        {
            if (_startGameButton != null)
            {
                _startGameButton.onClick.RemoveListener(OnStartGameClicked);
            }
            
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }
            
            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitClicked);
            }
        }
        
        #endregion

        #region 按钮回调
        
        private void OnStartGameClicked()
        {
            Debug.Log("[MainMenuUI] 开始游戏");
            
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadGameScene();
            }
            else
            {
                Debug.LogWarning("[MainMenuUI] SceneController 未找到");
            }
        }
        
        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] 打开设置");
            
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
            }
        }
        
        private void OnSettingsCloseClicked()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }
        
        private void OnExitClicked()
        {
            Debug.Log("[MainMenuUI] 退出游戏");
            
            if (SceneController.Instance != null)
            {
                SceneController.Instance.QuitGame();
            }
            else
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }
        
        #endregion

        #region 音量控制
        
        private void OnMasterVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
            UpdateVolumeTexts();
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(value);
            }
            UpdateVolumeTexts();
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
            UpdateVolumeTexts();
        }
        
        private void UpdateVolumeTexts()
        {
            if (_masterVolumeText != null && _masterVolumeSlider != null)
            {
                _masterVolumeText.text = $"主音量: {Mathf.RoundToInt(_masterVolumeSlider.value * 100)}%";
            }
            
            if (_musicVolumeText != null && _musicVolumeSlider != null)
            {
                _musicVolumeText.text = $"音乐: {Mathf.RoundToInt(_musicVolumeSlider.value * 100)}%";
            }
            
            if (_sfxVolumeText != null && _sfxVolumeSlider != null)
            {
                _sfxVolumeText.text = $"音效: {Mathf.RoundToInt(_sfxVolumeSlider.value * 100)}%";
            }
        }
        
        #endregion
    }
}
