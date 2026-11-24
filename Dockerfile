# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["EmployeeAttendanceApp.csproj", "./"]
RUN dotnet restore "EmployeeAttendanceApp.csproj"

# Copy all source files and publish
COPY . .
RUN dotnet publish "EmployeeAttendanceApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Cloud environment settings
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80;http://+:8080
ENV PORT=8080
ENV DATA_DIR=/tmp

EXPOSE 80
EXPOSE 8080

ENTRYPOINT ["dotnet", "EmployeeAttendanceApp.dll"]
