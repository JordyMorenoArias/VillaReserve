# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

COPY ["src/VillaReserve.Api.csproj", "src/"]
RUN dotnet restore "src/VillaReserve.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/VillaReserve.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VillaReserve.Api.dll"]
