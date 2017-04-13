﻿using FluentValidation;
using MediatR;
using SFA.DAS.Commitments.Application.Exceptions;
using SFA.DAS.Commitments.Domain;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities;
using SFA.DAS.Commitments.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Commitments.Application.Commands.UpdateApprenticeshipStatus
{
    public sealed class UpdateApprenticeshipStatusCommandHandler : AsyncRequestHandler<UpdateApprenticeshipStatusCommand>
    {
        private readonly ICommitmentRepository _commitmentRepository;
        private readonly IApprenticeshipRepository _apprenticeshipRepository;
        private readonly UpdateApprenticeshipStatusValidator _validator;
        private readonly ICurrentDateTime _currentDate;
        private readonly ICommitmentsLogger _logger;
        private readonly IApprenticeshipEvents _eventsApi;

        public UpdateApprenticeshipStatusCommandHandler(
            ICommitmentRepository commitmentRepository, 
            IApprenticeshipRepository apprenticeshipRepository, 
            UpdateApprenticeshipStatusValidator validator,
            ICurrentDateTime currentDate,
            IApprenticeshipEvents eventsApi,
            ICommitmentsLogger logger)
        {
            _commitmentRepository = commitmentRepository;
            _apprenticeshipRepository = apprenticeshipRepository;
            _validator = validator;
            _currentDate = currentDate;
            _eventsApi = eventsApi;
            _logger = logger;
        }

        protected override async Task HandleCore(UpdateApprenticeshipStatusCommand command)
        {
            _logger.Info($"Employer: {command.AccountId} has called UpdateApprenticeshipStatusCommand", accountId: command.AccountId, apprenticeshipId: command.ApprenticeshipId);

            var validationResult = _validator.Validate(command);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var apprenticeship = await _apprenticeshipRepository.GetApprenticeship(command.ApprenticeshipId);
            var commitment = await _commitmentRepository.GetCommitmentById(apprenticeship.CommitmentId);
            CheckAuthorization(command, commitment);

            var newPaymentStatus = (PaymentStatus)command.PaymentStatus.GetValueOrDefault((Api.Types.Apprenticeship.Types.PaymentStatus)apprenticeship.PaymentStatus);

            ValidateDateOfChange(command.PaymentStatus.Value, command.DateOfChange, apprenticeship);

            await SaveChange(command, commitment, newPaymentStatus);

            await _eventsApi.PublishChangeApprenticeshipStatusEvent(commitment, apprenticeship, newPaymentStatus);
        }

        private void ValidateDateOfChange(Api.Types.Apprenticeship.Types.PaymentStatus paymentStatus, DateTime dateOfChange, Apprenticeship apprenticeship)
        {
            switch (paymentStatus)
            {
                case Api.Types.Apprenticeship.Types.PaymentStatus.Withdrawn:
                    ValidateChangeDateForStop(dateOfChange, apprenticeship);
                    break;
                case Api.Types.Apprenticeship.Types.PaymentStatus.Active:
                case Api.Types.Apprenticeship.Types.PaymentStatus.Paused:
                    ValidateChangeDateForPauseResume(dateOfChange);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paymentStatus), "Not a valid value for change of status");
            }

            return;
        }

        private void ValidateChangeDateForStop(DateTime dateOfChange, Apprenticeship apprenticeship)
        {
            if (apprenticeship.IsWaitingToStart(_currentDate))
            {
                if (dateOfChange.Date != apprenticeship.StartDate.Value.Date)
                    throw new ValidationException("Invalid Date of Change. Date should be value of start date if training has not started.");
            }
            else
            {
                if (dateOfChange.Date > _currentDate.Now.Date)
                    throw new ValidationException("Invalid Date of Change. Date cannot be in the future.");

                if (dateOfChange.Date < apprenticeship.StartDate.Value.Date)
                    throw new ValidationException("Invalid Date of Change. Date cannot be before the training start date.");
            }
        }

        private void ValidateChangeDateForPauseResume(DateTime dateOfChange)
        {
            if (dateOfChange.Date > _currentDate.Now.Date)
                throw new ValidationException("Invalid Date of Change. Date should be todays date.");
        }

        private async Task SaveChange(UpdateApprenticeshipStatusCommand command, Commitment commitment, PaymentStatus newPaymentStatus)
        {
            if (newPaymentStatus == PaymentStatus.Withdrawn)
            {
                // Currently only called by Employer
                await _apprenticeshipRepository.StopApprenticeship(commitment.Id, command.ApprenticeshipId, command.DateOfChange, CallerType.Employer, command.UserId);

                return;
            }

            throw new NotImplementedException();
        }

        private static void CheckAuthorization(UpdateApprenticeshipStatusCommand message, Commitment commitment)
        {
            if (commitment.EmployerAccountId != message.AccountId)
                throw new UnauthorizedException($"Employer {message.AccountId} unauthorized to view commitment {commitment.Id}");
        }
    }
}
