﻿using SFA.DAS.Commitments.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Commitments.Domain.Data
{
    public interface IProviderRepository
    {
        Task<IList<ProviderPaymentPriorityItem>> GetCustomProviderPaymentPriority(long employerAccountId);
    }
}
