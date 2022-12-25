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

## Build the docker image
```powershell
$env:GH_OWNER="DotNetMicroservicesBasics"
$env:GH_PAT="[PAT HERE]"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t play.identity:$version .
```

## Run the docker image
```powershell
$adminPass="[PASSWORD HERE]"
docker run -it --rm -p 5229:5229 --name identity -e MongoDbSettings__Host=mongo -e RabbitMqSettings__Host=rabbitmq -e IdentitySettings__AdminUserPassword=$adminPass --network playinfrastructure_default play.identity:$version
```