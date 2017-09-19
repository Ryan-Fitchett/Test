﻿using SFA.DAS.Comitments.AcademicYearEndProcessor.WebJob.Updater;
using SFA.DAS.NLog.Logger;
using StructureMap;
using System;
using System.Reflection;
using Microsoft.Azure;
using Newtonsoft.Json;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities.DataLock;
using SFA.DAS.Commitments.Domain.Interfaces;
using SFA.DAS.Commitments.Infrastructure.Data;
using SFA.DAS.Commitments.Infrastructure.Services;
using SFA.DAS.Configuration;
using SFA.DAS.Configuration.AzureTableStorage;
using IConfiguration = SFA.DAS.Commitments.Domain.Interfaces.IConfiguration;

namespace SFA.DAS.Comitments.AcademicYearEndProcessor.WebJob.DependencyResolution
{
    public class DefaultRegistry : Registry
    {
        public DefaultRegistry()
        {
            Scan(
                scan =>
                {
                    scan.AssembliesAndExecutablesFromApplicationBaseDirectory(a => a.GetName().Name.StartsWith("SFA.DAS"));
                    scan.RegisterConcreteTypesAgainstTheFirstInterface();
                });


            var config = GetConfiguration("SFA.DAS.CommitmentsAcademicYearEndProcessor");
            For<IConfiguration>().Use(config);
            For<CommitmentsAcademicYearEndProcessorConfiguration>().Use(config);
            For<ILog>().Use(x => new NLogLogger(x.ParentType, new DummyRequestContext(), null)).AlwaysUnique();
            For<ICurrentDateTime>().Use(x => new CurrentDateTime(config.CurrentStartTime));

            For<IDataLockRepository>().Use<DataLockRepository>().Ctor<string>().Is(config.DatabaseConnectionString);


            //config.CurrentStartTime = DateTime.UtcNow.AddDays(79);
            //config.ExpirableDataLockErrorCodes = DataLockErrorCode.Dlock03 |
            //                                     DataLockErrorCode.Dlock04 |
            //                                     DataLockErrorCode.Dlock05 |
            //                                     DataLockErrorCode.Dlock06 |
            //                                     DataLockErrorCode.Dlock07;

            //System.IO.File.WriteAllText(@"d:\test.json", JsonConvert.SerializeObject(config));


            For<IAcademicYearEndExpiryProcessor>()
                .Use<AcademicYearEndExpiryProcessor>()
                .Ctor<DataLockErrorCode>().Is(config.ExpirableDataLockErrorCodes);


        }
        private CommitmentsAcademicYearEndProcessorConfiguration GetConfiguration(string serviceName)
        {
            var environment = Environment.GetEnvironmentVariable("DASENV");
            if (string.IsNullOrEmpty(environment))
            {
                environment = CloudConfigurationManager.GetSetting("EnvironmentName");
            }
            if (environment.Equals("LOCAL") || environment.Equals("AT") || environment.Equals("TEST"))
            {
                //todo: is this required?
                PopulateSystemDetails(environment);
            }

            var configurationRepository = GetConfigurationRepository();
            var configurationService = new ConfigurationService(configurationRepository, 
                new ConfigurationOptions(serviceName, environment, "1.0"));

            var result = configurationService.Get<CommitmentsAcademicYearEndProcessorConfiguration>();

            return result;
        }

        private static IConfigurationRepository GetConfigurationRepository()
        {
            return new AzureTableStorageConfigurationRepository(CloudConfigurationManager.GetSetting("ConfigurationStorageConnectionString"));
        }
        private void PopulateSystemDetails(string envName)
        {
            SystemDetails.EnvironmentName = envName;
            SystemDetails.VersionNumber = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public static class SystemDetails
        {
            public static string VersionNumber { get; set; }
            public static string EnvironmentName { get; set; }
        }
    }





    public class DummyRequestContext : IRequestContext
    {
        public string Url { get; }

        public string IpAddress { get; }
    }
}
