using System.Collections.Generic;
using System.Linq;
using Definition.Enum;
using Event;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityGameFramework.Runtime;

namespace CustomComponent
{
    /// <summary>
    /// 核心玩法A控制器，负责拼装规则校验、进度统计与完成判定。
    /// </summary>
    [DisallowMultipleComponent]
    public class CombineComponent : GameFrameworkComponent
    {
        #region Inspector Config

        /// <summary>
        /// 是否自动收集子节点中的槽位和部件。
        /// </summary>
        [SerializeField] private bool _autoCollectChildren = true;

        /// <summary>
        /// 组件启用时是否自动开始拼装。
        /// </summary>
        [SerializeField] private bool _autoStartOnEnable = false;

        /// <summary>
        /// 是否对所有槽位启用全局严格顺序。
        /// </summary>
        [SerializeField] private bool _strictGlobalOrder = true;

        /// <summary>
        /// 拖拽时的临时父节点。
        /// </summary>
        [SerializeField] private Transform _dragRoot = null;

        /// <summary>
        /// 拼装节点根对象。
        /// </summary>
        [SerializeField] private Transform _puzzleRoot = null;

        /// <summary>
        /// 拼装开始时提示文本。
        /// </summary>
        [SerializeField]
        [TextArea(2, 4)]
        private string _startHint = "Assemble dougong from lower-to-upper and inner-to-outer.";

        /// <summary>
        /// 放置顺序错误提示文本。
        /// </summary>
        [SerializeField]
        [TextArea(2, 4)]
        private string _wrongOrderHint = "Wrong order. Place lower or inner parts first.";

        /// <summary>
        /// 部件不匹配提示文本。
        /// </summary>
        [SerializeField]
        [TextArea(2, 4)]
        private string _wrongPartHint = "Part does not match this slot.";

        /// <summary>
        /// 槽位已占用提示文本。
        /// </summary>
        [SerializeField]
        [TextArea(2, 4)]
        private string _slotOccupiedHint = "This slot is already occupied.";

        /// <summary>
        /// 拼装完成提示文本。
        /// </summary>
        [SerializeField]
        [TextArea(2, 4)]
        private string _completeHint = "Dougong assembly completed.";

        /// <summary>
        /// 当前参与拼装的槽位列表。
        /// </summary>
        [SerializeField] private List<CombineSlot> _slots = new List<CombineSlot>();

        /// <summary>
        /// 当前参与拼装的可拖拽部件列表。
        /// </summary>
        [SerializeField] private List<CombineDraggablePart> _parts = new List<CombineDraggablePart>();

        /// <summary>
        /// 拼装完成事件。
        /// </summary>
        [SerializeField] private UnityEvent _onPuzzleCompleted = new UnityEvent();

        #endregion

        #region Runtime State

        private CombineFormContext _formContext;
        private CombineFormController _formController;

        /// <summary>
        /// 按顺序排序后的槽位缓存。
        /// </summary>
        private readonly List<CombineSlot> _orderedSlots = new List<CombineSlot>();

        /// <summary>
        /// 当前期望放置的槽位索引。
        /// </summary>
        private int _nextOrderSlotIndex = 0;

        /// <summary>
        /// 当前已放置部件数量。
        /// </summary>
        private int _placedCount = 0;

        /// <summary>
        /// 拼装是否已开始。
        /// </summary>
        private bool _isStarted = false;

        /// <summary>
        /// 拼装是否已完成。
        /// </summary>
        private bool _isCompleted = false;

        /// <summary>
        /// 拼装是否处于暂停状态。
        /// </summary>
        private bool _isPaused = false;

        #endregion

        #region Public Query

        /// <summary>
        /// 获取拼装是否完成。
        /// </summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>
        /// 获取当前步数。
        /// </summary>
        public int CurrentStep => _placedCount;

        /// <summary>
        /// 获取总步数。
        /// </summary>
        public int TotalStep => _orderedSlots.Count;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 组件启用时可选自动开始拼装。
        /// </summary>
        private void OnEnable()
        {
            if (_autoStartOnEnable)
            {
                StartPuzzle();
            }
        }

        #endregion

        #region Level Lifecycle

        /// <summary>
        /// 使用关卡数据启动关卡，并打开玩法UI。
        /// </summary>
        public int? StartLevel(CombineFormContext context)
        {
            if (context == null)
            {
                Log.Warning("CoreGameplayA start failed. context is null.");
                return null;
            }

            ClearRuntimeContext();
            SetFormContext(context);
            EnsureFormController();

            // 先关闭上一次UI，避免重复打开。
            _formController.CloseUI();

            int? serialId = _formController.OpenUI(context);
            if (!serialId.HasValue)
            {
                Log.Warning("CoreGameplayA start failed. OpenUI returned null.");
                return null;
            }

            return serialId;
        }

        /// <summary>
        /// 停止当前关卡并清理运行态。
        /// </summary>
        public void StopLevel()
        {
            _formController?.CloseUI();
            ClearRuntimeContext();
        }

        #endregion

        #region Puzzle Flow

        /// <summary>
        /// 启动拼装流程。
        /// </summary>
        public void StartPuzzle()
        {
            if (_autoCollectChildren)
            {
                CollectChildren();
            }

            BuildOrderedSlotList();
            PrepareSlots();
            PrepareParts();

            _placedCount = 0;
            _nextOrderSlotIndex = 0;
            _isStarted = true;
            _isCompleted = false;
            _isPaused = false;

            GameEntry.Event.Fire(this, CombineProgressEventArgs.Create(_placedCount, _orderedSlots.Count));

            if (!string.IsNullOrEmpty(_startHint))
            {
                GameEntry.Event.Fire(this, CombineGuideMessageEventArgs.Create(_startHint));
            }
        }

        /// <summary>
        /// 暂停拼装输入。
        /// </summary>
        public void PausePuzzle()
        {
            _isPaused = true;
        }

        /// <summary>
        /// 恢复拼装输入。
        /// </summary>
        public void ResumePuzzle()
        {
            _isPaused = false;
        }

        /// <summary>
        /// 重置并重新开始拼装。
        /// </summary>
        public void ResetPuzzle()
        {
            StartPuzzle();
        }

        #endregion

        #region Placement

        /// <summary>
        /// 尝试将部件放置到目标槽位。
        /// </summary>
        public bool TryPlacePart(CombineDraggablePart part, CombineSlot slot)
        {
            if (!CanPlacePart(part, slot))
            {
                return false;
            }

            if (!ValidateSlotAvailability(part, slot))
            {
                return false;
            }

            if (!ValidatePartType(part, slot))
            {
                return false;
            }

            if (!ValidateOrder(part, slot))
            {
                return false;
            }

            ApplyPlacement(part, slot);
            return true;
        }

        /// <summary>
        /// 放置前通用状态校验。
        /// </summary>
        private bool CanPlacePart(CombineDraggablePart part, CombineSlot slot)
        {
            return _isStarted && !_isCompleted && !_isPaused && part != null && slot != null;
        }

        /// <summary>
        /// 校验槽位是否可放置。
        /// </summary>
        private bool ValidateSlotAvailability(CombineDraggablePart part, CombineSlot slot)
        {
            if (!slot.IsOccupied)
            {
                return true;
            }

            RejectPlace(part, _slotOccupiedHint);
            return false;
        }

        /// <summary>
        /// 校验部件类型是否匹配槽位。
        /// </summary>
        private bool ValidatePartType(CombineDraggablePart part, CombineSlot slot)
        {
            if (slot.RequiredPartType == part.PartType)
            {
                return true;
            }

            string hint = string.IsNullOrEmpty(slot.MismatchHint) ? _wrongPartHint : slot.MismatchHint;
            RejectPlace(part, hint);
            return false;
        }

        /// <summary>
        /// 校验放置顺序是否符合规则。
        /// </summary>
        private bool ValidateOrder(CombineDraggablePart part, CombineSlot slot)
        {
            if (!NeedValidateOrder(slot) || IsExpectedOrderSlot(slot))
            {
                return true;
            }

            RejectPlace(part, _wrongOrderHint);
            return false;
        }

        /// <summary>
        /// 应用成功放置结果，并推进进度。
        /// </summary>
        private void ApplyPlacement(CombineDraggablePart part, CombineSlot slot)
        {
            slot.SetOccupiedPart(part);
            part.PlaceToSlot(slot);

            _placedCount++;
            AdvanceOrderCursor();
            PublishExplanation(part, slot);

            GameEntry.Event.Fire(this, CombineProgressEventArgs.Create(_placedCount, _orderedSlots.Count));
            TryCompletePuzzle();
        }

        /// <summary>
        /// 检查是否完成全部步骤并触发完成事件。
        /// </summary>
        private void TryCompletePuzzle()
        {
            if (_placedCount < _orderedSlots.Count)
            {
                return;
            }

            _isCompleted = true;

            if (!string.IsNullOrEmpty(_completeHint))
            {
                GameEntry.Event.Fire(this, CombineGuideMessageEventArgs.Create(_completeHint));
            }

            _onPuzzleCompleted.Invoke();
        }

        /// <summary>
        /// 处理放置失败：部件回弹并提示。
        /// </summary>
        private void RejectPlace(CombineDraggablePart part, string hint)
        {
            part.ReturnToSpawn();

            if (!string.IsNullOrEmpty(hint))
            {
                GameEntry.Event.Fire(this, CombineGuideMessageEventArgs.Create(hint));
            }
        }

        #endregion

        #region Runtime Context

        /// <summary>
        /// 绑定运行时上下文根节点。
        /// </summary>
        /// <param name="puzzleRoot">拼装根节点。</param>
        /// <param name="dragRoot">拖拽根节点。</param>
        public void BindRuntimeContext(Transform puzzleRoot, Transform dragRoot = null)
        {
            _puzzleRoot = puzzleRoot;
            if (dragRoot != null)
            {
                _dragRoot = dragRoot;
            }
        }

        /// <summary>
        /// 设置当前UI上下文数据。
        /// </summary>
        public void SetFormContext(CombineFormContext context)
        {
            _formContext = context;
        }

        /// <summary>
        /// 获取当前UI上下文数据。
        /// </summary>
        public CombineFormContext GetFormContext()
        {
            return _formContext;
        }

        /// <summary>
        /// 清理运行时上下文与状态。
        /// </summary>
        public void ClearRuntimeContext()
        {
            _formContext = null;
            _puzzleRoot = null;
            _dragRoot = null;
            _slots.Clear();
            _parts.Clear();
            _orderedSlots.Clear();
            _nextOrderSlotIndex = 0;
            _placedCount = 0;
            _isStarted = false;
            _isCompleted = false;
            _isPaused = false;
        }

        /// <summary>
        /// 获取当前拖拽根节点。
        /// </summary>
        public Transform GetDragRoot()
        {
            return _dragRoot;
        }

        /// <summary>
        /// 确保表单控制器已创建。
        /// </summary>
        private void EnsureFormController()
        {
            if (_formController == null)
            {
                _formController = new CombineFormController(this);
            }
        }

        #endregion

        #region Collection And Preparation

        /// <summary>
        /// 收集拼装所需的槽位与部件。
        /// </summary>
        private void CollectChildren()
        {
            _slots.Clear();
            _parts.Clear();

            Transform collectRoot = _puzzleRoot != null ? _puzzleRoot : transform;
            _slots = collectRoot.GetComponentsInChildren<CombineSlot>(true).ToList();

            CollectUniqueParts(collectRoot);
            if (_dragRoot != null && !ReferenceEquals(_dragRoot, collectRoot))
            {
                CollectUniqueParts(_dragRoot);
            }

            if (_slots.Count == 0 || _parts.Count == 0)
            {
                Log.Warning("CoreGameplayA collect failed. slots={0}, parts={1}.", _slots.Count.ToString(),
                    _parts.Count.ToString());
            }
        }

        /// <summary>
        /// 从指定根节点收集不重复的可拖拽部件。
        /// </summary>
        private void CollectUniqueParts(Transform root)
        {
            if (root == null)
            {
                return;
            }

            CombineDraggablePart[] parts = root.GetComponentsInChildren<CombineDraggablePart>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                CombineDraggablePart part = parts[i];
                if (part == null || _parts.Contains(part))
                {
                    continue;
                }

                _parts.Add(part);
            }
        }

        /// <summary>
        /// 构建按 BuildOrder 排序后的槽位列表。
        /// </summary>
        private void BuildOrderedSlotList()
        {
            _orderedSlots.Clear();

            for (int i = 0; i < _slots.Count; i++)
            {
                CombineSlot slot = _slots[i];
                if (slot != null)
                {
                    _orderedSlots.Add(slot);
                }
            }

            _orderedSlots.Sort((a, b) => a.BuildOrder.CompareTo(b.BuildOrder));
        }

        /// <summary>
        /// 初始化槽位控制器绑定与占用状态。
        /// </summary>
        private void PrepareSlots()
        {
            for (int i = 0; i < _orderedSlots.Count; i++)
            {
                CombineSlot slot = _orderedSlots[i];
                slot.BindController(this);
                slot.ResetSlot();
            }
        }

        /// <summary>
        /// 初始化部件控制器绑定与出生点状态。
        /// </summary>
        private void PrepareParts()
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                CombineDraggablePart part = _parts[i];
                if (part == null)
                {
                    continue;
                }

                part.BindController(this);
                part.CacheSpawnState();
                part.ResetToSpawn();
            }
        }

        #endregion

        #region Rule Helpers

        /// <summary>
        /// 是否需要校验严格顺序。
        /// </summary>
        private bool NeedValidateOrder(CombineSlot slot)
        {
            return _strictGlobalOrder || slot.RequireStrictOrder;
        }

        /// <summary>
        /// 当前槽位是否是下一步期望槽位。
        /// </summary>
        private bool IsExpectedOrderSlot(CombineSlot slot)
        {
            if (_nextOrderSlotIndex < 0 || _nextOrderSlotIndex >= _orderedSlots.Count)
            {
                return false;
            }

            return ReferenceEquals(_orderedSlots[_nextOrderSlotIndex], slot);
        }

        /// <summary>
        /// 推进顺序游标到下一个未占用槽位。
        /// </summary>
        private void AdvanceOrderCursor()
        {
            while (_nextOrderSlotIndex < _orderedSlots.Count && _orderedSlots[_nextOrderSlotIndex].IsOccupied)
            {
                _nextOrderSlotIndex++;
            }
        }

        /// <summary>
        /// 发布当前放置对应的力学说明文案。
        /// </summary>
        private void PublishExplanation(CombineDraggablePart part, CombineSlot slot)
        {
            string explanation = slot.MechanicsExplanation;
            if (string.IsNullOrEmpty(explanation))
            {
                explanation = part.MechanicsExplanation;
            }

            if (string.IsNullOrEmpty(explanation))
            {
                explanation = GetDefaultExplanation(part.PartType);
            }

            if (!string.IsNullOrEmpty(explanation))
            {
                GameEntry.Event.Fire(this, CombinePartMessageEventArgs.Create(explanation));
            }
        }

        /// <summary>
        /// 兜底力学说明文案。
        /// </summary>
        private static string GetDefaultExplanation(CombinePartType partType)
        {
            switch (partType)
            {
                case CombinePartType.Dou:
                    return "Dou transfers upper load and works as the base node.";
                case CombinePartType.Sheng:
                    return "Sheng raises layer height to form the bracket hierarchy.";
                case CombinePartType.Gong:
                    return "Gong spreads force laterally through overhang.";
                case CombinePartType.Qiao:
                    return "Qiao continues force transfer to the outer side.";
                case CombinePartType.Ang:
                    return "Ang uses leverage to redirect eave load inward.";
                default:
                    return string.Empty;
            }
        }

        #endregion
    }
}
