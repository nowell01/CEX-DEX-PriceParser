# See https://aka.ms/customizecontainer to learn how to customize your debug container.

# Runtime base (also used by Visual Studio "fast mode" debugging)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Build + publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CEX-DEX-Parser/CEX-DEX-Parser.csproj", "CEX-DEX-Parser/"]
RUN dotnet restore "./CEX-DEX-Parser/CEX-DEX-Parser.csproj"
COPY . .
WORKDIR "/src/CEX-DEX-Parser"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CEX-DEX-Parser.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CEX-DEX-Parser.dll"]