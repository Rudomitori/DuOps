using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
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
        string operationArgs
    );

    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}).InterResults[{InterResultDiscriminator}] = '{SerializedResult}'"
    )]
    internal static partial void LogInterResultAdded(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        InterResultDiscriminator interResultDiscriminator,
        string serializedResult
    );

    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}).InterResults[{InterResultDiscriminator}][{InterResultKey}] = '{SerializedResult}'"
    )]
    internal static partial void LogInterResultAdded(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        InterResultDiscriminator interResultDiscriminator,
        string interResultKey,
        string serializedResult
    );

    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}) yielded because {YieldReason}: {YieldReasonMessage}"
    )]
    internal static partial void LogOperationYielded(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        string yieldReason,
        string yieldReasonMessage
    );

    [LoggerMessage(LogLevel.Error, "{OperationDiscriminator}({OperationId}) threw an exception")]
    internal static partial void LogOperationThrewException(
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
        string interResultKey
    );

    [LoggerMessage(
        LogLevel.Information,
        "{OperationDiscriminator}({OperationId}).Result = '{SerializedResult}'"
    )]
    internal static partial void LogOperationFinished(
        this ILogger<OperationTelemetry> logger,
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        string serializedResult
    );
}
