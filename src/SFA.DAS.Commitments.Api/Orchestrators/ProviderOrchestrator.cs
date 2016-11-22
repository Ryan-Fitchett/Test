﻿using System;
using System.Threading.Tasks;
using MediatR;
using NLog;
using SFA.DAS.Commitments.Api.Types;
using SFA.DAS.Commitments.Application.Commands.CreateApprenticeship;
using SFA.DAS.Commitments.Application.Commands.UpdateApprenticeship;
using SFA.DAS.Commitments.Application.Commands.UpdateCommitmentAgreement;
using SFA.DAS.Commitments.Application.Commands.UpdateCommitmentStatus;
using SFA.DAS.Commitments.Application.Queries.GetApprenticeship;
using SFA.DAS.Commitments.Application.Queries.GetCommitment;
using SFA.DAS.Commitments.Application.Queries.GetCommitments;
using SFA.DAS.Commitments.Domain;

namespace SFA.DAS.Commitments.Api.Orchestrators
{
    public class ProviderOrchestrator
    {
        private readonly IMediator _mediator;
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public ProviderOrchestrator(IMediator mediator)
        {
            if (mediator == null)
                throw new ArgumentNullException(nameof(mediator));
            _mediator = mediator;
        }

        public async Task<GetCommitmentsResponse> GetCommitments(long providerId)
        {
            Logger.Info($"Getting commitments for provider {providerId}");

            return await _mediator.SendAsync(new GetCommitmentsRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                }
            });
        }

        public async Task<GetCommitmentResponse> GetCommitment(long providerId, long commitmentId)
        {
            Logger.Info($"Getting commitment {commitmentId} for provider {providerId}");

            return await _mediator.SendAsync(new GetCommitmentRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                },
                CommitmentId = commitmentId
            });
        }

        public async Task<GetApprenticeshipResponse> GetApprenticeship(long providerId, long commitmentId, long apprenticeshipId)
        {
            Logger.Info($"Getting apprenticeship {apprenticeshipId} in commitment {commitmentId} for provider {providerId}");

            return await _mediator.SendAsync(new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                },
                CommitmentId = commitmentId,
                ApprenticeshipId = apprenticeshipId
            });
        }

        public async Task<long> CreateApprenticeship(long providerId, long commitmentId, Apprenticeship apprenticeship)
        {
            Logger.Info($"Creating apprenticeship for commitment {commitmentId} for provider {providerId}");

            return await _mediator.SendAsync(new CreateApprenticeshipCommand
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                },
                CommitmentId = commitmentId,
                Apprenticeship = apprenticeship
            });
        }

        public async Task PutApprenticeship(long providerId, long commitmentId, long apprenticeshipId, Apprenticeship apprenticeship)
        {
            Logger.Info($"Updating apprenticeship {apprenticeshipId} in commitment {commitmentId} for provider {providerId}");

            await _mediator.SendAsync(new UpdateApprenticeshipCommand
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                },
                CommitmentId = commitmentId,
                ApprenticeshipId = apprenticeshipId,
                Apprenticeship = apprenticeship
            });
        }

        public async Task PutCommitment(long providerId, long commitmentId, CommitmentStatus commitmentStatus)
        {
            Logger.Info($"Updating commitment {commitmentId} for provider {providerId}");

            await _mediator.SendAsync(new UpdateCommitmentStatusCommand
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                },
                CommitmentId = commitmentId,
                CommitmentStatus = commitmentStatus
            });
        }

        public async Task PatchCommitment(long providerId, long commitmentId, AgreementStatus agreementStatus)
        {
            Logger.Info($"Updating agreement status to {agreementStatus} for commitment {commitmentId} for provider {providerId}");

            await _mediator.SendAsync(new UpdateCommitmentAgreementCommand
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                },
                CommitmentId = commitmentId,
                AgreementStatus = agreementStatus
            });
        }
    }
}
