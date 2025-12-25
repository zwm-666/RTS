// ============================================================
// MobileCameraController.cs
// 移动端摄像机控制器 - 触控拖拽和缩放
// ============================================================

using UnityEngine;

namespace RTS.Controllers
{
    /// <summary>
    /// 移动端摄像机控制器
    /// 挂载位置：主摄像机
    /// </summary>
    public class MobileCameraController : MonoBehaviour
    {
        #region 配置
        
        [Header("平移配置")]
        [Tooltip("拖拽灵敏度")]
        [SerializeField] private float _panSensitivity = 0.05f;
        
        [Tooltip("平移平滑度")]
        [SerializeField] private float _panSmoothing = 5f;
        
        [Header("缩放配置")]
        [Tooltip("缩放灵敏度")]
        [SerializeField] private float _zoomSensitivity = 0.5f;
        
        [Tooltip("最小高度（Y轴）")]
        [SerializeField] private float _minHeight = 10f;
        
        [Tooltip("最大高度（Y轴）")]
        [SerializeField] private float _maxHeight = 50f;
        
        [Header("边界限制")]
        [Tooltip("地图最小X")]
        [SerializeField] private float _minX = 0f;
        
        [Tooltip("地图最大X")]
        [SerializeField] private float _maxX = 128f;
        
        [Tooltip("地图最小Z")]
        [SerializeField] private float _minZ = 0f;
        
        [Tooltip("地图最大Z")]
        [SerializeField] private float _maxZ = 128f;
        
        [Header("PC 支持")]
        [Tooltip("启用鼠标控制（PC平台）")]
        [SerializeField] private bool _enableMouseControl = true;
        
        [Tooltip("鼠标滚轮缩放灵敏度")]
        [SerializeField] private float _mouseZoomSensitivity = 5f;
        
        [Tooltip("鼠标中键拖拽灵敏度")]
        [SerializeField] private float _mousePanSensitivity = 0.1f;
        
        [Header("触控识别")]
        [Tooltip("点击判定移动阈值（像素）")]
        [SerializeField] private float _tapThreshold = 20f;
        
        [Tooltip("点击判定时间阈值（秒）")]
        [SerializeField] private float _tapTimeThreshold = 0.3f;
        
        #endregion

        #region 私有字段
        
        private Vector3 _targetPosition;
        private float _targetHeight;
        
        // 触控状态
        private Vector2 _lastTouchPosition;
        private Vector2 _touchStartPosition;
        private float _touchStartTime;
        private bool _isDragging = false;
        private float _lastPinchDistance = 0f;
        
        // 公开的触控状态（供其他脚本查询）
        private bool _isTouchHandled = false;
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 当前触控是否被摄像机处理（用于区分拖拽和点击）
        /// </summary>
        public bool IsTouchHandled => _isTouchHandled;
        
        /// <summary>
        /// 当前是否正在拖拽
        /// </summary>
        public bool IsDragging => _isDragging;
        
        #endregion

        #region Unity 生命周期
        
        private void Start()
        {
            _targetPosition = transform.position;
            _targetHeight = transform.position.y;
        }
        
        private void Update()
        {
            // 处理输入
            HandleTouchInput();
            HandleMouseInput();
            
            // 平滑移动到目标位置
            ApplyMovement();
        }
        
        #endregion

        #region 触控输入
        
        private void HandleTouchInput()
        {
            _isTouchHandled = false;
            
            if (Input.touchCount == 0)
            {
                _isDragging = false;
                return;
            }
            
            if (Input.touchCount == 1)
            {
                HandleSingleTouch();
            }
            else if (Input.touchCount >= 2)
            {
                HandlePinchZoom();
            }
        }
        
        /// <summary>
        /// 单指触控处理
        /// </summary>
        private void HandleSingleTouch()
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStartPosition = touch.position;
                    _touchStartTime = Time.time;
                    _lastTouchPosition = touch.position;
                    _isDragging = false;
                    break;
                    
                case TouchPhase.Moved:
                    // 检查是否达到拖拽阈值
                    float moveDistance = Vector2.Distance(touch.position, _touchStartPosition);
                    
                    if (moveDistance > _tapThreshold)
                    {
                        _isDragging = true;
                        _isTouchHandled = true;
                        
                        // 计算拖拽偏移
                        Vector2 delta = touch.position - _lastTouchPosition;
                        PanCamera(-delta.x, -delta.y);
                    }
                    
                    _lastTouchPosition = touch.position;
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (!_isDragging)
                    {
                        // 这是一个点击（tap），不处理，交给其他脚本
                        float elapsed = Time.time - _touchStartTime;
                        float distance = Vector2.Distance(touch.position, _touchStartPosition);
                        
                        if (elapsed < _tapTimeThreshold && distance < _tapThreshold)
                        {
                            // 有效的点击，不标记为已处理
                            _isTouchHandled = false;
                        }
                    }
                    
                    _isDragging = false;
                    break;
            }
        }
        
        /// <summary>
        /// 双指缩放处理
        /// </summary>
        private void HandlePinchZoom()
        {
            _isDragging = true;
            _isTouchHandled = true;
            
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            
            float currentDistance = Vector2.Distance(touch0.position, touch1.position);
            
            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                _lastPinchDistance = currentDistance;
                return;
            }
            
            if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float deltaDistance = currentDistance - _lastPinchDistance;
                ZoomCamera(-deltaDistance * _zoomSensitivity * 0.01f);
                _lastPinchDistance = currentDistance;
            }
        }
        
        #endregion

        #region 鼠标输入（PC平台）
        
        private void HandleMouseInput()
        {
            if (!_enableMouseControl) return;
            
            #if UNITY_STANDALONE || UNITY_EDITOR
            
            // 滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                ZoomCamera(-scroll * _mouseZoomSensitivity);
            }
            
            // 中键拖拽
            if (Input.GetMouseButton(2))
            {
                float deltaX = Input.GetAxis("Mouse X");
                float deltaY = Input.GetAxis("Mouse Y");
                
                if (Mathf.Abs(deltaX) > 0.01f || Mathf.Abs(deltaY) > 0.01f)
                {
                    PanCamera(-deltaX * _mousePanSensitivity * 100f, -deltaY * _mousePanSensitivity * 100f);
                    _isTouchHandled = true;
                }
            }
            
            #endif
        }
        
        #endregion

        #region 摄像机控制
        
        /// <summary>
        /// 平移摄像机
        /// </summary>
        private void PanCamera(float deltaX, float deltaY)
        {
            // 根据摄像机朝向计算移动方向
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();
            
            // 根据高度调整灵敏度
            float heightFactor = transform.position.y / _maxHeight;
            float adjustedSensitivity = _panSensitivity * (1f + heightFactor);
            
            Vector3 move = (right * deltaX + forward * deltaY) * adjustedSensitivity;
            _targetPosition += move;
            
            // 限制边界
            ClampTargetPosition();
        }
        
        /// <summary>
        /// 缩放摄像机（调整高度）
        /// </summary>
        private void ZoomCamera(float delta)
        {
            _targetHeight = Mathf.Clamp(_targetHeight + delta, _minHeight, _maxHeight);
        }
        
        /// <summary>
        /// 限制目标位置在边界内
        /// </summary>
        private void ClampTargetPosition()
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, _minX, _maxX);
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, _minZ, _maxZ);
        }
        
        /// <summary>
        /// 应用平滑移动
        /// </summary>
        private void ApplyMovement()
        {
            // 平滑移动到目标位置
            Vector3 targetPos = new Vector3(_targetPosition.x, _targetHeight, _targetPosition.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * _panSmoothing);
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 跳转到指定位置
        /// </summary>
        public void JumpToPosition(Vector3 position)
        {
            _targetPosition = new Vector3(position.x, _targetHeight, position.z);
            ClampTargetPosition();
        }
        
        /// <summary>
        /// 设置边界
        /// </summary>
        public void SetBounds(float minX, float maxX, float minZ, float maxZ)
        {
            _minX = minX;
            _maxX = maxX;
            _minZ = minZ;
            _maxZ = maxZ;
        }
        
        /// <summary>
        /// 重置摄像机位置
        /// </summary>
        public void ResetCamera()
        {
            _targetPosition = new Vector3((_minX + _maxX) / 2f, 0, (_minZ + _maxZ) / 2f);
            _targetHeight = (_minHeight + _maxHeight) / 2f;
        }
        
        #endregion

        #region 调试
        
        private void OnDrawGizmosSelected()
        {
            // 显示边界
            Gizmos.color = Color.green;
            Vector3 center = new Vector3((_minX + _maxX) / 2f, 0, (_minZ + _maxZ) / 2f);
            Vector3 size = new Vector3(_maxX - _minX, 1, _maxZ - _minZ);
            Gizmos.DrawWireCube(center, size);
        }
        
        #endregion
    }
}
