using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BikeApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DatabaseController;

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
                string queryStations = "SELECT x, y, Nimi, Namn, Osoite, Kaupunki, Operaattor, Kapasiteet FROM dbo.Stations";
                var stations = (await connection.QueryAsync<Station>(queryStations)).ToList();

                _logger.LogInformation("Retrieved data from the database.");
                _logger.LogInformation($"Key: {result?.key_value}");

                var model = new KeyModel
                {
                    Key = result?.key_value,
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

        public IActionResult Journeys(int? month, int page = 1, int pageSize = 10)
        {
            if (!month.HasValue)
            {
                month = 5;
            }

            string tableName = $"dbo.[2021-{month:00}]";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string countQuery = $"SELECT COUNT(*) FROM {tableName}";
                int totalRecords = connection.ExecuteScalar<int>(countQuery);

                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                string query = $@"SELECT *,
    CAST([Departure] AS DATETIME) AS [Departure],
    CAST([Return] AS DATETIME) AS [Return]
FROM (
    SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum, 
        [Departure], [Return], [Departure station id], [Departure station name],
        [Return station id], [Return station name], [Covered distance_m], [Duration_sec]
    FROM {tableName}
) AS J
WHERE J.RowNum BETWEEN @StartIndex AND @EndIndex";




                int startIndex = (page - 1) * pageSize + 1;
                int endIndex = page * pageSize;

                var journeys = connection.Query<JourneyModel>(query, new { StartIndex = startIndex, EndIndex = endIndex }).ToList();

                var model = new JourneysViewModel
                {
                    Journeys = journeys,
                    Pagination = new PaginationViewModel
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = totalPages
                    }
                };

                return View(model);
            }
        }


    }
}
