﻿using System.Net;
using System.Threading.Tasks;
using System.Web.Http.Results;
using FluentAssertions;
using MediatR;
using Moq;
using NUnit.Framework;
using SFA.DAS.Commitments.Api.Controllers;
using SFA.DAS.Commitments.Api.Types;
using SFA.DAS.Commitments.Application.Commands.UpdateApprenticeship;
using SFA.DAS.Commitments.Application.Exceptions;

namespace SFA.DAS.Commitments.Api.UnitTests.Controllers.EmployerControllerTests
{
    [TestFixture]
    public class WhenUpdatingAnApprenticeship
    {
        private const long TestAccountId = 1L;
        private const long TestCommitmentId = 2L;
        private const long TestApprenticeshipId = 3L;
        private EmployerController _controller;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void Setup()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new EmployerController(_mockMediator.Object);
        }

        [Test]
        public async Task ThenANoContentCodeIsReturnedOnSuccess()
        {
            var result = await _controller.PutApprenticeship(TestAccountId, TestCommitmentId, TestApprenticeshipId, new Apprenticeship());

            result.Should().BeOfType<StatusCodeResult>();

            (result as StatusCodeResult).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Test]
        public async Task ThenTheMediatorIsCalledToCreateApprenticeship()
        {
            var newApprenticeship = new Apprenticeship();
            var result = await _controller.PutApprenticeship(TestAccountId, TestCommitmentId, TestApprenticeshipId, newApprenticeship);

            _mockMediator.Verify(x => x.SendAsync(It.Is<UpdateApprenticeshipCommand>(a => a.AccountId == TestAccountId && a.CommitmentId == TestCommitmentId && a.ApprenticeshipId == TestApprenticeshipId && a.Apprenticeship == newApprenticeship)));
        }

        [Test]
        public async Task ThenABadResponseIsReturnedWhenAnInvalidRequestExceptionThrown()
        {
            _mockMediator.Setup(x => x.SendAsync(It.IsAny<UpdateApprenticeshipCommand>())).Throws<InvalidRequestException>();

            var result = await _controller.PutApprenticeship(TestAccountId, TestCommitmentId, TestApprenticeshipId, new Apprenticeship());

            result.Should().BeOfType<BadRequestResult>();
        }
    }
}
