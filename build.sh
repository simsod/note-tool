dotnet pack NoteTool/NoteTool.csproj -c Release -o artifacts
dotnet tool update -g --add-source artifacts NoteTool