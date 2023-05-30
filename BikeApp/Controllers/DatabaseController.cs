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


//Old controller, here to avoid routing issues. Can ignore.


[ApiController]
[Route("[controller]")]
public class DatabaseController : Controller
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(IConfiguration configuration, ILogger<DatabaseController> logger)
    {
        _logger = logger;

        var vaultUri = Environment.GetEnvironmentVariable("VaultUri");
        var secretName = "DbConnect";

        var kvClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
        var secret = kvClient.GetSecretAsync(secretName).Result;

        _connectionString = secret.Value.Value;
    }

    public class ApiKey
    {
        public string key_value { get; set; }
    }

    [HttpGet("GetApiKey")]
    public async Task<IActionResult> GetApiKey()
    {

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = "SELECT key_value FROM dbo.api_key";
            var result = await connection.QueryFirstOrDefaultAsync<ApiKey>(query);

            var model = new KeyModel { Key = result?.key_value };
            return View("~/Views/Home/Index.cshtml", model);
        }
    }
}
