FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PushoverDownAlert/PushoverDownAlert.csproj", "PushoverDownAlert/"]
RUN dotnet restore "PushoverDownAlert/PushoverDownAlert.csproj"
COPY . .
WORKDIR "/src/PushoverDownAlert"
RUN dotnet build "./PushoverDownAlert.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PushoverDownAlert.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PushoverDownAlert.dll"]
