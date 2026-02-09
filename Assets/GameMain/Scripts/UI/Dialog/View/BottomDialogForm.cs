using Definition.Enum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class BottomDialogForm : DialogFormBase
    {
        public override DialogFormMode UIMode => DialogFormMode.BottomBox;

        [SerializeField] private TMP_Text _speakerNameText;

        [SerializeField] private TMP_Text _contentText;

        [SerializeField] private Image _leftSprite;

        [SerializeField] private Image _rightSprite;

        [SerializeField] private int _leftSpritePosition = 450;

        [SerializeField] private int _rightSpritePosition = -450;

        private readonly int _singleSpeakerCenterPosition = Screen.width / 2;

        private string _leftSpeakerToken = string.Empty;
        private string _rightSpeakerToken = string.Empty;

        public override void StartDialog(DialogFormContext context)
        {
            if (context == null)
            {
                Log.Warning("BottomDialogForm start failed. context is null.");
                return;
            }

            _context = context;

            string speakerName = NormalizeValue(context.SpeakerName);
            if (string.IsNullOrEmpty(speakerName))
            {
                speakerName = NormalizeValue(context.SpeakerId);
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = speakerName;
            }

            if (_contentText != null)
            {
                _contentText.text = NormalizeValue(context.Text);
            }

            if (string.IsNullOrEmpty(speakerName))
            {
                ClearSpeakerState();
                ApplySpeakerLayout(false, false);
                return;
            }

            bool isRightSpeaker = context.Direction > 0;
            if (isRightSpeaker)
            {
                _rightSpeakerToken = speakerName;
            }
            else
            {
                _leftSpeakerToken = speakerName;
            }

            bool hasLeftSpeaker = !string.IsNullOrEmpty(_leftSpeakerToken);
            bool hasRightSpeaker = !string.IsNullOrEmpty(_rightSpeakerToken);
            ApplySpeakerLayout(hasLeftSpeaker, hasRightSpeaker);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            ClearSpeakerState();
            ApplySpeakerLayout(false, false);
            base.OnClose(isShutdown, userData);
        }

        private void ClearSpeakerState()
        {
            _leftSpeakerToken = string.Empty;
            _rightSpeakerToken = string.Empty;
        }

        private void ApplySpeakerLayout(bool hasLeftSpeaker, bool hasRightSpeaker)
        {
            if (hasLeftSpeaker && hasRightSpeaker)
            {
                SetSpriteVisible(_leftSprite, true);
                SetSpriteVisible(_rightSprite, true);
                SetSpritePosition(_leftSprite.rectTransform, _leftSpritePosition);
                SetSpritePosition(_rightSprite.rectTransform, _rightSpritePosition);
                return;
            }

            if (hasLeftSpeaker)
            {
                SetSpriteVisible(_leftSprite, true);
                SetSpriteVisible(_rightSprite, false);
                SetSpritePosition(_leftSprite.rectTransform, _singleSpeakerCenterPosition);
                return;
            }

            if (hasRightSpeaker)
            {
                SetSpriteVisible(_leftSprite, false);
                SetSpriteVisible(_rightSprite, true);
                SetSpritePosition(_rightSprite.rectTransform, -_singleSpeakerCenterPosition);
                return;
            }

            SetSpriteVisible(_leftSprite, false);
            SetSpriteVisible(_rightSprite, false);
        }

        private static void SetSpriteVisible(Image spriteImage, bool visible)
        {
            if (spriteImage == null)
            {
                return;
            }

            spriteImage.gameObject.SetActive(visible);
        }

        private static void SetSpritePosition(RectTransform rectTransform, float xPosition)
        {
            if (rectTransform == null)
            {
                return;
            }

            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            anchoredPosition.x = xPosition;
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}