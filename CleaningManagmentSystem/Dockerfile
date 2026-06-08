# ── Build stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["CleaningManagmentSystem.csproj", "."]
RUN dotnet restore "CleaningManagmentSystem.csproj"

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# Create upload directories
RUN mkdir -p /app/wwwroot/uploads/posts \
  && mkdir -p /app/uploads \
  && mkdir -p /app/wwwroot/transport-uploads

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "CleaningManagmentSystem.dll"]
