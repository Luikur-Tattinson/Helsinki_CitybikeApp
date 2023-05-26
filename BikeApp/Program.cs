using Azure.Identity;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri"));
builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<DatabaseController>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbController = scope.ServiceProvider.GetRequiredService<DatabaseController>();
    await dbController.GetApiKey();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Call a method to configure Dapper mappings
ConfigureDapperMappings();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

void ConfigureDapperMappings()
{
    var assembly = Assembly.GetExecutingAssembly();
    var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Model")).ToList();

    foreach (var type in types)
    {
        var tableName = type.Name.Replace("Model", "");
        var props = type.GetProperties().Where(p => p.GetCustomAttribute(typeof(ColumnAttribute)) != null);

        var map = new CustomPropertyTypeMap(type, (t, columnName) =>
        {
            var prop = props.FirstOrDefault(p =>
            {
                var columnAttr = p.GetCustomAttribute<ColumnAttribute>();
                return columnAttr != null && columnAttr.Name == columnName;
            });

            if (prop != null)
            {
                return prop;
            }
            else
            {
                return GetPropertyByColumnName(t, columnName);
            }
        });

        SqlMapper.SetTypeMap(type, map);
    }
}

PropertyInfo GetPropertyByColumnName(Type type, string columnName)
{
    var properties = type.GetProperties();

    foreach (var property in properties)
    {
        var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
        if (columnAttr != null && columnAttr.Name == columnName)
        {
            return property;
        }
    }

    return null;
}


app.Run();
