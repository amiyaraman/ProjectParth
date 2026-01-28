using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Important for Async
using BigBrems.Models;
using BigBrems.Services;

namespace BigBrems.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;

        // NEW: Data Pools Collection
        public ObservableCollection<DataPool> DataPools { get; set; }
        public ObservableCollection<Dataset> Datasets { get; set; }
        public ObservableCollection<Channel> Channels { get; set; }
        public ObservableCollection<MeasurementData> DisplayData { get; set; }

        // NEW: Selected Data Pool
        private DataPool _selectedDataPool;
        public DataPool SelectedDataPool
        {
            get { return _selectedDataPool; }
            set
            {
                if (_selectedDataPool != value)
                {
                    _selectedDataPool = value;
                    OnPropertyChanged();
                    LoadDatasets(); // When Pool changes, load Datasets
                }
            }
        }

        public MainViewModel()
        {
            DataPools = new ObservableCollection<DataPool>();
            Datasets = new ObservableCollection<Dataset>();
            Channels = new ObservableCollection<Channel>();
            DisplayData = new ObservableCollection<MeasurementData>();

            // Use the API Service now!
            _dataService = new ApiDataService();

            // Start the Async Initialization
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            // 1. Silent Login
            bool success = await _dataService.AuthenticateAsync();
            if (success)
            {
                // 2. If login works, load Data Pools
                var pools = await _dataService.GetDataPoolsAsync();
                foreach (var p in pools) DataPools.Add(p);
            }
        }

        private async void LoadDatasets()
        {
            Datasets.Clear();
            if (SelectedDataPool == null) return;

            // Fetch from API
            var list = await _dataService.GetDatasetsAsync(SelectedDataPool.Id);
            foreach (var item in list)
            {
                item.PropertyChanged += Dataset_PropertyChanged;
                Datasets.Add(item);
            }
        }

        private async void Dataset_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Dataset.IsSelected))
            {
                // Temporarily detach events
                foreach (var ch in Channels) ch.PropertyChanged -= Channel_PropertyChanged;
                Channels.Clear();

                var selectedDatasets = Datasets.Where(d => d.IsSelected).ToList();
                foreach (var ds in selectedDatasets)
                {
                    var newChannels = await _dataService.GetChannelsAsync(ds.Id);
                    foreach (var ch in newChannels)
                    {
                        ch.Name = $"{ch.Name} ({ds.Id})";
                        ch.PropertyChanged += Channel_PropertyChanged;
                        Channels.Add(ch);
                    }
                }
            }
        }

        private async void Channel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Channel.IsSelected))
            {
                DisplayData.Clear();
                var selectedChannels = Channels.Where(c => c.IsSelected).ToList();

                foreach (var ch in selectedChannels)
                {
                    var data = await _dataService.GetDataAsync(ch.ParentDatasetId, ch.Id);
                    foreach (var row in data) DisplayData.Add(row);
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