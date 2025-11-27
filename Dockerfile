FROM jtl-tkgiharbor.hq.bni.co.id/govtech/custom/base-image:6.0 AS base
ENV ASPNETCORE_URLS http://*:8080
ENV ASPNETCORE_ENVIRONMENT=Development
ENV TZ=Asia/Jakarta
WORKDIR /app

EXPOSE 8080

FROM  jtl-tkgiharbor.hq.bni.co.id/govtech/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DashboardDevaBNI/DashboardDevaBNI.csproj", "DashboardDevaBNI/"]
RUN dotnet restore "DashboardDevaBNI/DashboardDevaBNI.csproj" --disable-parallel
COPY . .
WORKDIR "/src/DashboardDevaBNI"
RUN dotnet build "DashboardDevaBNI.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "DashboardDevaBNI.csproj" -c Debug -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN bash -c 'mkdir /DashboardDevaFile/'
RUN bash -c 'mkdir /DashboardDevaFilePendukung/'
RUN bash -c 'mkdir LOG LOGS'
RUN bash -c 'chmod -R 777 /DashboardDevaFile/'
RUN bash -c 'chmod -R 777 /DashboardDevaFilePendukung/'
RUN bash -c 'chmod -R 777 LOG LOGS'

ENTRYPOINT ["dotnet", "DashboardDevaBNI.dll"]