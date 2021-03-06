using System;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities.DataLock;

namespace SFA.DAS.Commitments.Application.Queries.GetDataLock
{
    public sealed class GetDataLockQueryHandler : IAsyncRequestHandler<GetDataLockRequest, GetDataLockResponse>
    {
        private readonly AbstractValidator<GetDataLockRequest> _validator;
        private readonly IDataLockRepository _dataLockRepository;

        public GetDataLockQueryHandler(AbstractValidator<GetDataLockRequest> validator, IDataLockRepository dataLockRepository)
        {
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));
            if (dataLockRepository == null)
                throw new ArgumentNullException(nameof(IDataLockRepository));

            _validator = validator;
            _dataLockRepository = dataLockRepository;
        }

        public async Task<GetDataLockResponse> Handle(GetDataLockRequest message)
        {
            var validationResult = _validator.Validate(message);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var data = await _dataLockRepository.GetDataLock(message.DataLockEventId);

            AssertDataLockBelongsToApprenticeship(message.ApprenticeshipId, data);

            return new GetDataLockResponse
            {
                Data = data
            };
        }

        private void AssertDataLockBelongsToApprenticeship(long apprenticeshipId, DataLockStatus dataLockStatus)
        {
            if (apprenticeshipId != dataLockStatus.ApprenticeshipId)
            {
                throw new ValidationException($"Data lock {dataLockStatus.DataLockEventId} does not belong to Apprenticeship {apprenticeshipId}");
            }
        }
    }
}
