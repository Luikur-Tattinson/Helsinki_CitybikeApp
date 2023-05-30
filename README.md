# Helsinki city bike app

### This is the pre-assignment for [Solita Dev Academy Finland 2023](https://github.com/solita/dev-academy-2023-exercise).
### The app can be found here: https://helsinki-citybikeapp.azurewebsites.net/

## App Description
---
### The application shows a map of stations, and journeys for [Helsinki City Bikes.](https://www.hsl.fi/kaupunkipyorat)

## Instructions
---
## Adding new stations:
### Right-clicking the map adds a green map marker to the map. Left-clicking this marker opens a form to add a new station.

## Technologies used
---
### The application was developed using ASP.NET Core, with C# for backend implementation and Razor Pages for frontend presentation.
### The backend services used are Azure App Service and Azure SQL database.
#### The reason for choosing these technologies was familiarity, as well as all of them being in the Microsoft ecosystem, which streamlined things.

## Backend handling
---
### Data verification of the csv files was done with [OpenRefine.](https://openrefine.org/) This included removing duplicate data, journeys that were shorter than 10.
### The database connection is done through Azure Key Vault, so none of the sensitive data is visible in the code. This includes the google maps API key, which is stored in the database.
