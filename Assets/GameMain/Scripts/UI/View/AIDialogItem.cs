using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class AIDialogItem : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;

        [SerializeField] private TMP_Text _contentText;

        private LayoutElement _layoutElement;
        private float _minHeight;

        private void Awake()
        {
            CacheReferences();
        }

        public void OnInit(AIDialogItemContext context)
        {
            if (context == null)
            {
                return;
            }

            OnInit(context.ParentWidth, context.Content);
        }

        public void OnInit(float parentWidth, string content)
        {
            CacheReferences();
            if (_rectTransform == null || _contentText == null)
            {
                return;
            }

            _contentText.text = content ?? string.Empty;

            if (parentWidth > 0f)
            {
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth);
            }

            float preferredHeight = _contentText.GetPreferredValues(_contentText.text, _rectTransform.rect.width,
                float.PositiveInfinity).y;
            float targetHeight = Mathf.Max(_minHeight, preferredHeight);

            if (_layoutElement != null)
            {
                _layoutElement.preferredHeight = targetHeight;
            }

            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        }

        public void SetContent(string content, float parentWidth)
        {
            OnInit(parentWidth, content);
        }

        private void CacheReferences()
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (_contentText == null)
            {
                _contentText = GetComponentInChildren<TMP_Text>(true);
            }

            if (_layoutElement == null && _rectTransform != null)
            {
                _layoutElement = gameObject.GetComponent<LayoutElement>();
                if (_layoutElement == null)
                {
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
                }
            }

            if (_rectTransform != null && _minHeight <= 0f)
            {
                _minHeight = Mathf.Max(20f, _rectTransform.rect.height);
            }
        }
    }
}
