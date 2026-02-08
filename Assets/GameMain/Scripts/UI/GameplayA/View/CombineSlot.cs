using CustomComponent;
using Definition.Enum;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// 拼装槽位：接收拖拽部件并转发给玩法控制器做规则校验。
    /// </summary>
    [DisallowMultipleComponent]
    public class CombineSlot : MonoBehaviour, IDropHandler
    {
        #region Inspector Config

        /// <summary>
        /// 槽位要求的部件类型。
        /// </summary>
        [SerializeField] private CombinePartType _requiredPartType = CombinePartType.Dou;

        /// <summary>
        /// 槽位在拼装流程中的顺序值。
        /// </summary>
        [SerializeField] private int _buildOrder;

        /// <summary>
        /// 是否要求严格按顺序放置。
        /// </summary>
        [SerializeField] private bool _requireStrictOrder = true;

        /// <summary>
        /// 部件吸附点；为空时使用自身 RectTransform。
        /// </summary>
        [SerializeField] private RectTransform _snapPoint;

        /// <summary>
        /// 放置成功后的力学说明文案。
        /// </summary>
        [SerializeField] [TextArea(2, 4)] private string _mechanicsExplanation = string.Empty;

        /// <summary>
        /// 部件不匹配时的自定义提示文案。
        /// </summary>
        [SerializeField] [TextArea(2, 4)] private string _mismatchHint = string.Empty;

        #endregion

        #region Runtime State

        /// <summary>
        /// 玩法控制器引用。
        /// </summary>
        private CombineComponent _controller;

        /// <summary>
        /// 当前占用该槽位的部件。
        /// </summary>
        private CombineDraggablePart _occupiedPart;

        #endregion

        #region Public Query

        /// <summary>
        /// 获取槽位要求的部件类型。
        /// </summary>
        public CombinePartType RequiredPartType => _requiredPartType;

        /// <summary>
        /// 获取槽位顺序值。
        /// </summary>
        public int BuildOrder => _buildOrder;

        /// <summary>
        /// 获取槽位是否要求严格顺序。
        /// </summary>
        public bool RequireStrictOrder => _requireStrictOrder;

        /// <summary>
        /// 获取槽位是否已被占用。
        /// </summary>
        public bool IsOccupied => _occupiedPart != null;

        /// <summary>
        /// 获取槽位的力学说明文案。
        /// </summary>
        public string MechanicsExplanation => _mechanicsExplanation;

        /// <summary>
        /// 获取槽位不匹配提示文案。
        /// </summary>
        public string MismatchHint => _mismatchHint;

        /// <summary>
        /// 获取槽位吸附点。
        /// </summary>
        public RectTransform SnapPoint => _snapPoint != null ? _snapPoint : transform as RectTransform;

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
        /// 使用运行时数据初始化槽位配置。
        /// </summary>
        public void Initialize(CombineSlotContext data)
        {
            _requiredPartType = data.RequiredPartType;
            _buildOrder = data.BuildOrder;
            _requireStrictOrder = data.RequireStrictOrder;
            _mechanicsExplanation = data.MechanicsExplanation ?? string.Empty;
            _mismatchHint = data.MismatchHint ?? string.Empty;

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = data.AnchoredPosition;
                rectTransform.sizeDelta = data.SizeDelta;
            }
        }

        /// <summary>
        /// 设置槽位吸附点，为空时回退到槽位自身。
        /// </summary>
        public void SetSnapPoint(RectTransform snapPoint)
        {
            _snapPoint = snapPoint;
        }

        /// <summary>
        /// 重置槽位占用状态。
        /// </summary>
        public void ResetSlot()
        {
            _occupiedPart = null;
        }

        #endregion

        #region Drop Handling

        /// <summary>
        /// 拖拽放下回调：识别部件并交给控制器校验放置。
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (!CanHandleDrop(eventData))
            {
                return;
            }

            CombineDraggablePart part = eventData.pointerDrag.GetComponent<CombineDraggablePart>();
            if (part == null)
            {
                return;
            }

            _controller.TryPlacePart(part, this);
        }

        /// <summary>
        /// 校验是否满足处理放下事件的前置条件。
        /// </summary>
        private bool CanHandleDrop(PointerEventData eventData)
        {
            return _controller != null && eventData != null && eventData.pointerDrag != null;
        }

        #endregion

        #region Occupancy

        /// <summary>
        /// 标记当前槽位被指定部件占用。
        /// </summary>
        internal void SetOccupiedPart(CombineDraggablePart part)
        {
            _occupiedPart = part;
        }

        /// <summary>
        /// 清理槽位占用，仅在占用者与传入部件一致时生效。
        /// </summary>
        internal void ClearOccupiedPart(CombineDraggablePart part)
        {
            if (_occupiedPart == part)
            {
                _occupiedPart = null;
            }
        }

        #endregion
    }
}
