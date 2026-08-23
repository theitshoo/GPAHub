FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GPAHub.Domain/GPAHub.Domain.csproj", "GPAHub.Domain/"]
COPY ["GPAHub.Application/GPAHub.Application.csproj", "GPAHub.Application/"]
COPY ["GPAHub.Infrastructure/GPAHub.Infrastructure.csproj", "GPAHub.Infrastructure/"]
COPY ["GPAHub.Web/GPAHub.Web.csproj", "GPAHub.Web/"]
RUN dotnet restore GPAHub.Web/GPAHub.Web.csproj

COPY . .
RUN dotnet publish GPAHub.Web -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GPAHub.Web.dll"]
