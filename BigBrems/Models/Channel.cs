using System;
using System.Collections.Generic;
using System.Text;

namespace BigBrems.Models
{
    public class Channel : ObservableObject // Inherit from our new helper
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }

        // We need to know which Dataset this channel belongs to now!
        public string ParentDatasetId { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }
}