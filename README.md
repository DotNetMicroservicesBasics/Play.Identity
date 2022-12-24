# Play.Identity
Play Economy Identity microservice

## Create and publish package
```powershell

$version="1.0.2"
$owner="DotNetMicroservicesBasics"
$local_packages_path="D:\Dev\NugetPackages"
$gh_pat="PAT HERE"

dotnet pack src\Play.Identity.Contracts --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Play.Identity -o $local_packages_path

dotnet nuget push $local_packages_path\Play.Identity.Contracts.$version.nupkg --api-key $gh_pat --source github
```