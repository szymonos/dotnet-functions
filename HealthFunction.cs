using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FunctionApp
{
    public class HealthFunction
    {
        private readonly ILogger<HealthFunction> _logger;
        private readonly HealthCheckService _healthCheck;


        public HealthFunction(HealthCheckService healthCheck, ILogger<HealthFunction> logger)
        {
            _healthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [FunctionName("Health")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestMessage req,
            ILogger log)
        {
            _logger.Log(LogLevel.Information, "Received health request");

            var status = await _healthCheck.CheckHealthAsync();

            return new OkObjectResult(Enum.GetName(typeof(HealthStatus), status.Status));
        }
    }

}
