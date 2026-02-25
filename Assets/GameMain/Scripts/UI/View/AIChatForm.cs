using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class AIChatForm : UGuiForm
    {
        [SerializeField] private TMP_Text _returnButtonText;
        
        [SerializeField] private TMP_InputField _inputField;
        
        [SerializeField] private RectTransform _dialogContent;
        
        [SerializeField] private TMP_Text _titleText;
        
        [SerializeField] private ScrollRect _historyScrollRect;
        
        [SerializeField] private AIDialogItem _aiDialogItemPrefab;
        
        [SerializeField] private PlayerDialogItem _playerDialogItemPrefab;
        
        [SerializeField] private HorizonSelectGroup _languageSelectGroup;
            
        [SerializeField] private string _returnButtonNormalText = "<sprite name=\"KEYBOARD_Esc\"> Back";
        
        [SerializeField] private string _returnButtonHoverText = "<sprite name=\"KEYBOARD_Esc\"><u> Back </u>";

        private readonly List<Component> _dialogItems = new List<Component>();
        private AIChatFormController _controller;
        private VerticalLayoutGroup _contentLayoutGroup;

        public Vector2 ContentSize => _dialogContent != null
            ? new Vector2(_dialogContent.rect.width, _dialogContent.rect.height)
            : Vector2.zero;

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            EnsureReferences();

            if (_inputField != null)
            {
                _inputField.onSubmit.RemoveListener(OnInputFieldSubmitted);
                _inputField.onSubmit.AddListener(OnInputFieldSubmitted);
            }

            if (!(userData is AIChatFormContext context))
            {
                Log.Error("AIChatFormContext is invalid.");
                return;
            }

            RefreshUI(context);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            if (_inputField != null)
            {
                _inputField.onSubmit.RemoveListener(OnInputFieldSubmitted);
            }

            ClearDialogItems();
            _dialogItems.Clear();
            _controller = null;

            base.OnClose(isShutdown, userData);
        }

        public void RefreshUI(AIChatFormContext context)
        {
            EnsureReferences();
            if (context == null)
            {
                return;
            }

            _controller = context.Controller;

            if (_titleText != null && !string.IsNullOrEmpty(context.Title))
            {
                _titleText.text = context.Title;
            }

            if (_languageSelectGroup != null)
            {
                int targetLanguageMode = Mathf.Max(0, context.LanguageMode);
                try
                {
                    _languageSelectGroup.SetValue(targetLanguageMode);
                }
                catch
                {
                    _languageSelectGroup.SetValue(0);
                    context.LanguageMode = 0;
                }
            }

            if (context.ClearHistoryOnOpen || _dialogItems.Count == 0)
            {
                ClearDialogItems();
                BuildHistory(context.Messages);
            }

            FocusInputField();
        }

        public int AppendAIDialog(AIDialogItemContext context)
        {
            EnsureReferences();
            if (_aiDialogItemPrefab == null || _dialogContent == null)
            {
                Log.Warning("Append AI dialog failed. Missing AI item prefab or content root.");
                return -1;
            }

            AIDialogItem item = Instantiate(_aiDialogItemPrefab, _dialogContent, false);
            item.gameObject.SetActive(true);
            item.OnInit(context);
            _dialogItems.Add(item);
            ScrollToBottom();
            return _dialogItems.Count - 1;
        }

        public void UpdateAIDialog(int itemIndex, AIDialogItemContext context)
        {
            if (itemIndex < 0 || itemIndex >= _dialogItems.Count)
            {
                return;
            }

            if (!(_dialogItems[itemIndex] is AIDialogItem item))
            {
                return;
            }

            item.OnInit(context);
            ScrollToBottom();
        }

        public int AppendPlayerDialog(PlayerDialogItemContext context)
        {
            EnsureReferences();
            if (_playerDialogItemPrefab == null || _dialogContent == null)
            {
                Log.Warning("Append player dialog failed. Missing player item prefab or content root.");
                return -1;
            }

            PlayerDialogItem item = Instantiate(_playerDialogItemPrefab, _dialogContent, false);
            item.gameObject.SetActive(true);
            item.OnInit(context);
            _dialogItems.Add(item);
            ScrollToBottom();
            return _dialogItems.Count - 1;
        }

        public void SetInputInteractable(bool interactable)
        {
            if (_inputField == null)
            {
                return;
            }

            _inputField.interactable = interactable;
        }

        public void FocusInputField()
        {
            if (_inputField == null || !_inputField.interactable)
            {
                return;
            }

            _inputField.Select();
            _inputField.ActivateInputField();
        }

        public void OnCommitButtonClick()
        {
            CommitInput();
        }

        public void OnReturnButtonHover()
        {
            if (_returnButtonText != null)
            {
                _returnButtonText.text = _returnButtonHoverText;
            }
        }

        public void OnReturnButtonClick()
        {
            _controller?.CloseUI();
        }

        public void OnReturnButtonHoverEnd()
        {
            if (_returnButtonText != null)
            {
                _returnButtonText.text = _returnButtonNormalText;
            }
        }

        private void EnsureReferences()
        {
            if (_historyScrollRect == null)
            {
                _historyScrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (_historyScrollRect != null && _dialogContent == null)
            {
                _dialogContent = _historyScrollRect.content;
            }

            if (_dialogContent != null)
            {
                _contentLayoutGroup = _dialogContent.GetComponent<VerticalLayoutGroup>();
                if (_contentLayoutGroup == null)
                {
                    _contentLayoutGroup = _dialogContent.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                _contentLayoutGroup.enabled = true;
                _contentLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                _contentLayoutGroup.childForceExpandHeight = false;
                _contentLayoutGroup.childForceExpandWidth = true;
                _contentLayoutGroup.childControlHeight = true;
                _contentLayoutGroup.childControlWidth = true;
                _contentLayoutGroup.spacing = 12f;
            }
        }

        private void BuildHistory(List<AIChatMessageContext> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return;
            }

            for (int i = 0; i < messages.Count; i++)
            {
                AIChatMessageContext message = messages[i];
                if (message == null || string.IsNullOrEmpty(message.Content))
                {
                    continue;
                }

                if (message.IsPlayer)
                {
                    AppendPlayerDialog(new PlayerDialogItemContext
                    {
                        Content = message.Content
                    });
                }
                else
                {
                    AppendAIDialog(new AIDialogItemContext
                    {
                        ParentWidth = ContentSize.x,
                        Content = message.Content
                    });
                }
            }
        }

        private void CommitInput()
        {
            if (_controller == null || _inputField == null || !_inputField.interactable)
            {
                return;
            }

            string content = _inputField.text;
            _inputField.text = string.Empty;
            _controller.SubmitPlayerMessage(content, GetLanguageMode());
            FocusInputField();
        }

        private void OnInputFieldSubmitted(string submittedText)
        {
            CommitInput();
        }

        private int GetLanguageMode()
        {
            return _languageSelectGroup != null ? _languageSelectGroup.GetIntValue() : 0;
        }

        private void ClearDialogItems()
        {
            if (_dialogContent == null)
            {
                _dialogItems.Clear();
                return;
            }

            for (int i = _dialogContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _dialogContent.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            _dialogItems.Clear();
        }

        private void ScrollToBottom()
        {
            if (_historyScrollRect == null)
            {
                return;
            }

            StopCoroutine(nameof(ScrollToBottomAtEndOfFrame));
            StartCoroutine(nameof(ScrollToBottomAtEndOfFrame));
        }

        private IEnumerator ScrollToBottomAtEndOfFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            _historyScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
