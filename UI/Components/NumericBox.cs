using System;
using TaleWorlds.Library;

namespace HannibalAI.UI.Components
{
    public class NumericBox : ViewModel
    {
        private float _value;
        private float _minValue;
        private float _maxValue;

        public NumericBox(float minValue, float maxValue, float initialValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _value = Math.Clamp(initialValue, minValue, maxValue);
        }

        [DataSourceProperty]
        public float Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = Math.Clamp(value, _minValue, _maxValue);
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public float GetValue() => _value;

        public void SetValue(float newValue)
        {
            Value = newValue;
        }
    }
} 