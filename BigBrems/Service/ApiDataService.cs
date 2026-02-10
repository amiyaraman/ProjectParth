using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BigBrems.Models;

namespace BigBrems.Services
{
    public class ApiDataService : IDataService
    {
        private readonly HttpClient _http;
        private string _accessToken;

        // --- CONFIGURATION ---
        private const bool USE_MOCK_API = true; // Set to FALSE when you have a real server!
        private const string BASE_URL = "https://api.bigbrems.com/v1";

        // Hardcoded Credentials
        private const string API_USER = "admin";
        private const string API_PASS = "secret123";

        public ApiDataService()
        {
            _http = new HttpClient();
        }

        public async Task<bool> AuthenticateAsync()
        {
            if (USE_MOCK_API) { _accessToken = "mock_token_123"; return true; }

            try
            {
                // Hit the real Login Endpoint
                var loginData = new { username = API_USER, password = API_PASS };
                var response = await _http.PostAsJsonAsync($"{BASE_URL}/auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    // Assuming response is like: { "token": "xyz..." }
                    var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    _accessToken = result["token"];

                    // Attach token to all future requests
                    _http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _accessToken);

                    return true;
                }
            }
            catch (Exception ex) { /* Log Error */ }
            return false;
        }

        public async Task<List<DataPool>> GetDataPoolsAsync()
        {
            if (USE_MOCK_API)
            {
                // Simulate network delay
                await Task.Delay(500);
                return new List<DataPool>
                {
                    new DataPool { Id = "POOL_A", Name = "Region: Europe (Test Track)" },
                    new DataPool { Id = "POOL_B", Name = "Region: USA (Lab)" }
                };
            }

            return await _http.GetFromJsonAsync<List<DataPool>>($"{BASE_URL}/datapools");
        }

        public async Task<List<Dataset>> GetDatasetsAsync(string poolId)
        {
            if (USE_MOCK_API)
            {
                // Return different dummy data based on Pool ID
                var list = new List<Dataset>();
                if (poolId == "POOL_A")
                {
                    //list.Add(new Dataset { Id = "DS_101", Name = "Run 101: Nurburgring" });
                    //list.Add(new Dataset { Id = "DS_102", Name = "Run 102: Hockenheim" });
                }
                else
                {
                    //list.Add(new Dataset { Id = "DS_201", Name = "Lab Test: Thermal" });
                }
                return list;
            }

            return await _http.GetFromJsonAsync<List<Dataset>>($"{BASE_URL}/datasets?poolId={poolId}");
        }

        public async Task<List<Channel>> GetChannelsAsync(string datasetId)
        {
            if (USE_MOCK_API)
            {
                return new List<Channel>
                {
                    new Channel { Id = "Speed", Name = "Vehicle Speed", Unit = "km/h", ParentDatasetId = datasetId },
                    new Channel { Id = "Temp", Name = "Brake Temp", Unit = "C", ParentDatasetId = datasetId }
                };
            }
            return await _http.GetFromJsonAsync<List<Channel>>($"{BASE_URL}/channels?datasetId={datasetId}");
        }

        public async Task<List<MeasurementData>> GetDataAsync(string datasetId, string channelId)
        {
            if (USE_MOCK_API)
            {
                var rng = new Random();
                var list = new List<MeasurementData>();
                for (int i = 0; i < 10; i++)
                {
                    list.Add(new MeasurementData
                    {
                        Timestamp = DateTime.Now.AddSeconds(-i),
                        Value = rng.Next(100),
                        ChannelName = channelId,
                        Unit = "Unit"
                    });
                }
                return list;
            }
            return await _http.GetFromJsonAsync<List<MeasurementData>>($"{BASE_URL}/data?ds={datasetId}&ch={channelId}");
        }
    }
}