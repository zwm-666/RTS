// ============================================================
// ConfigValidatorEditor.cs - 配置校验编辑器工具
// ============================================================
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTS.Config;
using RTS.Presentation;

namespace RTS.Editor
{
    /// <summary>
    /// 配置校验编辑器工具
    /// 检查 ConfigData 中的所有 ID 是否在 PrefabRegistry 中有对应条目
    /// 使用方式：顶部菜单 RTS -> Config Validator
    /// </summary>
    public class ConfigValidatorEditor : EditorWindow
    {
        #region 字段
        
        private PrefabRegistry _registry;
        private Vector2 _scrollPosition;
        private List<ValidationResult> _results = new List<ValidationResult>();
        private bool _hasValidated = false;
        
        #endregion

        #region 菜单入口
        
        [MenuItem("RTS/Config Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConfigValidatorEditor>("配置校验器");
            window.minSize = new Vector2(400, 300);
        }
        
        #endregion

        #region GUI
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 标题
            GUILayout.Label("配置校验工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "此工具用于检查 GameConfig.json 中的所有单位/建筑ID是否在 PrefabRegistry 中有对应的预制体配置。",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 选择 Registry
            _registry = (PrefabRegistry)EditorGUILayout.ObjectField(
                "Prefab Registry", 
                _registry, 
                typeof(PrefabRegistry), 
                false);
            
            EditorGUILayout.Space(10);
            
            // 校验按钮
            EditorGUI.BeginDisabledGroup(_registry == null);
            if (GUILayout.Button("执行校验", GUILayout.Height(30)))
            {
                ValidateConfig();
            }
            EditorGUI.EndDisabledGroup();
            
            if (_registry == null)
            {
                EditorGUILayout.HelpBox("请先指定 PrefabRegistry 资源", MessageType.Warning);
            }
            
            EditorGUILayout.Space(10);
            
            // 结果显示
            if (_hasValidated)
            {
                DrawResults();
            }
        }
        
        private void DrawResults()
        {
            // 统计
            int errorCount = 0;
            int warningCount = 0;
            int successCount = 0;
            
            foreach (var result in _results)
            {
                switch (result.type)
                {
                    case ResultType.Error: errorCount++; break;
                    case ResultType.Warning: warningCount++; break;
                    case ResultType.Success: successCount++; break;
                }
            }
            
            // 摘要
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"✓ 成功: {successCount}", EditorStyles.label);
            GUILayout.Label($"⚠ 警告: {warningCount}", EditorStyles.label);
            GUILayout.Label($"✗ 错误: {errorCount}", EditorStyles.label);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 详细结果
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            foreach (var result in _results)
            {
                MessageType msgType = result.type switch
                {
                    ResultType.Error => MessageType.Error,
                    ResultType.Warning => MessageType.Warning,
                    _ => MessageType.Info
                };
                
                EditorGUILayout.HelpBox(result.message, msgType);
            }
            
            EditorGUILayout.EndScrollView();
            
            // 总结
            EditorGUILayout.Space(10);
            if (errorCount == 0 && warningCount == 0)
            {
                EditorGUILayout.HelpBox("✓ 所有配置均已正确关联预制体！", MessageType.Info);
            }
            else if (errorCount > 0)
            {
                EditorGUILayout.HelpBox($"发现 {errorCount} 个错误，请在 PrefabRegistry 中添加缺失的预制体映射。", MessageType.Error);
            }
        }
        
        #endregion

        #region 校验逻辑
        
        private void ValidateConfig()
        {
            _results.Clear();
            _hasValidated = true;
            
            // 加载配置
            var loader = new ConfigLoader();
            var config = loader.LoadFromResources();
            
            if (config == null)
            {
                _results.Add(new ValidationResult
                {
                    type = ResultType.Error,
                    message = "无法加载 GameConfig.json，请检查 Resources/Config/GameConfig.json 是否存在"
                });
                return;
            }
            
            // 构建 Registry 中的 ID 集合
            var registeredUnitIds = new HashSet<string>();
            var registeredBuildingIds = new HashSet<string>();
            
            foreach (var id in _registry.GetAllUnitIds())
            {
                registeredUnitIds.Add(id);
            }
            
            foreach (var id in _registry.GetAllBuildingIds())
            {
                registeredBuildingIds.Add(id);
            }
            
            // 校验单位
            if (config.units != null)
            {
                foreach (var unit in config.units)
                {
                    if (string.IsNullOrEmpty(unit.unitId))
                    {
                        _results.Add(new ValidationResult
                        {
                            type = ResultType.Warning,
                            message = $"[单位] '{unit.displayName}' 缺少 unitId"
                        });
                        continue;
                    }
                    
                    if (registeredUnitIds.Contains(unit.unitId))
                    {
                        _results.Add(new ValidationResult
                        {
                            type = ResultType.Success,
                            message = $"[单位] {unit.unitId} ({unit.displayName}) ✓"
                        });
                    }
                    else
                    {
                        _results.Add(new ValidationResult
                        {
                            type = ResultType.Error,
                            message = $"[单位] {unit.unitId} ({unit.displayName}) - 缺少预制体映射"
                        });
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
                        _results.Add(new ValidationResult
                        {
                            type = ResultType.Warning,
                            message = $"[建筑] '{building.displayName}' 缺少 buildingId"
                        });
                        continue;
                    }
                    
                    if (registeredBuildingIds.Contains(building.buildingId))
                    {
                        _results.Add(new ValidationResult
                        {
                            type = ResultType.Success,
                            message = $"[建筑] {building.buildingId} ({building.displayName}) ✓"
                        });
                    }
                    else
                    {
                        _results.Add(new ValidationResult
                        {
                            type = ResultType.Error,
                            message = $"[建筑] {building.buildingId} ({building.displayName}) - 缺少预制体映射"
                        });
                    }
                }
            }
            
            Debug.Log($"[ConfigValidator] 校验完成: {_results.Count} 条结果");
        }
        
        #endregion

        #region 数据结构
        
        private enum ResultType
        {
            Success,
            Warning,
            Error
        }
        
        private class ValidationResult
        {
            public ResultType type;
            public string message;
        }
        
        #endregion
    }
}

#endif
