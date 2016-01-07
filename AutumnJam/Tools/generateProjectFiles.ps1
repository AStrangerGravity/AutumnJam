# Windows Only
# Generates the Visual Studio solution files so that we can perform linting and other CI analysis on it.

# Print each command as we go
# Set-PSDebug -Step
try {
# Find our project path
$tools_dir = Split-Path $MyInvocation.MyCommand.Path
$project_dir = Join-Path $tools_dir "/../" -resolve

# Query the assembly function to generate the project
$exitCode = [Diagnostics.Process]::Start("C:\Program Files\Unity\Editor\Unity.exe", 
    "-batchmode -logFile -projectPath $project_dir -executeMethod SyntaxTree.VisualStudio.Unity.Bridge.ProjectFilesGenerator.GenerateProject -quit").WaitForExit(60000) # Wait for 60 seconds

    
  # your code here
} Catch {
  $ErrorMessage = $_.Exception.Message
  Write-Output $ErrorMessage
  exit(1)
}