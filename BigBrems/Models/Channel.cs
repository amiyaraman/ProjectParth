namespace BigBrems.Models
{
    public class Channel : ObservableObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public string ParentDatasetId { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; OnPropertyChanged(); }
        }
    }
}