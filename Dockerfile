# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
# COPY data-as-a-service.sln .
COPY ./src/ ./

# Restore dependencies
RUN dotnet restore ./servers/Web/Daas.Api/Daas.Api.csproj
RUN dotnet restore ./servers/Web/Daas.Application/Daas.Application.csproj
RUN dotnet restore ./servers/Web/Daas.Domain/Daas.Domain.csproj
RUN dotnet restore ./servers/Web/Daas.Infrastructure/Daas.Infrastructure.csproj

# Build and publish
RUN dotnet publish ./servers/Web/Daas.Api/Daas.Api.csproj -c Release -o /app --no-restore
RUN dotnet publish ./servers/Web/Daas.Application/Daas.Application.csproj -c Release -o /app --no-restore
RUN dotnet publish ./servers/Web/Daas.Domain/Daas.Domain.csproj -c Release -o /app --no-restore
RUN dotnet publish ./servers/Web/Daas.Infrastructure/Daas.Infrastructure.csproj -c Release -o /app --no-restore


# ---------- RUNTIME STAGE (SLIM) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app .

EXPOSE 5247
EXPOSE 7268

ENTRYPOINT ["dotnet", "Daas.Api.dll"]
