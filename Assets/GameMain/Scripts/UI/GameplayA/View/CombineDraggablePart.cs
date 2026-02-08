using CustomComponent;
using DG.Tweening;
using Definition.Enum;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// 可拖拽拼装部件：负责拖拽交互、出生点回退与放置到槽位。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class CombineDraggablePart : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Inspector Config

        [SerializeField] private CombinePartType _partType = CombinePartType.Dou;

        [SerializeField] private string _partDisplayName = "Dou";

        [SerializeField] [TextArea(2, 4)] private string _mechanicsExplanation = string.Empty;

        [SerializeField] private bool _lockAfterPlaced = true;

        [SerializeField] private RectTransform _rectTransform;

        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private float _defaultScaleMultiplier = 0.8f;

        [SerializeField] private float _hoverScaleMultiplier = 1f;

        [SerializeField] private float _hoverTweenDuration = 0.15f;

        [SerializeField] private Ease _hoverTweenEase = Ease.OutQuad;

        [SerializeField] private float _returnToSpawnDuration = 0.4f;

        [SerializeField] private Ease _returnToSpawnEase = Ease.OutCubic;

        #endregion

        #region Spawn State

        private bool _spawnStateCached;

        private Transform _spawnParent;

        private Vector3 _spawnWorldPosition = Vector3.zero;

        private Quaternion _spawnWorldRotation = Quaternion.identity;

        private Vector3 _spawnWorldScale = Vector3.one;

        private int _spawnSiblingIndex;

        #endregion

        #region Runtime State

        private CombineComponent _controller;

        private CombineSlot _currentSlot;

        private bool _isPlaced;

        private bool _isLocked;

        private bool _isDragging;

        private Vector3 _originalLocalScale = Vector3.one;

        private Tween _scaleTween;

        private Tween _returnTween;

        #endregion

        #region Public Query

        public CombinePartType PartType => _partType;

        public string MechanicsExplanation => _mechanicsExplanation;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            }

            _originalLocalScale = _rectTransform.localScale;
            ApplyScaleImmediate(_defaultScaleMultiplier);
        }

        private void OnDestroy()
        {
            KillScaleTween();
            KillReturnTween();
        }

        #endregion

        #region Setup

        /// <summary>
        /// 绑定玩法控制器。
        /// </summary>
        public void BindController(CombineComponent controller)
        {
            _controller = controller;
        }

        /// <summary>
        /// 使用运行时数据初始化部件配置。
        /// </summary>
        public void Initialize(CombinePartContext data)
        {
            _partType = data.PartType;
            _partDisplayName = string.IsNullOrEmpty(data.PartDisplayName)
                ? data.PartType.ToString()
                : data.PartDisplayName;
            _mechanicsExplanation = data.MechanicsExplanation ?? string.Empty;
            _lockAfterPlaced = data.LockAfterPlaced;
        }

        /// <summary>
        /// 缓存出生点状态（仅首次缓存）。
        /// </summary>
        public void CacheSpawnState()
        {
            if (_spawnStateCached)
            {
                return;
            }

            _spawnStateCached = true;
            _spawnParent = _rectTransform.parent;
            _spawnWorldPosition = _rectTransform.position;
            _spawnWorldRotation = _rectTransform.rotation;
            _spawnWorldScale = _rectTransform.localScale;
            _spawnSiblingIndex = _rectTransform.GetSiblingIndex();
        }

        /// <summary>
        /// 重置部件到出生点，并清理槽位占用状态。
        /// </summary>
        public void ResetToSpawn()
        {
            ClearSlotOccupancy();
            _isDragging = false;
            _isPlaced = false;
            _isLocked = false;
            ReturnToSpawn();
        }

        #endregion

        #region Drag Flow

        /// <summary>
        /// 开始拖拽：关闭射线阻挡并切到拖拽层级。
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanStartDrag())
            {
                return;
            }

            KillReturnTween();
            _isDragging = true;
            _canvasGroup.blocksRaycasts = false;
            MoveToDragRoot();
            _rectTransform.SetAsLastSibling();
        }

        /// <summary>
        /// 拖拽中：按指针增量更新锚点位置。
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!CanDrag(eventData))
            {
                return;
            }

            _rectTransform.anchoredPosition += eventData.delta;
        }

        /// <summary>
        /// 结束拖拽：恢复射线阻挡；未成功放置则回到出生点。
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;

            if (_isLocked)
            {
                return;
            }

            _canvasGroup.blocksRaycasts = true;

            if (!_isPlaced)
            {
                ReturnToSpawnAnimated();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isLocked || _isPlaced || _isDragging)
            {
                return;
            }

            PlayScaleTween(_hoverScaleMultiplier);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isLocked || _isPlaced || _isDragging)
            {
                return;
            }

            PlayScaleTween(_defaultScaleMultiplier);
        }

        /// <summary>
        /// 校验是否允许开始拖拽。
        /// </summary>
        private bool CanStartDrag()
        {
            return !_isLocked && !_isPlaced;
        }

        /// <summary>
        /// 校验拖拽中是否可更新位置。
        /// </summary>
        private bool CanDrag(PointerEventData eventData)
        {
            return !_isLocked && !_isPlaced && eventData != null;
        }

        /// <summary>
        /// 将部件切到控制器提供的拖拽根节点。
        /// </summary>
        private void MoveToDragRoot()
        {
            if (_controller == null)
            {
                return;
            }

            Transform dragRoot = _controller.GetDragRoot();
            if (dragRoot != null)
            {
                _rectTransform.SetParent(dragRoot, true);
            }
        }

        #endregion

        #region Placement

        /// <summary>
        /// 将部件恢复到出生点状态。
        /// </summary>
        public void ReturnToSpawn()
        {
            KillReturnTween();

            if (_spawnParent == null)
            {
                return;
            }

            _rectTransform.SetParent(_spawnParent, true);
            _rectTransform.position = _spawnWorldPosition;
            _rectTransform.rotation = _spawnWorldRotation;
            _rectTransform.localScale = _spawnWorldScale;
            _rectTransform.SetSiblingIndex(_spawnSiblingIndex);
        }

        /// <summary>
        /// 应用成功放置到槽位后的表现与状态。
        /// </summary>
        internal void PlaceToSlot(CombineSlot slot)
        {
            KillScaleTween();
            KillReturnTween();
            _isDragging = false;
            _currentSlot = slot;
            _isPlaced = true;
            _isLocked = _lockAfterPlaced;

            RectTransform snapPoint = slot.SnapPoint;
            _rectTransform.SetParent(snapPoint != null ? snapPoint : slot.transform, false);
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.localRotation = Quaternion.identity;
            _rectTransform.localScale = Vector3.one;
            _canvasGroup.blocksRaycasts = !_isLocked;
        }

        /// <summary>
        /// 清理当前槽位占用记录。
        /// </summary>
        private void ClearSlotOccupancy()
        {
            if (_currentSlot != null)
            {
                _currentSlot.ClearOccupiedPart(this);
                _currentSlot = null;
            }
        }

        private void ApplyScaleImmediate(float multiplier)
        {
            _rectTransform.localScale = _originalLocalScale * Mathf.Max(0f, multiplier);
        }

        private void PlayScaleTween(float targetMultiplier)
        {
            KillScaleTween();
            _scaleTween = _rectTransform
                .DOScale(_originalLocalScale * Mathf.Max(0f, targetMultiplier), _hoverTweenDuration)
                .SetEase(_hoverTweenEase)
                .SetUpdate(true);
        }

        public void ReturnToSpawnAnimated()
        {
            if (_spawnParent == null)
            {
                return;
            }

            if (_returnToSpawnDuration <= 0f)
            {
                ReturnToSpawn();
                return;
            }

            KillReturnTween();
            _rectTransform.SetParent(_spawnParent, true);
            _rectTransform.SetSiblingIndex(_spawnSiblingIndex);

            _returnTween = DOTween.Sequence()
                .Append(_rectTransform.DOMove(_spawnWorldPosition, _returnToSpawnDuration))
                .Join(_rectTransform.DORotateQuaternion(_spawnWorldRotation, _returnToSpawnDuration))
                .Join(_rectTransform.DOScale(_spawnWorldScale, _returnToSpawnDuration))
                .SetEase(_returnToSpawnEase)
                .SetUpdate(true)
                .OnKill(() => _returnTween = null);
        }

        private void KillScaleTween()
        {
            if (_scaleTween != null && _scaleTween.IsActive())
            {
                _scaleTween.Kill();
            }

            _scaleTween = null;
        }

        private void KillReturnTween()
        {
            if (_returnTween != null && _returnTween.IsActive())
            {
                _returnTween.Kill();
            }

            _returnTween = null;
        }

        #endregion
    }
}
