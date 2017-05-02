﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using NUnit.Framework;
using SFA.DAS.Commitments.Api.Types.Validation;
using SFA.DAS.Commitments.Application.Commands.CreateApprenticeshipUpdate;
using SFA.DAS.Commitments.Application.Exceptions;
using SFA.DAS.Commitments.Application.Queries.GetOverlappingApprenticeships;
using SFA.DAS.Commitments.Domain;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities;
using SFA.DAS.Commitments.Domain.Interfaces;

namespace SFA.DAS.Commitments.Application.UnitTests.Commands.CreateApprenticeshipUpdate
{
    [TestFixture]
    public class WhenCreatingApprenticeshipUpdate
    {
        private Mock<CreateApprenticeshipUpdateValidator> _validator;

        private Mock<IApprenticeshipUpdateRepository> _apprenticeshipUpdateRepository;
        private Mock<IApprenticeshipRepository> _apprenticeshipRepository;
        private Mock<IMediator> _mediator;

        private CreateApprenticeshipUpdateCommandHandler _handler;

        private Apprenticeship _existingApprenticeship;

        [SetUp]
        public void Arrange()
        {
            _validator = new Mock<CreateApprenticeshipUpdateValidator>();
            _validator.Setup(x => x.Validate(It.IsAny<CreateApprenticeshipUpdateCommand>()))
                .Returns(() => new ValidationResult());

            _apprenticeshipUpdateRepository = new Mock<IApprenticeshipUpdateRepository>();
            _apprenticeshipUpdateRepository.Setup(x => x.GetPendingApprenticeshipUpdate(It.IsAny<long>()))
                .ReturnsAsync(null);

            _apprenticeshipUpdateRepository.Setup(x => x.CreateApprenticeshipUpdate(It.IsAny<ApprenticeshipUpdate>(), It.IsAny<Apprenticeship>()))
                .Returns(() => Task.FromResult(new Unit()));

            _existingApprenticeship = new Apprenticeship
            {
                EmployerAccountId = 1,
                ProviderId = 2,
                ULN = " 123",
                StartDate = new DateTime(DateTime.Now.Year + 2, 5, 1),
                EndDate = new DateTime(DateTime.Now.Year + 2, 9, 1),
                Id = 3
            };

            _apprenticeshipRepository = new Mock<IApprenticeshipRepository>();
            _apprenticeshipRepository.Setup(x => x.GetApprenticeship(It.IsAny<long>()))
                .ReturnsAsync(_existingApprenticeship);

            _mediator = new Mock<IMediator>();
            _mediator.Setup(x => x.SendAsync(It.IsAny<GetOverlappingApprenticeshipsRequest>()))
                .ReturnsAsync(new GetOverlappingApprenticeshipsResponse
                {
                    Data = new List<OverlappingApprenticeship>()
                });

            _handler = new CreateApprenticeshipUpdateCommandHandler(_validator.Object, _apprenticeshipUpdateRepository.Object, Mock.Of<ICommitmentsLogger>(), _apprenticeshipRepository.Object, _mediator.Object);
        }

        [Test]
        public async Task ThenTheRequesIsValidated()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(1, CallerType.Employer),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate()
            };

            //Act
            await _handler.Handle(command);

            //Assert
            _validator.Verify(x => x.Validate(It.IsAny<CreateApprenticeshipUpdateCommand>()), Times.Once);
        }

        [Test]
        public void ThenIfTheProviderFailsAuthorisationThenAnExceptionIsThrown()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(666, CallerType.Provider),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate()
            };

            //Act && Assert
            Func<Task> act = async () => await _handler.Handle(command);
            act.ShouldThrow<UnauthorizedException>();
        }

        [Test]
        public void ThenIfTheEmployerFailsAuthorisationThenAnExceptionIsThrown()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(666, CallerType.Employer),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate()
            };

            //Act && Assert
            Func<Task> act = async () => await _handler.Handle(command);
            act.ShouldThrow<UnauthorizedException>();
        }

        [Test]
        public void ThenIfTheRequestIsInvalidThenAValidationFailureExceptionIsThrown()
        {
            //Arrange
            _validator.Setup(x => x.Validate(It.IsAny<CreateApprenticeshipUpdateCommand>()))
                .Returns(() =>
                        new ValidationResult(new List<ValidationFailure>
                        {
                            new ValidationFailure("Error", "Error Message")
                        }));

            var request = new CreateApprenticeshipUpdateCommand();

            //Act && Assert
            Func<Task> act = async () => await _handler.Handle(request);
            act.ShouldThrow<ValidationException>();
        }

        [Test]
        public async Task ThenTheRepositoryIsCalledToCreateRecord()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(2, CallerType.Provider),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate()
            };

            //Act
            await _handler.Handle(command);

            //Assert
            _apprenticeshipUpdateRepository.Verify(x => x.CreateApprenticeshipUpdate(It.IsAny<ApprenticeshipUpdate>(), It.IsAny<Apprenticeship>()), Times.Once);
        }

        [Test]
        public async Task ThenTheRepositoryIsCalledToCreateRecordWhenStarted()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(2, CallerType.Provider),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate()
            };

            _apprenticeshipRepository.Setup(x => x.GetApprenticeship(It.IsAny<long>()))
                .ReturnsAsync(new Apprenticeship
                {
                    EmployerAccountId = 1,
                    ProviderId = 2,
                    ULN = "123",
                    StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1),
                    EndDate = new DateTime(DateTime.Now.Year + 1, 5, 1),
                    Id = 3
                });

            //Act
            await _handler.Handle(command);

            //Assert
            _apprenticeshipUpdateRepository.Verify(x => x.CreateApprenticeshipUpdate(It.IsAny<ApprenticeshipUpdate>(), It.IsAny<Apprenticeship>()), Times.Once);
        }

        [Test]
        public void ThenIfTheApprenticeshipAlreadyHasAPendingUpdateThenAnExceptionIsThrown()
        {
            //Arrange
            _apprenticeshipUpdateRepository.Setup(x => x.GetPendingApprenticeshipUpdate(It.IsAny<long>()))
                .ReturnsAsync(new ApprenticeshipUpdate());

            var command = new CreateApprenticeshipUpdateCommand
            {
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate()
            };

            //Act && Assert
            Func<Task> act = async () => await _handler.Handle(command);
            act.ShouldThrow<ValidationException>();

            //Assert
            _apprenticeshipUpdateRepository.Verify(x => x.CreateApprenticeshipUpdate(It.IsAny<ApprenticeshipUpdate>(), It.IsAny<Apprenticeship>()), Times.Never);
        }

        [Test]
        public async Task ThenIfTheUpdateContainsNoDataForImmediateEffectThenTheApprenticeshipIsNotUpdated()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(2, CallerType.Provider),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate
                {
                    FirstName = "Test",
                    LastName = "Tester"
                }
            };

            //Act
            await _handler.Handle(command);

            //Assert
            _apprenticeshipUpdateRepository.Verify(x => x.CreateApprenticeshipUpdate(
                It.Is<ApprenticeshipUpdate>(y=> y != null),
                It.Is<Apprenticeship>(y=> y == null)),
                Times.Once);
        }

        [Test]
        public async Task ThenIfTheUpdateContainsNoDataForApprovalThenTheUpdateIsNotCreated()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(2, CallerType.Provider),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate
                {
                }
            };

            //Act
            await _handler.Handle(command);

            //Assert
            _apprenticeshipUpdateRepository.Verify(x => x.CreateApprenticeshipUpdate(
                It.Is<ApprenticeshipUpdate>(y => y == null),
                It.Is<Apprenticeship>(y => y != null)),
                Times.Never);
        }

        [Test]
        public async Task ThenTheMediatorIsCalledToCheckOverlappingApprenticeships()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(2, CallerType.Provider),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate
                {
                    ULN = "123"
                }
            };

            //Act
            await _handler.Handle(command);

            //Assert
            _mediator.Verify(x => x.SendAsync(It.IsAny<GetOverlappingApprenticeshipsRequest>()), Times.Once);
        }

        [Test]
        public void ThenIfApprenticeHasStarted_ValidationFailureExceptionIsThrown_IfUlnHasChanged()
        {
            _apprenticeshipRepository.Setup(x => x.GetApprenticeship(It.IsAny<long>()))
                .ReturnsAsync(new Apprenticeship
                {
                    EmployerAccountId = 1,
                    ProviderId = 2,
                    ULN = " 123",
                    StartDate = DateTime.Now.AddMonths(-1),
                    EndDate = new DateTime(DateTime.Now.Year + 1, 5, 1),
                    Id = 3
                });

            var request = new CreateApprenticeshipUpdateCommand
                              {
                                  ApprenticeshipUpdate =
                                      new Api.Types.Apprenticeship.ApprenticeshipUpdate
                                          {
                                              Id = 5,
                                              ApprenticeshipId = 42,
                                              ULN = "1112223301"
                                          }
                              };

            //Act && Assert
            Func<Task> act = async () => await _handler.Handle(request);
            act.ShouldThrow<ValidationException>();
        }

        [Test]
        public void ThenIfApprenticeHasStarted_ValidationFailureExceptionIsThrown_IfUpdated_StartDate()
        {
            _apprenticeshipRepository.Setup(x => x.GetApprenticeship(It.IsAny<long>()))
                .ReturnsAsync(new Apprenticeship
                {
                    EmployerAccountId = 1,
                    ProviderId = 2,
                    ULN = " 123",
                    StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1),
                    EndDate = new DateTime(DateTime.Now.Year + 1, 5, 1),
                    Id = 3
                });

            var request = new CreateApprenticeshipUpdateCommand
                              {
                                  ApprenticeshipUpdate =
                                      new Api.Types.Apprenticeship.ApprenticeshipUpdate
                                          {
                                              Id = 5,
                                              ApprenticeshipId = 42,
                                              StartDate = new DateTime(DateTime.Now.Year + 2, 5, 1)
                                          }
                              };

            //Act && Assert
            Func<Task> act = async () => await _handler.Handle(request);
            act.ShouldThrow<ValidationException>();
        }

        [Test]
        public void ThenIfApprenticeHasStarted_ValidationFailureExceptionIsThrown_IfUpdatedEndDate()
        {
            _apprenticeshipRepository.Setup(x => x.GetApprenticeship(It.IsAny<long>()))
                .ReturnsAsync(new Apprenticeship
                {
                    EmployerAccountId = 1,
                    ProviderId = 2,
                    ULN = " 123",
                    StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1),
                    EndDate = new DateTime(DateTime.Now.Year + 1, 5, 1),
                    Id = 3
                });

            var request = new CreateApprenticeshipUpdateCommand
            {
                ApprenticeshipUpdate =
                                      new Api.Types.Apprenticeship.ApprenticeshipUpdate
                                      {
                                          Id = 5,
                                          ApprenticeshipId = 42,
                                          EndDate = new DateTime(DateTime.Now.Year + 2, 5, 1)
                                      }
            };

            //Act && Assert
            Func<Task> act = async () => await _handler.Handle(request);
            act.ShouldThrow<ValidationException>();
        }

        [Test]
        public void ThenIfApprenticeHasStarted_ValidationFailureExceptionIsThrown_IfUpdated_TrainingCode()
        {
            _apprenticeshipRepository.Setup(x => x.GetApprenticeship(It.IsAny<long>()))
                .ReturnsAsync(new Apprenticeship
                {
                    EmployerAccountId = 1,
                    ProviderId = 2,
                    ULN = " 123",
                    StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1),
                    EndDate = new DateTime(DateTime.Now.Year + 1, 5, 1),
                    Id = 3
                });

            var request = new CreateApprenticeshipUpdateCommand
            {
                ApprenticeshipUpdate =
                                      new Api.Types.Apprenticeship.ApprenticeshipUpdate
                                      {
                                          Id = 5,
                                          ApprenticeshipId = 42,
                                          TrainingCode = "abc-123"
                                      }
            };

            //Act && Assert
            Func<Task> act = async () => await _handler.Handle(request);
            act.ShouldThrow<ValidationException>();
        }

        [Test]
        public async Task ThenIfTheApprenticeshipIsWaitingToStartThenTheChangeWillBeEffectiveFromTheApprenticeshipStartDate()
        {
            //Arrange
            var command = new CreateApprenticeshipUpdateCommand
            {
                Caller = new Caller(2, CallerType.Provider),
                ApprenticeshipUpdate = new Api.Types.Apprenticeship.ApprenticeshipUpdate
                {
                    ULN = "123",
                    Cost = 100
                }
            };

            //Act
            await _handler.Handle(command);

            //Assert
            _apprenticeshipUpdateRepository.Verify(
                x => x.CreateApprenticeshipUpdate(
                    It.Is<ApprenticeshipUpdate>(u => u.EffectiveFromDate == _existingApprenticeship.StartDate),
                    It.IsAny<Apprenticeship>()));
        }
    }
}
