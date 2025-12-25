// ============================================================
// ConfigLoader.cs - JSON配置加载器
// ============================================================

using System.IO;
using UnityEngine;

namespace RTS.Config
{
    /// <summary>
    /// 配置加载器
    /// 只负责读取和反序列化JSON，不做业务逻辑
    /// </summary>
    public class ConfigLoader
    {
        private const string DEFAULT_RESOURCES_PATH = "Config/GameConfig";
        
        #region 从 Resources 加载
        
        /// <summary>
        /// 从 Resources 文件夹加载配置
        /// </summary>
        /// <param name="path">相对于 Resources 的路径（不含扩展名）</param>
        public GameConfigDto LoadFromResources(string path = null)
        {
            path ??= DEFAULT_RESOURCES_PATH;
            
            TextAsset textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                Debug.LogError($"[ConfigLoader] 无法从 Resources 加载配置: {path}");
                return null;
            }
            
            return ParseJson(textAsset.text, path);
        }
        
        #endregion

        #region 从 StreamingAssets 加载
        
        /// <summary>
        /// 从 StreamingAssets 文件夹加载配置（支持热更新）
        /// </summary>
        /// <param name="fileName">文件名（含扩展名）</param>
        public GameConfigDto LoadFromStreamingAssets(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            
            if (!File.Exists(path))
            {
                Debug.LogError($"[ConfigLoader] 文件不存在: {path}");
                return null;
            }
            
            string json = File.ReadAllText(path);
            return ParseJson(json, path);
        }
        
        #endregion

        #region 从字符串加载
        
        /// <summary>
        /// 从 JSON 字符串加载配置
        /// </summary>
        public GameConfigDto LoadFromString(string json)
        {
            return ParseJson(json, "string input");
        }
        
        #endregion

        #region 辅助方法
        
        private GameConfigDto ParseJson(string json, string source)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[ConfigLoader] JSON 内容为空: {source}");
                return null;
            }
            
            try
            {
                var config = JsonUtility.FromJson<GameConfigDto>(json);
                
                if (config == null)
                {
                    Debug.LogError($"[ConfigLoader] JSON 解析结果为空: {source}");
                    return null;
                }
                
                Debug.Log($"[ConfigLoader] 配置加载成功: {source}, 版本: {config.version}");
                return config;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ConfigLoader] JSON 解析失败: {source}, 错误: {e.Message}");
                return null;
            }
        }
        
        #endregion

        #region 校验方法
        
        /// <summary>
        /// 校验配置完整性
        /// </summary>
        public bool ValidateConfig(GameConfigDto config)
        {
            if (config == null)
            {
                Debug.LogError("[ConfigLoader] 配置为空");
                return false;
            }
            
            bool isValid = true;
            
            // 校验单位
            if (config.units != null)
            {
                foreach (var unit in config.units)
                {
                    if (string.IsNullOrEmpty(unit.unitId))
                    {
                        Debug.LogWarning($"[ConfigLoader] 单位缺少 unitId: {unit.displayName}");
                        isValid = false;
                    }
                }
            }
            
            // 校验建筑
            if (config.buildings != null)
            {
                foreach (var building in config.buildings)
                {
                    if (string.IsNullOrEmpty(building.buildingId))
                    {
                        Debug.LogWarning($"[ConfigLoader] 建筑缺少 buildingId: {building.displayName}");
                        isValid = false;
                    }
                }
            }
            
            return isValid;
        }
        
        #endregion
    }
}
