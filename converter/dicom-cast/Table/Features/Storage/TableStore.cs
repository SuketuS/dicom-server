﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Microsoft.Health.DicomCast.TableStorage.Features.Storage.Entities;

namespace Microsoft.Health.DicomCast.TableStorage.Features.Storage
{
    /// <inheritdoc/>
    public class TableStore : ITableStore
    {
        private readonly CloudTableClient _client;
        private readonly ILogger<TableStore> _logger;

        public TableStore(
            CloudTableClient client,
            ILogger<TableStore> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _client = client;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task StoreExceptionToTable(string studyUid, string seriesUid, string instanceUid, long changeFeedSequence, Exception exceptionToStore, TableErrorType errorType, CancellationToken cancellationToken)
        {
            CloudTable table;
            TableEntity entity;

            if (errorType == TableErrorType.FhirError)
            {
                table = _client.GetTableReference(Constants.FhirTableName);
                entity = new FhirIntransientEntity(studyUid, seriesUid, instanceUid, changeFeedSequence, exceptionToStore);
            }
            else if (errorType == TableErrorType.DicomError)
            {
                table = _client.GetTableReference(Constants.DicomValidationTableName);
                entity = new FhirIntransientEntity(studyUid, seriesUid, instanceUid, changeFeedSequence, exceptionToStore);
            }
            else
            {
                return;
            }

            TableOperation operation = TableOperation.InsertOrMerge(entity);

            try
            {
                TableResult result = await table.ExecuteAsync(operation, cancellationToken);
                _logger.LogInformation(exceptionToStore, "Error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Stored to table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(exceptionToStore, "Error when processsing changefeed entry: {ChangeFeedSequence} for DICOM instance with StudyUID: {StudyUID}, SeriesUID: {SeriesUID}, InstanceUID: {InstanceUID}. Failed to store to table storage.", changeFeedSequence, studyUid, seriesUid, instanceUid);
                throw new DataStoreException(ex);
            }
        }
    }
}
