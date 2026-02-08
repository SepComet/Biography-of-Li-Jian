using System.Collections.Generic;
using CustomComponent;
using Event;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    /// <summary>
    /// MVC UI form for GameplayA. It builds slots and draggable parts from external data.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombineForm : UGuiForm
    {
        #region Property

        [SerializeField] private CombineComponent _controller;

        [SerializeField] private RectTransform _slotRoot;

        [SerializeField] private RectTransform _partRoot;

        [SerializeField] private CombineSlot _slotPrefab;

        [SerializeField] private CombineDraggablePart _partPrefab;

        [SerializeField] private TMP_Text _guideText;

        [SerializeField] private TMP_Text _explanationText;

        [SerializeField] private TMP_Text _progressText;

        [SerializeField] private string _progressFormat = "{0}/{1}";

        [SerializeField] private Vector2 _partStartAnchoredPosition = new(0f, -40f);

        [SerializeField] private float _partVerticalSpacing = 120f;

        [SerializeField] [Range(0f, 1f)] private float _partSpawnMinXNormalized = 0.55f;

        [SerializeField] [Range(0f, 1f)] private float _partSpawnMaxXNormalized = 0.95f;

        [SerializeField] [Range(0f, 1f)] private float _partSpawnMinYNormalized = 0.1f;

        [SerializeField] [Range(0f, 1f)] private float _partSpawnMaxYNormalized = 0.9f;

        private readonly List<GameObject> _runtimeNodes = new();

        #endregion

        #region FSM

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            if (_controller == null)
            {
                _controller = GameEntry.Combine;
            }

            ResetTexts();
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (_controller == null)
            {
                _controller = GameEntry.Combine;
            }
            
            if (userData is CombineFormContext context)
            {
                if (context.Slots.Count == 0)
                {
                    Log.Warning("CombineMvcForm open failed. Slot model is empty.");
                    return;
                }

                Build(context);
                _controller.BindRuntimeContext(_slotRoot, _partRoot);

                GameEntry.Event.Subscribe(CombineGuideMessageEventArgs.EventId, OnGuideMessageChanged);
                GameEntry.Event.Subscribe(CombinePartMessageEventArgs.EventId, OnPartExplained);
                GameEntry.Event.Subscribe(CombineProgressEventArgs.EventId, OnProgressChanged);
                GameEntry.Event.Subscribe(CombineCompletedEventArgs.EventId, OnPuzzleCompleted);

                if (context.AutoStart)
                {
                    _controller.StartPuzzle();
                }
                else
                {
                    GameEntry.Event.Fire(this,
                        CombineProgressEventArgs.Create(_controller.CurrentStep, _controller.TotalStep));
                }
            }
            else
            {
                Log.Error("CombineMvcForm open failed. userData is invalid.");
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            GameEntry.Event.Unsubscribe(CombineGuideMessageEventArgs.EventId, OnGuideMessageChanged);
            GameEntry.Event.Unsubscribe(CombinePartMessageEventArgs.EventId, OnPartExplained);
            GameEntry.Event.Unsubscribe(CombineProgressEventArgs.EventId, OnProgressChanged);
            GameEntry.Event.Unsubscribe(CombineCompletedEventArgs.EventId, OnPuzzleCompleted);

            if (_controller != null)
            {
                _controller.ClearRuntimeContext();
            }

            ClearRuntimeNodes();

            ResetTexts();

            base.OnClose(isShutdown, userData);
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (_controller != null)
            {
                _controller.PausePuzzle();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (_controller != null)
            {
                _controller.ResumePuzzle();
            }
        }

        protected override void OnCover()
        {
            base.OnCover();

            if (_controller != null)
            {
                _controller.PausePuzzle();
            }
        }

        protected override void OnReveal()
        {
            base.OnReveal();

            if (_controller != null)
            {
                _controller.ResumePuzzle();
            }
        }

        #endregion

        #region Other Methods

        private void ClearRuntimeNodes()
        {
            for (int i = _runtimeNodes.Count - 1; i >= 0; i--)
            {
                GameObject node = _runtimeNodes[i];
                if (node != null)
                {
                    GameObject.Destroy(node);
                }
            }

            _runtimeNodes.Clear();
        }
        
        private void ResetTexts()
        {
            GameEntry.Event.Fire(this, CombineGuideMessageEventArgs.Create(string.Empty));
            GameEntry.Event.Fire(this, CombinePartMessageEventArgs.Create(string.Empty));
            GameEntry.Event.Fire(this, CombineProgressEventArgs.Create(0, 0));
        }

        private void Build(CombineFormContext context)
        {
            ClearRuntimeNodes();

            foreach (var slotData in context.Slots)
            {
                CombineSlot slot = Instantiate(_slotPrefab, _slotRoot, false);
                slot.Initialize(slotData);
                slot.SetSnapPoint(slot.transform as RectTransform);
                _runtimeNodes.Add(slot.gameObject);
            }

            List<CombinePartContext> partContexts = BuildPartContextList(context);
            for (int i = 0; i < partContexts.Count; i++)
            {
                CombinePartContext partData = partContexts[i];
                CombineDraggablePart part = Instantiate(_partPrefab, _partRoot, false);
                part.Initialize(partData);
                SetPartSpawnPosition(part, i);
                _runtimeNodes.Add(part.gameObject);
            }
        }

        private List<CombinePartContext> BuildPartContextList(CombineFormContext context)
        {
            if (context.Parts != null && context.Parts.Count > 0)
            {
                return context.Parts;
            }

            List<CombinePartContext> fallbackParts = new List<CombinePartContext>(context.Slots.Count);
            for (int i = 0; i < context.Slots.Count; i++)
            {
                CombineSlotContext slot = context.Slots[i];
                fallbackParts.Add(new CombinePartContext
                {
                    PartType = slot.RequiredPartType,
                    PartDisplayName = slot.RequiredPartType.ToString(),
                    MechanicsExplanation = slot.MechanicsExplanation,
                    LockAfterPlaced = true
                });
            }

            return fallbackParts;
        }

        private void SetPartSpawnPosition(CombineDraggablePart part, int index)
        {
            RectTransform partRect = part.transform as RectTransform;
            if (partRect == null || _partRoot == null)
            {
                return;
            }

            Rect rootRect = _partRoot.rect;
            if (rootRect.width <= 0f || rootRect.height <= 0f)
            {
                partRect.anchoredPosition = _partStartAnchoredPosition + Vector2.down * (_partVerticalSpacing * index);
                return;
            }

            float minX = Mathf.Lerp(rootRect.xMin, rootRect.xMax, Mathf.Min(_partSpawnMinXNormalized, _partSpawnMaxXNormalized));
            float maxX = Mathf.Lerp(rootRect.xMin, rootRect.xMax, Mathf.Max(_partSpawnMinXNormalized, _partSpawnMaxXNormalized));
            float minY = Mathf.Lerp(rootRect.yMin, rootRect.yMax, Mathf.Min(_partSpawnMinYNormalized, _partSpawnMaxYNormalized));
            float maxY = Mathf.Lerp(rootRect.yMin, rootRect.yMax, Mathf.Max(_partSpawnMinYNormalized, _partSpawnMaxYNormalized));

            partRect.anchoredPosition = new Vector2(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY));
        }

        #endregion

        #region Event Handlers

        private void OnGuideMessageChanged(object sender, GameEventArgs e)
        {
            if (!(e is CombineGuideMessageEventArgs args)) return;
            if (_guideText != null)
            {
                _guideText.text = args.Message;
            }
        }

        private void OnPartExplained(object sender, GameEventArgs e)
        {
            if (!(e is CombinePartMessageEventArgs args)) return;
            if (_explanationText != null)
            {
                _explanationText.text = args.Message;
            }
        }

        private void OnProgressChanged(object sender, GameEventArgs e)
        {
            if (!(e is CombineProgressEventArgs args)) return;
            if (_progressText != null)
            {
                _progressText.text = string.Format(_progressFormat, args.CurrentStep, args.TotalSteps);
            }
        }

        private void OnPuzzleCompleted(object sender, GameEventArgs e)
        {
            if (!(e is CombineCompletedEventArgs args)) return;
            if (_progressText != null)
            {
                _progressText.text = string.Format(_progressFormat, _controller.TotalStep, _controller.TotalStep);
            }
        }

        #endregion
    }
}
