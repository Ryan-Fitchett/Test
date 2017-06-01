﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities;
using SFA.DAS.EAS.Account.Api.Client;
using SFA.DAS.EAS.Account.Api.Types;
using SFA.DAS.NLog.Logger;
using SFA.DAS.Notifications.Api.Types;

namespace SFA.DAS.Commitments.Notification.WebJob
{
    public interface IEmailTemplatesService
    {
        Task<IEnumerable<Email>> GetEmails();
    }

    public class EmailTemplatesService : IEmailTemplatesService
    {
        private readonly IApprenticeshipRepository _apprenticeshipRepository;
        private readonly IAccountApiClient _accountApi;
        private readonly ILog _logger;

        public EmailTemplatesService(
            IApprenticeshipRepository apprenticeshipRepository,
            IAccountApiClient accountApi,
            ILog logger)
        {
            if(apprenticeshipRepository == null)
                throw new ArgumentNullException($"{nameof(apprenticeshipRepository)} is null");
            if (accountApi == null)
                throw new ArgumentNullException($"{nameof(accountApi)} is null");
            if (logger == null)
                throw new ArgumentNullException($"{nameof(logger)} is null");
            _apprenticeshipRepository = apprenticeshipRepository;
            _accountApi = accountApi;
            _logger = logger;
        }

        public async Task<IEnumerable<Email>> GetEmails()
        {
            var alertSummaries = await _apprenticeshipRepository.GetEmployerApprenticeshipAlertSummary();

            _logger.Info($"Found {alertSummaries.Count} summery records.");

            var userPerAccountTask =
                 alertSummaries
                .Select(m => m.EmployerAccountId)
                .Distinct()
                .Select(async m =>
                    new UserModel
                    {
                        AccountId = m,
                        Users = (await _accountApi.GetAccountUsers(m.ToString()))
                            .Where(u => u.Role != "")
                    })
                .ToList();

            var userPerAccount = await Task.WhenAll(userPerAccountTask);
            return 
                userPerAccount.SelectMany(m =>
                    m.Users.SelectMany(userModel =>
                        MapToEmail(userModel, alertSummaries.Where(sum => sum.EmployerAccountId == m.AccountId))
                    )
                );
        }

        private IEnumerable<Email> MapToEmail(TeamMemberViewModel userModel, IEnumerable<AlertSummary> alertSummary)
        {
            return alertSummary.Select(item => new Email
            {
                RecipientsAddress = userModel.Email,
                TemplateId = "EmployerAlertSummaryNotification",
                ReplyToAddress = "digital.apprenticeship.service@notifications.service.gov.uk",
                Subject = "Items for your attention: apprenticeship service",
                SystemId = "x",
                Tokens =
                               new Dictionary<string, string>
                                   {
                                       { "name", userModel.Name },
                                       { "total_count_text", item.TotalCount == 1 
                                            ? "is 1 apprentice" 
                                            : $"are {item.TotalCount} apprentices" },
                                       { "legal_entity_name", item.LegalEntityName },
                                       { "changes_for_review", item.ChangeOfCircCount > 0 
                                            ? $"{item.ChangeOfCircCount} with changes for review" 
                                            : string.Empty },
                                       { "requested_changes", item.RestartRequestCount > 0 
                                            ? $"{item.RestartRequestCount} with requested changes" 
                                            : string.Empty },
                                   }
                });
        }
    }

    internal class UserModel
    {
        public long AccountId { get; set; }

        public IEnumerable<TeamMemberViewModel> Users { get; set; }

    }
}
