#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update
RUN apt-get install -y ffmpeg
RUN ln -s /usr/bin/ffmpeg /app/ffmpeg


FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim-amd64 AS build
WORKDIR /src
COPY ["Server/Concerto.Server.csproj", "Server/"]
COPY ["Client/Concerto.Client.csproj", "Client/"]
COPY ["Shared/Concerto.Shared.csproj", "Shared/"]
RUN apt-get update
RUN apt-get install --no-install-recommends --yes python3
RUN dotnet workload restore "Client/Concerto.Client.csproj"
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