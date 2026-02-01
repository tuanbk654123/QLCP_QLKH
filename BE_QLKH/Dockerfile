# Sử dụng image SDK của .NET 10 để build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy file csproj và restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ source code và build
COPY . ./
RUN dotnet publish -c Release -o out

# Sử dụng image Runtime để chạy ứng dụng (nhẹ hơn SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# Mở port cho Render (Render thường dùng PORT environment variable)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BE_QLKH.dll"]
