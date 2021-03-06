﻿using System;
using Moq;
using NUnit.Framework;
using SFA.DAS.Commitments.AcademicYearEndProcessor.WebJob.Updater;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Interfaces;
using SFA.DAS.NLog.Logger;

namespace SFA.DAS.Commitments.AcademicYearEndProcessor.UnitTests
{
    [TestFixture]
    public class WhenTestingAcademicYearEndExpiryProcessor
    {
        private Mock<ILog> _logger;
        private Mock<IAcademicYearDateProvider> _academicYearProvider;
        private Mock<IDataLockRepository> _dataLockRepository;
        private Mock<IApprenticeshipUpdateRepository> _apprenticeshipUpdateRepository;

        [SetUp]
        public void Arrange()
        {
            // ARRANGE
            _logger = new Mock<ILog>();
            _academicYearProvider = new Mock<IAcademicYearDateProvider>();
            _dataLockRepository = new Mock<IDataLockRepository>();
            _apprenticeshipUpdateRepository = new Mock<IApprenticeshipUpdateRepository>();

        }

        [Test]
        public void AndANullLoggerIsPassedItThrowsAnArgumentNullException()
        {
            Assert.Throws<ArgumentException>(() => new AcademicYearEndExpiryProcessor(
                null,
                _academicYearProvider.Object,
                _dataLockRepository.Object,
                _apprenticeshipUpdateRepository.Object,
                new StubCurrentDateTime(DateTime.UtcNow)));   
        }

        [Test]
        public void AndANullAccademicYearProviderIsPassedItThrowsAnArgumentNullException()
        {
            Assert.Throws<ArgumentException>(() => new AcademicYearEndExpiryProcessor(
                _logger.Object, 
                null, 
                _dataLockRepository.Object,
                _apprenticeshipUpdateRepository.Object,
                new StubCurrentDateTime(DateTime.UtcNow)));
        }


        [Test]
        public void AndANullDataLockStatusRepositoryIsPassedItThrowsAnArgumentNullException()
        {
            Assert.Throws<ArgumentException>(() => new AcademicYearEndExpiryProcessor(
                _logger.Object,
                _academicYearProvider.Object, 
                null,
                _apprenticeshipUpdateRepository.Object,
                new StubCurrentDateTime(DateTime.UtcNow)));
        }


        [Test]
        public void AndANulltimeMachineIsPassedItThrowsAnArgumentNullException()
        {
            Assert.Throws<ArgumentException>(() => new AcademicYearEndExpiryProcessor(
                _logger.Object,
                _academicYearProvider.Object, 
                _dataLockRepository.Object,
                _apprenticeshipUpdateRepository.Object,
                null));
        }

    }
}