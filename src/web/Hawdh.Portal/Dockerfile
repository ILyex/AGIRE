FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Hawdh.Portal.csproj ./
RUN dotnet restore Hawdh.Portal.csproj
COPY . ./
RUN dotnet publish Hawdh.Portal.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DataProtection__KeysPath=/var/lib/hawdh/keys
EXPOSE 8080
COPY --from=build /app/publish .
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/lib/hawdh/keys \
    && chown -R $APP_UID:$APP_UID /var/lib/hawdh
VOLUME ["/var/lib/hawdh/keys"]
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 CMD curl --fail --silent http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "Hawdh.Portal.dll"]
