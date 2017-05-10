﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using SFA.DAS.Commitments.Domain.Data;
using SFA.DAS.Commitments.Domain.Entities;
using SFA.DAS.Commitments.Domain.Entities.DataLock;
using SFA.DAS.Commitments.Infrastructure.Data.Transactions;

namespace SFA.DAS.Commitments.Infrastructure.Data
{
    public class DataLockRepository : BaseRepository, IDataLockRepository
    {
        private readonly IApprenticeshipUpdateTransactions _apprenticeshipUpdateTransactions;
        private readonly IDataLockTransactions _dataLockTransactions;

        public DataLockRepository(string connectionString,
            IApprenticeshipUpdateTransactions apprenticeshipUpdateTransactions,
            IDataLockTransactions dataLockTransactions) : base(connectionString)
        {
            _apprenticeshipUpdateTransactions = apprenticeshipUpdateTransactions;
            _dataLockTransactions = dataLockTransactions;
        }

        public async Task<long> GetLastDataLockEventId()
        {
            return await WithConnection(async connection =>
            {
                var parameters = new DynamicParameters();
                var results = await connection.QueryAsync<long?>(
                    sql: $"[dbo].[GetLastDataLockEventId]",
                    param: parameters,
                    commandType: CommandType.StoredProcedure);
                var result = results.Single();
                return result ?? 0;
            });
        }

        public async Task<long> UpdateDataLockStatus(DataLockStatus dataLockStatus)
        {
            return await WithConnection(async connection =>
            {
                var parameters = new DynamicParameters();

                parameters.Add("@DataLockEventId", dataLockStatus.DataLockEventId);
                parameters.Add("@DataLockEventDatetime", dataLockStatus.DataLockEventDatetime);
                parameters.Add("@PriceEpisodeIdentifier", dataLockStatus.PriceEpisodeIdentifier);
                parameters.Add("@ApprenticeshipId", dataLockStatus.ApprenticeshipId);
                parameters.Add("@IlrTrainingCourseCode", dataLockStatus.IlrTrainingCourseCode);
                parameters.Add("@IlrTrainingType", dataLockStatus.IlrTrainingType);
                parameters.Add("@IlrActualStartDate", dataLockStatus.IlrActualStartDate);
                parameters.Add("@IlrEffectiveFromDate", dataLockStatus.IlrEffectiveFromDate);
                parameters.Add("@IlrTotalCost", dataLockStatus.IlrTotalCost);
                parameters.Add("@ErrorCode", dataLockStatus.ErrorCode);
                parameters.Add("@Status", dataLockStatus.Status);
                parameters.Add("@TriageStatus", dataLockStatus.TriageStatus);
                parameters.Add("@ApprenticeshipUpdateId", dataLockStatus.ApprenticeshipUpdateId);
                parameters.Add("@IsResolved", dataLockStatus.IsResolved);

                return await connection.ExecuteAsync(
                    sql: $"[dbo].[UpdateDataLockStatus]",
                    param: parameters,
                    commandType: CommandType.StoredProcedure);
            });
        }

        public async Task<List<DataLockStatus>> GetDataLocks(long apprenticeshipId)
        {
            return await WithConnection(async connection =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ApprenticeshipId", apprenticeshipId);
                var results = await connection.QueryAsync<DataLockStatus>(
                    sql: $"[dbo].[GetDataLockStatusesByApprenticeshipId]",
                    param: parameters,
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            });
        }

        public async Task<DataLockStatus> GetDataLock(long dataLockEventId)
        {
            return await WithConnection(async connection =>
            {
                var parameters = new DynamicParameters();
                parameters.Add("@DataLockEventId", dataLockEventId);
                var results = await connection.QueryAsync<DataLockStatus>(
                    sql: $"[dbo].[GetDataLockStatus]",
                    param: parameters,
                    commandType: CommandType.StoredProcedure);
                return results.SingleOrDefault();
            });
        }

        public async Task<long> UpdateDataLockTriageStatus(long dataLockEventId, TriageStatus triageStatus, ApprenticeshipUpdate apprenticeshipUpdate)
        {
            return await WithTransaction(async (connection, trans) =>
            {
                var apprenticeshipUpdateId = default(long?);

                if (triageStatus == TriageStatus.Change)
                {
                    apprenticeshipUpdateId = await _apprenticeshipUpdateTransactions.CreateApprenticeshipUpdate(connection, trans,
                        apprenticeshipUpdate);
                }

                await _dataLockTransactions.UpdateDataLockTriageStatus(connection, trans,
                    dataLockEventId, triageStatus, apprenticeshipUpdateId);
                
                return 0;
            });
        }
    }
}