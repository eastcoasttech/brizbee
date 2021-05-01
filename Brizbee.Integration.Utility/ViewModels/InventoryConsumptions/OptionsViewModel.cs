using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Brizbee.Integration.Utility.ViewModels.InventoryConsumptions
{
    public class OptionsViewModel : INotifyPropertyChanged
    {
        #region Public Fields
        public string SelectedMethod { get; set; } = "Sales Receipt";
        public string SelectedValue { get; set; } = "Purchase Cost";
        public bool IsEnabled { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public List<string> Values
        {
            get
            {
                return new List<string>() { "Zero", "Sales Price", "Purchase Cost" };
            }
        }
        public List<string> Methods
        {
            get
            {
                return new List<string>() { "Sales Receipt", "Inventory Adjustment" };
            }
        }
        #endregion

        public void Update()
        {
            Application.Current.Properties["SelectedMethod"] = SelectedMethod;
            Application.Current.Properties["SelectedValue"] = SelectedValue;
        }

        public void RefreshEnabled()
        {
            if (SelectedMethod == "Sales Receipt")
                IsEnabled = true;
            else if (SelectedMethod == "Inventory Adjustment")
                IsEnabled = false;

            SelectedValue = "Purchase Cost";
            OnPropertyChanged("SelectedValue");
            OnPropertyChanged("IsEnabled");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
