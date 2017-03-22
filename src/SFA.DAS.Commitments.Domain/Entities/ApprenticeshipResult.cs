﻿using System;

namespace SFA.DAS.Commitments.Domain.Entities
{
    public class ApprenticeshipResult
    {
        public long Id { get; set; }
        public string Uln { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long EmployerAccountId { get; set; }
        public string LegalEntityName { get; set; }
        public long ProviderId { get; set; }
        public string ProviderName { get; set; }
        public long CommitmentId { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public AgreementStatus AgreementStatus { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public TrainingType TrainingType { get; set; }
        public string TrainingCode { get; set; }
        public string TrainingName { get; set; }
        public int? Cost { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string EmployerRef { get; set; }
        public string ProviderRef { get; set; }
    }
}