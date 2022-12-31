using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace Play.Identity.Service.HealthChecks
{
    public class MongoDbHealthCheck : IHealthCheck
    {
        private MongoClient _mongoClient;

        public MongoDbHealthCheck(MongoClient mongoClient)
        {
            _mongoClient = mongoClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _mongoClient.ListDatabaseNamesAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(exception: ex);                
            }
        }
    }
}