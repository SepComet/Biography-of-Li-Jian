using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerDialogItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _contentText;

        [SerializeField] private Image _bg;

        [SerializeField] private float _preferredWidth;

        [SerializeField] private float _horizontalPadding;

        [SerializeField] private float _verticalPadding;

        private RectTransform _rootRectTransform;
        private LayoutElement _layoutElement;

        private void Awake()
        {
            CacheReferences();
        }

        public void OnInit(PlayerDialogItemContext context)
        {
            if (context == null)
            {
                return;
            }

            OnInit(context.Content);
        }

        public void OnInit(string content)
        {
            CacheReferences();

            if (_contentText == null || _bg == null || _rootRectTransform == null)
            {
                return;
            }

            _contentText.text = content ?? string.Empty;

            Vector2 unconstrainedSize = _contentText.GetPreferredValues(_contentText.text, float.PositiveInfinity,
                float.PositiveInfinity);
            float targetWidth = Mathf.Min(_preferredWidth, unconstrainedSize.x + _horizontalPadding);
            targetWidth = Mathf.Max(_horizontalPadding, targetWidth);

            float textWidth = Mathf.Max(0f, targetWidth - _horizontalPadding);
            Vector2 constrainedSize = _contentText.GetPreferredValues(_contentText.text, textWidth,
                float.PositiveInfinity);
            float targetHeight = Mathf.Max(0f, constrainedSize.y + _verticalPadding);

            RectTransform bgRect = _bg.rectTransform;
            bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

            if (_layoutElement != null)
            {
                _layoutElement.preferredHeight = targetHeight;
                _layoutElement.minHeight = targetHeight;
            }

            _rootRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        }

        private void CacheReferences()
        {
            if (_rootRectTransform == null)
            {
                _rootRectTransform = transform as RectTransform;
            }

            if (_contentText == null)
            {
                _contentText = GetComponentInChildren<TMP_Text>(true);
            }

            if (_bg == null)
            {
                _bg = GetComponentInChildren<Image>(true);
            }

            if (_layoutElement == null && _rootRectTransform != null)
            {
                _layoutElement = gameObject.GetComponent<LayoutElement>();
                if (_layoutElement == null)
                {
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
                }
            }

            if (_contentText == null || _bg == null || _rootRectTransform == null)
            {
                return;
            }

            RectTransform bgRect = _bg.rectTransform;
            RectTransform textRect = _contentText.rectTransform;

            _horizontalPadding = Mathf.Max(0f, bgRect.rect.width - textRect.rect.width);
            _verticalPadding = Mathf.Max(0f, bgRect.rect.height - textRect.rect.height);

            if (_preferredWidth <= 0f)
            {
                _preferredWidth = bgRect.rect.width > 0f ? bgRect.rect.width : _rootRectTransform.rect.width;
            }
        }
    }
}
