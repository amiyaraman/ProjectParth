using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigBrems.Models;

namespace BigBrems.Services
{
    public class CsvDataService : IDataService
    {
        private string _basePath;
        private List<MeasurementRow> _currentFileCache;
        private string _currentCacheId;

        // Helper class to store the raw CSV row data in memory
        private class MeasurementRow : MeasurementData
        {
            public string ChannelId { get; set; }
            public string Unit { get; set; } // <--- Added to hold the unit from CSV
        }

        public CsvDataService()
        {
            _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DummyData");
            _currentFileCache = new List<MeasurementRow>();
        }

        public List<Dataset> GetDatasets()
        {
            var list = new List<Dataset>();
            if (!Directory.Exists(_basePath)) return list;

            var files = Directory.GetFiles(_basePath, "*.csv");
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                list.Add(new Dataset { Id = name, Name = $"Run: {name}" });
            }
            return list;
        }

        public List<Channel> GetChannels(string datasetId)
        {
            LoadFileIfNeeded(datasetId);

            return _currentFileCache
                    .GroupBy(x => x.ChannelId)
                    .Select(g => new Channel
                    {
                        Id = g.Key,
                        Name = g.Key,
                        Unit = g.First().Unit,
                        ParentDatasetId = datasetId // <--- IMPORTANT: Tag the source!
                    })
                    .ToList();
        }

        public List<MeasurementData> GetData(string datasetId, string channelId)
        {
            LoadFileIfNeeded(datasetId);

            return _currentFileCache
                   .Where(x => x.ChannelId == channelId)
                   .Cast<MeasurementData>()
                   .OrderBy(x => x.Timestamp)
                   .ToList();
        }

        // Inside CsvDataService.cs

        // ... (previous code)

        private void LoadFileIfNeeded(string datasetId)
        {
            if (_currentCacheId == datasetId && _currentFileCache.Count > 0) return;

            _currentFileCache.Clear();
            _currentCacheId = datasetId;

            string path = Path.Combine(_basePath, $"{datasetId}.csv");
            if (!File.Exists(path)) return;

            try
            {
                var lines = File.ReadAllLines(path);

                // Skip header
                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(',');

                    // Expected CSV: Timestamp, ChannelId, Value, ChannelName, Unit
                    if (parts.Length < 5) continue;

                    _currentFileCache.Add(new MeasurementRow
                    {
                        Timestamp = DateTime.Parse(parts[0]),
                        ChannelId = parts[1], // This is still used for filtering/logic
                        Value = double.Parse(parts[2]),
                        ChannelName = parts[3], // <--- READ CHANNEL NAME HERE (Index 3)
                        Unit = parts[4].Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading CSV: {ex.Message}");
            }
        }
    }
}