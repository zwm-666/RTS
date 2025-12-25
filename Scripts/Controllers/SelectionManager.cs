// ============================================================
// SelectionManager.cs
// 选择管理器 - 处理点选和框选
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.Interfaces;

namespace RTS.Controllers
{
    /// <summary>
    /// 选择管理器 - 负责管理所有选择相关功能
    /// 挂载位置：GameManager 空对象上
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        #region 单例
        
        private static SelectionManager _instance;
        public static SelectionManager Instance => _instance;
        
        #endregion

        #region 事件
        
        /// <summary>
        /// 选择变化事件
        /// </summary>
        public event Action<List<ISelectable>> OnSelectionChanged;
        
        #endregion

        #region 配置
        
        [Header("玩家配置")]
        [SerializeField] private int _localPlayerId = 0;
        
        [Header("射线检测")]
        [SerializeField] private LayerMask _selectableLayer;
        
        [Header("框选配置")]
        [SerializeField] private bool _enableBoxSelection = true;
        [SerializeField] private float _boxSelectThreshold = 10f;
        
        [Header("框选样式")]
        [SerializeField] private Color _boxFillColor = new Color(0, 1, 0, 0.2f);
        [SerializeField] private Color _boxBorderColor = new Color(0, 1, 0, 0.8f);
        [SerializeField] private float _boxBorderWidth = 2f;
        
        [Header("高亮配置")]
        [SerializeField] private Color _selectionHighlightColor = Color.green;
        [SerializeField] private float _highlightIntensity = 0.3f;
        
        #endregion

        #region 私有字段
        
        private List<ISelectable> _selectedObjects = new List<ISelectable>();
        private Camera _mainCamera;
        
        // 框选状态
        private bool _isBoxSelecting;
        private Vector3 _boxSelectStartPos;
        
        // 材质缓存（用于高亮）
        private Dictionary<ISelectable, Material[]> _originalMaterials = new Dictionary<ISelectable, Material[]>();
        
        // GUI 样式
        private Texture2D _boxFillTexture;
        private Texture2D _boxBorderTexture;
        
        #endregion

        #region 属性
        
        public List<ISelectable> SelectedObjects => _selectedObjects;
        public int SelectedCount => _selectedObjects.Count;
        public int LocalPlayerId => _localPlayerId;
        public bool HasSelection => _selectedObjects.Count > 0;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            _instance = this;
            _mainCamera = Camera.main;
            
            // 创建框选纹理
            CreateBoxTextures();
        }
        
        private void Update()
        {
            HandleSelectionInput();
        }
        
        private void OnGUI()
        {
            if (_isBoxSelecting)
            {
                DrawSelectionBox();
            }
        }
        
        #endregion

        #region 初始化
        
        private void CreateBoxTextures()
        {
            // 填充纹理
            _boxFillTexture = new Texture2D(1, 1);
            _boxFillTexture.SetPixel(0, 0, _boxFillColor);
            _boxFillTexture.Apply();
            
            // 边框纹理
            _boxBorderTexture = new Texture2D(1, 1);
            _boxBorderTexture.SetPixel(0, 0, _boxBorderColor);
            _boxBorderTexture.Apply();
        }
        
        #endregion

        #region 输入处理
        
        private void HandleSelectionInput()
        {
            // 左键按下 - 开始选择
            if (Input.GetMouseButtonDown(0))
            {
                OnLeftMouseDown();
            }
            
            // 左键持续按住 - 框选中
            if (Input.GetMouseButton(0) && _enableBoxSelection)
            {
                OnLeftMouseHold();
            }
            
            // 左键释放 - 完成选择
            if (Input.GetMouseButtonUp(0))
            {
                OnLeftMouseUp();
            }
            
            // ESC - 取消选择
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearSelection();
            }
        }
        
        private void OnLeftMouseDown()
        {
            _boxSelectStartPos = Input.mousePosition;
            _isBoxSelecting = false;
            
            // 检测点击可选对象
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _selectableLayer))
            {
                ISelectable clicked = hit.collider.GetComponent<ISelectable>();
                if (clicked == null)
                {
                    clicked = hit.collider.GetComponentInParent<ISelectable>();
                }
                
                if (clicked != null && clicked.PlayerId == _localPlayerId && clicked.IsAlive)
                {
                    // Shift 加选
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        ToggleSelection(clicked);
                    }
                    else
                    {
                        // 替换选择
                        ClearSelection();
                        Select(clicked);
                    }
                    return;
                }
            }
            
            // 点击空地，准备框选
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                // 不是 Shift 键时清除选择
                // 注意：这里暂不清除，等框选完成后再决定
            }
        }
        
        private void OnLeftMouseHold()
        {
            float dragDistance = Vector3.Distance(_boxSelectStartPos, Input.mousePosition);
            if (dragDistance > _boxSelectThreshold)
            {
                _isBoxSelecting = true;
            }
        }
        
        private void OnLeftMouseUp()
        {
            if (_isBoxSelecting)
            {
                FinishBoxSelection();
            }
            else
            {
                // 单击空地，清除选择（如果没有点中任何东西）
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out _, 1000f, _selectableLayer))
                {
                    if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                    {
                        ClearSelection();
                    }
                }
            }
            
            _isBoxSelecting = false;
        }
        
        #endregion

        #region 选择操作
        
        /// <summary>
        /// 选择对象
        /// </summary>
        public void Select(ISelectable obj)
        {
            if (obj == null || _selectedObjects.Contains(obj)) return;
            
            _selectedObjects.Add(obj);
            obj.OnSelected();
            ApplyHighlight(obj, true);
            
            OnSelectionChanged?.Invoke(_selectedObjects);
            Debug.Log($"[Selection] 选中: {(obj as MonoBehaviour)?.name}");
        }
        
        /// <summary>
        /// 取消选择对象
        /// </summary>
        public void Deselect(ISelectable obj)
        {
            if (obj == null || !_selectedObjects.Contains(obj)) return;
            
            _selectedObjects.Remove(obj);
            obj.OnDeselected();
            ApplyHighlight(obj, false);
            
            OnSelectionChanged?.Invoke(_selectedObjects);
        }
        
        /// <summary>
        /// 切换选择状态
        /// </summary>
        public void ToggleSelection(ISelectable obj)
        {
            if (_selectedObjects.Contains(obj))
            {
                Deselect(obj);
            }
            else
            {
                Select(obj);
            }
        }
        
        /// <summary>
        /// 清除所有选择
        /// </summary>
        public void ClearSelection()
        {
            foreach (var obj in _selectedObjects.ToArray())
            {
                if (obj != null)
                {
                    obj.OnDeselected();
                    ApplyHighlight(obj, false);
                }
            }
            _selectedObjects.Clear();
            
            OnSelectionChanged?.Invoke(_selectedObjects);
            Debug.Log("[Selection] 清除所有选择");
        }
        
        /// <summary>
        /// 获取所有选中的可命令对象
        /// </summary>
        public List<ICommandable> GetSelectedCommandables()
        {
            List<ICommandable> commandables = new List<ICommandable>();
            foreach (var obj in _selectedObjects)
            {
                if (obj is ICommandable cmd && obj.IsAlive)
                {
                    commandables.Add(cmd);
                }
            }
            return commandables;
        }
        
        #endregion

        #region 框选
        
        private void FinishBoxSelection()
        {
            Vector3 boxEnd = Input.mousePosition;
            
            // 计算框选矩形
            float minX = Mathf.Min(_boxSelectStartPos.x, boxEnd.x);
            float maxX = Mathf.Max(_boxSelectStartPos.x, boxEnd.x);
            float minY = Mathf.Min(_boxSelectStartPos.y, boxEnd.y);
            float maxY = Mathf.Max(_boxSelectStartPos.y, boxEnd.y);
            
            Rect selectionRect = new Rect(minX, minY, maxX - minX, maxY - minY);
            
            // Shift 键不清除原有选择
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                ClearSelection();
            }
            
            // 查找所有在框选区域内的可选对象
            MonoBehaviour[] allSelectables = FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in allSelectables)
            {
                if (mb is ISelectable selectable)
                {
                    if (selectable.PlayerId != _localPlayerId || !selectable.IsAlive) continue;
                    
                    Vector3 screenPos = _mainCamera.WorldToScreenPoint(selectable.Transform.position);
                    if (screenPos.z > 0 && selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
                    {
                        Select(selectable);
                    }
                }
            }
            
            Debug.Log($"[Selection] 框选完成，选中 {_selectedObjects.Count} 个对象");
        }
        
        private void DrawSelectionBox()
        {
            Vector3 boxEnd = Input.mousePosition;
            
            // 计算矩形（GUI 坐标系 Y 轴翻转）
            float x = Mathf.Min(_boxSelectStartPos.x, boxEnd.x);
            float y = Screen.height - Mathf.Max(_boxSelectStartPos.y, boxEnd.y);
            float width = Mathf.Abs(boxEnd.x - _boxSelectStartPos.x);
            float height = Mathf.Abs(boxEnd.y - _boxSelectStartPos.y);
            
            Rect rect = new Rect(x, y, width, height);
            
            // 绘制填充
            GUI.DrawTexture(rect, _boxFillTexture);
            
            // 绘制边框
            // 上边
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, _boxBorderWidth), _boxBorderTexture);
            // 下边
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - _boxBorderWidth, rect.width, _boxBorderWidth), _boxBorderTexture);
            // 左边
            GUI.DrawTexture(new Rect(rect.x, rect.y, _boxBorderWidth, rect.height), _boxBorderTexture);
            // 右边
            GUI.DrawTexture(new Rect(rect.x + rect.width - _boxBorderWidth, rect.y, _boxBorderWidth, rect.height), _boxBorderTexture);
        }
        
        #endregion

        #region 高亮系统
        
        /// <summary>
        /// 应用选择高亮效果
        /// </summary>
        private void ApplyHighlight(ISelectable obj, bool selected)
        {
            MonoBehaviour mb = obj as MonoBehaviour;
            if (mb == null) return;
            
            Renderer[] renderers = mb.GetComponentsInChildren<Renderer>();
            
            if (selected)
            {
                // 保存原始材质并应用高亮
                if (!_originalMaterials.ContainsKey(obj))
                {
                    List<Material> mats = new List<Material>();
                    foreach (var r in renderers)
                    {
                        foreach (var m in r.materials)
                        {
                            mats.Add(new Material(m));
                        }
                    }
                    _originalMaterials[obj] = mats.ToArray();
                }
                
                // 应用高亮颜色
                foreach (var r in renderers)
                {
                    foreach (var m in r.materials)
                    {
                        if (m.HasProperty("_EmissionColor"))
                        {
                            m.EnableKeyword("_EMISSION");
                            m.SetColor("_EmissionColor", _selectionHighlightColor * _highlightIntensity);
                        }
                        else
                        {
                            // 备用：修改主颜色
                            Color original = m.color;
                            m.color = Color.Lerp(original, _selectionHighlightColor, 0.3f);
                        }
                    }
                }
            }
            else
            {
                // 恢复原始材质
                if (_originalMaterials.TryGetValue(obj, out Material[] originals))
                {
                    int index = 0;
                    foreach (var r in renderers)
                    {
                        Material[] mats = r.materials;
                        for (int i = 0; i < mats.Length && index < originals.Length; i++, index++)
                        {
                            if (mats[i].HasProperty("_EmissionColor"))
                            {
                                mats[i].SetColor("_EmissionColor", originals[index].GetColor("_EmissionColor"));
                            }
                            mats[i].color = originals[index].color;
                        }
                    }
                    _originalMaterials.Remove(obj);
                }
            }
        }
        
        #endregion
    }
}
