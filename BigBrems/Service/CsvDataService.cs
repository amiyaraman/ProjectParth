using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks; // Required for Task<>
using System.Windows;
using BigBrems.Models;

namespace BigBrems.Services
{
    public class CsvDataService : IDataService
    {
        private string _basePath;
        private List<MeasurementRow> _currentFileCache;
        private string _currentCacheId;

        private class MeasurementRow : MeasurementData
        {
            public string ChannelId { get; set; }
            public string Unit { get; set; }
        }

        public CsvDataService()
        {
            _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DummyData");
            _currentFileCache = new List<MeasurementRow>();
        }

        // --- NEW METHODS REQUIRED BY INTERFACE ---

        // 1. Fake Login (Always returns true for CSV mode)
        public Task<bool> AuthenticateAsync()
        {
            return Task.FromResult(true);
        }

        // 2. Fake Data Pools (Returns one "Local Storage" pool)
        public Task<List<DataPool>> GetDataPoolsAsync()
        {
            var pools = new List<DataPool>
            {
                new DataPool { Id = "LOCAL_CSV", Name = "Local CSV Folder" }
            };
            return Task.FromResult(pools);
        }

        // ----------------------------------------

        public Task<List<Dataset>> GetDatasetsAsync(string poolId)
        {
            // We ignore poolId because we just read the folder
            var list = new List<Dataset>();

            if (Directory.Exists(_basePath))
            {
                var files = Directory.GetFiles(_basePath, "*.csv");
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    list.Add(new Dataset { Id = name, Name = $"File: {name}" });
                }
            }

            // Debug fallback
            if (list.Count == 0)
            {
                list.Add(new Dataset { Id = "DEBUG", Name = "No CSVs Found (Check Folder)" });
            }

            return Task.FromResult(list);
        }

        public Task<List<Channel>> GetChannelsAsync(string datasetId)
        {
            // Note: Since method signature is now Async, we wrap the result
            LoadFileIfNeeded(datasetId);

            var channels = _currentFileCache
                    .GroupBy(x => x.ChannelId)
                    .Select(g => new Channel
                    {
                        Id = g.Key,
                        Name = g.Key,
                        Unit = g.First().Unit,
                        ParentDatasetId = datasetId
                    })
                    .ToList();

            return Task.FromResult(channels);
        }

        public Task<List<MeasurementData>> GetDataAsync(string datasetId, string channelId)
        {
            LoadFileIfNeeded(datasetId);

            var data = _currentFileCache
                   .Where(x => x.ChannelId == channelId)
                   .Cast<MeasurementData>()
                   .OrderBy(x => x.Timestamp)
                   .ToList();

            return Task.FromResult(data);
        }

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
                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length < 5) continue;

                    _currentFileCache.Add(new MeasurementRow
                    {
                        Timestamp = DateTime.Parse(parts[0]),
                        ChannelId = parts[1],
                        Value = double.Parse(parts[2]),
                        ChannelName = parts[3],
                        Unit = parts[4].Trim()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file: " + ex.Message);
            }
        }
    }
}