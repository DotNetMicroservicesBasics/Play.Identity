# Play.Identity
Play Economy Identity microservice

## Create and publish package
```powershell

$version="1.0.15"
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

# list secrets
kubectl get secrets -n $namespace

#delete secrets 
kubectl delete secret playidentity-secrets -n $namespace
```

## Create kubernetes pod
```powershell
kubectl apply -f .\kubernetes\identity.yaml -n $namespace

# list pods in namespace
kubectl get pods -n $namespace -w

# output pod logs
$podname="playidentity-deployement-67c468b845-mdzg6"
kubectl logs $podname -n $namespace

# list pod details
kubectl describe pod $podname -n $namespace

#delete pod
kubectl delete pod $podname -n $namespace

# list services (see puplic ip)
kubectl get services -n $namespace

# see events
kubectl get events -n $namespace

# list deployments
kubectl get deployments -n $namespace

# delete deployment
kubectl delete deployment playidentity-deployement -n $namespace
```

## Create Azure Managed Identity and granting it access to Key Vault secrets
```powershell
$appname="playeconomy"
az identity create --resource-group $appname --name $namespace

$kvname="playeconomyazurekeyvault"
$IDENTITY_CLIENT_ID=az identity show -g $appname -n $namespace --query clientId -otsv
az keyvault set-policy -n $kvname --secret-permissions get list --spn $IDENTITY_CLIENT_ID
```

## Establish the federated identity credential
```powershell
$aksname="playeconomyakscluster"
$AKS_OIDC_ISSUER=az aks show -n $aksname -g $appname --query "oidcIssuerProfile.issuerUrl" -otsv

az identity federated-credential create --name $namespace --identity-name $namespace --resource-group $appname --issuer $AKS_OIDC_ISSUER --subject system:serviceaccount:"${namespace}":"${namespace}-serviceaccount"
```

## Create the signing certifiacate
```powershell
kubectl apply -f .\kubernetes\signing-cert.yaml -n $namespace

kubectl get secret -n $namespace

kubectl get secret playidentity-signing-cert -n $namespace -o yaml
```

## Install Helm Chart
```powershell
helm install playidentity-svc .\helm -f .\helm\values.yaml -n $namespace
```

## Install Helm Chart from Container Registery
```powershell

$helmUser=[guid]::Empty.Guid
$helmPassword=az acr login --name $acrname --expose-token --query accessToken -o tsv

helm registry login "$acrname.azurecr.io" --username $helmUser --password $helmPassword

$hemlChartVersion="0.1.0"

helm upgrade --install playidentity-svc oci://$acrname.azurecr.io/helm/microservice --version $hemlChartVersion -f .\helm\values.yaml -n $namespace

# if failed add --debug to see more info
helm upgrade --install playidentity-svc oci://$acrname.azurecr.io/helm/microservice --version $hemlChartVersion -f .\helm\values.yaml -n $namespace --debug

# to make sure helm Charts cash updated
helm repo update

```