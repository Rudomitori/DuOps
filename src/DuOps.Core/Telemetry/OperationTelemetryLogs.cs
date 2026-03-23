using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using Microsoft.Extensions.Logging;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core.Telemetry;

internal static partial class OperationTelemetryLogs
{
    [LoggerMessage(
        LogLevel.Information,
        "{operationType}({serializedOperationId}).InnerResults[{innerResultType}] = '{innerResultValue}'"
    )]
    internal static partial void LogInnerResultAdded(
        this ILogger<OperationTelemetry> logger,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        InnerResultType innerResultType,
        SerializedInnerResultValue innerResultValue
    );

    [LoggerMessage(
        LogLevel.Information,
        "{operationType}({serializedOperationId}).InnerResults[{innerResultType}][{innerResultId}] = '{innerResultValue}'"
    )]
    internal static partial void LogInnerResultAdded(
        this ILogger<OperationTelemetry> logger,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        InnerResultType innerResultType,
        SerializedInnerResultId innerResultId,
        SerializedInnerResultValue innerResultValue
    );

    [LoggerMessage(LogLevel.Information, "{operationType}({serializedOperationId}) yielded")]
    internal static partial void LogOperationYielded(
        this ILogger<OperationTelemetry> logger,
        OperationType operationType,
        SerializedOperationId serializedOperationId
    );

    [LoggerMessage(
        LogLevel.Information,
        "{operationType}({serializedOperationId}) is waiting until {WaitingUntil:u} because {Reason}"
    )]
    internal static partial void LogOperationWaiting(
        this ILogger<OperationTelemetry> logger,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTimeOffset waitingUntil,
        string reason
    );

    [LoggerMessage(
        LogLevel.Error,
        "{operationType}({serializedOperationId}) threw an exception and will be retried at {RetryingAt}"
    )]
    internal static partial void LogOperationThrewException(
        this ILogger<OperationTelemetry> logger,
        Exception exception,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTimeOffset retryingAt
    );

    [LoggerMessage(
        LogLevel.Error,
        "{operationType}({serializedOperationId}) threw an exception and is marked as failed"
    )]
    internal static partial void LogOperationFailed(
        this ILogger<OperationTelemetry> logger,
        Exception exception,
        OperationType operationType,
        SerializedOperationId serializedOperationId
    );

    [LoggerMessage(
        LogLevel.Error,
        "{operationType}({serializedOperationId}).InnerResult[{innerResulttype}] threw an exception"
    )]
    internal static partial void LogInnerResultThrewException(
        this ILogger<OperationTelemetry> logger,
        Exception exception,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        InnerResultType innerResulttype
    );

    [LoggerMessage(
        LogLevel.Error,
        "{operationType}({serializedOperationId}).InnerResult[{innerResulttype}][{innerResultId}] threw an exception"
    )]
    internal static partial void LogInnerResultThrewException(
        this ILogger<OperationTelemetry> logger,
        Exception exception,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        InnerResultType innerResulttype,
        SerializedInnerResultId innerResultId
    );

    [LoggerMessage(
        LogLevel.Information,
        "{operationType}({serializedOperationId}).Result = '{SerializedResult}'"
    )]
    internal static partial void LogOperationFinished(
        this ILogger<OperationTelemetry> logger,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        SerializedOperationResult serializedResult
    );
}
