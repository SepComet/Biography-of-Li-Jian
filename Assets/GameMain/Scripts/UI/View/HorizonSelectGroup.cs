using System;
using TMPro;
using UnityEngine;

namespace UI
{
    public class HorizonSelectGroup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _showText;

        [SerializeField] private GameObject _leftButton;

        [SerializeField] private GameObject _rightButton;

        [SerializeField] private int _currentValue;

        [SerializeField] private string[] _showTexts;

        private int LeftBound = 0;
        private int RightBound => _showTexts.Length - 1;

        private void Start()
        {
            SetValue(_currentValue);
            _leftButton.SetActive(false);
            _rightButton.SetActive(false);
        }

        public void OnLeftClick()
        {
            _currentValue--;
            _showText.text = _showTexts[_currentValue];
            
            UpdateButtonState();
        }

        public void OnRightClick()
        {
            _currentValue++;
            _showText.text = _showTexts[_currentValue];
            
            UpdateButtonState();
        }

        public void SetValue(int value)
        {
            if (value < 0 || value >= _showTexts.Length)
            {
                throw new IndexOutOfRangeException();
            }

            _currentValue = value;
            _showText.text = _showTexts[_currentValue];
            
            UpdateButtonState();
        }

        public void SetValue(bool value)
        {
            _currentValue = value ? 1 : 0;
            _showText.text = _showTexts[_currentValue];

            UpdateButtonState();
        }

        public void UpdateButtonState()
        {
            _leftButton.SetActive(_currentValue != LeftBound);
            _rightButton.SetActive(_currentValue != RightBound);
        }

        public void HideButtons()
        {
            _leftButton.SetActive(false);
            _rightButton.SetActive(false);
        }
        
        public int GetIntValue() => _currentValue;

        public bool GetBoolValue() => _currentValue == 1;
    }
}