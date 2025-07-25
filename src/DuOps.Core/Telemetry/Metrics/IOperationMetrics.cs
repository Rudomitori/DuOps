using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Telemetry.Metrics;

public interface IOperationMetrics
{
    void OnOperationStarted(OperationDiscriminator discriminator);

    void OnInterResultAdded(
        OperationDiscriminator operationDiscriminator,
        InterResultDiscriminator interResultDiscriminator
    );

    void OnOperationThrewException(
        OperationDiscriminator operationDiscriminator,
        Exception exception
    );

    void OnInterResultThrewException(
        OperationDiscriminator operationDiscriminator,
        InterResultDiscriminator interResultDiscriminator,
        Exception exception
    );

    void OnOperationYielded(OperationDiscriminator discriminator, string reason);
    void OnOperationFinished(OperationDiscriminator discriminator);
}
