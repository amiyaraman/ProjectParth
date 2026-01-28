using System;
using System.Collections.Generic;
using System.Text;

namespace BigBrems.Models
{
    public class Dataset : ObservableObject // Inherit from our new helper
    {
        public string Id { get; set; }
        public string Name { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged(); } // Notify when checked!
        }
    }
}
