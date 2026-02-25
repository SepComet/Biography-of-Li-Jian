using System.Collections.Generic;
using System.Text;
using CustomComponent;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class AIChatFormController : IFormController<AIChatFormContext>
    {
        private AIChatFormContext _context;
        private AIChatForm _aiChatForm;
        private int? _formSerialId;
        private bool _pendingRefresh;

        private bool _aiChatEventsBound;
        private bool _isStreaming;
        private int _streamingMessageIndex = -1;
        private readonly StringBuilder _streamingMessageBuffer = new StringBuilder();

        public AIChatFormController()
        {
            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIFormComplete);
        }

        public int? OpenUI(AIChatFormContext context)
        {
            if (context == null)
            {
                Log.Warning("AIChatFormController open failed. context is null.");
                return null;
            }

            EnsureAIChatEventBinding();

            context.Controller = this;
            context.Messages ??= new List<AIChatMessageContext>();
            _context = context;

            if (_aiChatForm != null)
            {
                _aiChatForm.RefreshUI(_context);
                return _formSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            _formSerialId = GameEntry.UI.OpenUIForm(UIFormId.AIChatForm, context);
            return _formSerialId;
        }

        public void CloseUI()
        {
            _pendingRefresh = false;

            if (_formSerialId.HasValue)
            {
                GameEntry.UI.CloseUIForm(_formSerialId.Value);
                return;
            }

            if (_aiChatForm != null)
            {
                _aiChatForm.Close();
            }
        }

        public void SubmitPlayerMessage(string input, int languageMode)
        {
            string trimmedInput = input == null ? string.Empty : input.Trim();
            if (string.IsNullOrEmpty(trimmedInput))
            {
                return;
            }

            if (_isStreaming)
            {
                PublishErrorMessage("AI is still responding. Please wait.");
                return;
            }

            EnsureAIChatEventBinding();

            if (_context == null)
            {
                _context = new AIChatFormContext
                {
                    Controller = this,
                    LanguageMode = 0,
                    Messages = new List<AIChatMessageContext>()
                };
            }

            _context.LanguageMode = languageMode;
            AddMessageToContext(true, trimmedInput);
            _aiChatForm?.AppendPlayerDialog(new PlayerDialogItemContext
            {
                Content = trimmedInput
            });

            AIChatComponent aiChat = GameEntry.AIChat;
            if (aiChat == null)
            {
                PublishErrorMessage("AIChatComponent is missing.");
                return;
            }

            bool sendStarted = aiChat.SendChat(trimmedInput, BuildLanguageInstruction(languageMode));
            if (!sendStarted)
            {
                PublishErrorMessage(string.IsNullOrEmpty(aiChat.LastRequestErrorMessage)
                    ? "Failed to start AI chat request."
                    : aiChat.LastRequestErrorMessage);
                return;
            }

            _isStreaming = true;
            _streamingMessageIndex = -1;
            _streamingMessageBuffer.Length = 0;
            _aiChatForm?.SetInputInteractable(false);
        }

        private static string BuildLanguageInstruction(int languageMode)
        {
            if (languageMode == 1)
            {
                return "\u8bf7\u4f7f\u7528\u6587\u8a00\u6587\u56de\u7b54\uff0c\u8bed\u8a00\u5c3d\u91cf\u7b80\u6d01\u3001\u96c5\u6b63\u3001\u901a\u987a\u3002";
            }

            return "\u8bf7\u4f7f\u7528\u73b0\u4ee3\u767d\u8bdd\u6587\u56de\u7b54\uff0c\u8bed\u8a00\u6e05\u6670\u81ea\u7136\u3001\u6613\u4e8e\u7406\u89e3\u3002";
        }

        ~AIChatFormController()
        {
            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIFormComplete);
            UnbindAIChatEvents();
        }

        private void EnsureAIChatEventBinding()
        {
            if (_aiChatEventsBound)
            {
                return;
            }

            AIChatComponent aiChat = GameEntry.AIChat;
            if (aiChat == null)
            {
                return;
            }

            aiChat.StreamTextUpdated += OnStreamTextUpdated;
            aiChat.StreamRequestCompleted += OnStreamRequestCompleted;
            aiChat.StreamRequestFailed += OnStreamRequestFailed;
            _aiChatEventsBound = true;
        }

        private void UnbindAIChatEvents()
        {
            if (!_aiChatEventsBound)
            {
                return;
            }

            AIChatComponent aiChat = GameEntry.AIChat;
            if (aiChat == null)
            {
                _aiChatEventsBound = false;
                return;
            }

            aiChat.StreamTextUpdated -= OnStreamTextUpdated;
            aiChat.StreamRequestCompleted -= OnStreamRequestCompleted;
            aiChat.StreamRequestFailed -= OnStreamRequestFailed;
            _aiChatEventsBound = false;
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_aiChatForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _aiChatForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        private void AddMessageToContext(bool isPlayer, string content)
        {
            if (_context == null)
            {
                return;
            }

            _context.Messages ??= new List<AIChatMessageContext>();
            _context.Messages.Add(new AIChatMessageContext
            {
                IsPlayer = isPlayer,
                Content = content
            });
        }

        private void PublishErrorMessage(string errorMessage)
        {
            string content = string.IsNullOrEmpty(errorMessage) ? "Unknown error." : errorMessage;
            string formattedContent = "[Error] " + content;
            AddMessageToContext(false, formattedContent);
            _aiChatForm?.AppendAIDialog(new AIDialogItemContext
            {
                ParentWidth = GetContentWidth(),
                Content = formattedContent
            });
            _aiChatForm?.SetInputInteractable(true);
            _isStreaming = false;
            _streamingMessageIndex = -1;
            _streamingMessageBuffer.Length = 0;
        }

        private void EndStreaming(string finalAIContent)
        {
            string content = string.IsNullOrEmpty(finalAIContent) ? string.Empty : finalAIContent;

            if (_streamingMessageIndex >= 0)
            {
                _aiChatForm?.UpdateAIDialog(_streamingMessageIndex, new AIDialogItemContext
                {
                    ParentWidth = GetContentWidth(),
                    Content = content
                });
            }
            else if (!string.IsNullOrEmpty(content))
            {
                _aiChatForm?.AppendAIDialog(new AIDialogItemContext
                {
                    ParentWidth = GetContentWidth(),
                    Content = content
                });
            }

            if (!string.IsNullOrEmpty(content))
            {
                AddMessageToContext(false, content);
            }

            _isStreaming = false;
            _streamingMessageIndex = -1;
            _streamingMessageBuffer.Length = 0;
            _aiChatForm?.SetInputInteractable(true);
            _aiChatForm?.FocusInputField();
        }

        private void OnStreamTextUpdated(string delta)
        {
            if (!_isStreaming)
            {
                return;
            }

            if (string.IsNullOrEmpty(delta))
            {
                return;
            }

            _streamingMessageBuffer.Append(delta);

            if (_streamingMessageIndex < 0 && _aiChatForm != null)
            {
                _streamingMessageIndex = _aiChatForm.AppendAIDialog(new AIDialogItemContext
                {
                    ParentWidth = GetContentWidth(),
                    Content = string.Empty
                });
            }

            if (_streamingMessageIndex >= 0)
            {
                _aiChatForm?.UpdateAIDialog(_streamingMessageIndex, new AIDialogItemContext
                {
                    ParentWidth = GetContentWidth(),
                    Content = _streamingMessageBuffer.ToString()
                });
            }
        }

        private float GetContentWidth()
        {
            return _aiChatForm != null ? _aiChatForm.ContentSize.x : 0f;
        }

        private void OnStreamRequestCompleted(string response)
        {
            if (!_isStreaming)
            {
                return;
            }

            string finalText = string.IsNullOrEmpty(response) ? _streamingMessageBuffer.ToString() : response;
            EndStreaming(finalText);
        }

        private void OnStreamRequestFailed(string errorMessage)
        {
            if (!_isStreaming)
            {
                PublishErrorMessage(errorMessage);
                return;
            }

            string partialContent = _streamingMessageBuffer.ToString();
            if (string.IsNullOrEmpty(partialContent))
            {
                EndStreaming("[Error] " + (string.IsNullOrEmpty(errorMessage) ? "Request failed." : errorMessage));
                return;
            }

            EndStreaming(partialContent + "\n\n[Error] " +
                         (string.IsNullOrEmpty(errorMessage) ? "Request failed." : errorMessage));
        }

        private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_formSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _formSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _aiChatForm = args.UIForm.Logic as AIChatForm;
            if (_aiChatForm == null)
            {
                Log.Warning("AIChatFormController open success but form logic is invalid.");
                return;
            }

            if (_pendingRefresh)
            {
                TryRefreshUI();
            }
        }

        private void OnCloseUIFormComplete(object sender, GameEventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args))
            {
                return;
            }

            if (args.SerialId != _formSerialId)
            {
                return;
            }

            _aiChatForm = null;
            _formSerialId = null;
            _pendingRefresh = false;
        }
    }
}
