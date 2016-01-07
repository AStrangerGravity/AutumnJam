# Windows Only
# Generates the Visual Studio solution files so that we can perform linting and other CI analysis on it.

# Print each command as we go
# Set-PSDebug -Step

# Find our project path
$working_dir = Split-Path $MyInvocation.MyCommand.Path
#$project_dir = Join-Path $working_dir "/../" -resolve

# Query the assembly function to generate the project
$exitCode = [Diagnostics.Process]::Start("C:\Program Files\Unity\Editor\Unity.exe", 
    "-batchmode -logFile -projectPath $working_dir -executeMethod SyntaxTree.VisualStudio.Unity.Bridge.ProjectFilesGenerator.GenerateProject -quit").WaitForExit(60000) # Wait for 60 seconds

