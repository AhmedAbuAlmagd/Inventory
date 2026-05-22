FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/SmartInventory.API/SmartInventory.API.csproj", "src/SmartInventory.API/"]
COPY ["src/SmartInventory.Application/SmartInventory.Application.csproj", "src/SmartInventory.Application/"]
COPY ["src/SmartInventory.Domain/SmartInventory.Domain.csproj", "src/SmartInventory.Domain/"]
RUN dotnet restore "src/SmartInventory.API/SmartInventory.API.csproj"
COPY . .
WORKDIR "/src/src/SmartInventory.API"
RUN dotnet build "SmartInventory.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SmartInventory.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartInventory.API.dll"]
