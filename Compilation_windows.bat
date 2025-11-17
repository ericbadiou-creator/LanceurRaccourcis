dotnet publish .\LanceurRaccourcis.csproj -c Release -r win-x64 --self-contained true  -p:PublishSingleFile=true -p:VersionPrefix=0.0.0 -o .\exe
pause