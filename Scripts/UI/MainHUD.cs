// ============================================================
// MainHUD.cs
// RTS 游戏主界面 - 资源栏、信息面板、命令面板
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTS.Core;
using RTS.Managers;
using RTS.Controllers;
using RTS.Units;
using RTS.Buildings;
using RTS.Interfaces;
using RTS.Domain.Repositories;

namespace RTS.UI
{
    /// <summary>
    /// 主界面 HUD
    /// 挂载位置：Canvas 对象上
    /// </summary>
    public class MainHUD : MonoBehaviour
    {
        #region 资源栏 UI 引用
        
        [Header("===== 顶部资源栏 =====")]
        [SerializeField] private Text _goldText;
        [SerializeField] private Text _woodText;
        [SerializeField] private Text _foodText;
        [SerializeField] private Text _populationText;
        
        [Header("资源图标（可选）")]
        [SerializeField] private Image _goldIcon;
        [SerializeField] private Image _woodIcon;
        [SerializeField] private Image _foodIcon;
        
        #endregion

        #region 信息面板 UI 引用
        
        [Header("===== 底部信息面板 =====")]
        [SerializeField] private GameObject _infoPanel;
        [SerializeField] private Image _entityIcon;
        [SerializeField] private Text _entityNameText;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Text _healthText;
        [SerializeField] private Text _attackText;
        [SerializeField] private Text _armorText;
        [SerializeField] private Text _multiSelectText;
        
        [Header("生命条组件")]
        [SerializeField] private HealthBar _healthBar;
        
        #endregion

        #region 命令面板 UI 引用
        
        [Header("===== 右下角命令面板 =====")]
        [SerializeField] private GameObject _commandPanel;
        [SerializeField] private Transform _commandButtonsParent;
        [SerializeField] private GameObject _commandButtonPrefab;
        
        [Header("命令按钮（预设）")]
        [SerializeField] private Button _stopButton;
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _moveButton;
        
        #endregion

        #region 配置
        
        [Header("===== 配置 =====")]
        [SerializeField] private int _localPlayerId = 0;
        [SerializeField] private float _updateInterval = 0.1f;
        
        #endregion

        #region 私有字段
        
        private List<ISelectable> _currentSelection = new List<ISelectable>();
        private List<GameObject> _dynamicButtons = new List<GameObject>();
        private float _updateTimer;
        
        // 缓存单选对象（用于实时更新血量）
        private Unit _selectedUnit;
        private Building _selectedBuilding;
        
        #endregion

        #region Unity 生命周期
        
        private void Start()
        {
            // 初始化资源显示
            UpdateResourceDisplay();
            
            // 订阅资源变化事件
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
            }
            
            // 订阅选择变化事件
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged += OnSelectionChanged;
            }
            
            // 初始隐藏面板
            SetInfoPanelVisible(false);
            SetCommandPanelVisible(false);
            
            // 绑定基础按钮事件
            BindBasicCommandButtons();
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
            }
            
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged -= OnSelectionChanged;
            }
        }
        
        private void Update()
        {
            // 定时更新（用于实时血量等）
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0;
                UpdateSelectionInfo();
            }
        }
        
        #endregion

        #region 资源栏
        
        /// <summary>
        /// 更新资源显示
        /// </summary>
        private void UpdateResourceDisplay()
        {
            if (ResourceManager.Instance == null) return;
            
            int gold = ResourceManager.Instance.GetResource(_localPlayerId, ResourceType.Gold);
            int wood = ResourceManager.Instance.GetResource(_localPlayerId, ResourceType.Wood);
            int food = ResourceManager.Instance.GetResource(_localPlayerId, ResourceType.Food);
            
            if (_goldText != null) _goldText.text = gold.ToString();
            if (_woodText != null) _woodText.text = wood.ToString();
            if (_foodText != null) _foodText.text = food.ToString();
            
            // TODO: 人口显示（需要从人口管理获取）
            // if (_populationText != null) _populationText.text = $"{usedPop}/{maxPop}";
        }
        
        /// <summary>
        /// 资源变化回调
        /// </summary>
        private void OnResourceChanged(int playerId, ResourceType type, int newAmount)
        {
            if (playerId != _localPlayerId) return;
            
            switch (type)
            {
                case ResourceType.Gold:
                    if (_goldText != null) _goldText.text = newAmount.ToString();
                    break;
                case ResourceType.Wood:
                    if (_woodText != null) _woodText.text = newAmount.ToString();
                    break;
                case ResourceType.Food:
                    if (_foodText != null) _foodText.text = newAmount.ToString();
                    break;
            }
        }
        
        #endregion

        #region 信息面板
        
        /// <summary>
        /// 选择变化回调
        /// </summary>
        private void OnSelectionChanged(List<ISelectable> selection)
        {
            _currentSelection = selection ?? new List<ISelectable>();
            
            // 清除缓存
            _selectedUnit = null;
            _selectedBuilding = null;
            
            if (_currentSelection.Count == 0)
            {
                // 无选择
                SetInfoPanelVisible(false);
                SetCommandPanelVisible(false);
            }
            else if (_currentSelection.Count == 1)
            {
                // 单选
                SetInfoPanelVisible(true);
                SetCommandPanelVisible(true);
                
                var selected = _currentSelection[0];
                
                // 缓存对象引用
                if (selected is Unit unit)
                {
                    _selectedUnit = unit;
                    ShowUnitInfo(unit);
                    ShowUnitCommands(unit);
                }
                else if (selected is Building building)
                {
                    _selectedBuilding = building;
                    ShowBuildingInfo(building);
                    ShowBuildingCommands(building);
                }
                
                // 隐藏多选文本
                if (_multiSelectText != null) _multiSelectText.gameObject.SetActive(false);
            }
            else
            {
                // 多选
                SetInfoPanelVisible(true);
                SetCommandPanelVisible(true);
                
                ShowMultiSelectionInfo();
                ShowMultiSelectionCommands();
            }
        }
        
        /// <summary>
        /// 显示单位信息
        /// </summary>
        private void ShowUnitInfo(Unit unit)
        {
            if (unit == null || unit.DomainData == null) return;
            
            var data = unit.DomainData;
            
            // 名称
            if (_entityNameText != null)
            {
                _entityNameText.text = data.DisplayName;
            }
            
            // 生命值
            UpdateHealthDisplay(unit.CurrentHealth, unit.MaxHealth);
            
            // 攻击力
            if (_attackText != null)
            {
                _attackText.text = $"攻击: {data.AttackDamage}";
            }
            
            // 护甲
            if (_armorText != null)
            {
                _armorText.text = $"护甲: {data.Armor}";
            }
            
            // 图标（如果有）
            // if (_entityIcon != null && data.Icon != null)
            // {
            //     _entityIcon.sprite = data.Icon;
            // }
        }
        
        /// <summary>
        /// 显示建筑信息
        /// </summary>
        private void ShowBuildingInfo(Building building)
        {
            if (building == null || building.DomainData == null) return;
            
            var data = building.DomainData;
            
            // 名称
            if (_entityNameText != null)
            {
                _entityNameText.text = data.DisplayName;
            }
            
            // 生命值
            UpdateHealthDisplay(building.CurrentHealth, building.MaxHealth);
            
            // 攻击力（如果有）
            if (_attackText != null)
            {
                if (data.CanAttack)
                {
                    _attackText.text = $"攻击: {data.AttackDamage}";
                    _attackText.gameObject.SetActive(true);
                }
                else
                {
                    _attackText.gameObject.SetActive(false);
                }
            }
            
            // 护甲
            if (_armorText != null)
            {
                _armorText.text = $"护甲: {data.Armor}";
            }
        }
        
        /// <summary>
        /// 显示多选信息
        /// </summary>
        private void ShowMultiSelectionInfo()
        {
            // 统计单位和建筑数量
            int unitCount = 0;
            int buildingCount = 0;
            
            foreach (var obj in _currentSelection)
            {
                if (obj is Unit) unitCount++;
                else if (obj is Building) buildingCount++;
            }
            
            // 隐藏单选详情
            if (_entityNameText != null) _entityNameText.gameObject.SetActive(false);
            if (_healthSlider != null) _healthSlider.gameObject.SetActive(false);
            if (_attackText != null) _attackText.gameObject.SetActive(false);
            if (_armorText != null) _armorText.gameObject.SetActive(false);
            
            // 显示多选文本
            if (_multiSelectText != null)
            {
                _multiSelectText.gameObject.SetActive(true);
                
                if (unitCount > 0 && buildingCount > 0)
                {
                    _multiSelectText.text = $"已选择: {unitCount} 个单位, {buildingCount} 个建筑";
                }
                else if (unitCount > 0)
                {
                    _multiSelectText.text = $"已选择: {unitCount} 个单位";
                }
                else
                {
                    _multiSelectText.text = $"已选择: {buildingCount} 个建筑";
                }
            }
        }
        
        /// <summary>
        /// 实时更新选择信息（主要是血量）
        /// </summary>
        private void UpdateSelectionInfo()
        {
            if (_currentSelection.Count != 1) return;
            
            if (_selectedUnit != null && _selectedUnit.IsAlive)
            {
                UpdateHealthDisplay(_selectedUnit.CurrentHealth, _selectedUnit.MaxHealth);
            }
            else if (_selectedBuilding != null && _selectedBuilding.IsAlive)
            {
                UpdateHealthDisplay(_selectedBuilding.CurrentHealth, _selectedBuilding.MaxHealth);
            }
        }
        
        /// <summary>
        /// 更新生命值显示
        /// </summary>
        private void UpdateHealthDisplay(int current, int max)
        {
            float ratio = max > 0 ? (float)current / max : 0;
            
            if (_healthSlider != null)
            {
                _healthSlider.gameObject.SetActive(true);
                _healthSlider.value = ratio;
            }
            
            if (_healthText != null)
            {
                _healthText.text = $"{current}/{max}";
            }
            
            if (_healthBar != null)
            {
                _healthBar.UpdateHealth(ratio);
            }
        }
        
        private void SetInfoPanelVisible(bool visible)
        {
            if (_infoPanel != null)
            {
                _infoPanel.SetActive(visible);
            }
            
            // 恢复单选详情显示
            if (visible)
            {
                if (_entityNameText != null) _entityNameText.gameObject.SetActive(true);
                if (_healthSlider != null) _healthSlider.gameObject.SetActive(true);
                if (_attackText != null) _attackText.gameObject.SetActive(true);
                if (_armorText != null) _armorText.gameObject.SetActive(true);
            }
        }
        
        #endregion

        #region 命令面板
        
        /// <summary>
        /// 绑定基础命令按钮
        /// </summary>
        private void BindBasicCommandButtons()
        {
            if (_stopButton != null)
            {
                _stopButton.onClick.AddListener(OnStopClicked);
            }
            
            if (_attackButton != null)
            {
                _attackButton.onClick.AddListener(OnAttackClicked);
            }
            
            if (_moveButton != null)
            {
                _moveButton.onClick.AddListener(OnMoveClicked);
            }
        }
        
        /// <summary>
        /// 显示单位命令
        /// </summary>
        private void ShowUnitCommands(Unit unit)
        {
            ClearDynamicButtons();
            
            // 显示基础按钮
            if (_stopButton != null) _stopButton.gameObject.SetActive(true);
            if (_attackButton != null) _attackButton.gameObject.SetActive(true);
            if (_moveButton != null) _moveButton.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 显示建筑命令（生产按钮）
        /// </summary>
        private void ShowBuildingCommands(Building building)
        {
            ClearDynamicButtons();
            
            // 隐藏单位基础按钮
            if (_stopButton != null) _stopButton.gameObject.SetActive(false);
            if (_attackButton != null) _attackButton.gameObject.SetActive(false);
            if (_moveButton != null) _moveButton.gameObject.SetActive(false);
            
            if (building == null || building.DomainData == null) return;
            
            var producibleUnits = building.DomainData.ProducibleUnitIds;
            if (producibleUnits == null || producibleUnits.Count == 0) return;
            
            // 获取单位仓库
            if (!ServiceLocator.TryGet<IUnitRepository>(out var unitRepo)) return;
            
            // 为每个可生产单位创建按钮
            foreach (var unitId in producibleUnits)
            {
                var unitData = unitRepo.GetById(unitId);
                if (unitData == null) continue;
                
                CreateProductionButton(building, unitId, unitData.DisplayName);
            }
        }
        
        /// <summary>
        /// 创建生产按钮
        /// </summary>
        private void CreateProductionButton(Building building, string unitId, string displayName)
        {
            if (_commandButtonPrefab == null || _commandButtonsParent == null) return;
            
            GameObject btnObj = Instantiate(_commandButtonPrefab, _commandButtonsParent);
            _dynamicButtons.Add(btnObj);
            
            Button btn = btnObj.GetComponent<Button>();
            Text btnText = btnObj.GetComponentInChildren<Text>();
            
            if (btnText != null)
            {
                btnText.text = displayName;
            }
            
            if (btn != null)
            {
                // 捕获当前值
                string capturedUnitId = unitId;
                Building capturedBuilding = building;
                
                btn.onClick.AddListener(() => OnProductionButtonClicked(capturedBuilding, capturedUnitId));
            }
        }
        
        /// <summary>
        /// 显示多选命令（基础命令）
        /// </summary>
        private void ShowMultiSelectionCommands()
        {
            ClearDynamicButtons();
            
            // 只显示基础命令
            if (_stopButton != null) _stopButton.gameObject.SetActive(true);
            if (_attackButton != null) _attackButton.gameObject.SetActive(true);
            if (_moveButton != null) _moveButton.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 清除动态生成的按钮
        /// </summary>
        private void ClearDynamicButtons()
        {
            foreach (var btn in _dynamicButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _dynamicButtons.Clear();
        }
        
        private void SetCommandPanelVisible(bool visible)
        {
            if (_commandPanel != null)
            {
                _commandPanel.SetActive(visible);
            }
        }
        
        #endregion

        #region 命令回调
        
        private void OnStopClicked()
        {
            Debug.Log("[MainHUD] 停止命令");
            
            foreach (var obj in _currentSelection)
            {
                if (obj is Unit unit)
                {
                    unit.Stop();
                }
            }
        }
        
        private void OnAttackClicked()
        {
            Debug.Log("[MainHUD] 攻击命令（等待选择目标）");
            // TODO: 进入攻击目标选择模式
        }
        
        private void OnMoveClicked()
        {
            Debug.Log("[MainHUD] 移动命令（等待选择目的地）");
            // TODO: 进入移动目标选择模式
        }
        
        private void OnProductionButtonClicked(Building building, string unitId)
        {
            if (building == null)
            {
                Debug.LogWarning("[MainHUD] 建筑已销毁");
                return;
            }
            
            bool success = building.ProduceUnit(unitId);
            if (success)
            {
                Debug.Log($"[MainHUD] 开始生产: {unitId}");
            }
            else
            {
                Debug.LogWarning($"[MainHUD] 无法生产: {unitId} (可能资源不足或队列已满)");
            }
        }
        
        #endregion
    }
}
