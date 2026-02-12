using System.Collections;
using Definition.Enum;
using Event;
using TMPro;
using UnityEngine;

namespace UI
{
    public abstract class DialogFormBase : UGuiForm
    {
        protected DialogFormContext _context;
        private Coroutine _typingCoroutine;
        private TMP_Text _typingTargetText;
        private bool _isTypewriting;

        public abstract DialogFormMode UIMode { get; }

        public abstract void StartDialog(DialogFormContext context);

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (!(userData is DialogFormContext context))
            {
                return;
            }

            _context = context;
            StartDialog(context);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            StopTypewriter();
            _context = null;
            base.OnClose(isShutdown, userData);
        }

        public void OnClickNextLine()
        {
            if (CompleteTypewriterIfRunning())
            {
                return;
            }

            GameEntry.Event.Fire(this, DialogNextLineRequestEventArgs.Create());
        }

        public void OnClickSkipDialog()
        {
            GameEntry.Event.Fire(this, DialogSkipRequestEventArgs.Create());
        }

        public void OnClickStopDialog()
        {
            GameEntry.Event.Fire(this, DialogStopRequestEventArgs.Create());
        }

        protected void PlayTypewriter(TMP_Text targetText, string text, float charsPerSecond)
        {
            StopTypewriter();

            if (targetText == null)
            {
                return;
            }

            string finalText = text ?? string.Empty;
            _typingTargetText = targetText;

            if (charsPerSecond <= 0f || string.IsNullOrEmpty(finalText))
            {
                targetText.text = finalText;
                targetText.maxVisibleCharacters = int.MaxValue;
                _isTypewriting = false;
                return;
            }

            _isTypewriting = true;
            _typingCoroutine = StartCoroutine(TypewriterRoutine(targetText, finalText, charsPerSecond));
        }

        protected void StopTypewriter()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            _typingTargetText = null;
            _isTypewriting = false;
        }

        private bool CompleteTypewriterIfRunning()
        {
            if (!_isTypewriting || _typingTargetText == null)
            {
                return false;
            }

            _typingTargetText.maxVisibleCharacters = int.MaxValue;
            StopTypewriter();
            return true;
        }

        private IEnumerator TypewriterRoutine(TMP_Text targetText, string finalText, float charsPerSecond)
        {
            targetText.text = finalText;
            targetText.ForceMeshUpdate();

            int totalCharacters = targetText.textInfo.characterCount;
            if (totalCharacters <= 0)
            {
                targetText.maxVisibleCharacters = int.MaxValue;
                _typingCoroutine = null;
                _typingTargetText = null;
                _isTypewriting = false;
                yield break;
            }

            targetText.maxVisibleCharacters = 0;
            float elapsed = 0f;
            int visibleCharacters = 0;

            while (visibleCharacters < totalCharacters)
            {
                elapsed += Time.unscaledDeltaTime;
                int nextVisible = Mathf.Min(totalCharacters, Mathf.FloorToInt(elapsed * charsPerSecond));
                if (nextVisible != visibleCharacters)
                {
                    visibleCharacters = nextVisible;
                    targetText.maxVisibleCharacters = visibleCharacters;
                }

                yield return null;
            }

            targetText.maxVisibleCharacters = int.MaxValue;
            _typingCoroutine = null;
            _typingTargetText = null;
            _isTypewriting = false;
        }
    }
}
