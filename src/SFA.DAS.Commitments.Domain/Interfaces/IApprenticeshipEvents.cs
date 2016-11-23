﻿using System;
using System.Threading.Tasks;
using SFA.DAS.Commitments.Domain.Entities;

namespace SFA.DAS.Commitments.Domain.Interfaces
{
    public interface IApprenticeshipEvents
    {
        Task PublishEvent(Commitment commitment, Apprenticeship apprenticeship, string @event);
    }
}