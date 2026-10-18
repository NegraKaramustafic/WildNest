FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY eCommerce.WebAPI/eCommerce.WebAPI.csproj eCommerce.WebAPI/
COPY eCommerce.Model/eCommerce.Model.csproj eCommerce.Model/
COPY eCommerce.Services/eCommerce.Services.csproj eCommerce.Services/
COPY eCommerce.Common.Services/eCommerce.Common.Services.csproj eCommerce.Common.Services/
RUN dotnet restore eCommerce.WebAPI/eCommerce.WebAPI.csproj

COPY eCommerce.WebAPI/ eCommerce.WebAPI/
COPY eCommerce.Model/ eCommerce.Model/
COPY eCommerce.Services/ eCommerce.Services/
COPY eCommerce.Common.Services/ eCommerce.Common.Services/
RUN dotnet publish eCommerce.WebAPI/eCommerce.WebAPI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5126
EXPOSE 5126
COPY --from=build /app .
ENTRYPOINT ["dotnet", "eCommerce.WebAPI.dll"]
