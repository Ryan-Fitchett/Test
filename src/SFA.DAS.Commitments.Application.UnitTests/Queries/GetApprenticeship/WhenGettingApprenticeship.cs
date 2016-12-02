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
        private Apprenticeship _fakeRepositoryApprenticeship;

        [SetUp]
        public void SetUp()
        {
            _mockCommitmentRespository = new Mock<ICommitmentRepository>();
            _handler = new GetApprenticeshipQueryHandler(_mockCommitmentRespository.Object, new GetApprenticeshipValidator());

            var dataFixture = new Fixture();
            _fakeRepositoryApprenticeship = dataFixture.Build<Apprenticeship>().Create();
            
            _exampleValidRequest = new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Employer,
                    Id = _fakeRepositoryApprenticeship.EmployerAccountId
                },
                ApprenticeshipId = _fakeRepositoryApprenticeship.Id
            };
        }

        [Test]
        public async Task ThenTheCommitmentRepositoryIsCalled()
        {
            await _handler.Handle(_exampleValidRequest);

            _mockCommitmentRespository.Verify(x => x.GetApprenticeship(It.IsAny<long>()), Times.Once);
        }

        [Test]
        public async Task ThenShouldReturnAnApprenticeshipInResponse()
        {
            _mockCommitmentRespository.Setup(x => x.GetApprenticeship(It.IsAny<long>())).ReturnsAsync(_fakeRepositoryApprenticeship);

            var response = await _handler.Handle(_exampleValidRequest);

            response.Data.Id.Should().Be(_fakeRepositoryApprenticeship.Id);
            response.Data.FirstName.Should().Be(_fakeRepositoryApprenticeship.FirstName);
            response.Data.LastName.Should().Be(_fakeRepositoryApprenticeship.LastName);
        }

        [Test]
        public void ThenIfApprenticeshipIdIsZeroItThrowsAnInvalidRequestException()
        {
            Func<Task> act = async () => await _handler.Handle(new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Employer,
                    Id = 1
                }
            });
            act.ShouldThrow<ValidationException>();
        }

        [Test]
        public async Task ThenReturnsAResponseWithNullIfTheCommitmentIsNotFound()
        {
            _mockCommitmentRespository.Setup(x => x.GetApprenticeship(It.IsAny<long>())).ReturnsAsync(default(Apprenticeship));

            var response = await _handler.Handle(_exampleValidRequest);

            response.Data.Should().BeNull();
        }

        [Test]
        public void ThenIfAnAccountIdIsProvidedThatDoesntMatchTheApprenticeshipThrowsAnException()
        {
            _mockCommitmentRespository.Setup(x => x.GetApprenticeship(It.IsAny<long>())).ReturnsAsync(_fakeRepositoryApprenticeship);

            var employerId = _fakeRepositoryApprenticeship.EmployerAccountId++;

            Func<Task> act = async () => await _handler.Handle(new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Employer,
                    Id = employerId
                },
                ApprenticeshipId = _fakeRepositoryApprenticeship.Id
            });

            act.ShouldThrow<UnauthorizedException>().WithMessage($"Employer {employerId} unauthorized to view apprenticeship {_fakeRepositoryApprenticeship.Id}");
        }

        [Test]
        public void ThenIfAProviderIdIsProvidedThatDoesntMatchTheApprenticeshipThrowsAnException()
        {
            _mockCommitmentRespository.Setup(x => x.GetApprenticeship(It.IsAny<long>())).ReturnsAsync(_fakeRepositoryApprenticeship);

            var providerId = _fakeRepositoryApprenticeship.ProviderId++;

            Func<Task> act = async () => await _handler.Handle(new GetApprenticeshipRequest
            {
                Caller = new Caller
                {
                    CallerType = CallerType.Provider,
                    Id = providerId
                },
                ApprenticeshipId = _fakeRepositoryApprenticeship.Id
            });

            act.ShouldThrow<UnauthorizedException>().WithMessage($"Provider {providerId} unauthorized to view apprenticeship {_fakeRepositoryApprenticeship.Id}");
        }
    }
}
