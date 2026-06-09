FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Backend.sln", "./"]
COPY ["src/API/API.csproj", "src/API/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Core/Core.csproj", "src/Core/"]
COPY ["src/Users/Users.csproj", "src/Users/"]
COPY ["src/TripService/TripService.csproj", "src/TripService/"]

# MessageService projects so the API's ProjectReference resolves during docker build
COPY ["src/MessageService/MessageService.API/MessageService.API.csproj", "src/MessageService/MessageService.API/"]
COPY ["src/MessageService/MessageService.Application/MessageService.Application.csproj", "src/MessageService/MessageService.Application/"]
COPY ["src/MessageService/MessageService.Infrastructure/MessageService.Infrastructure.csproj", "src/MessageService/MessageService.Infrastructure/"]
COPY ["src/MessageService/MessageService.Core/MessageService.Core.csproj", "src/MessageService/MessageService.Core/"]
COPY ["src/MessageService/MessageService.UnitTests/MessageService.UnitTests.csproj", "src/MessageService/MessageService.UnitTests/"]
COPY ["src/MessageService/MessageService.IntegrationTests/MessageService.IntegrationTests.csproj", "src/MessageService/MessageService.IntegrationTests/"]

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
