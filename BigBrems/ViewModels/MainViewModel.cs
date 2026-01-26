using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BigBrems.Models; // <--- This is crucial now!

namespace BigBrems.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // --- 1. Collections for the UI ---
        public ObservableCollection<Dataset> Datasets { get; set; }
        public ObservableCollection<Channel> Channels { get; set; }
        public ObservableCollection<MeasurementData> DisplayData { get; set; }

        // --- 2. Selected Properties (Trigger Logic) ---

        private Dataset _selectedDataset;
        public Dataset SelectedDataset
        {
            get { return _selectedDataset; }
            set
            {
                if (_selectedDataset != value)
                {
                    _selectedDataset = value;
                    OnPropertyChanged();
                    LoadChannels(); // <--- Auto-load channels when Dataset changes
                }
            }
        }

        private Channel _selectedChannel;
        public Channel SelectedChannel
        {
            get { return _selectedChannel; }
            set
            {
                if (_selectedChannel != value)
                {
                    _selectedChannel = value;
                    OnPropertyChanged();
                    LoadData(); // <--- Auto-load data when Channel changes
                }
            }
        }

        // --- 3. Constructor ---
        public MainViewModel()
        {
            // Initialize the lists
            Datasets = new ObservableCollection<Dataset>();
            Channels = new ObservableCollection<Channel>();
            DisplayData = new ObservableCollection<MeasurementData>();

            // Load the initial list of datasets
            LoadDatasets();
        }

        // --- 4. Mock Data Logic (Simulating Database/File Read) ---

        private void LoadDatasets()
        {
            Datasets.Clear();
            // In a real app, you would call a DataService here
            Datasets.Add(new Dataset { Id = "RUN_2023_001", Name = "Track Test: Nürburgring" });
            Datasets.Add(new Dataset { Id = "RUN_2023_002", Name = "Lab Test: Thermal Stress" });
            Datasets.Add(new Dataset { Id = "RUN_2023_003", Name = "Dyno: Panic Stop Simulation" });
        }

        private void LoadChannels()
        {
            Channels.Clear();
            DisplayData.Clear(); // Clear old data to avoid confusion

            if (SelectedDataset == null) return;

            // Mock logic: Add relevant sensors based on what the user picked
            Channels.Add(new Channel { Id = "SENS_01", Name = "Brake Pressure (FL)", Unit = "bar" });
            Channels.Add(new Channel { Id = "SENS_02", Name = "Rotor Temperature (FL)", Unit = "°C" });
            Channels.Add(new Channel { Id = "SENS_03", Name = "Vehicle Speed", Unit = "km/h" });

            // Add extra sensors specifically for the Lab Test dataset
            if (SelectedDataset.Id == "RUN_2023_002")
            {
                Channels.Add(new Channel { Id = "SENS_04", Name = "Pad Wear Indicator", Unit = "%" });
            }
        }

        private void LoadData()
        {
            DisplayData.Clear();

            if (SelectedChannel == null) return;

            // Mock logic: Generate fake data rows
            var rng = new Random();
            double baseValue = 50;

            // Adjust base value based on the channel unit
            if (SelectedChannel.Unit == "°C") baseValue = 450;
            if (SelectedChannel.Unit == "bar") baseValue = 120;

            for (int i = 0; i < 20; i++)
            {
                double noise = (rng.NextDouble() * 10) - 5; // Random fluctuation

                DisplayData.Add(new MeasurementData
                {
                    Timestamp = DateTime.Now.AddSeconds(-i * 5), // Data every 5 seconds
                    Value = Math.Round(baseValue + noise, 2),
                    Status = "OK"
                });
            }
        }

        // --- 5. INotifyPropertyChanged Implementation ---
        // This makes sure the UI updates automatically when variables change
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}