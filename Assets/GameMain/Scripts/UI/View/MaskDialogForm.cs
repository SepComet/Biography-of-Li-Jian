using Definition.Enum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class MaskDialogForm : DialogFormBase
    {
        public override DialogFormMode UIMode => DialogFormMode.Mask;

        [SerializeField] private Image _maskImage;

        [SerializeField] private TMP_Text _text;
        
        private float _currentPlayingSpeed;

        public override void StartDialog(DialogFormContext context)
        {
            if (context == null)
            {
                Log.Warning("MaskDialogForm start failed. context is null.");
                return;
            }

            _context = context;

            if (_maskImage != null)
            {
                _maskImage.gameObject.SetActive(true);
            }

            if ((int)_context.PlayingSpeed * 5 != (int)_currentPlayingSpeed)
            {
                _currentPlayingSpeed = 5f * (int)_context.PlayingSpeed;
            }
            
            PlayTypewriter(_text, context.Text, _currentPlayingSpeed);
        }
    }
}
