using DG.Tweening;
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

        [SerializeField] private GameObject _speakerArea;

        [SerializeField] private TMP_Text _speakerNameText;

        [SerializeField] private TMP_Text _contentText;

        [SerializeField] private Image _leftSprite;

        [SerializeField] private Image _rightSprite;

        [SerializeField] private int _leftSpritePosition = 450;

        [SerializeField] private int _rightSpritePosition = -450;

        [SerializeField] private float _moveDuration = 0.25f;

        [SerializeField] private Ease _moveEase = Ease.OutCubic;
        
        private readonly int _singleSpeakerCenterPosition = Screen.width / 2;

        private string _leftSpeakerToken = string.Empty;
        private string _rightSpeakerToken = string.Empty;
        private Sequence _layoutSequence;

        public override void StartDialog(DialogFormContext context)
        {
            if (context == null)
            {
                Log.Warning("BottomDialogForm start failed. context is null.");
                return;
            }

            _context = context;

            string speakerName = _context.SpeakerName;

            if (_speakerArea != null)
            {
                _speakerArea.SetActive(!string.IsNullOrEmpty(speakerName));
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = speakerName;
            }

            PlayTypewriter(_contentText, _context.Text, _context.PlayingSpeed);

            if (string.IsNullOrEmpty(speakerName))
            {
                ClearSpeakerState();
                ApplySpeakerLayout(false, false, true);
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
            ApplySpeakerLayout(hasLeftSpeaker, hasRightSpeaker, false);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            ClearSpeakerState();
            KillLayoutTween();
            ApplySpeakerLayout(false, false, true);
            base.OnClose(isShutdown, userData);
        }

        private void ClearSpeakerState()
        {
            _leftSpeakerToken = string.Empty;
            _rightSpeakerToken = string.Empty;
        }

        private void KillLayoutTween()
        {
            if (_layoutSequence != null)
            {
                _layoutSequence.Kill();
                _layoutSequence = null;
            }
        }

        private void ApplySpeakerLayout(bool hasLeftSpeaker, bool hasRightSpeaker, bool instant)
        {
            if (_leftSprite == null || _rightSprite == null)
            {
                return;
            }

            bool leftCurrentlyVisible = _leftSprite.gameObject.activeSelf;
            bool rightCurrentlyVisible = _rightSprite.gameObject.activeSelf;

            bool leftTargetVisible = hasLeftSpeaker;
            bool rightTargetVisible = hasRightSpeaker;

            float leftTargetX = GetTargetX(true, hasLeftSpeaker, hasRightSpeaker);
            float rightTargetX = GetTargetX(false, hasLeftSpeaker, hasRightSpeaker);

            KillLayoutTween();

            PrepareStartState(
                leftCurrentlyVisible,
                rightCurrentlyVisible,
                leftTargetVisible,
                rightTargetVisible,
                hasLeftSpeaker,
                hasRightSpeaker);

            if (instant || _moveDuration <= 0f)
            {
                SetSpritePosition(_leftSprite.rectTransform, leftTargetX);
                SetSpritePosition(_rightSprite.rectTransform, rightTargetX);
                SetSpriteVisible(_leftSprite, leftTargetVisible);
                SetSpriteVisible(_rightSprite, rightTargetVisible);
                return;
            }

            _layoutSequence = DOTween.Sequence();

            Tween leftTween = CreateMoveTween(_leftSprite.rectTransform, leftTargetX);
            if (leftTween != null)
            {
                _layoutSequence.Join(leftTween);
            }

            Tween rightTween = CreateMoveTween(_rightSprite.rectTransform, rightTargetX);
            if (rightTween != null)
            {
                _layoutSequence.Join(rightTween);
            }

            if (_layoutSequence.active && _layoutSequence.Duration(false) > 0f)
            {
                _layoutSequence.OnComplete(() =>
                {
                    SetSpriteVisible(_leftSprite, leftTargetVisible);
                    SetSpriteVisible(_rightSprite, rightTargetVisible);
                    _layoutSequence = null;
                });
            }
            else
            {
                SetSpritePosition(_leftSprite.rectTransform, leftTargetX);
                SetSpritePosition(_rightSprite.rectTransform, rightTargetX);
                SetSpriteVisible(_leftSprite, leftTargetVisible);
                SetSpriteVisible(_rightSprite, rightTargetVisible);
                _layoutSequence.Kill();
                _layoutSequence = null;
            }
        }

        private void PrepareStartState(
            bool leftCurrentlyVisible,
            bool rightCurrentlyVisible,
            bool leftTargetVisible,
            bool rightTargetVisible,
            bool hasLeftSpeaker,
            bool hasRightSpeaker)
        {
            if (leftTargetVisible && !leftCurrentlyVisible)
            {
                float leftStartX = GetAppearStartX(true, rightCurrentlyVisible, hasLeftSpeaker, hasRightSpeaker);
                SetSpritePosition(_leftSprite.rectTransform, leftStartX);
                SetSpriteVisible(_leftSprite, true);
            }

            if (rightTargetVisible && !rightCurrentlyVisible)
            {
                float rightStartX = GetAppearStartX(false, leftCurrentlyVisible, hasLeftSpeaker, hasRightSpeaker);
                SetSpritePosition(_rightSprite.rectTransform, rightStartX);
                SetSpriteVisible(_rightSprite, true);
            }

            if (leftCurrentlyVisible && !leftTargetVisible)
            {
                SetSpriteVisible(_leftSprite, true);
            }

            if (rightCurrentlyVisible && !rightTargetVisible)
            {
                SetSpriteVisible(_rightSprite, true);
            }
        }

        private float GetAppearStartX(bool isLeft, bool otherCurrentlyVisible, bool hasLeftSpeaker,
            bool hasRightSpeaker)
        {
            if (hasLeftSpeaker && hasRightSpeaker)
            {
                // single -> multi: hidden side starts from center, then both move to side positions.
                if (otherCurrentlyVisible)
                {
                    return _singleSpeakerCenterPosition;
                }

                return isLeft ? _leftSpritePosition : _rightSpritePosition;
            }

            // single appears: active side starts from its side and moves to center.
            return isLeft ? _leftSpritePosition : _rightSpritePosition;
        }

        private float GetTargetX(bool isLeft, bool hasLeftSpeaker, bool hasRightSpeaker)
        {
            if (hasLeftSpeaker && hasRightSpeaker)
            {
                return isLeft ? _leftSpritePosition : _rightSpritePosition;
            }

            if (hasLeftSpeaker || hasRightSpeaker)
            {
                // multi -> single: both move to center first, then inactive side hides.
                return _singleSpeakerCenterPosition;
            }

            return isLeft ? _leftSpritePosition : _rightSpritePosition;
        }

        private Tween CreateMoveTween(RectTransform rectTransform, float targetX)
        {
            if (rectTransform == null)
            {
                return null;
            }

            if (Mathf.Abs(rectTransform.anchoredPosition.x - targetX) < 0.01f)
            {
                return null;
            }

            return rectTransform.DOAnchorPosX(targetX, _moveDuration).SetEase(_moveEase);
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
