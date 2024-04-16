FROM mcr.microsoft.com/dotnet/sdk:6.0 as Build
WORKDIR /app

# Copy the .csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy the remaining source code and build the application
COPY . ./
RUN dotnet publish -c Release -o build

# Build the runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app/build .

# Entry point when the container starts
CMD ["dotnet", "Baguettefy.dll"]