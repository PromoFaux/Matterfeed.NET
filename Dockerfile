# Stage 1
FROM microsoft/aspnetcore-build:1.0-2.0 AS builder
WORKDIR /source

COPY . .
RUN dotnet restore MattermostRSS.sln
RUN dotnet publish MattermostRSS.sln -c Release -o /publish

# Stage 2
FROM microsoft/dotnet:1.1-runtime
WORKDIR /app
COPY --from=builder /publish .
ENTRYPOINT ["dotnet", "MattermostRSS.dll"]
