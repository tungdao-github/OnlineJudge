# Base image để chạy app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# SDK image để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file csproj
COPY ./OnlineJudgeAPI.csproj ./OnlineJudgeAPI/

WORKDIR /src/OnlineJudgeAPI
RUN dotnet restore "OnlineJudgeAPI.csproj"

# Copy toàn bộ source code
COPY . .

# Build Release
RUN dotnet build "OnlineJudgeAPI.csproj" -c Release -o /app/build

# Publish app (bắt buộc phải publish để chạy ASP.NET Core Production)
RUN dotnet publish "OnlineJudgeAPI.csproj" -c Release -o /app/publish

# Install các compiler (gcc, g++, docker, java, ...)
# RUN apt-get update && \
#     apt-get install -y \
#     build-essential \
#     g++ \
#     gcc \
#     default-jdk \
#     docker.io

# Final stage
FROM base AS final
WORKDIR /app

# Copy publish folder
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OnlineJudgeAPI.dll"]
