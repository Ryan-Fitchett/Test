﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using SFA.DAS.Commitments.Application.Commands.ApproveDataLockTriage;
using SFA.DAS.Commitments.Application.Interfaces.ApprenticeshipEvents;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities;
using SFA.DAS.Commitments.Domain.Entities.DataLock;
using SFA.DAS.Commitments.Domain.Interfaces;

namespace SFA.DAS.Commitments.Application.UnitTests.Commands.ApproveDataLockTriage
{
    [TestFixture]
    public class WhenApprovingDataLock
    {
        private ApproveDataLockTriageCommandHandler _sut;
        private Mock<AbstractValidator<ApproveDataLockTriageCommand>> _validator;
        private Mock<IDataLockRepository> _dataLockRepository;
        private Mock<IApprenticeshipRepository> _apprenticeshipRepository;
        private Mock<ICommitmentRepository> _commitmentRepository;

        private ApproveDataLockTriageCommand _command;

        [SetUp]
        public void SetUp()
        {
            _validator = new Mock<AbstractValidator<ApproveDataLockTriageCommand>>();
            _dataLockRepository = new Mock<IDataLockRepository>();
            _apprenticeshipRepository = new Mock<IApprenticeshipRepository>();

            _apprenticeshipRepository.Setup(x => x.GetApprenticeship(It.IsAny<long>()))
                .ReturnsAsync(new Apprenticeship());

            _commitmentRepository = new Mock<ICommitmentRepository>();
            _commitmentRepository.Setup(x => x.GetCommitmentById(It.IsAny<long>()))
                .ReturnsAsync(new Commitment());

            _command = new ApproveDataLockTriageCommand
            {
                ApprenticeshipId = 4321
            };

            _validator.Setup(m => m.Validate(_command))
                .Returns(new ValidationResult());

            _sut = new ApproveDataLockTriageCommandHandler(
            _validator.Object, 
            _dataLockRepository.Object, 
            _apprenticeshipRepository.Object,
            Mock.Of<IApprenticeshipEventsPublisher>(),
            Mock.Of<IApprenticeshipEventsList>(),
            _commitmentRepository.Object,
            Mock.Of<ICurrentDateTime>());
        }

        [Test]
        public async Task ShouldCallApprenticeshipRepositoryToGetDataForPublishingEvent()
        {
            _dataLockRepository.Setup(m => m.GetDataLocks(_command.ApprenticeshipId))
                .ReturnsAsync(new List<DataLockStatus>
                                  {
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, IsResolved = false, Status = Status.Fail, IlrTotalCost = 400, ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Now, DataLockEventId = 3, TriageStatus = TriageStatus.Change}
                                  });

            await _sut.Handle(_command);

            _apprenticeshipRepository.Verify(x => x.GetApprenticeship(It.IsAny<long>()), Times.Once);
        }

        [Test]
        public async Task ShouldCallCommitmentRepositoryToGetDataForPublishingEvent()
        {
            _dataLockRepository.Setup(m => m.GetDataLocks(_command.ApprenticeshipId))
                .ReturnsAsync(new List<DataLockStatus>
                      {
                            new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, IsResolved = false, Status = Status.Fail, IlrTotalCost = 400, ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Now, DataLockEventId = 3, TriageStatus = TriageStatus.Change}
                      });

            await _sut.Handle(_command);

            _commitmentRepository.Verify(x => x.GetCommitmentById(It.IsAny<long>()), Times.Once);
        }

        [Test]
        public void ShouldValidateInput()
        {
            _validator.Setup(m => m.Validate(_command))
                .Returns(new ValidationResult { Errors = { new ValidationFailure("Error", "Oh no!")}});

            Func<Task> act = () => _sut.Handle(_command);
            act.ShouldThrow<ValidationException>();
        }

        [Test]
        public async Task ShouldNotUpdatePriceIfNoNewFromDataLock()
        {
            Debug.Assert(_dataLockRepository != null, "_dataLockRepository != null");
            _dataLockRepository.Setup(m => m.GetDataLocks(_command.ApprenticeshipId))
                .ReturnsAsync(new List<DataLockStatus>());

            await _sut.Handle(_command);

            _apprenticeshipRepository.Verify(
                m => m.InsertPriceHistory(
                    _command.ApprenticeshipId,
                    It.Is<IEnumerable<PriceHistory>>(ph => AssertPriceHistory(ph, 0))),
                    Times.Never);
        }

        [Test]
        public async Task ShouldNotUpdateWhenDataLockIsUnhandled()
        {
            Debug.Assert(_dataLockRepository != null, "_dataLockRepository != null");
            _dataLockRepository.Setup(m => m.GetDataLocks(_command.ApprenticeshipId))
                .ReturnsAsync(new List<DataLockStatus>
                                  {
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, IsResolved = true, IlrTotalCost = 505, ErrorCode = DataLockErrorCode.Dlock07, DataLockEventId = 1, TriageStatus = TriageStatus.Change},
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, Status = Status.Pass, IlrTotalCost = 499, ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Now, DataLockEventId = 2},
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, IsResolved = false, Status = Status.Fail, IlrTotalCost = 400, ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Now.AddMonths(1), DataLockEventId = 3, TriageStatus = TriageStatus.Change}
                                  });

            await _sut.Handle(_command);

            _apprenticeshipRepository.Verify(
                m => m.InsertPriceHistory(
                    _command.ApprenticeshipId,
                    It.Is<IEnumerable<PriceHistory>>(ph => AssertPriceHistory(ph, 2))),
                    Times.Once);
            _dataLockRepository.Verify(m => m.ResolveDataLock(
                It.Is<IEnumerable<long>>(d => d.Contains(3L) && d.Count() == 1)), Times.Once);
        }


        [Test]
        public async Task ShouldOnlyUpdateDataLockWithPrice()
        {
            Debug.Assert(_dataLockRepository != null, "_dataLockRepository != null");
            _dataLockRepository.Setup(m => m.GetDataLocks(_command.ApprenticeshipId))
                .ReturnsAsync(new List<DataLockStatus>
                                  {
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, Status = Status.Fail, IlrTotalCost = 505, ErrorCode = (DataLockErrorCode)76, DataLockEventId = 1},
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, Status = Status.Fail, IlrTotalCost = 506, ErrorCode = DataLockErrorCode.Dlock06, DataLockEventId = 2},
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, Status = Status.Fail, IlrTotalCost = 400, ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Now, DataLockEventId = 3, TriageStatus = TriageStatus.Change}
                                  });

            await _sut.Handle(_command);

            _apprenticeshipRepository.Verify(
                m => m.InsertPriceHistory(
                    _command.ApprenticeshipId,
                    It.Is<IEnumerable<PriceHistory>>(ph => AssertPriceHistory(ph, 1))),
                    Times.Once);
            _dataLockRepository.Verify(m => m.ResolveDataLock(
                It.Is<IEnumerable<long>>(d => d.Contains(3L) && d.Count() == 1)), Times.Once);
        }

        [Test]
        public async Task ShouldSetEndDateForNewPriceHistory()
        {
            _dataLockRepository.Setup(m => m.GetDataLocks(_command.ApprenticeshipId))
                    .ReturnsAsync(new List<DataLockStatus>
                      {
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, Status = Status.Fail, IlrTotalCost = 1500,
                                          ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Parse("2017-06-01"),  DataLockEventId = 1, TriageStatus = TriageStatus.Change},
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, Status = Status.Fail, IlrTotalCost = 1600,
                                          ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Parse("2017-07-01"), DataLockEventId = 2, TriageStatus = TriageStatus.Change},
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, Status = Status.Fail, IlrTotalCost = 1700,
                                          ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Parse("2017-12-01"), DataLockEventId = 3, TriageStatus = TriageStatus.Change}
                      });

            _apprenticeshipRepository.Setup(m => m.GetApprenticeship(_command.ApprenticeshipId))
                .ReturnsAsync(new Apprenticeship { CommitmentId = 123456L });

            IEnumerable<PriceHistory> prices = null;
            _apprenticeshipRepository.Setup(
                m => m.InsertPriceHistory(_command.ApprenticeshipId, It.IsAny<IEnumerable<PriceHistory>>()))
                .Callback<long, IEnumerable<PriceHistory>>((i, l) => prices = l)
                .Returns(Task.FromResult(1L));

            await _sut.Handle(_command);

            var p1 = prices.Single(m => m.Cost == 1500);
            var p2 = prices.Single(m => m.Cost == 1600);
            var p3 = prices.Single(m => m.Cost == 1700);

            p1.ToDate.Should().Be(p2.FromDate.AddDays(-1));
            p2.ToDate.Should().Be(p3.FromDate.AddDays(-1));
            p3.ToDate.Should().Be(null);
        }

        [Test]
        public async Task ShouldSetEndDateForNewPriceHistoryOneRecord()
        {
            _dataLockRepository.Setup(m => m.GetDataLocks(_command.ApprenticeshipId))
                    .ReturnsAsync(new List<DataLockStatus>
                      {
                                      new DataLockStatus { ApprenticeshipId = _command.ApprenticeshipId, Status = Status.Fail, IlrTotalCost = 1500,
                                          ErrorCode = DataLockErrorCode.Dlock07, IlrEffectiveFromDate = DateTime.Parse("2017-06-01"),  DataLockEventId = 1, TriageStatus = TriageStatus.Change}
                      });

            _apprenticeshipRepository.Setup(m => m.GetApprenticeship(_command.ApprenticeshipId))
                .ReturnsAsync(new Apprenticeship { CommitmentId = 123456L });

            IEnumerable<PriceHistory> prices = null;
            _apprenticeshipRepository.Setup(
                m => m.InsertPriceHistory(_command.ApprenticeshipId, It.IsAny<IEnumerable<PriceHistory>>()))
                .Callback<long, IEnumerable<PriceHistory>>((i, l) => prices = l)
                .Returns(Task.FromResult(1L));

            await _sut.Handle(_command);

            var p1 = prices.Single(m => m.Cost == 1500);

            p1.ToDate.Should().Be(null);
        }

        private bool AssertPriceHistory(IEnumerable<PriceHistory> ph, int expectedTotal)
        {
            return
                    ph.Count() == expectedTotal
                && !ph.Any(m => m.Cost > 500);
        }
    }
}