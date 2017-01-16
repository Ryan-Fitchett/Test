﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.Commitments.Domain.Entities;

namespace SFA.DAS.Commitments.Domain.Interfaces
{
    public interface IApprenticeshipEvents
    {
        Task PublishEvent(Apprenticeship apprenticeship, string @event);
        Task PublishEvent(Commitment commitment, Apprenticeship apprenticeship, string @event);
        Task BulkPublishEvent(Commitment commitment, IList<Apprenticeship> insertedApprenticeships, string v);
    }
}
