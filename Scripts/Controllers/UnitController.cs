// ============================================================
// UnitController.cs
// 单位控制器 - 处理鼠标点击和选择
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using RTS.Units;
using RTS.Map;

namespace RTS.Controllers
{
    /// <summary>
    /// 单位控制器 - 处理玩家输入和单位命令
    /// </summary>
    public class UnitController : MonoBehaviour
    {
        #region 单例
        
        private static UnitController _instance;
        public static UnitController Instance => _instance;
        
        #endregion

        #region 配置
        
        [Header("玩家配置")]
        [SerializeField] private int _localPlayerId = 0;
        
        [Header("射线检测")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _unitLayer;
        
        [Header("选择框")]
        [SerializeField] private bool _enableBoxSelection = true;
        [SerializeField] private Color _selectionBoxColor = new Color(0, 1, 0, 0.3f);
        [SerializeField] private Color _selectionBoxBorderColor = Color.green;
        
        [Header("移动标记")]
        [SerializeField] private GameObject _moveMarkerPrefab;
        [SerializeField] private float _moveMarkerDuration = 1f;
        
        #endregion

        #region 私有字段
        
        private List<Unit> _selectedUnits = new List<Unit>();
        private Camera _mainCamera;
        
        // 框选
        private bool _isBoxSelecting;
        private Vector3 _boxSelectStart;
        
        #endregion

        #region 属性
        
        public List<Unit> SelectedUnits => _selectedUnits;
        public int SelectedCount => _selectedUnits.Count;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            _instance = this;
            _mainCamera = Camera.main;
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void OnGUI()
        {
            if (_isBoxSelecting)
            {
                DrawSelectionBox();
            }
        }
        
        #endregion

        #region 输入处理
        
        private void HandleInput()
        {
            // 左键 - 选择
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }
            
            // 左键拖拽 - 框选
            if (Input.GetMouseButton(0) && _enableBoxSelection)
            {
                HandleBoxSelection();
            }
            
            if (Input.GetMouseButtonUp(0) && _isBoxSelecting)
            {
                FinishBoxSelection();
            }
            
            // 右键 - 移动/攻击命令
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
            
            // ESC - 取消选择
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearSelection();
            }
            
            // S - 停止
            if (Input.GetKeyDown(KeyCode.S))
            {
                StopSelectedUnits();
            }
        }
        
        /// <summary>
        /// 处理左键点击（选择单位）
        /// </summary>
        private void HandleLeftClick()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // 检测是否点击了单位
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _unitLayer))
            {
                Unit clickedUnit = hit.collider.GetComponent<Unit>();
                if (clickedUnit == null)
                {
                    clickedUnit = hit.collider.GetComponentInParent<Unit>();
                }
                
                if (clickedUnit != null && clickedUnit.PlayerId == _localPlayerId)
                {
                    // Shift 加选，否则替换选择
                    if (!Input.GetKey(KeyCode.LeftShift))
                    {
                        ClearSelection();
                    }
                    
                    SelectUnit(clickedUnit);
                    return;
                }
            }
            
            // 点击空地，开始框选
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
            }
            
            _boxSelectStart = Input.mousePosition;
            _isBoxSelecting = true;
        }
        
        /// <summary>
        /// 处理右键点击（移动/攻击命令）
        /// </summary>
        private void HandleRightClick()
        {
            if (_selectedUnits.Count == 0) return;
            
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // 检测是否点击了敌方单位
            if (Physics.Raycast(ray, out RaycastHit unitHit, 1000f, _unitLayer))
            {
                Unit targetUnit = unitHit.collider.GetComponent<Unit>();
                if (targetUnit == null)
                {
                    targetUnit = unitHit.collider.GetComponentInParent<Unit>();
                }
                
                if (targetUnit != null && targetUnit.PlayerId != _localPlayerId)
                {
                    // 攻击命令
                    AttackTarget(targetUnit);
                    return;
                }
            }
            
            // 点击地面，移动命令
            if (Physics.Raycast(ray, out RaycastHit groundHit, 1000f, _groundLayer))
            {
                MoveSelectedUnits(groundHit.point);
            }
        }
        
        #endregion

        #region 选择系统
        
        /// <summary>
        /// 选择单位
        /// </summary>
        public void SelectUnit(Unit unit)
        {
            if (unit == null || _selectedUnits.Contains(unit)) return;
            
            _selectedUnits.Add(unit);
            unit.OnDeath += HandleUnitDeath;
            
            // 显示选择效果（可扩展）
            Debug.Log($"[Controller] 选择单位: {unit.UnitData?.displayName ?? unit.name}");
        }
        
        /// <summary>
        /// 取消选择单位
        /// </summary>
        public void DeselectUnit(Unit unit)
        {
            if (unit == null || !_selectedUnits.Contains(unit)) return;
            
            _selectedUnits.Remove(unit);
            unit.OnDeath -= HandleUnitDeath;
        }
        
        /// <summary>
        /// 清除所有选择
        /// </summary>
        public void ClearSelection()
        {
            foreach (var unit in _selectedUnits)
            {
                if (unit != null)
                {
                    unit.OnDeath -= HandleUnitDeath;
                }
            }
            _selectedUnits.Clear();
            Debug.Log("[Controller] 清除选择");
        }
        
        /// <summary>
        /// 处理单位死亡
        /// </summary>
        private void HandleUnitDeath(Unit unit)
        {
            DeselectUnit(unit);
        }
        
        #endregion

        #region 框选
        
        private void HandleBoxSelection()
        {
            // 检测拖拽距离，超过阈值才算框选
            if (Vector3.Distance(_boxSelectStart, Input.mousePosition) < 10f)
            {
                return;
            }
        }
        
        private void FinishBoxSelection()
        {
            _isBoxSelecting = false;
            
            Vector3 boxEnd = Input.mousePosition;
            
            // 计算框选区域
            float minX = Mathf.Min(_boxSelectStart.x, boxEnd.x);
            float maxX = Mathf.Max(_boxSelectStart.x, boxEnd.x);
            float minY = Mathf.Min(_boxSelectStart.y, boxEnd.y);
            float maxY = Mathf.Max(_boxSelectStart.y, boxEnd.y);
            
            Rect selectionRect = new Rect(minX, minY, maxX - minX, maxY - minY);
            
            // 如果框选区域太小，不处理
            if (selectionRect.width < 10f || selectionRect.height < 10f)
            {
                return;
            }
            
            // 查找所有在框选区域内的单位
            Unit[] allUnits = FindObjectsOfType<Unit>();
            foreach (var unit in allUnits)
            {
                if (unit.PlayerId != _localPlayerId || !unit.IsAlive) continue;
                
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(unit.transform.position);
                if (screenPos.z > 0 && selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
                {
                    SelectUnit(unit);
                }
            }
            
            Debug.Log($"[Controller] 框选完成，选中 {_selectedUnits.Count} 个单位");
        }
        
        private void DrawSelectionBox()
        {
            Vector3 boxEnd = Input.mousePosition;
            
            // 计算矩形
            float width = boxEnd.x - _boxSelectStart.x;
            float height = boxEnd.y - _boxSelectStart.y;
            
            Rect rect = new Rect(_boxSelectStart.x, Screen.height - _boxSelectStart.y, width, -height);
            
            // 绘制填充
            Texture2D fillTexture = new Texture2D(1, 1);
            fillTexture.SetPixel(0, 0, _selectionBoxColor);
            fillTexture.Apply();
            GUI.DrawTexture(rect, fillTexture);
            
            // 绘制边框
            DrawRectBorder(rect, _selectionBoxBorderColor, 2f);
        }
        
        private void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            Texture2D borderTexture = new Texture2D(1, 1);
            borderTexture.SetPixel(0, 0, color);
            borderTexture.Apply();
            
            // 上边
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), borderTexture);
            // 下边
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), borderTexture);
            // 左边
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), borderTexture);
            // 右边
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), borderTexture);
        }
        
        #endregion

        #region 单位命令
        
        /// <summary>
        /// 移动选中的单位
        /// </summary>
        public void MoveSelectedUnits(Vector3 destination)
        {
            if (_selectedUnits.Count == 0) return;
            
            // 显示移动标记
            ShowMoveMarker(destination);
            
            // 计算阵型偏移（简单分散）
            int count = _selectedUnits.Count;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            float spacing = 1.5f;
            
            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                Unit unit = _selectedUnits[i];
                if (unit == null || !unit.IsAlive) continue;
                
                // 计算分散位置
                int row = i / cols;
                int col = i % cols;
                Vector3 offset = new Vector3(
                    (col - cols / 2f) * spacing,
                    0,
                    (row - count / cols / 2f) * spacing
                );
                
                Vector3 targetPos = destination + offset;
                unit.MoveTo(targetPos);
            }
            
            Debug.Log($"[Controller] 命令 {_selectedUnits.Count} 个单位移动到 {destination}");
        }
        
        /// <summary>
        /// 攻击目标
        /// </summary>
        public void AttackTarget(Unit target)
        {
            if (_selectedUnits.Count == 0 || target == null) return;
            
            foreach (var unit in _selectedUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                unit.SetTarget(target);
            }
            
            Debug.Log($"[Controller] 命令 {_selectedUnits.Count} 个单位攻击 {target.UnitData?.displayName}");
        }
        
        /// <summary>
        /// 停止选中的单位
        /// </summary>
        public void StopSelectedUnits()
        {
            foreach (var unit in _selectedUnits)
            {
                if (unit == null) continue;
                unit.StopMoving();
            }
            Debug.Log("[Controller] 单位停止");
        }
        
        /// <summary>
        /// 显示移动标记
        /// </summary>
        private void ShowMoveMarker(Vector3 position)
        {
            if (_moveMarkerPrefab != null)
            {
                GameObject marker = Instantiate(_moveMarkerPrefab, position + Vector3.up * 0.1f, Quaternion.identity);
                Destroy(marker, _moveMarkerDuration);
            }
        }
        
        #endregion
    }
}
