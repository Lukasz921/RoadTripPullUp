FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Backend.sln", "./"]
COPY ["src/API/API.csproj", "src/API/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Core/Core.csproj", "src/Core/"]
COPY ["src/Users/Users.csproj", "src/Users/"]
COPY ["src/TripService/TripService.csproj", "src/TripService/"]
COPY ["tests/UnitTests/UnitTests.csproj", "tests/UnitTests/"]
COPY ["tests/IntegrationTests/IntegrationTests.csproj", "tests/IntegrationTests/"]
COPY ["tests/Users.Tests/Users.Tests.csproj", "tests/Users.Tests/"]

RUN dotnet restore "Backend.sln"

COPY . .
WORKDIR "/src/src/API"
RUN dotnet publish "API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "API.dll"]
