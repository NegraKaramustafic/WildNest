FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY eCommerce.Subscriber/eCommerce.Subscriber.csproj eCommerce.Subscriber/
COPY eCommerce.Model/eCommerce.Model.csproj eCommerce.Model/
COPY eCommerce.Services/eCommerce.Services.csproj eCommerce.Services/
COPY eCommerce.Common.Services/eCommerce.Common.Services.csproj eCommerce.Common.Services/
RUN dotnet restore eCommerce.Subscriber/eCommerce.Subscriber.csproj

COPY eCommerce.Subscriber/ eCommerce.Subscriber/
COPY eCommerce.Model/ eCommerce.Model/
COPY eCommerce.Services/ eCommerce.Services/
COPY eCommerce.Common.Services/ eCommerce.Common.Services/
RUN dotnet publish eCommerce.Subscriber/eCommerce.Subscriber.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "eCommerce.Subscriber.dll"]
