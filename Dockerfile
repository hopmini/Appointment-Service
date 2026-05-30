FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AppointmentService.csproj", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish "AppointmentService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 5150

ENV ASPNETCORE_URLS=http://+:5150
ENV ASPNETCORE_ENVIRONMENT=Development

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AppointmentService.dll"]
