﻿using System.Threading.Tasks;
using System.Web.Http;
using MediatR;
using SFA.DAS.Commitments.Application.Queries;
using SFA.DAS.Commitments.Application.Queries.GetEmployerCommitments;
using SFA.DAS.Commitments.Application.Queries.GetProviderCommitments;

namespace SFA.DAS.Commitments.Api.Controllers
{
    public class CommitmentsController : ApiController
    {
        private IMediator _mediator;

        public CommitmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/commitments/5
        public async Task<IHttpActionResult> Get(long id)
        {
            GetCommitmentsResponseBase response;

            if (id % 2 == 0)
            {
                response = await _mediator.SendAsync(new GetProviderCommitmentsRequest { ProviderId = id });
            }
            else
            {
                response = await _mediator.SendAsync(new GetEmployerCommitmentsRequest { AccountId = id });
            }

            if (response.HasError)
            {
                return BadRequest();
            }

            return Ok(response.Commitments);
        }
    }
}
