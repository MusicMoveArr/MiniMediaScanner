FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

# Install ffmpeg, fpcalc and ca-certs
USER root
RUN apt-get update
RUN apt-get install -y ffmpeg libchromaprint-tools
RUN apt-get install -y ca-certificates
RUN update-ca-certificates

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MiniMediaScanner/MiniMediaScanner.csproj", "MiniMediaScanner/"]
RUN dotnet restore "MiniMediaScanner/MiniMediaScanner.csproj"
COPY . .
WORKDIR "/src/MiniMediaScanner"
RUN dotnet build "MiniMediaScanner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MiniMediaScanner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MiniMediaScanner.dll"]
