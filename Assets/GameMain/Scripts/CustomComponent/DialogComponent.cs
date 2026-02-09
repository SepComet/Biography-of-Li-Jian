using System.Collections.Generic;
using DataTable;
using Definition.Enum;
using Event;
using GameFramework.DataTable;
using GameFramework.Event;
using UI;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace CustomComponent
{
    [DisallowMultipleComponent]
    public class DialogComponent : GameFrameworkComponent
    {
        #region Property

        [SerializeField] private float _playingSpeed = 1.0f;
        
        private const int DialogChapterDivisor = 1000;
        private const int LineChapterDivisor = 100000000;
        private const int LineDialogDivisor = 100000;

        private readonly Dictionary<int, DRDialog> _dialogMap = new();

        private readonly Dictionary<int, List<DRDialogLine>> _dialogLinesMap = new();

        private readonly Dictionary<int, int> _dialogFirstLineIdMap = new();

        private IDataTable<DRDialog> _dtDialog;
        private IDataTable<DRDialogLine> _dtDialogLine;
        private DialogFormController _formController;
        private DialogFormContext _formContext;

        private int _currentChapterId;
        private int _currentLineIndex = -1;
        private bool _isInitialized;
        private bool _isPlaying;

        public float PlayingSpeed => _playingSpeed;

        public bool IsInitialized => _isInitialized;

        public bool IsPlaying => _isPlaying;

        public int CurrentChapterId => _currentChapterId;

        public int CurrentDialogId => _formContext != null ? _formContext.DialogId : 0;

        public int CurrentLineId => _formContext != null ? _formContext.CurrentLineId : 0;

        #endregion


        private void Start()
        {
            GameEntry.Event.Subscribe(DialogNextLineRequestEventArgs.EventId, OnDialogNextLineRequest);
            GameEntry.Event.Subscribe(DialogSkipRequestEventArgs.EventId, OnDialogSkipRequest);
            GameEntry.Event.Subscribe(DialogStopRequestEventArgs.EventId, OnDialogStopRequest);
        }

        private void OnDestroy()
        {
            GameEntry.Event.Unsubscribe(DialogNextLineRequestEventArgs.EventId, OnDialogNextLineRequest);
            GameEntry.Event.Unsubscribe(DialogSkipRequestEventArgs.EventId, OnDialogSkipRequest);
            GameEntry.Event.Unsubscribe(DialogStopRequestEventArgs.EventId, OnDialogStopRequest);
        }

        public bool Init(int chapterId)
        {
            if (chapterId <= 0)
            {
                Log.Warning("Dialog init failed. chapterId must be positive.");
                return false;
            }

            if (!EnsureDataTables())
            {
                return false;
            }

            StopDialog();
            _dialogMap.Clear();
            _dialogLinesMap.Clear();
            _dialogFirstLineIdMap.Clear();

            DRDialog[] dialogRows = _dtDialog.GetDataRows((a, b) => a.Id.CompareTo(b.Id));
            for (int i = 0; i < dialogRows.Length; i++)
            {
                DRDialog dialogRow = dialogRows[i];
                if (dialogRow == null)
                {
                    continue;
                }

                if (ParseChapterIdFromDialogId(dialogRow.Id) != chapterId)
                {
                    continue;
                }

                _dialogMap[dialogRow.Id] = dialogRow;
            }

            if (_dialogMap.Count == 0)
            {
                Log.Warning("Dialog init failed. No dialog rows found for chapter '{0}'.", chapterId.ToString());
                return false;
            }

            DRDialogLine[] lineRows = _dtDialogLine.GetDataRows((a, b) => a.Id.CompareTo(b.Id));
            for (int i = 0; i < lineRows.Length; i++)
            {
                DRDialogLine lineRow = lineRows[i];
                if (lineRow == null)
                {
                    continue;
                }

                if (ParseChapterIdFromLineId(lineRow.Id) != chapterId)
                {
                    continue;
                }

                int dialogId = ParseDialogIdFromLineId(lineRow.Id);
                if (!_dialogMap.ContainsKey(dialogId))
                {
                    continue;
                }

                if (!_dialogLinesMap.TryGetValue(dialogId, out List<DRDialogLine> dialogLines))
                {
                    dialogLines = new List<DRDialogLine>();
                    _dialogLinesMap.Add(dialogId, dialogLines);
                }

                dialogLines.Add(lineRow);
            }

            List<int> invalidDialogIds = new List<int>();
            foreach (KeyValuePair<int, DRDialog> dialogPair in _dialogMap)
            {
                if (!_dialogLinesMap.TryGetValue(dialogPair.Key, out List<DRDialogLine> dialogLines) ||
                    dialogLines.Count == 0)
                {
                    Log.Warning("Dialog init warning. Dialog '{0}' has no lines and will be ignored.",
                        dialogPair.Key.ToString());
                    invalidDialogIds.Add(dialogPair.Key);
                    continue;
                }

                dialogLines.Sort((a, b) => a.Id.CompareTo(b.Id));
                _dialogFirstLineIdMap[dialogPair.Key] = dialogLines[0].Id;
            }

            for (int i = 0; i < invalidDialogIds.Count; i++)
            {
                _dialogMap.Remove(invalidDialogIds[i]);
            }

            if (_dialogMap.Count == 0)
            {
                Log.Warning("Dialog init failed. No valid dialog remains for chapter '{0}'.", chapterId.ToString());
                return false;
            }

            EnsureFormController();
            _formContext = null;
            _currentChapterId = chapterId;
            _currentLineIndex = -1;
            _isInitialized = true;
            _isPlaying = false;
            return true;
        }

        public bool StartDialog(int dialogId)
        {
            if (!_isInitialized)
            {
                Log.Warning("Start dialog failed. Dialog component is not initialized.");
                return false;
            }

            if (ParseChapterIdFromDialogId(dialogId) != _currentChapterId)
            {
                Log.Warning("Start dialog failed. Dialog '{0}' does not belong to chapter '{1}'.", dialogId.ToString(),
                    _currentChapterId.ToString());
                return false;
            }

            if (!_dialogMap.TryGetValue(dialogId, out DRDialog dialogRow))
            {
                Log.Warning("Start dialog failed. Dialog '{0}' was not found in current chapter cache.",
                    dialogId.ToString());
                return false;
            }

            if (!_dialogLinesMap.TryGetValue(dialogId, out List<DRDialogLine> dialogLines) || dialogLines.Count == 0)
            {
                Log.Warning("Start dialog failed. Dialog '{0}' has no playable lines.", dialogId.ToString());
                return false;
            }

            if (_isPlaying)
            {
                StopDialog();
            }

            EnsureFormController();
            if (_formContext == null)
            {
                _formContext = new DialogFormContext();
            }

            _formContext.ChapterId = _currentChapterId;
            _formContext.DialogId = dialogRow.Id;
            _formContext.DialogTitle = dialogRow.Title;
            _formContext.DialogUIMode = dialogRow.UIMode;
            _formContext.PlayingSpeed = Mathf.Max(0f, _playingSpeed);

            _currentLineIndex = 0;
            ApplyLineToContext(dialogLines[_currentLineIndex], _currentLineIndex, dialogLines.Count);
            _isPlaying = true;

            _formController.OpenUI(_formContext);
            _formController.OnDialogStarted(_formContext);
            _formController.OnDialogLineChanged(_formContext);
            return true;
        }

        public bool NextLine()
        {
            if (!_isPlaying)
            {
                Log.Warning("Next line failed. No dialog is playing.");
                return false;
            }

            if (!TryGetCurrentDialogLines(out List<DRDialogLine> dialogLines))
            {
                StopDialog();
                return false;
            }

            int nextLineIndex = _currentLineIndex + 1;
            if (nextLineIndex >= dialogLines.Count)
            {
                EndDialogInternal();
                return true;
            }

            _currentLineIndex = nextLineIndex;
            ApplyLineToContext(dialogLines[_currentLineIndex], _currentLineIndex, dialogLines.Count);
            _formController.OnDialogLineChanged(_formContext);
            return true;
        }

        public bool SkipDialog()
        {
            if (!_isPlaying)
            {
                Log.Warning("Skip dialog failed. No dialog is playing.");
                return false;
            }

            EndDialogInternal();
            return true;
        }

        public void StopDialog()
        {
            if (!_isPlaying)
            {
                return;
            }

            EndDialogInternal();
        }

        public void ClearRuntimeContext()
        {
            StopDialog();

            _dialogMap.Clear();
            _dialogLinesMap.Clear();
            _dialogFirstLineIdMap.Clear();

            _formContext = null;
            _currentChapterId = 0;
            _currentLineIndex = -1;
            _isInitialized = false;
            _isPlaying = false;
        }

        private bool EnsureDataTables()
        {
            _dtDialog = GameEntry.DataTable.GetDataTable<DRDialog>();
            if (_dtDialog == null)
            {
                Log.Warning("Dialog init failed. Data table DRDialog is missing.");
                return false;
            }

            _dtDialogLine = GameEntry.DataTable.GetDataTable<DRDialogLine>();
            if (_dtDialogLine == null)
            {
                Log.Warning("Dialog init failed. Data table DRDialogLine is missing.");
                return false;
            }

            return true;
        }

        private void EnsureFormController()
        {
            if (_formController == null)
            {
                _formController = new DialogFormController();
            }
        }

        private bool TryGetCurrentDialogLines(out List<DRDialogLine> dialogLines)
        {
            dialogLines = null;
            if (_formContext == null)
            {
                Log.Warning("Dialog state invalid. Form context is null.");
                return false;
            }

            if (!_dialogLinesMap.TryGetValue(_formContext.DialogId, out dialogLines))
            {
                Log.Warning("Dialog state invalid. Dialog lines are missing for dialog '{0}'.",
                    _formContext.DialogId.ToString());
                return false;
            }

            return true;
        }

        private void EndDialogInternal()
        {
            _isPlaying = false;
            _currentLineIndex = -1;

            _formController.OnDialogEnded(_formContext);
            _formController.CloseUI();
        }

        private void ApplyLineToContext(DRDialogLine lineRow, int lineIndex, int totalLines)
        {
            _formContext.CurrentLineId = lineRow.Id;
            _formContext.SpeakerId = lineRow.SpeakerId;
            _formContext.SpeakerName = lineRow.SpeakerName;
            _formContext.Expression = lineRow.Expression;
            _formContext.Direction = lineRow.Direction;
            _formContext.Text = lineRow.Text;
            _formContext.Emphasis = lineRow.Emphasis;
            _formContext.PlayingSpeed = Mathf.Max(0f, _playingSpeed);
            _formContext.LineIndex = lineIndex;
            _formContext.TotalLines = totalLines;
            _formContext.IsLastLine = lineIndex >= totalLines - 1;
        }

        private static int ParseChapterIdFromDialogId(int dialogId)
        {
            return dialogId / DialogChapterDivisor;
        }

        private static int ParseChapterIdFromLineId(int lineId)
        {
            return lineId / LineChapterDivisor;
        }

        private static int ParseDialogIdFromLineId(int lineId)
        {
            return lineId / LineDialogDivisor;
        }

        #region Event Handlers

        private void OnDialogNextLineRequest(object sender, GameEventArgs e)
        {
            if (!(e is DialogNextLineRequestEventArgs))
            {
                return;
            }

            NextLine();
        }

        private void OnDialogSkipRequest(object sender, GameEventArgs e)
        {
            if (!(e is DialogSkipRequestEventArgs))
            {
                return;
            }

            SkipDialog();
        }

        private void OnDialogStopRequest(object sender, GameEventArgs e)
        {
            if (!(e is DialogStopRequestEventArgs))
            {
                return;
            }

            StopDialog();
        }

        #endregion
    }
}
