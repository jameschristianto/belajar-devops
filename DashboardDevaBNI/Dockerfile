FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY "DashboardDevaBNI.csproj" .
RUN dotnet restore "DashboardDevaBNI.csproj"
COPY . .
RUN dotnet publish "DashboardDevaBNI.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as final
EXPOSE 8080
WORKDIR /app
COPY --from=build /app/publish/ ./
ENTRYPOINT ["dotnet", "DashboardDevaBNI.dll"]
