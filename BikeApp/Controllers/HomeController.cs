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
using Microsoft.AspNetCore.Http;

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

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string orderBy = "Nimi", string sortOrder = "asc", string search = "")
        {
            _logger.LogInformation("Connecting to the database...");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                _logger.LogInformation("Connected to the database.");

                string query = "SELECT key_value FROM dbo.api_key";
                var result = await connection.QueryFirstOrDefaultAsync<ApiKey>(query);

                string queryStations = $"SELECT x, y, Nimi, Namn, Osoite, Kaupunki, Operaattor, Kapasiteet, jrn_to, jrn_from FROM dbo.Stations WHERE Nimi LIKE '%{search}%' ORDER BY {orderBy} {sortOrder}";
                var stations = (await connection.QueryAsync<Station>(queryStations)).ToList();

                _logger.LogInformation("Retrieved data from the database.");
                _logger.LogInformation($"Key: {result?.key_value}");

                var totalRecords = stations.Count;
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                

                // Apply pagination
                var startIndex = (page - 1) * pageSize;
                var endIndex = startIndex + pageSize - 1;
                var pagedStations = stations.Skip(startIndex).Take(pageSize).ToList();

                var model = new KeyModel
                {
                    Key = result?.key_value,
                    Stations = pagedStations,
                    AllStations = stations, // Store the complete list of stations
                    Pagination = new PaginationViewModel
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = totalPages
                    }
                };

                ViewData["OrderBy"] = orderBy;
                ViewData["SortOrder"] = sortOrder;
                ViewData["Search"] = search;

                _logger.LogInformation($"Model Key: {model?.Key}");
                if (model.Stations != null && model.Stations.Count > 0)
                {
                    var firstStation = model.Stations[0];
                    _logger.LogInformation($"First station: X= {firstStation.X}, Y = {firstStation.Y}");
                }

                return View(model);
            }
        }



        [HttpPost]
        public async Task<IActionResult> AddStation(Station station)
        {
            if (ModelState.IsValid)
            {

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("X: @X, Y: @Y");
                    string insertQuery = @"INSERT INTO dbo.Stations (Nimi, Namn, Osoite, Kaupunki, Operaattor, Kapasiteet, x, y)
                                   VALUES (@Nimi, @Namn, @Osoite, @Kaupunki, @Operaattor, @Kapasiteet, @X, @Y)";

                    await connection.ExecuteAsync(insertQuery, station);

                    _logger.LogInformation("Station added to the database.");

                    // Redirect to the Index action to display the updated list of stations
                    return RedirectToAction("Index");
                }
            }
            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    _logger.LogError(error.ErrorMessage);
                }
            }
            _logger.LogInformation($"X: {station.X}, Y: {station.Y}, NIMI: {station.Nimi}");
            // If the model state is not valid, return the form view with validation errors
            return PartialView("addStationForm", station);
        }

        public IActionResult Journeys(int? month, string orderBy, string sortOrder, string searchTerm, int page = 1, int pageSize = 10)
        {
            // Retrieve the selected month from the session
            int? selectedMonth = HttpContext.Session.GetInt32("SelectedMonth");

            // Use the selected month if available, otherwise use the default month
            if (!month.HasValue || month < 5 || month > 7)
            {
                month = selectedMonth ?? 5; // Default month
            }

            // Store the selected month in the session
            HttpContext.Session.SetInt32("SelectedMonth", month.Value);

            string tableName = $"dbo.[2021-{month:00}]";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string countQuery = $"SELECT COUNT(*) FROM {tableName}";
                int totalRecords = connection.ExecuteScalar<int>(countQuery);

                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                string orderByClause = GetOrderByClause(orderBy, sortOrder);

                string searchCondition = string.IsNullOrEmpty(searchTerm) ? string.Empty : $"WHERE [Departure station name] LIKE '%' + @SearchTerm + '%'";

                string query = $@"
            SELECT *,
                CAST([Departure] AS DATETIME) AS [Departure],
                CAST([Return] AS DATETIME) AS [Return]
            FROM (
                SELECT ROW_NUMBER() OVER (ORDER BY {orderByClause}) AS RowNum, 
                    [Departure], [Return], [Departure station id], [Departure station name],
                    [Return station id], [Return station name], [Covered distance_m], [Duration_sec]
                FROM {tableName}
                {searchCondition}
            ) AS J
            WHERE J.RowNum BETWEEN @StartIndex AND @EndIndex";

                int startIndex = (page - 1) * pageSize + 1;
                int endIndex = page * pageSize;

                var journeys = connection.Query<JourneyModel>(query, new { StartIndex = startIndex, EndIndex = endIndex, SearchTerm = searchTerm }).ToList();

                var model = new JourneysViewModel
                {
                    Journeys = journeys,
                    Pagination = new PaginationViewModel
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalPages = totalPages
                    },
                    OrderBy = orderBy,
                    SortOrder = sortOrder,
                    SelectedMonth = month.Value, // Add the selected month to the view model
                    SearchTerm = searchTerm // Add the search term to the view model
                };

                return View(model);
            }
        }

        private string GetOrderByClause(string orderBy, string sortOrder)
        {
            string orderByClause = string.Empty;

            switch (orderBy)
            {
                case "Departure":
                    orderByClause = $"[Departure] {sortOrder}";
                    break;
                case "Return":
                    orderByClause = $"[Return] {sortOrder}";
                    break;
                case "DepartureStationId":
                    orderByClause = $"[Departure station id] {sortOrder}";
                    break;
                case "DepartureStationName":
                    orderByClause = $"[Departure station name] {sortOrder}";
                    break;
                case "ReturnStationId":
                    orderByClause = $"[Return station id] {sortOrder}";
                    break;
                case "ReturnStationName":
                    orderByClause = $"[Return station name] {sortOrder}";
                    break;
                case "CoveredDistance":
                    orderByClause = $"[Covered distance_m] {sortOrder}";
                    break;
                case "Duration":
                    orderByClause = $"[Duration_sec] {sortOrder}";
                    break;
                default:
                    orderByClause = "[Departure] DESC"; // Default sorting by Departure column
                    break;
            }

            return orderByClause;
        }



    }
}
