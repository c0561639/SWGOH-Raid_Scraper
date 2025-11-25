FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy project file and restore dependencies
COPY SWGOH-Raid_Scraper.csproj .
RUN dotnet restore "SWGOH-Raid_Scraper.csproj"

# copy source code
COPY src/ ./src/
COPY Properties/ ./Properties/

# build
RUN dotnet build "SWGOH-Raid_Scraper.csproj" -c Release -o /app/build

# publish
FROM build AS publish
RUN dotnet publish "SWGOH-Raid_Scraper.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

# copy html folder with raid data
COPY html/ ./html/

# placeholder
RUN touch .env

# run 
ENTRYPOINT ["dotnet", "SWGOH-Raid_Scraper.dll"]
