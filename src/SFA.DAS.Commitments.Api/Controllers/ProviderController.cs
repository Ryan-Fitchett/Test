﻿using System;
using System.Threading.Tasks;
using System.Web.Http;
using MediatR;
using SFA.DAS.Commitments.Application.Exceptions;
using SFA.DAS.Commitments.Application.Queries.GetCommitment;
using SFA.DAS.Commitments.Application.Queries.GetProviderApprenticeship;
using SFA.DAS.Commitments.Application.Queries.GetProviderCommitments;

namespace SFA.DAS.Commitments.Api.Controllers
{
    [RoutePrefix("api/provider")]
    public class ProviderController : ApiController
    {
        private readonly IMediator _mediator;

        public ProviderController(IMediator mediator)
        {
            if (mediator == null)
                throw new ArgumentNullException(nameof(mediator));
            _mediator = mediator;
        }

        [Route("{id}/commitments")]
        public async Task<IHttpActionResult> GetCommitments(long id)
        {
            try
            {
                var response = await _mediator.SendAsync(new GetProviderCommitmentsRequest { ProviderId = id });

                return Ok(response.Data);
            }
            catch (InvalidRequestException)
            {
                return BadRequest();
            }
            catch (UnauthorizedException)
            {
                return Unauthorized();
            }
        }

        [Route("{providerId}/commitments/{commitmentId}")]
        public async Task<IHttpActionResult> GetCommitment(long providerId, long commitmentId)
        {
            try
            {
                var response = await _mediator.SendAsync(new GetCommitmentRequest { ProviderId = providerId, CommitmentId = commitmentId });

                return Ok(response.Data);
            }
            catch (InvalidRequestException)
            {
                return BadRequest();
            }
            catch (UnauthorizedException)
            {
                return Unauthorized();
            }
        }

        [Route("{providerId}/commitments/{commitmentId}/apprenticeships/{apprenticeshipId}")]
        public async Task<IHttpActionResult> GetApprenticeship(long providerId, long commitmentId, long apprenticeshipId)
        {
            try
            {
                var response = await _mediator.SendAsync(new GetProviderApprenticeshipQueryRequest
                {
                    ProviderId = providerId,
                    CommitmentId = commitmentId,
                    ApprenticeshipId = apprenticeshipId
                });

                return Ok(response.Data);
            }
            catch (InvalidRequestException)
            {
                return BadRequest();
            }
            catch (UnauthorizedException)
            {
                return Unauthorized();
            }
        }
    }
}
