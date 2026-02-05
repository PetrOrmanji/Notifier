FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Notifier/Notifier/Notifier.csproj Notifier/Notifier/
RUN dotnet restore Notifier/Notifier/Notifier.csproj

COPY . .
RUN dotnet publish Notifier/Notifier/Notifier.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Notifier.dll"]