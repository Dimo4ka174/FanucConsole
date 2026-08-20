FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FanucFocasConsole.csproj", "."]
RUN dotnet restore "./FanucFocasConsole.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./FanucFocasConsole.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FanucFocasConsole.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
# Native FANUC library is required for the app to run.
COPY NativeLibs/libfwlib32-linux-x64.so.1.0.5 /app/libfwlib32.so
RUN chmod +x /app/libfwlib32.so \
    && ln -sf /app/libfwlib32.so /app/libfwlib32.so.1 \
    && mkdir -p /app/logs && chown -R $APP_UID /app/logs

USER $APP_UID
ENV LD_LIBRARY_PATH=/app
ENTRYPOINT ["dotnet", "FanucFocasConsole.dll"]