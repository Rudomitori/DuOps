using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;
using Microsoft.Extensions.Logging;

namespace DuOps.Core.Telemetry;

internal static partial class OperationTelemetryLogs
{
    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}) started with schedule {ScheduleId} and args {OperationArgs}"
    )]
    internal static partial void LogOperationStartedInBackground(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        OperationPollingScheduleId? scheduleId,
        SerializedOperationArgs operationArgs
    );

    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}).InterResults[{InterResultDiscriminator}] = '{InterResultValue}'"
    )]
    internal static partial void LogInterResultAdded(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        InterResultDiscriminator interResultDiscriminator,
        SerializedInterResultValue interResultValue
    );

    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}).InterResults[{InterResultDiscriminator}][{InterResultKey}] = '{InterResultValue}'"
    )]
    internal static partial void LogInterResultAdded(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        InterResultDiscriminator interResultDiscriminator,
        SerializedInterResultKey interResultKey,
        SerializedInterResultValue interResultValue
    );

    [LoggerMessage(LogLevel.Information, "{OperationDiscriminator}({OperationId}) yielded")]
    internal static partial void LogOperationYielded(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId
    );

    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}) is waiting until {WaitingUntil:u} because {Reason}"
    )]
    internal static partial void LogOperationWaiting(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        DateTimeOffset waitingUntil,
        string reason
    );

    [LoggerMessage(
        LogLevel.Error,
        "{OperationDiscriminator}({OperationId}) threw an exception and will be retried at {RetryingAt}"
    )]
    internal static partial void LogOperationThrewException(
        this ILogger<OperationTelemetry> logger,
        Exception exception,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        DateTimeOffset retryingAt
    );

    [LoggerMessage(
        LogLevel.Error,
        "{OperationDiscriminator}({OperationId}) threw an exception and is marked as failed"
    )]
    internal static partial void LogOperationFailed(
        this ILogger<OperationTelemetry> logger,
        Exception exception,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId
    );

    [LoggerMessage(
        LogLevel.Error,
        "{OperationDiscriminator}({OperationId}).InterResult[{InterResultDiscriminator}] threw an exception"
    )]
    internal static partial void LogInterResultThrewException(
        this ILogger<OperationTelemetry> logger,
        Exception exception,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        InterResultDiscriminator interResultDiscriminator
    );

    [LoggerMessage(
        LogLevel.Error,
        "{OperationDiscriminator}({OperationId}).InterResult[{InterResultDiscriminator}][{InterResultKey}] threw an exception"
    )]
    internal static partial void LogInterResultThrewException(
        this ILogger<OperationTelemetry> logger,
        Exception exception,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        InterResultDiscriminator interResultDiscriminator,
        SerializedInterResultKey interResultKey
    );

    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}).Result = '{SerializedResult}'"
    )]
    internal static partial void LogOperationFinished(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedOperationResult serializedResult
    );
}
