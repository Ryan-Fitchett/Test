﻿using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.Commitments.Domain.Entities;

namespace SFA.DAS.Commitments.Application.UnitTests.Commands.SharedValidation.Apprenticeships
{
    [TestFixture]
    public sealed class ValidatingTrainingCode : ApprenticeshipValidationTestBase
    {
        [Test]
        public void ShouldBeValidIfNoTrainingCodeValuesSet()
        {
            ExampleValidApprenticeship.TrainingType = TrainingType.Standard; // Default value
            ExampleValidApprenticeship.TrainingCode = null;
            ExampleValidApprenticeship.TrainingName = null;

            var result = Validator.Validate(ExampleValidApprenticeship);

            result.IsValid.Should().BeTrue();
        }

        [Test]
        public void ShouldBeInValidIfTrainingCodeNotSet()
        {
            ExampleValidApprenticeship.TrainingType = TrainingType.Standard;
            ExampleValidApprenticeship.TrainingCode = null;
            ExampleValidApprenticeship.TrainingName = "Test Training Name";

            var result = Validator.Validate(ExampleValidApprenticeship);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void ShouldBeInValidIfTrainingNameNotSet()
        {
            ExampleValidApprenticeship.TrainingType = TrainingType.Standard;
            ExampleValidApprenticeship.TrainingCode = "22";
            ExampleValidApprenticeship.TrainingName = null;

            var result = Validator.Validate(ExampleValidApprenticeship);

            result.IsValid.Should().BeFalse();
        }

        [Test]
        public void ShouldBeInValidIfTrainingTypeInvalid()
        {
            ExampleValidApprenticeship.TrainingType = (TrainingType)5;
            ExampleValidApprenticeship.TrainingCode = "22";
            ExampleValidApprenticeship.TrainingName = "Test Training Name";

            var result = Validator.Validate(ExampleValidApprenticeship);

            result.IsValid.Should().BeFalse();
        }

        [TestCase("22")]
        [TestCase("22-2211-22")]
        [TestCase("22-22")]
        [TestCase("02-2211-22")]
        public void ShouldBeInValidIfFrameworkCodeNotInCorrectFormat(string code)
        {
            ExampleValidApprenticeship.TrainingType = TrainingType.Framework;
            ExampleValidApprenticeship.TrainingCode = code;
            ExampleValidApprenticeship.TrainingName = "Test Training Name";

            var result = Validator.Validate(ExampleValidApprenticeship);

            result.IsValid.Should().BeFalse();
        }

        [TestCase("-1")]
        [TestCase("22-2211-22")]
        [TestCase("abc")]
        public void ShouldBeInValidIfStandardCodeNotInCorrectFormat(string code)
        {
            ExampleValidApprenticeship.TrainingType = TrainingType.Standard;
            ExampleValidApprenticeship.TrainingCode = code;
            ExampleValidApprenticeship.TrainingName = "Test Training Name";

            var result = Validator.Validate(ExampleValidApprenticeship);

            result.IsValid.Should().BeFalse();
        }
    }
}
