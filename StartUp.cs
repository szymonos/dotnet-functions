using System;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Azure.Identity;

[assembly: FunctionsStartup(typeof(FunctionApp.Startup))]

namespace FunctionApp
{
    class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            string endpoint = Environment.GetEnvironmentVariable("APPCF_ENDPOINT");
            var credential = new DefaultAzureCredential();
            string env = builder.GetContext().EnvironmentName;

            builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
            {
                options.Connect(new Uri(endpoint), credential)
                    .Select("TestApp:Settings:*", env)
                    .Select("ConnectionStrings:SqlDb:*", env)
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
                var builder = new SqlConnectionStringBuilder
                {
                    ConnectionString = config.GetConnectionString("SqlDb"),
                    UserID = "user_name",
                    Password = config["TestApp:Settings:db_password"],
                    MultipleActiveResultSets = true
                };
                var conn = new SqlConnection(builder.ConnectionString);
                conn.Open();
                return conn;
            });
        }
    }
}
