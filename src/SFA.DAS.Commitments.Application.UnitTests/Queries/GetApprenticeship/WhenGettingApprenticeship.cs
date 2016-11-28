﻿using NUnit.Framework;
using System.Threading.Tasks;
using SFA.DAS.Commitments.Domain.Data;
using Moq;
using SFA.DAS.Commitments.Domain;
using FluentAssertions;
using System;
using FluentValidation;
using SFA.DAS.Commitments.Application.Exceptions;
using Ploeh.AutoFixture;
using SFA.DAS.Commitments.Application.Queries.GetApprenticeship;
using SFA.DAS.Commitments.Domain.Entities;

namespace SFA.DAS.Commitments.Application.UnitTests.Queries.GetApprenticeship
{
    [TestFixture]
    public sealed class WhenGettingApprenticeship
    {
        private Mock<ICommitmentRepository> _mockCommitmentRespository;
        private GetApprenticeshipQueryHandler _handler;
        private GetApprenticeshipRequest _exampleValidRequest;
        private Commitment _fakeRepositoryCommitment;

        [SetUp]
        public void SetUp()
        {
            _mockCommitmentRespository = new Mock<ICommitmentRepository>();
            _handler = new GetApprenticeshipQueryHandler(_mockCommitmentRespository.Object, new GetApprenticeshipValidator());

            Fixture dataFixture = new Fixture();
            _fakeRepositoryCommitment = dataFixture.Build<Commitment>().Create();
            
            _exampleValidRequest = new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Employer,
                    Id = _fakeRepositoryCommitment.EmployerAccountId
                },
                CommitmentId = _fakeRepositoryCommitment.Id,
                ApprenticeshipId = _fakeRepositoryCommitment.Apprenticeships[0].Id
            };
        }

        [Test]
        public async Task ThenTheCommitmentRepositoryIsCalled()
        {
            await _handler.Handle(_exampleValidRequest);

            _mockCommitmentRespository.Verify(x => x.GetCommitmentById(It.IsAny<long>()), Times.Once);
        }

        [Test]
        public async Task ThenShouldReturnAnApprenticeshipInResponse()
        {
            _mockCommitmentRespository.Setup(x => x.GetCommitmentById(It.IsAny<long>())).ReturnsAsync(_fakeRepositoryCommitment);

            var response = await _handler.Handle(_exampleValidRequest);

            response.Data.Id.Should().Be(_fakeRepositoryCommitment.Apprenticeships[0].Id);
            response.Data.FirstName.Should().Be(_fakeRepositoryCommitment.Apprenticeships[0].FirstName);
            response.Data.LastName.Should().Be(_fakeRepositoryCommitment.Apprenticeships[0].LastName);
        }

        [Test]
        public void ThenIfCommitmentIdIsZeroItThrowsAnInvalidRequestException()
        {
            Func<Task> act = async () => await _handler.Handle(new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Employer,
                    Id = 1
                },
                CommitmentId = 0
            });
            act.ShouldThrow<ValidationException>();
        }

        [Test]
        public async Task ThenReturnsAResponseWithNullIfTheCommitmentIsNotFound()
        {
            _mockCommitmentRespository.Setup(x => x.GetCommitmentById(It.IsAny<long>())).ReturnsAsync(default(Commitment));

            var response = await _handler.Handle(_exampleValidRequest);

            response.Data.Should().BeNull();
        }

        [Test]
        public void ThenIfAnAccountIdIsProvidedThatDoesntMatchTheCommitmentThrowsAnException()
        {
            _mockCommitmentRespository.Setup(x => x.GetCommitmentById(It.IsAny<long>())).ReturnsAsync(_fakeRepositoryCommitment);

            var providerId = _fakeRepositoryCommitment.ProviderId++.Value;

            Func<Task> act = async () => await _handler.Handle(new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Employer,
                    Id = providerId
                },
                CommitmentId = _fakeRepositoryCommitment.Id,
                ApprenticeshipId = _fakeRepositoryCommitment.Apprenticeships[0].Id
            });

            act.ShouldThrow<UnauthorizedException>().WithMessage($"Employer {providerId} unauthorized to view apprenticeship {_fakeRepositoryCommitment.Apprenticeships[0].Id}");
        }

        [Test]
        public void ThenIfAProviderIdIsProvidedThatDoesntMatchTheCommitmentThrowsAnException()
        {
            _mockCommitmentRespository.Setup(x => x.GetCommitmentById(It.IsAny<long>())).ReturnsAsync(_fakeRepositoryCommitment);

            var providerId = _fakeRepositoryCommitment.ProviderId++.Value;

            Func<Task> act = async () => await _handler.Handle(new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                },
                CommitmentId = _fakeRepositoryCommitment.Id,
                ApprenticeshipId = _fakeRepositoryCommitment.Apprenticeships[0].Id
            });

            act.ShouldThrow<UnauthorizedException>().WithMessage($"Provider {providerId} unauthorized to view apprenticeship {_fakeRepositoryCommitment.Apprenticeships[0].Id}");
        }

        [Test]
        public async Task ThenAnApprenticeshipIdThatDoesNotBelongToTheCommitmentReturnsNull()
        {
            _mockCommitmentRespository.Setup(x => x.GetCommitmentById(It.IsAny<long>())).ReturnsAsync(_fakeRepositoryCommitment);

            var response =  await _handler.Handle(new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Employer,
                    Id = _fakeRepositoryCommitment.EmployerAccountId
                },
                CommitmentId = _fakeRepositoryCommitment.Id,
                ApprenticeshipId = 999999
            });

            response.Data.Should().BeNull();
        }
    }
}
