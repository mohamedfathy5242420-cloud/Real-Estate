FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /source
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY .config/dotnet-tools.json .config/
COPY src/ src/
RUN dotnet restore src/BrokerHub.Api/BrokerHub.Api.csproj --disable-parallel
RUN dotnet publish src/BrokerHub.Api/BrokerHub.Api.csproj -c Release --no-restore -o /output -m:1
RUN dotnet tool restore && dotnet ef migrations bundle --no-build --configuration Release --project src/BrokerHub.Infrastructure --startup-project src/BrokerHub.Api --output /output/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates && rm -rf /var/lib/apt/lists/*
COPY docker/trust/local-rabbitmq-ca.crt /usr/local/share/ca-certificates/local-rabbitmq-ca.crt
RUN update-ca-certificates
WORKDIR /app
COPY --from=build /output/ ./
COPY docker/api-entrypoint.sh /app/start.sh
RUN mkdir -p /data/images /data/keys /data/identity && chown -R $APP_UID:$APP_UID /data && chmod +x /app/start.sh /app/efbundle
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["/bin/sh", "/app/start.sh"]
