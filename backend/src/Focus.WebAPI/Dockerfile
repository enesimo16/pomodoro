FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["backend/Directory.Build.props", "backend/global.json", "backend/"]
COPY ["backend/src/Focus.Domain/Focus.Domain.csproj", "backend/src/Focus.Domain/"]
COPY ["backend/src/Focus.Application/Focus.Application.csproj", "backend/src/Focus.Application/"]
COPY ["backend/src/Focus.Infrastructure/Focus.Infrastructure.csproj", "backend/src/Focus.Infrastructure/"]
COPY ["backend/src/Focus.WebAPI/Focus.WebAPI.csproj", "backend/src/Focus.WebAPI/"]
RUN dotnet restore "backend/src/Focus.WebAPI/Focus.WebAPI.csproj"
COPY backend/ backend/
RUN dotnet publish "backend/src/Focus.WebAPI/Focus.WebAPI.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Focus.WebAPI.dll"]
