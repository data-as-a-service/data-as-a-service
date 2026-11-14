# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY data-as-a-service.sln .
COPY src ./src/

# Restore the API project and its referenced projects. The test projects are not
# included in the runtime image build context.
RUN dotnet restore src/servers/Web/Daas.Api/Daas.Api.csproj

# Build and publish
RUN dotnet publish src/servers/Web/Daas.Api/Daas.Api.csproj -c Release -o /app --no-restore


# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Daas.Api.dll"]
