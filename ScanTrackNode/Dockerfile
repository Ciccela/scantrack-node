
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ScanTrackNode/*.csproj ScanTrackNode/
RUN dotnet restore ScanTrackNode/ScanTrackNode.csproj

COPY . .
RUN dotnet publish ScanTrackNode/ScanTrackNode.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ScanTrackNode.dll"]