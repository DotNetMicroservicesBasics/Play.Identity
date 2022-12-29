# Play.Identity
Play Economy Identity microservice

## Create and publish package
```powershell

$version="1.0.9"
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

## Run the docker image on local machine
```powershell
$adminPass="[PASSWORD HERE]"
$cosmosDbConnectionString="[CONNECTION_STRING HERE]"
docker run -it --rm -p 5229:5229 --name identity -e MongoDbSettings__Host=mongo=$cosmosDbConnectionString -e RabbitMqSettings__Host=rabbitmq -e IdentitySettings__AdminUserPassword=$adminPass --network playinfrastructure_default play.identity:$version
```


## Run the docker image with az CosmosDb & ServiceBus
```powershell
$adminPass="[PASSWORD HERE]"
$cosmosDbConnectionString="[CONNECTION_STRING HERE]"
$serviceBusConnetionString="[CONNECTION_STRING HERE]"
$messageBroker="AZURESERVICEBUS"
docker run -it --rm -p 5229:5229 --name identity -e MongoDbSettings__ConnectionString=$cosmosDbConnectionString -e ServiceSettings__MessageBroker=$messageBroker -e ServiceBusSettings__ConnectionString=$serviceBusConnetionString -e IdentitySettings__AdminUserPassword=$adminPass play.identity:$version
```


## Publish the docker image on Azure
```powershell
$acrname="playeconomyazurecontainerregistry"
docker tag play.identity:$version "$acrname.azurecr.io/play.identity:$version"
az acr login --name $acrname
docker push "$acrname.azurecr.io/play.identity:$version"
```

## Create kubernetes namespace
```powershell
$namespace="playidentity"
kubectl create namespace $namespace
```

## Create kubernetes secrets
```powershell
kubectl create secret generic playidentity-secrets --from-literal=cosmosdb-connectionstring=$cosmosDbConnectionString --from-literal=servicebus-connectionstring=$serviceBusConnetionString --from-literal=admin-password=$adminPass -n $namespace
```

## Create kubernetes pod
```powershell
kubectl apply -f .\kubernetes\identity.yaml -n $namespace

# list pods in name space
kubectl get pods -n $namespace

# output pod logs
$podname="playidentity-deployement-64888b9bd9-r29jj"
kubectl logs $podname -n $namespace

# list pod details
kubectl describe pod $podname -n $namespace

# list services
kubectl get services -n $namespace

# see events
kubectl get events -n $namespace
```