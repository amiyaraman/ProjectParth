using System.Collections.Generic;

namespace BigBrems.Models
{
    public class SearchCriterion : ObservableObject
    {
        private string _logicalLink = "AND";
        public string LogicalLink
        {
            get => _logicalLink;
            set { _logicalLink = value; OnPropertyChanged(); }
        }

        private string _selectedKey;
        public string SelectedKey
        {
            get => _selectedKey;
            set { _selectedKey = value; OnPropertyChanged(); }
        }

        private string _condition = "Contains";
        public string Condition
        {
            get => _condition;
            set { _condition = value; OnPropertyChanged(); }
        }

        private string _value = "";
        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        private bool _isCaseSensitive = false;
        public bool IsCaseSensitive
        {
            get => _isCaseSensitive;
            set { _isCaseSensitive = value; OnPropertyChanged(); }
        }

        // Dropdown Options
        public List<string> LogicalOptions { get; } = new List<string> { "AND", "OR" };
        public List<string> ConditionOptions { get; } = new List<string> { "Contains", "Equals", "Starts With" };
    }
}