// ============================================================
// AudioManager.cs
// 全局音效管理器 - 高性能音频播放系统
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace RTS.Managers
{
    /// <summary>
    /// 音效优先级
    /// </summary>
    public enum SFXPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// 音效播放信息
    /// </summary>
    public class SFXPlayInfo
    {
        public AudioSource Source;
        public float StartTime;
        public SFXPriority Priority;
    }

    /// <summary>
    /// 全局音效管理器（单例模式）
    /// 使用对象池模式管理 AudioSource
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region 单例
        
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;
        
        #endregion

        #region 配置
        
        [Header("音频混合器（可选）")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _masterVolumeParam = "MasterVolume";
        [SerializeField] private string _musicVolumeParam = "MusicVolume";
        [SerializeField] private string _sfxVolumeParam = "SFXVolume";
        
        [Header("BGM 配置")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private float _defaultMusicVolume = 0.7f;
        
        [Header("SFX 配置")]
        [SerializeField] private int _initialPoolSize = 8;
        [SerializeField] private int _maxPoolSize = 16;
        [SerializeField] private float _defaultSFXVolume = 1f;
        
        [Header("淡入淡出")]
        [SerializeField] private float _defaultFadeDuration = 1f;
        
        #endregion

        #region 私有字段
        
        // SFX 对象池
        private List<SFXPlayInfo> _sfxPool = new List<SFXPlayInfo>();
        private Transform _sfxPoolParent;
        
        // 音量控制
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        
        // BGM 淡入淡出
        private Coroutine _musicFadeCoroutine;
        private AudioClip _currentMusicClip;
        
        #endregion

        #region 属性
        
        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        #endregion

        #region 初始化
        
        private void Initialize()
        {
            // 创建 BGM Source
            if (_musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
                _musicSource.spatialBlend = 0f; // 2D 音效
            }
            
            // 创建 SFX 对象池容器
            _sfxPoolParent = new GameObject("SFXPool").transform;
            _sfxPoolParent.SetParent(transform);
            
            // 预创建 SFX Source
            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateSFXSource();
            }
            
            // 应用初始音量
            SetMasterVolume(_masterVolume);
            SetMusicVolume(_musicVolume);
            SetSFXVolume(_sfxVolume);
            
            Debug.Log($"[AudioManager] 初始化完成 - SFX池大小: {_sfxPool.Count}");
        }
        
        private SFXPlayInfo CreateSFXSource()
        {
            GameObject sfxObj = new GameObject($"SFXSource_{_sfxPool.Count}");
            sfxObj.transform.SetParent(_sfxPoolParent);
            
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f; // 3D 音效
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 50f;
            
            var info = new SFXPlayInfo
            {
                Source = source,
                StartTime = 0f,
                Priority = SFXPriority.Normal
            };
            
            _sfxPool.Add(info);
            return info;
        }
        
        #endregion

        #region BGM 控制
        
        /// <summary>
        /// 播放背景音乐（带淡入淡出）
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeDuration = -1f)
        {
            if (clip == null) return;
            if (clip == _currentMusicClip && _musicSource.isPlaying) return;
            
            if (fadeDuration < 0) fadeDuration = _defaultFadeDuration;
            
            _currentMusicClip = clip;
            
            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }
            
            _musicFadeCoroutine = StartCoroutine(FadeMusicCoroutine(clip, fadeDuration));
        }
        
        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopMusic(float fadeDuration = -1f)
        {
            if (fadeDuration < 0) fadeDuration = _defaultFadeDuration;
            
            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }
            
            _musicFadeCoroutine = StartCoroutine(FadeOutMusicCoroutine(fadeDuration));
        }
        
        /// <summary>
        /// 暂停/恢复背景音乐
        /// </summary>
        public void PauseMusic(bool pause)
        {
            if (_musicSource == null) return;
            
            if (pause)
            {
                _musicSource.Pause();
            }
            else
            {
                _musicSource.UnPause();
            }
        }
        
        private IEnumerator FadeMusicCoroutine(AudioClip newClip, float duration)
        {
            float targetVolume = _defaultMusicVolume * _musicVolume * _masterVolume;
            
            // 淡出当前音乐
            if (_musicSource.isPlaying)
            {
                float startVolume = _musicSource.volume;
                float elapsed = 0f;
                
                while (elapsed < duration / 2f)
                {
                    elapsed += Time.deltaTime;
                    _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration / 2f));
                    yield return null;
                }
                
                _musicSource.Stop();
            }
            
            // 切换并淡入新音乐
            _musicSource.clip = newClip;
            _musicSource.volume = 0f;
            _musicSource.Play();
            
            float fadeInElapsed = 0f;
            while (fadeInElapsed < duration / 2f)
            {
                fadeInElapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(0f, targetVolume, fadeInElapsed / (duration / 2f));
                yield return null;
            }
            
            _musicSource.volume = targetVolume;
        }
        
        private IEnumerator FadeOutMusicCoroutine(float duration)
        {
            if (!_musicSource.isPlaying) yield break;
            
            float startVolume = _musicSource.volume;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            
            _musicSource.Stop();
            _currentMusicClip = null;
        }
        
        #endregion

        #region SFX 控制
        
        /// <summary>
        /// 播放音效
        /// </summary>
        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, SFXPriority priority = SFXPriority.Normal)
        {
            if (clip == null) return;
            
            SFXPlayInfo info = GetAvailableSFXSource(priority);
            if (info == null) return;
            
            info.Source.transform.position = position;
            info.Source.clip = clip;
            info.Source.volume = volume * _sfxVolume * _masterVolume;
            info.Source.pitch = 1f;
            info.StartTime = Time.time;
            info.Priority = priority;
            info.Source.Play();
        }
        
        /// <summary>
        /// 播放带随机音调的音效
        /// </summary>
        public void PlaySFXRandomPitch(AudioClip clip, Vector3 position, float volume = 1f, 
            float minPitch = 0.9f, float maxPitch = 1.1f, SFXPriority priority = SFXPriority.Normal)
        {
            if (clip == null) return;
            
            SFXPlayInfo info = GetAvailableSFXSource(priority);
            if (info == null) return;
            
            info.Source.transform.position = position;
            info.Source.clip = clip;
            info.Source.volume = volume * _sfxVolume * _masterVolume;
            info.Source.pitch = Random.Range(minPitch, maxPitch);
            info.StartTime = Time.time;
            info.Priority = priority;
            info.Source.Play();
        }
        
        /// <summary>
        /// 播放2D音效（不受位置影响）
        /// </summary>
        public void PlaySFX2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            
            SFXPlayInfo info = GetAvailableSFXSource(SFXPriority.Normal);
            if (info == null) return;
            
            info.Source.spatialBlend = 0f; // 2D
            info.Source.clip = clip;
            info.Source.volume = volume * _sfxVolume * _masterVolume;
            info.Source.pitch = 1f;
            info.StartTime = Time.time;
            info.Source.Play();
            
            // 播放完后恢复3D
            StartCoroutine(ResetSpatialBlendAfterPlay(info));
        }
        
        private IEnumerator ResetSpatialBlendAfterPlay(SFXPlayInfo info)
        {
            yield return new WaitWhile(() => info.Source.isPlaying);
            info.Source.spatialBlend = 1f;
        }
        
        /// <summary>
        /// 获取可用的 SFX Source
        /// </summary>
        private SFXPlayInfo GetAvailableSFXSource(SFXPriority priority)
        {
            // 1. 寻找空闲的 Source
            foreach (var info in _sfxPool)
            {
                if (!info.Source.isPlaying)
                {
                    return info;
                }
            }
            
            // 2. 如果未达上限，创建新的
            if (_sfxPool.Count < _maxPoolSize)
            {
                return CreateSFXSource();
            }
            
            // 3. 复用最旧且优先级最低的
            SFXPlayInfo oldest = null;
            float oldestTime = float.MaxValue;
            
            foreach (var info in _sfxPool)
            {
                // 不打断高优先级音效
                if (info.Priority > priority) continue;
                
                if (info.StartTime < oldestTime)
                {
                    oldestTime = info.StartTime;
                    oldest = info;
                }
            }
            
            if (oldest != null)
            {
                oldest.Source.Stop();
                return oldest;
            }
            
            // 4. 如果都是高优先级，只打断最旧的低优先级
            return null;
        }
        
        #endregion

        #region 音量控制
        
        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            
            if (_audioMixer != null)
            {
                // 使用 AudioMixer（分贝值）
                float dB = _masterVolume > 0 ? Mathf.Log10(_masterVolume) * 20f : -80f;
                _audioMixer.SetFloat(_masterVolumeParam, dB);
            }
            else
            {
                // 直接调整音量
                UpdateMusicVolume();
            }
        }
        
        /// <summary>
        /// 设置音乐音量
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            
            if (_audioMixer != null)
            {
                float dB = _musicVolume > 0 ? Mathf.Log10(_musicVolume) * 20f : -80f;
                _audioMixer.SetFloat(_musicVolumeParam, dB);
            }
            else
            {
                UpdateMusicVolume();
            }
        }
        
        /// <summary>
        /// 设置音效音量
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            
            if (_audioMixer != null)
            {
                float dB = _sfxVolume > 0 ? Mathf.Log10(_sfxVolume) * 20f : -80f;
                _audioMixer.SetFloat(_sfxVolumeParam, dB);
            }
        }
        
        private void UpdateMusicVolume()
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.volume = _defaultMusicVolume * _musicVolume * _masterVolume;
            }
        }
        
        /// <summary>
        /// 静音/取消静音
        /// </summary>
        public void SetMute(bool mute)
        {
            AudioListener.volume = mute ? 0f : 1f;
        }
        
        #endregion

        #region 便捷方法
        
        /// <summary>
        /// 播放UI音效（2D，中心位置）
        /// </summary>
        public void PlayUISound(AudioClip clip, float volume = 1f)
        {
            PlaySFX2D(clip, volume);
        }
        
        /// <summary>
        /// 播放一次性音效（类似 PlayClipAtPoint）
        /// </summary>
        public static void PlayOneShotAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (Instance != null)
            {
                Instance.PlaySFX(clip, position, volume);
            }
            else
            {
                // 回退到原生方法
                AudioSource.PlayClipAtPoint(clip, position, volume);
            }
        }
        
        #endregion

        #region 调试
        
        /// <summary>
        /// 获取当前活跃的音效数量
        /// </summary>
        public int GetActiveSFXCount()
        {
            int count = 0;
            foreach (var info in _sfxPool)
            {
                if (info.Source.isPlaying) count++;
            }
            return count;
        }
        
        #endregion
    }
}
