#	Use a Microsoft image with .NET core runtime (https://hub.docker.com/r/microsoft/dotnet/tags/)
FROM jekyll/builder:3.8 AS final

#	Copy package
COPY bin .

#	Define environment variables
#   None...

#	Run startup script
CMD ["dotnet", "MultiPerfClient.dll"]