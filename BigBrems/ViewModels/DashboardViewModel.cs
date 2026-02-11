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
            AddCriterionCommand = new RelayCommand(ExecuteAddCriterion);
            RemoveCriterionCommand = new RelayCommand(ExecuteRemoveCriterion);

            // Add one default empty row to the builder so it's not blank
            CriteriaList.Add(new SearchCriterion { SelectedKey = AvailableSearchKeys.FirstOrDefault() });
            LoadJsonData();
        }

        private void ExecuteAddCriterion(object parameter)
        {
            CriteriaList.Add(new SearchCriterion { SelectedKey = AvailableSearchKeys.FirstOrDefault() });
        }

        private void ExecuteRemoveCriterion(object parameter)
        {
            if (parameter is SearchCriterion criterionToRemove)
            {
                CriteriaList.Remove(criterionToRemove);
                // Ensure there is always at least one row
                if (CriteriaList.Count == 0)
                {
                    CriteriaList.Add(new SearchCriterion { SelectedKey = AvailableSearchKeys.FirstOrDefault() });
                }
            }
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
        // ==========================================
        // UI TOGGLE STATE (Radio Buttons)
        // ==========================================
        private bool _isSearchByIdMode = true; // Default to the first view
        public bool IsSearchByIdMode
        {
            get => _isSearchByIdMode;
            set { _isSearchByIdMode = value; OnPropertyChanged(); }
        }

        private bool _isSearchByCriteriaMode;
        public bool IsSearchByCriteriaMode
        {
            get => _isSearchByCriteriaMode;
            set { _isSearchByCriteriaMode = value; OnPropertyChanged(); }
        }

        // ==========================================
        // CRITERIA BUILDER PROPERTIES
        // ==========================================

        // Optional Dataset ID for the top of the Criteria view
        private string _criteriaDatasetId;
        public string CriteriaDatasetId
        {
            get => _criteriaDatasetId;
            set { _criteriaDatasetId = value; OnPropertyChanged(); }
        }

        // The list of dynamic query rows
        public ObservableCollection<SearchCriterion> CriteriaList { get; set; } = new ObservableCollection<SearchCriterion>();

        // The keys users can search by
        public ObservableCollection<string> AvailableSearchKeys { get; set; } = new ObservableCollection<string>
        {
            "Brand", "Status", "Name", "Description", "DataType"
        };

        // Commands for adding/removing rows
        public ICommand AddCriterionCommand { get; }
        public ICommand RemoveCriterionCommand { get; }

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
            List<Dataset> results = new List<Dataset>();

            if (IsSearchByIdMode)
            {
                // VIEW 1 LOGIC: Just get the datasets where the CheckBox is ticked
                results = _allDatasets.Where(d => d.IsSelected).ToList();
            }
            else if (IsSearchByCriteriaMode)
            {
                // VIEW 2 LOGIC: Advanced Builder

                // 1. Grab the datasets the user specifically checked in the ListBox
                var checkedDatasets = _allDatasets.Where(d => d.IsSelected).ToList();

                // 2. If they didn't check any specific boxes, fallback to searching ALL datasets of the selected DataType
                var query = checkedDatasets.Any()
                    ? checkedDatasets.AsEnumerable()
                    : _allDatasets.Where(d => d.DataType == SelectedDataType);

                // 3. Apply all the user's custom Key-Value rows to that list
                foreach (var criteria in CriteriaList)
                {
                    if (string.IsNullOrWhiteSpace(criteria.SelectedKey) || string.IsNullOrWhiteSpace(criteria.Value))
                        continue;

                    query = query.Where(dataset =>
                    {
                        string datasetValue = "";

                        if (dataset.Properties.ContainsKey(criteria.SelectedKey))
                        {
                            datasetValue = dataset.Properties[criteria.SelectedKey];
                        }
                        else if (criteria.SelectedKey == "DataType")
                        {
                            datasetValue = dataset.DataType;
                        }

                        if (string.IsNullOrEmpty(datasetValue)) return false;

                        var comparisonType = criteria.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                        if (criteria.Condition == "Equals")
                            return datasetValue.Equals(criteria.Value, comparisonType);
                        if (criteria.Condition == "Starts With")
                            return datasetValue.StartsWith(criteria.Value, comparisonType);

                        return datasetValue.Contains(criteria.Value, comparisonType);
                    });
                }

                results = query.ToList();
            }

            // Update the bottom UI table
            SearchResults = new ObservableCollection<Dataset>(results);
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