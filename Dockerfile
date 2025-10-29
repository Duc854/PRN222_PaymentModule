# Multi-stage Dockerfile for .NET 8 Razor Pages / ASP.NET Core app (PaymentModule.Web)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything (keep simple to work with typical repo layout)
COPY . .

# Restore and publish the web project
RUN dotnet restore "PaymentModule.Web/PaymentModule.Web.csproj"
RUN dotnet publish "PaymentModule.Web/PaymentModule.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Listen on port 8080 (matches docker-compose)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PaymentModule.Web.dll"]