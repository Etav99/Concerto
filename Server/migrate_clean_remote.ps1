$env:DB_STRING="Host=130.61.32.163;Port=5432;Database=ConcertoDb;username=postgres;Password=E20zX3yraj6AJlW0jq0Y0d3wQS"
dotnet ef database update 0
rm -R ./Migrations
dotnet ef migrations add Initial
dotnet ef database update