FROM brokerhub-api:local AS current
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
COPY --from=current /app/ /published/
WORKDIR /source
COPY integration/messaging-probe/ ./
RUN dotnet publish MessagingProbe.csproj -c Release -o /probe-output
# Direct file references otherwise select SqlClient's platform-neutral facade.
RUN cp /published/runtimes/unix/lib/net9.0/Microsoft.Data.SqlClient.dll /probe-output/Microsoft.Data.SqlClient.dll
FROM current
COPY --from=build /probe-output/ /probe/
ENTRYPOINT ["dotnet", "/probe/MessagingProbe.dll"]
