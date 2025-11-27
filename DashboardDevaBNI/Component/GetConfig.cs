using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DashboardDevaBNI.Component
{
    public class GetConfig
    {
        public static IConfiguration AppSetting { get; }
        static GetConfig()
        {
            AppSetting = new ConfigurationBuilder()
                    .AddEnvironmentVariables(prefix: "DEVA_")
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
        }
    }
}
