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

            foreach (var partData in context.Parts)
            {
                CombineDraggablePart part = Instantiate(_partPrefab, _partRoot, false);
                part.Initialize(partData);
                _runtimeNodes.Add(part.gameObject);
            }
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