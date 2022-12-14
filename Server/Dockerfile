#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Server/Concerto.Server.csproj", "Server/"]
COPY ["Client/Concerto.Client.csproj", "Client/"]
COPY ["Shared/Concerto.Shared.csproj", "Shared/"]
RUN dotnet restore "Server/Concerto.Server.csproj"
COPY . .
WORKDIR "/src/Server"
RUN dotnet build "Concerto.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Concerto.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Concerto.Server.dll"]