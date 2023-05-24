using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BikeApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static DatabaseController;
using System.Linq;

namespace BikeApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
        {
            _logger = logger;

            var vaultUri = Environment.GetEnvironmentVariable("VaultUri");
            var secretName = "DbConnect";

            var kvClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
            var secret = kvClient.GetSecretAsync(secretName).Result;

            _connectionString = secret.Value.Value;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Connecting to the database...");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                _logger.LogInformation("Connected to the database.");

                string query = "SELECT key_value FROM dbo.api_key";
                var result = await connection.QueryFirstOrDefaultAsync<ApiKey>(query);
                string queryStations = "SELECT x, y FROM dbo.Stations";
                var stations = (await connection.QueryAsync<Station>(queryStations)).ToList();

                _logger.LogInformation("Retrieved data from the database.");
                _logger.LogInformation($"Key: {result?.key_value}");

                var model = new KeyModel { Key = result?.key_value,
                Stations = stations
                };
                _logger.LogInformation($"Model Key: {model?.Key}");
                if (model.Stations != null && model.Stations.Count > 0)
                {
                    var firstStation = model.Stations[0];
                    _logger.LogInformation($"First station: X= {firstStation.X}, Y = {firstStation.Y}");
                }
                return View("~/Views/Home/Index.cshtml", model);
            }
        }
    }
}