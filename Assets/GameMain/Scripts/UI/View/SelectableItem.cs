using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class SelectableItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _bgImage;

        [SerializeField] private float _fadeDuration;

        private Sequence _fadeSequence;

        public UnityEvent _onSelect;

        public UnityEvent _onDeselect;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _fadeSequence.Kill();
            _fadeSequence = DOTween.Sequence();
            _fadeSequence.Append(_bgImage.DOFade(1, _fadeDuration));
            _onSelect.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _fadeSequence.Kill();
            _fadeSequence = DOTween.Sequence();
            _fadeSequence.Append(_bgImage.DOFade(0, _fadeDuration));
            _onDeselect.Invoke();
        }
    }
}