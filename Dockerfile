FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY DgSales.slnx global.json ./
COPY src/DgSales.Api/DgSales.Api.csproj src/DgSales.Api/
RUN dotnet restore src/DgSales.Api/DgSales.Api.csproj
COPY src src
COPY data data
COPY assets assets
RUN dotnet publish src/DgSales.Api/DgSales.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends fonts-dejavu-core curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 \
    QuotationBranding__FontPath=/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf \
    QuotationBranding__BoldFontPath=/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 CMD curl --fail http://localhost:8080/health/ready || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "DgSales.Api.dll"]
