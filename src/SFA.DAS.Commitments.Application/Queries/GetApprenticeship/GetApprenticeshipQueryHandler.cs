﻿using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using SFA.DAS.Commitments.Application.Exceptions;
using SFA.DAS.Commitments.Domain;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities;

namespace SFA.DAS.Commitments.Application.Queries.GetApprenticeship
{
    public sealed class GetApprenticeshipQueryHandler : IAsyncRequestHandler<GetApprenticeshipRequest, GetApprenticeshipResponse>
    {
        private readonly IApprenticeshipRepository _apprenticeshipRepository;
        private readonly AbstractValidator<GetApprenticeshipRequest> _validator;

        public GetApprenticeshipQueryHandler(IApprenticeshipRepository apprenticeshipRepository, AbstractValidator<GetApprenticeshipRequest> validator)
        {
            _apprenticeshipRepository = apprenticeshipRepository;
            _validator = validator;
        }

        public async Task<GetApprenticeshipResponse> Handle(GetApprenticeshipRequest message)
        {
            var validationResult = _validator.Validate(message);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var apprenticeship = await _apprenticeshipRepository.GetApprenticeship(message.ApprenticeshipId);

            if (apprenticeship== null)
            {
                return new GetApprenticeshipResponse();
            }

            CheckAuthorization(message, apprenticeship);

            return MapResponseFrom(apprenticeship, message.Caller.CallerType);
        }

        private static void CheckAuthorization(GetApprenticeshipRequest message, Apprenticeship apprenticeship)
        {
            switch (message.Caller.CallerType)
            {
                case CallerType.Provider:
                    if (apprenticeship.ProviderId != message.Caller.Id)
                        throw new UnauthorizedException($"Provider {message.Caller.Id} unauthorized to view apprenticeship {message.ApprenticeshipId}");
                    break;
                case CallerType.Employer:
                default:
                    if (apprenticeship.EmployerAccountId != message.Caller.Id)
                        throw new UnauthorizedException($"Employer {message.Caller.Id} unauthorized to view apprenticeship {message.ApprenticeshipId}");
                    break;
            }
        }

        private static GetApprenticeshipResponse MapResponseFrom(Apprenticeship matchingApprenticeship, CallerType callerType)
        {
            var response = new GetApprenticeshipResponse();

            if (matchingApprenticeship == null)
            {
                return response;
            }

            response.Data = new Api.Types.Apprenticeship
            {
                Id = matchingApprenticeship.Id,
                CommitmentId = matchingApprenticeship.CommitmentId,
                EmployerAccountId = matchingApprenticeship.EmployerAccountId,
                ProviderId = matchingApprenticeship.ProviderId,
                Reference = matchingApprenticeship.Reference,
                FirstName = matchingApprenticeship.FirstName,
                LastName = matchingApprenticeship.LastName,
                ULN = matchingApprenticeship.ULN,
                TrainingType = (Api.Types.TrainingType)matchingApprenticeship.TrainingType,
                TrainingCode = matchingApprenticeship.TrainingCode,
                TrainingName = matchingApprenticeship.TrainingName,
                Cost = matchingApprenticeship.Cost,
                StartDate = matchingApprenticeship.StartDate,
                EndDate = matchingApprenticeship.EndDate,
                PaymentStatus = (Api.Types.PaymentStatus)matchingApprenticeship.PaymentStatus,
                AgreementStatus = (Api.Types.AgreementStatus)matchingApprenticeship.AgreementStatus,
                DateOfBirth = matchingApprenticeship.DateOfBirth,
                NINumber = matchingApprenticeship.NINumber,
                EmployerRef = matchingApprenticeship.EmployerRef,
                ProviderRef = matchingApprenticeship.ProviderRef,
                CanBeApproved = callerType == CallerType.Employer ? matchingApprenticeship.EmployerCanApproveApprenticeship : matchingApprenticeship.ProviderCanApproveApprenticeship
            };

            return response;
        }
    }
}
