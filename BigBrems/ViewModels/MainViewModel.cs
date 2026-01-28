using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using BigBrems.Models;
using BigBrems.Services;

namespace BigBrems.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;

        // UI Collections
        public ObservableCollection<Dataset> Datasets { get; set; }
        public ObservableCollection<Channel> Channels { get; set; }
        public ObservableCollection<MeasurementData> DisplayData { get; set; }

        public MainViewModel()
        {
            Datasets = new ObservableCollection<Dataset>();
            Channels = new ObservableCollection<Channel>();
            DisplayData = new ObservableCollection<MeasurementData>();

            _dataService = new CsvDataService();

            LoadDatasets();
        }

        private void LoadDatasets()
        {
            Datasets.Clear();
            var list = _dataService.GetDatasets();
            foreach (var item in list)
            {
                // Subscribe to the checkbox event
                item.PropertyChanged += Dataset_PropertyChanged;
                Datasets.Add(item);
            }
        }

        // Triggered when you check/uncheck a DATASET
        private void Dataset_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Dataset.IsSelected))
            {
                LoadChannelsFromSelectedDatasets();
            }
        }

        private void LoadChannelsFromSelectedDatasets()
        {
            // 1. Temporarily detach events to avoid loops while clearing
            foreach (var ch in Channels) ch.PropertyChanged -= Channel_PropertyChanged;
            Channels.Clear();
            DisplayData.Clear();

            // 2. Find all checked datasets
            var selectedDatasets = Datasets.Where(d => d.IsSelected).ToList();

            // 3. Loop through them and combine all channels
            foreach (var ds in selectedDatasets)
            {
                var newChannels = _dataService.GetChannels(ds.Id);
                foreach (var ch in newChannels)
                {
                    // Update name to include dataset so user knows which is which
                    // e.g., "Speed (Run 1)"
                    ch.Name = $"{ch.Name} ({ds.Id})";

                    ch.PropertyChanged += Channel_PropertyChanged; // Listen for clicks
                    Channels.Add(ch);
                }
            }
        }

        // Triggered when you check/uncheck a CHANNEL
        private void Channel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Channel.IsSelected))
            {
                LoadDataFromSelectedChannels();
            }
        }

        private void LoadDataFromSelectedChannels()
        {
            DisplayData.Clear();

            // Find all checked channels
            var selectedChannels = Channels.Where(c => c.IsSelected).ToList();

            foreach (var ch in selectedChannels)
            {
                // We need the Dataset ID to ask the service for data
                // Good thing we added ParentDatasetId to the Channel model!
                var data = _dataService.GetData(ch.ParentDatasetId, ch.Id);

                foreach (var row in data)
                {
                    DisplayData.Add(row);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}