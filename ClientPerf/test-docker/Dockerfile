# Use a Microsoft image with .NET core SDK
FROM microsoft/aspnetcore-build

# Set the working directory to /work
WORKDIR /work

# Copy DLL
ADD temp/* /work/

# Define environment variables
ENV SCENARIO "Missing Scenario"

# Run console app
CMD ["dotnet", "run", "ClientConsole"]
