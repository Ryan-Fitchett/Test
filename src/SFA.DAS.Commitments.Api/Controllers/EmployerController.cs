﻿using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using NLog;
using SFA.DAS.Commitments.Api.Orchestrators;
using SFA.DAS.Commitments.Api.Types;

namespace SFA.DAS.Commitments.Api.Controllers
{
    [RoutePrefix("api/employer")]
    public class EmployerController : ApiController
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly EmployerOrchestrator _employerOrchestrator;

        public EmployerController(EmployerOrchestrator employerOrchestrator)
        {
            if (employerOrchestrator == null)
                throw new ArgumentNullException(nameof(employerOrchestrator));
            _employerOrchestrator = employerOrchestrator;
        }

        [Route("{id}/commitments")]
        public async Task<IHttpActionResult> GetCommitments(long id)
        {
            var response = await _employerOrchestrator.GetCommitments(id);

            return Ok(response.Data.Data);
        }

        [Route("{accountId}/commitments/{commitmentId}", Name = "GetCommitmentForEmployer")]
        public async Task<IHttpActionResult> GetCommitment(long accountId, long commitmentId)
        {
            var response = await _employerOrchestrator.GetCommitment(accountId, commitmentId);

            if (response.Data.Data == null)
            {
                return NotFound();
            }

            return Ok(response.Data.Data);
        }

        [Route("{accountId}/commitments/{commitmentId}/apprenticeships", Name = "CreateApprenticeshipForEmployer")]
        public async Task<IHttpActionResult> CreateApprenticeship(long accountId, long commitmentId, Apprenticeship apprenticeship)
        {
            var response = await _employerOrchestrator.CreateApprenticeship(accountId, commitmentId, apprenticeship);

            return CreatedAtRoute("GetApprenticeshipForEmployer", new { accountId = accountId, commitmentId = commitmentId, apprenticeshipId = response.Data }, default(Apprenticeship));
        }

        [Route("{accountId}/commitments/")]
        public async Task<IHttpActionResult> CreateCommitment(long accountId, Commitment commitment)
        {
            var response = await _employerOrchestrator.CreateCommitment(accountId, commitment);

            return CreatedAtRoute("GetCommitmentForEmployer", new { accountId = accountId, commitmentId = response.Data }, default(Commitment));
        }

        [Route("{accountId}/commitments/{commitmentId}/apprenticeships/{apprenticeshipId}", Name = "GetApprenticeshipForEmployer")]
        public async Task<IHttpActionResult> GetApprenticeship(long accountId, long commitmentId, long apprenticeshipId)
        {
            var response = await _employerOrchestrator.GetApprenticeship(accountId, commitmentId, apprenticeshipId);

            if (response.Data.Data == null)
            {
                return NotFound();
            }

            return Ok(response.Data.Data);
        }

        [Route("{accountId}/commitments/{commitmentId}/apprenticeships/{apprenticeshipId}")]
        public async Task<IHttpActionResult> PutApprenticeship(long accountId, long commitmentId, long apprenticeshipId, Apprenticeship apprenticeship)
        {
            await _employerOrchestrator.PutApprenticeship(accountId, commitmentId, apprenticeshipId, apprenticeship);

            return StatusCode(HttpStatusCode.NoContent);
        }

        [Route("{accountId}/commitments/{commitmentId}")]

        public async Task<IHttpActionResult> PatchCommitment(long accountId, long commitmentId, [FromBody]CommitmentStatus? status)
        {
            await _employerOrchestrator.PatchCommitment(accountId, commitmentId, status);

            return StatusCode(HttpStatusCode.NoContent);
        }

        [Route("{accountId}/commitments/{commitmentId}/apprenticeships/{apprenticeshipId}")]
        public async Task<IHttpActionResult> PatchApprenticeship(long accountId, long commitmentId, long apprenticeshipId, [FromBody]ApprenticeshipStatus? status)
        {
            await _employerOrchestrator.PatchApprenticeship(accountId, commitmentId, apprenticeshipId, status);

            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
