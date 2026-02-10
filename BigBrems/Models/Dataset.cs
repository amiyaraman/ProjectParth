using System.Collections.Generic;

namespace BigBrems.Models
{
    public class Dataset : ObservableObject
    {
        // The core properties we absolutely need for the app logic to function
        public string Id { get; set; }
        public string DataType { get; set; }

        // A flexible dictionary to hold ANY other values you want to show in the grid
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }
}