﻿namespace SFA.DAS.ProviderApprenticeshipsService.Domain
{
    public class CommitmentView
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long EmployerAccountId { get; set; }
        public string EmployerAccountName { get; set; }
        public long LegalEntityId { get; set; }
        public string LegalEntityName { get; set; }
        public long? ProviderId { get; set; }
        public string ProviderName { get; set; }
    }
}