# Use a Microsoft image with .NET core SDK (https://hub.docker.com/r/microsoft/aspnetcore-build/)
FROM microsoft/aspnetcore-build

# Set the working directory to /work
WORKDIR /work

# Copy DLL
ADD temp/* /work/

# Define environment variables
ENV CONNECTION_STRING "Missing Connection String"
ENV SCENARIO "Missing Scenario"

# Run console app
CMD ["dotnet", "run", "-c", "Release", "ClientConsole"]
