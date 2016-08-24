﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using SFA.DAS.Commitments.Domain;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Infrastructure.Configuration;

namespace SFA.DAS.Commitments.Infrastructure.Data
{
    public class CommitmentRepository : BaseRepository, ICommitmentRepository
    {
        public CommitmentRepository(CommitmentConfiguration configuration) 
            : base(configuration)
        {
        }

        public async Task Create(Commitment commitment)
        {
            await WithConnection(async c =>
            {
                int commitmentId;

                var parameters = new DynamicParameters();
                parameters.Add("@name", commitment.Name, DbType.String);
                parameters.Add("@legalEntityId", commitment.LegalEntityId, DbType.Int64);
                parameters.Add("@accountId", commitment.EmployerAccountId, DbType.Int64);
                parameters.Add("@providerId", commitment.ProviderId, DbType.Int64);

                using (var trans = c.BeginTransaction())
                {
                    commitmentId = await c.ExecuteAsync(
                        sql:
                            "INSERT INTO [dbo].[Commitment](Name, LegalEntityId, EmployerAccountId, ProviderId) VALUES (@name, @legalEntityId, @accountId, @providerId);",
                        param: parameters,
                        commandType: CommandType.Text,
                        transaction: trans);

                    foreach (var apprenticeship in commitment.Apprenticeships)
                    {
                        parameters = new DynamicParameters();
                        parameters.Add("@commitmentId", commitmentId, DbType.Int64);
                        parameters.Add("@cost", apprenticeship.Cost, DbType.Decimal);
                        parameters.Add("@startDate", apprenticeship.StartDate, DbType.DateTime);
                        parameters.Add("@endDate", apprenticeship.EndDate, DbType.DateTime);

                        await c.ExecuteAsync(
                            sql:
                                "INSERT INTO [dbo].[Apprenticeship](CommitmentId, Cost, StartDate, EndDate) VALUES (@commitmentId, @cost, @startDate, @endDate);",
                            param: parameters,
                            commandType: CommandType.Text,
                            transaction: trans);
                    }

                    trans.Commit();
                }
                return commitmentId;
            });
        }

        public async Task<IList<Commitment>> GetByEmployer(long accountId)
        {
            return await GetByIdentifier("EmployerAccountId", accountId);
        }

        public async Task<IList<Commitment>> GetByProvider(long providerId)
        {
            return await GetByIdentifier("ProviderId", providerId);
        }

        private Task<IList<Commitment>> GetByIdentifier(string identifierName, long identifierValue)
        {
            return WithConnection<IList<Commitment>>(async c =>
            {
                var parameters = new DynamicParameters();
                parameters.Add($"@id", identifierValue);

                var lookup = new Dictionary<long, Commitment>();
                var results = await c.QueryAsync<Commitment, Apprenticeship, Commitment>(
                    sql: $"SELECT c.*, a.* FROM [dbo].[Commitment] c LEFT JOIN [dbo].Apprenticeship a ON a.CommitmentId = c.Id WHERE c.{identifierName} = @id;",
                    param: parameters,
                    map: (x, y) =>
                    {
                        Commitment commitment;
                        if (!lookup.TryGetValue(x.Id, out commitment))
                        {
                            lookup.Add(x.Id, commitment = x);
                        }
                        if (commitment.Apprenticeships == null)
                            commitment.Apprenticeships = new List<Apprenticeship>();
                        commitment.Apprenticeships.Add(y);

                        return commitment;
                    });

                return lookup.Values.ToList();
            });
        }
    }
}