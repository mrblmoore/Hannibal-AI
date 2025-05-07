using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.UI.Components
{
    public class NumericBox : ViewModel
    {
        private float _value;
        private float _minValue;
        private float _maxValue;
        private float _step;

        [DataSourceProperty]
        public float Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public float MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue != value)
                {
                    _minValue = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public float MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue != value)
                {
                    _maxValue = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public float Step
        {
            get => _step;
            set
            {
                if (_step != value)
                {
                    _step = value;
                    OnPropertyChanged();
                }
            }
        }

        public NumericBox(float initialValue = 0f, float minValue = float.MinValue, float maxValue = float.MaxValue, float step = 1f)
        {
            _value = initialValue;
            _minValue = minValue;
            _maxValue = maxValue;
            _step = step;
        }

        public void Increase()
        {
            Value = MathF.Min(_value + _step, _maxValue);
        }

        public void Decrease()
        {
            Value = MathF.Max(_value - _step, _minValue);
        }
    }
} 