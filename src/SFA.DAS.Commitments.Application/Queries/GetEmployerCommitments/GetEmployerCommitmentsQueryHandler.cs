﻿using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using SFA.DAS.Commitments.Domain.Data;

namespace SFA.DAS.Commitments.Application.Queries.GetEmployerCommitments
{
    public sealed class GetEmployerCommitmentsQueryHandler : IAsyncRequestHandler<GetEmployerCommitmentsRequest, GetEmployerCommitmentsResponse>
    {
        private readonly ICommitmentRepository _commitmentRepository;
        private readonly AbstractValidator<GetEmployerCommitmentsRequest> _validator;

        public GetEmployerCommitmentsQueryHandler(ICommitmentRepository commitmentRepository, AbstractValidator<GetEmployerCommitmentsRequest> validator)
        {
            _commitmentRepository = commitmentRepository;
            _validator = validator;
        }

        public async Task<GetEmployerCommitmentsResponse> Handle(GetEmployerCommitmentsRequest message)
        {
            if (_validator.Validate(message).IsValid)
            {
                var commitments = await _commitmentRepository.GetByEmployer(message.AccountId);

                return new GetEmployerCommitmentsResponse { Commitments = commitments };
            }

            return new GetEmployerCommitmentsResponse { HasError = true };
        }
    }
}
