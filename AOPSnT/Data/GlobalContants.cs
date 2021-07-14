using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AOPSnT.Data
{
    public sealed class GlobalConstants
    {
        private static GlobalConstants globalConstants;
        public LiteDatabase MainDB;
        public string DataFolder;
        public GlobalConstants(IConfiguration configuration)
        {
            this.MainDB = new LiteDatabase(configuration.GetConnectionString("LDBConnection"));
            this.DataFolder = configuration.GetConnectionString("LDBFolder");

            globalConstants = this;
        }


        public static GlobalConstants Current
        {
            get
            {
                if (globalConstants == null)
                {
                    globalConstants = GetCurrentSettings();
                }

                return globalConstants;
            }
        }

        public static GlobalConstants GetCurrentSettings()
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            var settings = new GlobalConstants(configuration.GetSection("AppSettings"));

            return settings;
        }

    }
}
