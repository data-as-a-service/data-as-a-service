# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY data-as-a-service.sln .
COPY ./src/ ./

# Restore dependencies
RUN dotnet restore data-as-a-service.sln

# Build and publish
RUN dotnet publish src/servers/Web/Daas.Api/Daas.Api.csproj -c Release -o /app --no-restore


# ---------- RUNTIME STAGE (SLIM) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app .

EXPOSE 5247
EXPOSE 7268

ENTRYPOINT ["dotnet", "Daas.Api.dll"]
