FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ShipperMvp.API/ShipperMvp.API.csproj ShipperMvp.API/
COPY ShipperMvp.Application/ShipperMvp.Application.csproj ShipperMvp.Application/
COPY ShipperMvp.Domain/ShipperMvp.Domain.csproj ShipperMvp.Domain/
COPY ShipperMvp.Infrastructure/ShipperMvp.Infrastructure.csproj ShipperMvp.Infrastructure/
RUN dotnet restore ShipperMvp.API/ShipperMvp.API.csproj

COPY . .
RUN dotnet publish ShipperMvp.API/ShipperMvp.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShipperMvp.API.dll"]
