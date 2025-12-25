// ============================================================
// CommandManager.cs
// 命令管理器 - 处理移动和攻击命令
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using RTS.Interfaces;
using RTS.Units;

namespace RTS.Controllers
{
    /// <summary>
    /// 命令管理器 - 负责处理玩家命令输入
    /// 挂载位置：GameManager 空对象上
    /// </summary>
    public class CommandManager : MonoBehaviour
    {
        #region 单例
        
        private static CommandManager _instance;
        public static CommandManager Instance => _instance;
        
        #endregion

        #region 配置
        
        [Header("射线检测")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _unitLayer;
        
        [Header("移动命令")]
        [SerializeField] private GameObject _moveMarkerPrefab;
        [SerializeField] private float _moveMarkerDuration = 1.5f;
        [SerializeField] private Color _moveMarkerColor = Color.green;
        
        [Header("攻击命令")]
        [SerializeField] private GameObject _attackMarkerPrefab;
        [SerializeField] private float _attackMarkerDuration = 1f;
        [SerializeField] private Color _attackMarkerColor = Color.red;
        
        [Header("阵型配置")]
        [SerializeField] private float _formationSpacing = 2f;
        
        #endregion

        #region 私有字段
        
        private Camera _mainCamera;
        private SelectionManager _selectionManager;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            _instance = this;
            _mainCamera = Camera.main;
        }
        
        private void Start()
        {
            _selectionManager = SelectionManager.Instance;
            if (_selectionManager == null)
            {
                Debug.LogError("[CommandManager] 未找到 SelectionManager！");
            }
        }
        
        private void Update()
        {
            HandleCommandInput();
        }
        
        #endregion

        #region 输入处理
        
        private void HandleCommandInput()
        {
            // 右键 - 命令
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
            
            // S 键 - 停止
            if (Input.GetKeyDown(KeyCode.S))
            {
                IssueStopCommand();
            }
            
            // H 键 - 原地待命（停止并保持攻击）
            if (Input.GetKeyDown(KeyCode.H))
            {
                IssueHoldPositionCommand();
            }
            
            // A 键 + 左键 - 攻击移动（预留）
            if (Input.GetKeyDown(KeyCode.A))
            {
                // TODO: 进入攻击移动模式
            }
        }
        
        private void HandleRightClick()
        {
            if (_selectionManager == null || !_selectionManager.HasSelection) return;
            
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // 1. 检测是否点击了单位（敌方或友方）
            if (Physics.Raycast(ray, out RaycastHit unitHit, 1000f, _unitLayer))
            {
                ISelectable clickedTarget = unitHit.collider.GetComponent<ISelectable>();
                if (clickedTarget == null)
                {
                    clickedTarget = unitHit.collider.GetComponentInParent<ISelectable>();
                }
                
                if (clickedTarget != null && clickedTarget.IsAlive)
                {
                    // 点击敌方单位 - 攻击命令
                    if (clickedTarget.PlayerId != _selectionManager.LocalPlayerId)
                    {
                        IssueAttackCommand(clickedTarget);
                        return;
                    }
                    // 点击友方单位 - 可以扩展为跟随命令
                }
            }
            
            // 2. 点击地面 - 移动命令
            if (Physics.Raycast(ray, out RaycastHit groundHit, 1000f, _groundLayer))
            {
                IssueMoveCommand(groundHit.point);
            }
        }
        
        #endregion

        #region 命令发布
        
        /// <summary>
        /// 发布移动命令
        /// </summary>
        public void IssueMoveCommand(Vector3 destination)
        {
            List<ICommandable> units = _selectionManager.GetSelectedCommandables();
            if (units.Count == 0) return;
            
            // 显示移动标记
            ShowMoveMarker(destination);
            
            // 计算阵型位置
            Vector3[] positions = CalculateFormationPositions(destination, units.Count);
            
            for (int i = 0; i < units.Count; i++)
            {
                units[i].MoveTo(positions[i]);
            }
            
            Debug.Log($"[Command] 移动命令：{units.Count} 个单位移动到 {destination}");
        }
        
        /// <summary>
        /// 发布攻击命令
        /// </summary>
        public void IssueAttackCommand(ISelectable target)
        {
            List<ICommandable> units = _selectionManager.GetSelectedCommandables();
            if (units.Count == 0) return;
            
            // 显示攻击标记
            ShowAttackMarker(target.Transform.position);
            
            foreach (var unit in units)
            {
                unit.AttackTarget(target);
            }
            
            Debug.Log($"[Command] 攻击命令：{units.Count} 个单位攻击 {(target as MonoBehaviour)?.name}");
        }
        
        /// <summary>
        /// 发布停止命令
        /// </summary>
        public void IssueStopCommand()
        {
            List<ICommandable> units = _selectionManager.GetSelectedCommandables();
            if (units.Count == 0) return;
            
            foreach (var unit in units)
            {
                unit.Stop();
            }
            
            Debug.Log($"[Command] 停止命令：{units.Count} 个单位");
        }
        
        /// <summary>
        /// 发布原地待命命令
        /// </summary>
        public void IssueHoldPositionCommand()
        {
            List<ICommandable> units = _selectionManager.GetSelectedCommandables();
            if (units.Count == 0) return;
            
            foreach (var unit in units)
            {
                unit.Stop();
                // TODO: 设置单位为 HoldPosition 状态（只攻击进入范围的敌人）
            }
            
            Debug.Log($"[Command] 原地待命：{units.Count} 个单位");
        }
        
        #endregion

        #region 阵型计算
        
        /// <summary>
        /// 计算阵型位置（方阵）
        /// </summary>
        private Vector3[] CalculateFormationPositions(Vector3 center, int count)
        {
            Vector3[] positions = new Vector3[count];
            
            if (count == 1)
            {
                positions[0] = center;
                return positions;
            }
            
            // 计算方阵列数
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / cols);
            
            // 计算起始偏移（使阵型居中）
            float startX = -(cols - 1) * _formationSpacing / 2f;
            float startZ = -(rows - 1) * _formationSpacing / 2f;
            
            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                
                positions[i] = center + new Vector3(
                    startX + col * _formationSpacing,
                    0,
                    startZ + row * _formationSpacing
                );
            }
            
            return positions;
        }
        
        #endregion

        #region 视觉反馈
        
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
            else
            {
                // 使用默认标记
                CreateDefaultMarker(position, _moveMarkerColor, _moveMarkerDuration);
            }
        }
        
        /// <summary>
        /// 显示攻击标记
        /// </summary>
        private void ShowAttackMarker(Vector3 position)
        {
            if (_attackMarkerPrefab != null)
            {
                GameObject marker = Instantiate(_attackMarkerPrefab, position + Vector3.up * 0.1f, Quaternion.identity);
                Destroy(marker, _attackMarkerDuration);
            }
            else
            {
                // 使用默认标记
                CreateDefaultMarker(position, _attackMarkerColor, _attackMarkerDuration);
            }
        }
        
        /// <summary>
        /// 创建默认标记（圆环）
        /// </summary>
        private void CreateDefaultMarker(Vector3 position, Color color, float duration)
        {
            // 创建简单的圆柱作为标记
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "CommandMarker";
            marker.transform.position = position + Vector3.up * 0.05f;
            marker.transform.localScale = new Vector3(1f, 0.02f, 1f);
            
            // 移除碰撞器
            Destroy(marker.GetComponent<Collider>());
            
            // 设置颜色
            Material mat = marker.GetComponent<Renderer>().material;
            mat.color = color;
            
            // 自动销毁
            Destroy(marker, duration);
        }
        
        #endregion
    }
}
