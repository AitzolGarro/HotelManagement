# Base stage: Using the official ASP.NET 8 runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
# .NET 8 defaults to port 8080
EXPOSE 8080
EXPOSE 8081

# Build stage: Using the SDK to compile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the project file to restore dependencies (leverages Docker cache)
COPY ["HotelReservationSystem.csproj", "./"]
RUN dotnet restore "HotelReservationSystem.csproj"

# Copy all other source files and build
COPY . .
WORKDIR "/src"
RUN dotnet build "HotelReservationSystem.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage: Prepare the optimized production files
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HotelReservationSystem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage: Create the lean runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HotelReservationSystem.dll"]
