﻿CREATE TABLE [dbo].[Relationship]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY,
	[ProviderId] BIGINT NOT NULL,
	[ProviderName] NVARCHAR(100) NULL,
	[EmployerAccountId] BIGINT NOT NULL,
	[LegalEntityId] NVARCHAR(50) NOT NULL,
	[LegalEntityName] NVARCHAR(100) NOT NULL,
	[LegalEntityAddress] NVARCHAR(256) NOT NULL,
	[LegalEntityOrganisationType] TINYINT NOT NULL,
	[Verified] BIT NULL,
	[CreatedOn] DATETIME NULL,
	CONSTRAINT UQ_Relationship UNIQUE (EmployerAccountId,ProviderId,LegalEntityId)
)
GO

CREATE NONCLUSTERED INDEX [IX_Relationship_Employer_Provider_LegalEntity] ON [dbo].[Relationship] ([EmployerAccountId], [ProviderId], [LegalEntityId])