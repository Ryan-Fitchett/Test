// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultRegistry.cs" company="Web Advanced">
// Copyright 2012 Web Advanced (www.webadvanced.com)
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Web;
using FluentValidation;
using MediatR;
using Microsoft.Azure;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Interfaces;
using SFA.DAS.Commitments.Infrastructure.Configuration;
using SFA.DAS.Commitments.Infrastructure.Data;
using SFA.DAS.Commitments.Infrastructure.Logging;
using SFA.DAS.Configuration;
using SFA.DAS.Configuration.AzureTableStorage;
using SFA.DAS.Events.Api.Client;
using SFA.DAS.Events.Api.Client.Configuration;
using SFA.DAS.NLog.Logger;
using StructureMap;
using StructureMap.Graph;

namespace SFA.DAS.Commitments.Api.DependencyResolution
{
    public class DefaultRegistry : Registry
    {
        private const string ServiceName = "SFA.DAS.Commitments";
        private const string Version = "1.0";

        public DefaultRegistry()
        {
            Scan(
                scan =>
                {
                    scan.AssembliesFromApplicationBaseDirectory(a => a.GetName().Name.StartsWith(ServiceName));
                    scan.RegisterConcreteTypesAgainstTheFirstInterface();
                    scan.ConnectImplementationsToTypesClosing(typeof(AbstractValidator<>));
                    scan.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
                    scan.ConnectImplementationsToTypesClosing(typeof(IAsyncRequestHandler<,>));
                    scan.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
                    scan.ConnectImplementationsToTypesClosing(typeof(IAsyncNotificationHandler<>));
                });

            var config = GetConfiguration();

            For<IEventsApi>().Use<EventsApi>()
                .Ctor<IEventsApiClientConfiguration>().Is(config.EventsApi)
                .SelectConstructor(() => new EventsApi(null)); // The default one isn't the one we want to use.

            For<ICommitmentRepository>().Use<CommitmentRepository>().Ctor<string>().Is(config.DatabaseConnectionString);
            For<IApprenticeshipRepository>().Use<ApprenticeshipRepository>().Ctor<string>().Is(config.DatabaseConnectionString);
            For<IApprenticeshipUpdateRepository>().Use<ApprenticeshipUpdateRepository>().Ctor<string>().Is(config.DatabaseConnectionString);

            For<SingleInstanceFactory>().Use<SingleInstanceFactory>(ctx => t => ctx.GetInstance(t));
            For<MultiInstanceFactory>().Use<MultiInstanceFactory>(ctx => t => ctx.GetAllInstances(t));
            For<IMediator>().Use<Mediator>();

            ConfigureLogging();
        }

        private void ConfigureLogging()
        {
            For<IRequestContext>().Use(x => new RequestContext(new HttpContextWrapper(HttpContext.Current)));

            For<ICommitmentsLogger>().Use(x => GetBaseLogger(x)).AlwaysUnique();
        }

        private ICommitmentsLogger GetBaseLogger(IContext x)
        {
            var parentType = x.ParentType;
            return new CommitmentsLogger(new NLogLogger(parentType, x.GetInstance<IRequestContext>()));
        }

        private static CommitmentsApiConfiguration GetConfiguration()
        {
            var environment = CloudConfigurationManager.GetSetting("EnvironmentName");

            var configurationRepository = GetConfigurationRepository();
            var configurationService = new ConfigurationService(configurationRepository, new ConfigurationOptions(ServiceName, environment, Version));

            return configurationService.Get<CommitmentsApiConfiguration>();
        }

        private static IConfigurationRepository GetConfigurationRepository()
        {
            return new AzureTableStorageConfigurationRepository(CloudConfigurationManager.GetSetting("ConfigurationStorageConnectionString"));
        }
    }
}
