﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using NLog;
using SFA.DAS.Commitments.Application.Exceptions;
using SFA.DAS.Commitments.Application.Rules;
using SFA.DAS.Commitments.Domain;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities;
using SFA.DAS.Events.Api.Client;
using SFA.DAS.Events.Api.Types;

namespace SFA.DAS.Commitments.Application.Commands.UpdateCommitmentAgreement
{
    public sealed class UpdateCommitmentAgreementCommandHandler : AsyncRequestHandler<UpdateCommitmentAgreementCommand>
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IEventsApi _eventsApi;
        private readonly IApprenticeshipUpdateRules _apprenticeshipUpdateRules;
        private readonly ICommitmentRepository _commitmentRepository;

        public UpdateCommitmentAgreementCommandHandler(ICommitmentRepository commitmentRepository, IEventsApi eventsApi, IApprenticeshipUpdateRules apprenticeshipUpdateRules)
        {
            if (commitmentRepository == null)
                throw new ArgumentNullException(nameof(commitmentRepository));
            _commitmentRepository = commitmentRepository;
            _eventsApi = eventsApi;
            _apprenticeshipUpdateRules = apprenticeshipUpdateRules;
        }

        protected override async Task HandleCore(UpdateCommitmentAgreementCommand message)
        {
            Logger.Info(BuildInfoMessage(message));

            var commitment = await _commitmentRepository.GetById(message.CommitmentId);

            CheckCommitmentStatus(commitment);
            CheckEditStatus(message, commitment);
            CheckAuthorization(message, commitment);

            var newAgreementStatus = (AgreementStatus) message.AgreementStatus;

            // update apprenticeship agreement statuses
            foreach (var apprenticeship in commitment.Apprenticeships)
            {
                var hasChanged = false;
                var newApprenticeshipAgreementStatus = _apprenticeshipUpdateRules.DetermineNewAgreementStatus(apprenticeship.AgreementStatus, message.Caller.CallerType, newAgreementStatus);
                var newApprenticeshipPaymentStatus = _apprenticeshipUpdateRules.DetermineNewPaymentStatus(apprenticeship.PaymentStatus, newApprenticeshipAgreementStatus);

                if (apprenticeship.AgreementStatus != newApprenticeshipAgreementStatus)
                {
                    await _commitmentRepository.UpdateApprenticeshipStatus(message.CommitmentId, apprenticeship.Id, newApprenticeshipAgreementStatus);
                    hasChanged = true;
                }

                if (apprenticeship.PaymentStatus != newApprenticeshipPaymentStatus)
                {
                    await _commitmentRepository.UpdateApprenticeshipStatus(message.CommitmentId, apprenticeship.Id, newApprenticeshipPaymentStatus);
                    hasChanged = true;
                }

                if (hasChanged)
                {
                    await PublishEvent(commitment, apprenticeship.Id, "APPRENTICESHIP-AGREEMENT-UPDATED");
                }
            }

            //todo: reduce db calls
            var updatedCommitment = await _commitmentRepository.GetById(message.CommitmentId);
            var areAnyApprenticeshipsPendingAgreement = updatedCommitment.Apprenticeships.Any(a => a.AgreementStatus != AgreementStatus.BothAgreed);

            // update commitment statuses
            await _commitmentRepository.UpdateCommitmentStatus(message.CommitmentId, _apprenticeshipUpdateRules.DetermineNewEditStatus(message.Caller.CallerType, areAnyApprenticeshipsPendingAgreement));
            await _commitmentRepository.UpdateCommitmentStatus(message.CommitmentId, _apprenticeshipUpdateRules.DetermineNewCommmitmentStatus(areAnyApprenticeshipsPendingAgreement));
        }

        private static void CheckCommitmentStatus(Commitment commitment)
        {
            if (commitment.CommitmentStatus != CommitmentStatus.New && commitment.CommitmentStatus != CommitmentStatus.Active)
                throw new InvalidOperationException($"Commitment {commitment.Id} cannot be updated because status is {commitment.CommitmentStatus}");
        }

        private static void CheckEditStatus(UpdateCommitmentAgreementCommand message, Commitment commitment)
        {
            switch (message.Caller.CallerType)
            {
                case CallerType.Provider:
                    if (commitment.EditStatus != EditStatus.Both && commitment.EditStatus != EditStatus.ProviderOnly)
                        throw new UnauthorizedException($"Provider unauthorized to edit commitment: {message.CommitmentId}");
                    break;
                case CallerType.Employer:
                    if (commitment.EditStatus != EditStatus.Both && commitment.EditStatus != EditStatus.EmployerOnly)
                        throw new UnauthorizedException($"Employer unauthorized to edit commitment: {message.CommitmentId}");
                    break;
            }
        }

        private static void CheckAuthorization(UpdateCommitmentAgreementCommand message, Commitment commitment)
        {
            switch (message.Caller.CallerType)
            {
                case CallerType.Provider:
                    if (commitment.ProviderId != message.Caller.Id)
                        throw new UnauthorizedException($"Provider unauthorized to view commitment: {message.CommitmentId}");
                    break;
                case CallerType.Employer:
                    if (commitment.EmployerAccountId != message.Caller.Id)
                        throw new UnauthorizedException($"Employer unauthorized to view commitment: {message.CommitmentId}");
                    break;
            }
        }

        private static string BuildInfoMessage(UpdateCommitmentAgreementCommand cmd)
        {
            return $"{cmd.Caller.CallerType}: {cmd.Caller.Id} has called UpdateCommitmentAgreement for commitment {cmd.CommitmentId} with agreement status: {cmd.AgreementStatus}";
        }

        private async Task PublishEvent(Commitment commitment, long apprenticeshipId, string @event)
        {
            var apprenticeship = await _commitmentRepository.GetApprenticeship(apprenticeshipId);

            var apprenticeshipEvent = new ApprenticeshipEvent
            {
                AgreementStatus = apprenticeship.AgreementStatus.ToString(), ApprenticeshipId = apprenticeship.Id, EmployerAccountId = commitment.EmployerAccountId.ToString(), LearnerId = apprenticeship.ULN ?? "NULL", TrainingId = apprenticeship.TrainingCode, Event = @event, PaymentStatus = apprenticeship.PaymentStatus.ToString(), ProviderId = commitment.ProviderId.ToString(), TrainingEndDate = apprenticeship.EndDate ?? DateTime.MaxValue, TrainingStartDate = apprenticeship.StartDate ?? DateTime.MaxValue, TrainingTotalCost = apprenticeship.Cost ?? decimal.MinValue, TrainingType = apprenticeship.TrainingType == TrainingType.Framework ? TrainingTypes.Framework : TrainingTypes.Standard
            };

            //todo: publish event (temporarily disabled)
            //await _eventsApi.CreateApprenticeshipEvent(apprenticeshipEvent);
        }
    }
}
