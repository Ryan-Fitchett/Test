﻿using NUnit.Framework;
using SFA.DAS.Commitments.Api.Types;
using Ploeh.AutoFixture;
using FluentAssertions;
using SFA.DAS.Commitments.Application.Commands.UpdateApprenticeship;

namespace SFA.DAS.Commitments.Application.UnitTests.Commands.UpdateApprenticeship
{
    [TestFixture]
    public class WhenValidatingCommand
    {
        private UpdateApprenticeshipValidator _validator;
        private UpdateApprenticeshipCommand _exampleCommand;

        [SetUp]
        public void Setup()
        {
            Fixture fixture = new Fixture();

            _validator = new UpdateApprenticeshipValidator();
            var populatedCommitment = fixture.Build<Apprenticeship>().Create();
            _exampleCommand = new UpdateApprenticeshipCommand { ProviderId = 1L, CommitmentId = 123L, ApprenticeshipId = 333L, Apprenticeship = populatedCommitment };
        }
        
        [Test]
        public void ThenIsInvalidIfApprenticeshipIsNull()
        {
            _exampleCommand.Apprenticeship = null;
            var result = _validator.Validate(_exampleCommand);

            result.IsValid.Should().BeFalse();
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void ThenCommitmentIdIsLessThanOneIsInvalid(long commitmentId)
        {
            _exampleCommand.CommitmentId = commitmentId;

            var result = _validator.Validate(_exampleCommand);

            result.IsValid.Should().BeFalse();
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void ThenApprenticeshipIdIsLessThanOneIsInvalid(long apprenticeshipId)
        {
            _exampleCommand.ApprenticeshipId = apprenticeshipId;

            var result = _validator.Validate(_exampleCommand);

            result.IsValid.Should().BeFalse();
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void ThenProviderIdIsLessThanOneIsInvalid(long providerId)
        {
            _exampleCommand.ProviderId = providerId;

            var result = _validator.Validate(_exampleCommand);

            result.IsValid.Should().BeFalse();
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ThenIfTheAccountIdIsZeroOrLessIsNotValid(long testAccountId)
        {
            _exampleCommand.AccountId = testAccountId;

            var result = _validator.Validate(_exampleCommand);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void ThenIfBothProviderAndAccountIdsHaveAValueIsNotValid()
        {
            _exampleCommand.AccountId = 123L;
            var result = _validator.Validate(_exampleCommand);

            result.IsValid.Should().BeFalse();
        }

        public void ThenIfMessageIdPopulatedForAccountIdScenarioThenIsValid()
        {
            _exampleCommand.ProviderId = null;
            _exampleCommand.AccountId = 123L;
            var result = _validator.Validate(_exampleCommand);

            result.IsValid.Should().BeTrue();
        }

        public void ThenIfProviderIdICommitmentIdAndApprenticeshipIdAreAllGreaterThanZeroItIsValid()
        {
            _exampleCommand.ProviderId = 321L;
            _exampleCommand.AccountId = null;
            var result = _validator.Validate(_exampleCommand);

            result.IsValid.Should().BeTrue();
        }
    }
}