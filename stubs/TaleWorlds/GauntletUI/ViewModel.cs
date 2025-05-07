using System;
using System.ComponentModel;
using System.Reflection;

namespace TaleWorlds.GauntletUI
{
    /// <summary>
    /// Stub implementation of ViewModel to allow compilation without actual Bannerlord dependencies
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected void OnPropertyChangedWithValue(object value, string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public virtual void RefreshValues()
        {
            // Refresh all property values by raising change events
            foreach (PropertyInfo prop in GetType().GetProperties())
            {
                if (prop.CanRead)
                {
                    OnPropertyChanged(prop.Name);
                }
            }
        }
    }
}
