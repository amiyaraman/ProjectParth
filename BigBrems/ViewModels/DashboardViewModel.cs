using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using BigBrems.Models;

namespace BigBrems.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        // Master lists holding everything from the JSON
        private List<Dataset> _allDatasets = new List<Dataset>();
        private List<Channel> _allChannels = new List<Channel>();

        public DashboardViewModel()
        {
            SearchCommand = new RelayCommand(ExecuteSearch);
            NextCommand = new RelayCommand(ExecuteNext);
            LoadJsonData();
        }

        private void LoadJsonData()
        {
            try
            {
                string filePath = "Dummy/DatasetDummy.json";
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("DatasetDummy.json not found! Make sure it is set to 'Copy to Output Directory'.");
                    return;
                }

                string jsonString = File.ReadAllText(filePath);

                // Deserialize into the new Root object first
                var rootObject = JsonSerializer.Deserialize<JsonRootDto>(jsonString);

                // Extract the list from the "Data" property
                var rawData = rootObject?.Data ?? new List<JsonDatasetDto>();

                _allDatasets.Clear();
                _allChannels.Clear();

                int channelIdCounter = 1;

                foreach (var item in rawData)
                {
                    if (item.System == null) continue;

                    // Extract Dataset
                    var dataset = new Dataset
                    {
                        Id = item.System?.DirectoryNames ?? "UNKNOWN",
                        DataType = item.System?.DataType ?? "Unknown"
                    };

                    // Add the flexible data to the Dictionary. 
                    // These string keys ("Name", "Description", "Brand", "Status") can be anything.
                    dataset.Properties["Name"] = item.System?.DirectoryNames ?? "Unknown"; // or however you define Name
                    dataset.Properties["Description"] = item.Measurement?.Description ?? "No Description";
                    dataset.Properties["Status"] = item.System?.Status ?? "UNKNOWN";
                    dataset.Properties["Brand"] = item.Measurement?.Brand ?? "UNKNOWN";

                    _allDatasets.Add(dataset);

                    // Extract Channels
                    if (item.Metadata?.Channels != null)
                    {
                        foreach (var ch in item.Metadata.Channels)
                        {
                            _allChannels.Add(new Channel
                            {
                                Id = $"CH_{channelIdCounter++}",
                                Name = ch.ChannelName,
                                Unit = ch.ChannelUnit != null ? ch.ChannelUnit.Replace(" ", "°") : "",
                                ParentDatasetId = dataset.Id
                            });
                        }
                    }
                }

                // Populate the DataType Dropdown
                var myCustomDataTypes = new List<string> { "Sensor", "Logs", "Video", "Calibration" };
                AvailableDataTypes = new ObservableCollection<string>(myCustomDataTypes);

                // Select the first item. This will trigger the 'SelectedDataType' setter,
                // which automatically fires 'UpdateAvailableDatasets()' to filter the list.
                SelectedDataType = myCustomDataTypes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing JSON: {ex.Message}");
            }
        }

        // ==========================================
        // STEP 1: SELECT DATA TYPE (Top Panel)
        // ==========================================

        public ObservableCollection<string> AvailableDataTypes { get; set; }

        private string _selectedDataType;
        public string SelectedDataType
        {
            get => _selectedDataType;
            set
            {
                _selectedDataType = value;
                OnPropertyChanged();

                // When Data Type changes, clear the search text and update the checkbox list
                DatasetSearchText = string.Empty;
                UpdateAvailableDatasets();
            }
        }

        // ==========================================
        // STEP 2: SEARCH & SELECT DATASETS (Top Panel)
        // ==========================================

        private string _datasetSearchText;
        public string DatasetSearchText
        {
            get => _datasetSearchText;
            set
            {
                _datasetSearchText = value;
                OnPropertyChanged();
                UpdateAvailableDatasets(); // Filter as user types
            }
        }

        // The list of checkboxes shown in the top right
        private ObservableCollection<Dataset> _availableDatasetsForSelection;
        public ObservableCollection<Dataset> AvailableDatasetsForSelection
        {
            get => _availableDatasetsForSelection;
            set { _availableDatasetsForSelection = value; OnPropertyChanged(); }
        }

        private void UpdateAvailableDatasets()
        {
            // First, filter by the selected Data Type
            var filtered = _allDatasets.Where(d => d.DataType == SelectedDataType);

            // Then, filter by the Search Text (if any)
            if (!string.IsNullOrWhiteSpace(DatasetSearchText))
            {
                filtered = filtered.Where(d => d.Id.Contains(DatasetSearchText, StringComparison.OrdinalIgnoreCase));
            }

            AvailableDatasetsForSelection = new ObservableCollection<Dataset>(filtered);
        }

        // ==========================================
        // STEP 3: SEARCH BUTTON CLICK (Bottom Panel)
        // ==========================================

        public ICommand SearchCommand { get; }

        private ObservableCollection<Dataset> _searchResults;
        public ObservableCollection<Dataset> SearchResults
        {
            get => _searchResults;
            set { _searchResults = value; OnPropertyChanged(); }
        }

        private void ExecuteSearch(object parameter)
        {
            // 1. Gather the checked datasets
            var checkedDatasets = _allDatasets.Where(d => d.IsSelected).ToList();

            // 2. FORCE A MESSAGE BOX TO PROVE THE BUTTON WORKS
            //MessageBox.Show($"Search button clicked!\nYou have {checkedDatasets.Count} datasets checked.", "Diagnostic Test");

            // 3. Update the UI
            SearchResults = new ObservableCollection<Dataset>(checkedDatasets);
            SelectedDataset = null;
            DisplayedChannels?.Clear();
        }

        // ==========================================
        // STEP 4: SELECT DATASET FROM TABLE
        // ==========================================

        private Dataset _selectedDataset;
        public Dataset SelectedDataset
        {
            get => _selectedDataset;
            set
            {
                _selectedDataset = value;
                OnPropertyChanged();

                // This is the trigger! When you click a row, this runs.
                UpdateDisplayedChannels();

                IsAllChannelsSelected = false; // Reset the "Select All" box
            }
        }

        private void UpdateDisplayedChannels()
        {
            if (SelectedDataset == null)
            {
                DisplayedChannels = new ObservableCollection<Channel>();
                return;
            }

            // Filter the master list of channels to only show those belonging to the selected dataset
            var relevantChannels = _allChannels.Where(c => c.ParentDatasetId == SelectedDataset.Id);
            DisplayedChannels = new ObservableCollection<Channel>(relevantChannels);
        }

        private ObservableCollection<Channel> _displayedChannels;
        public ObservableCollection<Channel> DisplayedChannels
        {
            get => _displayedChannels;
            set { _displayedChannels = value; OnPropertyChanged(); }
        }



        // ==========================================
        // STEP 5: SELECT CHANNELS & CLICK NEXT
        // ==========================================

        private bool _isAllChannelsSelected;
        public bool IsAllChannelsSelected
        {
            get => _isAllChannelsSelected;
            set
            {
                _isAllChannelsSelected = value;
                OnPropertyChanged();
                if (DisplayedChannels != null)
                {
                    foreach (var channel in DisplayedChannels)
                    {
                        channel.IsSelected = value;
                    }
                }
            }
        }

        public ICommand NextCommand { get; }

        private void ExecuteNext(object parameter)
        {
            // 1. Gather all channels that have IsSelected == true
            // Because _allChannels is our single source of truth, any checkbox the user 
            // clicked in the UI automatically updated this master list!
            var selectedChannels = _allChannels.Where(c => c.IsSelected).ToList();

            // 2. Validation: Did they actually select anything?
            if (selectedChannels.Count == 0)
            {
                MessageBox.Show("Please select at least one channel from a dataset before continuing.",
                                "No Channels Selected",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            // 3. Build a formatted summary string to prove we captured the exact data
            string message = $"You have selected {selectedChannels.Count} channels for processing:\n\n";

            // Group the selected channels by their Parent Dataset ID so it looks organized
            var groupedChannels = selectedChannels.GroupBy(c => c.ParentDatasetId);

            foreach (var group in groupedChannels)
            {
                message += $"Dataset: {group.Key}\n";
                foreach (var channel in group)
                {
                    message += $"   • {channel.Name} [{channel.Unit}]\n";
                }
                message += "\n"; // Add a blank line between datasets
            }

            message += "Are you ready to proceed to the next step?";

            // 4. Show the confirmation dialog
            MessageBoxResult result = MessageBox.Show(message,
                                                      "Confirm Selection",
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                // THIS IS WHERE YOU ADD YOUR NEXT STEP LOGIC
                // Example: 
                // 1. Navigate to a new 'DownloadView'
                // 2. Generate a JSON payload of 'selectedChannels' to send to an API
                // 3. Kick off a Databricks job

                Console.WriteLine("User confirmed selection. Ready to process data.");
            }
        }
    }

    // ==========================================================
    // JSON DTOs 
    // ==========================================================

    public class JsonRootDto
    {
        [JsonPropertyName("data")]
        public List<JsonDatasetDto> Data { get; set; }
    }

    public class JsonDatasetDto
    {
        // REMOVED DataType FROM HERE

        [JsonPropertyName("system")]
        public JsonSystemDto System { get; set; }

        [JsonPropertyName("measurement")]
        public JsonMeasurementDto Measurement { get; set; }

        [JsonPropertyName("metadata")]
        public JsonMetadataDto Metadata { get; set; }
    }

    public class JsonSystemDto
    {
        [JsonPropertyName("directory_names")]
        public string DirectoryNames { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        // ADDED DataType HERE, where it actually lives in your JSON
        [JsonPropertyName("data_type")]
        public string DataType { get; set; }
    }

    public class JsonMeasurementDto
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }
        public string Brand { get; set; }
    }

    public class JsonMetadataDto
    {
        [JsonPropertyName("Channels")]
        public List<JsonChannelDto> Channels { get; set; }
    }

    public class JsonChannelDto
    {
        [JsonPropertyName("channel_name")]
        public string ChannelName { get; set; }

        [JsonPropertyName("channel_unit")]
        public string ChannelUnit { get; set; }
    }
}