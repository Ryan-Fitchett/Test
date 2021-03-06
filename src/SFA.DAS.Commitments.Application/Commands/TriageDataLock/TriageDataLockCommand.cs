using MediatR;
using SFA.DAS.Commitments.Domain.Entities.DataLock;

namespace SFA.DAS.Commitments.Application.Commands.TriageDataLock
{
    public sealed class TriageDataLockCommand : IAsyncRequest
    {
        public long ApprenticeshipId { get; set; }
        public long DataLockEventId { get; set; }
        public TriageStatus TriageStatus { get; set; }
        public string UserId { get; set; }
    }
}
