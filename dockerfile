# SDK image for build and tests
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Zapp.Core/Zapp.Core.csproj", "Zapp.Core/"]
COPY ["Zapp.Infrastructure/Zapp.Infrastructure.csproj", "Zapp.Infrastructure/"]
RUN dotnet restore "Zapp.Infrastructure/Zapp.Infrastructure.csproj"

COPY . .
WORKDIR "/src/Zapp.Core"
RUN dotnet build "Zapp.Core.csproj" -c Release -o /app/build

# execution and runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "Zapp.Core.dll"]