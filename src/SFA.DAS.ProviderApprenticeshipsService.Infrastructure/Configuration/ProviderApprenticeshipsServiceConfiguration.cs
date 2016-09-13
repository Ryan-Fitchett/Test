﻿using SFA.DAS.ProviderApprenticeshipsService.Domain.Interfaces;

namespace SFA.DAS.ProviderApprenticeshipsService.Infrastructure.Configuration
{
    public class ProviderApprenticeshipsServiceConfiguration : IConfiguration
    {
        public bool UseFakeIdentity { get; set; }
        public string DatabaseConnectionString { get; set; }
        public string ServiceBusConnectionString { get; set; }
    }
}