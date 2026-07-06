FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY src/PagueVeloz.API/PagueVeloz.API.csproj src/PagueVeloz.API/
COPY src/PagueVeloz.Application/PagueVeloz.Application.csproj src/PagueVeloz.Application/
COPY src/PagueVeloz.Domain/PagueVeloz.Domain.csproj src/PagueVeloz.Domain/
COPY src/PagueVeloz.Infrastructure/PagueVeloz.Infrastructure.csproj src/PagueVeloz.Infrastructure/

RUN dotnet restore src/PagueVeloz.API/PagueVeloz.API.csproj

COPY . .
WORKDIR /app/src/PagueVeloz.API
RUN dotnet publish PagueVeloz.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PagueVeloz.API.dll"]