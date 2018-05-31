# Use a Microsoft image with .NET core runtime only
FROM microsoft/aspnetcore

# Set the working directory to /work
WORKDIR /work

# Copy DLL
ADD ../ClientConsole/bin/Release/netcoreapp2.0/ClientConsole.dll /work

# Define environment variables
ENV SCENARIO "Missing Scenario"

# Run console app
CMD ["dotnet", "ClientConsole.dll"]
