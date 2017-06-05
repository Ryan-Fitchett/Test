﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SFA.DAS.Commitments.Application.Services;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities;
using SFA.DAS.Commitments.Domain.Interfaces;
using SFA.DAS.Commitments.Notification.WebJob.Models;
using SFA.DAS.EAS.Account.Api.Client;
using SFA.DAS.EAS.Account.Api.Types;
using SFA.DAS.NLog.Logger;
using SFA.DAS.Notifications.Api.Types;
using Polly;

namespace SFA.DAS.Commitments.Notification.WebJob
{
    public class EmailTemplatesService : IEmailTemplatesService
    {
        private readonly IApprenticeshipRepository _apprenticeshipRepository;
        private readonly IAccountApiClient _accountApi;
        private readonly IHashingService _hashingService;
        private readonly ILog _logger;
        private readonly Policy _retryPolicy;

        public EmailTemplatesService(
            IApprenticeshipRepository apprenticeshipRepository,
            IAccountApiClient accountApi,
            IHashingService hashingService,
            ILog logger)
        {
            if(apprenticeshipRepository == null)
                throw new ArgumentNullException($"{nameof(apprenticeshipRepository)} is null");
            if (accountApi == null)
                throw new ArgumentNullException($"{nameof(accountApi)} is null");
            if (hashingService == null)
                throw new ArgumentNullException($"{nameof(hashingService)} is null");
            if (logger == null)
                throw new ArgumentNullException($"{nameof(logger)} is null");
            _apprenticeshipRepository = apprenticeshipRepository;
            _accountApi = accountApi;
            _hashingService = hashingService;
            _logger = logger;
            _retryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(3,
                    (exception, retryCount) =>
                    {
                        _logger.Warn($"Error connecting to Account Api: ({exception.Message}). Retrying...attempt {retryCount})");
                    }
                );
        }

        public async Task<IEnumerable<Email>> GetEmails()
        {
            var alertSummaries = await _apprenticeshipRepository.GetEmployerApprenticeshipAlertSummary();

            _logger.Info($"Found {alertSummaries.Count} summary records.");

            var userPerAccountTask =
                 alertSummaries
                .Select(m => m.EmployerAccountId)
                .Distinct()
                .Select(ToUserModel)
                .ToList();

            var userPerAccount = 
                (await Task.WhenAll(userPerAccountTask))
                .Where(u => u.Users != null);

            return 
                userPerAccount.SelectMany(m =>
                    m.Users.SelectMany(userModel =>
                        MapToEmail(userModel, alertSummaries.Where(sum => sum.EmployerAccountId == m.AccountId), m.AccountId)
                    )
                );
        }

        private async Task<UserModel> ToUserModel(long accountId)
        {
            ICollection<TeamMemberViewModel> users = null;

            try
            {
                users = await _retryPolicy.ExecuteAsync(() => _accountApi.GetAccountUsers(accountId.ToString()));

                if (users == null || !users.Any())
                    _logger.Warn($"No users found for account: {accountId}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to get users for account: {accountId} from account api");
            }

            return new UserModel
                       {
                           AccountId = accountId,
                           Users = users
                       };
        }

        private IEnumerable<Email> MapToEmail(TeamMemberViewModel userModel, IEnumerable<AlertSummary> alertSummary, long accountId)
        {
            var hashedAccountId = _hashingService.HashValue(accountId);
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
                                            ? $"* {item.ChangeOfCircCount} with changes for review" 
                                            : string.Empty },
                                       { "requested_changes", item.RestartRequestCount > 0 
                                            ? $"* {item.RestartRequestCount} with requested changes" 
                                            : string.Empty },
                                       { "link_to_mange_apprenticeships", $"https://manage-apprenticeships.service.gov.uk/accounts/{hashedAccountId}/apprentices/manage/all?RecordStatus=ChangesForReview&RecordStatus=ChangeRequested" }
                                   }
                });
        }
    }
}