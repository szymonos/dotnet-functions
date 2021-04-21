using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(FunctionApp.Startup))]

namespace FunctionApp
{
    class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            string appcfEndpoint = Environment.GetEnvironmentVariable("APPCF_ENDPOINT");
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var credential = new DefaultAzureCredential();

            builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
            {
                options.Connect(new Uri(appcfEndpoint), credential)
                    .Select("TestApp:Settings:*", environmentName)
                    .Select("ConnectionStrings:SqlDb:*", environmentName)
                    .ConfigureRefresh(refresh =>
                        refresh.Register("TestApp:Settings:Sentinel", refreshAll: true)
                            .SetCacheExpiration(new TimeSpan(0, 5, 0))
                    );
                options.ConfigureKeyVault(kv => kv.SetCredential(credential));
            });
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.GetContext().Configuration;

            builder.Services.AddAzureAppConfiguration();
            builder.Services.AddScoped(provider =>
            {
                // build SQL connection string with SqlConnectionStringBuilder
                var sqlbuilder = new SqlConnectionStringBuilder
                {
                    ConnectionString = config.GetConnectionString("SqlDb"),
                    UserID = "user_name",
                    Password = config["TestApp:Settings:db_password"],
                    MultipleActiveResultSets = true
                };
                var conn = new SqlConnection(sqlbuilder.ConnectionString);
                conn.Open();
                return conn;
            });

            //AutoHealth
            builder.Services.AddHealthChecks();
        }
    }
}
