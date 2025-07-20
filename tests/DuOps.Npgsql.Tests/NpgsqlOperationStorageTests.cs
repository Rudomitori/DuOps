using Dapper;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults.Definitions;
using DuOps.Core.Storages;
using DuOps.Npgsql.Tests.TestOperation;
using DuOps.Npgsql.Tests.TestOperation.InterResults.First;
using DuOps.Npgsql.Tests.TestOperation.InterResults.Second;
using DuOps.Npgsql.Tests.TestOperation.InterResults.Third;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;

namespace DuOps.Npgsql.Tests;

[Parallelizable(ParallelScope.Children)]
public sealed class NpgsqlOperationStorageTests
{
    private PostgreSqlContainer _container;
    private NpgsqlDataSource _dataSource;
    private NpgsqlOperationStorage _storage;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _container = new PostgreSqlBuilder().Build();
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        _dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

        await using var connection = await _dataSource.OpenConnectionAsync();

        await connection.ExecuteAsync(
            """
            create table if not exists duops_operations
            (
                discriminator       text                     not null,
                shard_key           text,
                id                  text                     not null,

                polling_schedule_id text,
                started_at          timestamp with time zone not null,

                args                text                     not null,

                is_finished         bool                     not null,
                result              text,

                inter_results       jsonb                    not null,
                -- { 
                --     "discriminator1": "serializedValue",
                --     "discriminator2": {
                --         "key1": "serializedValue",
                --         "key1": "serializedValue"
                --     }
                -- }

                primary key (discriminator, id)
            );
            """
        );

        _storage = new NpgsqlOperationStorage(_dataSource);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Test]
    public async Task GetOrAdd_OperationDoesntExist_ReturnsAddedOperation()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        // Act
        var returnedOperation = await _storage.GetOrAdd(
            TestOperationDefinition.Instance,
            operation
        );

        // Assert
        returnedOperation.Discriminator.ShouldBe(operation.Discriminator);
        returnedOperation.Id.ShouldBe(operation.Id);
        returnedOperation.Args.ShouldBe(operation.Args);
        returnedOperation.SerializedInterResults.ShouldBe(operation.SerializedInterResults);
    }

    [Test]
    public async Task GetOrAdd_OperationAlreadyExists_ReturnsExisted()
    {
        // Arrange
        var operation1 = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        var operation2 = TestOperationDefinition.Instance.NewOperation(
            operation1.Id,
            new TestOperationArgs(20),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation1);

        // Act
        var returnedOperation = await _storage.GetOrAdd(
            TestOperationDefinition.Instance,
            operation2
        );

        // Assert
        returnedOperation.Discriminator.ShouldBe(operation1.Discriminator);
        returnedOperation.Id.ShouldBe(operation1.Id);
        returnedOperation.Args.ShouldBe(operation1.Args);
        returnedOperation.SerializedInterResults.ShouldBe(operation1.SerializedInterResults);
    }

    [Test]
    public async Task GetOrDefault_OperationDoesntExist_ReturnsNull()
    {
        // Act
        var returnedOperation = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            OperationId.NewGuid()
        );

        // Assert
        returnedOperation.ShouldBeNull();
    }

    [Test]
    public async Task GetOrDefault_OperationExists_ReturnsOperation()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        // Act
        var returnedOperation = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            operation.Id
        );

        // Assert
        returnedOperation.ShouldNotBeNull();
        returnedOperation.Discriminator.ShouldBe(operation.Discriminator);
        returnedOperation.Id.ShouldBe(operation.Id);
        returnedOperation.Args.ShouldBe(operation.Args);
        returnedOperation.SerializedInterResults.ShouldBe(operation.SerializedInterResults);
    }

    [Test]
    public async Task AddInterResult_OperationDoesntExist_Throws()
    {
        // Arrange
        var interResult = FirstInterResultDefinition.Instance.NewInterResult(
            new FirstInterResultValue(Guid.NewGuid())
        );

        var serializedInterResult = FirstInterResultDefinition.Instance.Serialize(interResult);

        // Act
        var action = () =>
            _storage.AddInterResult(
                TestOperationDefinition.Instance.Discriminator,
                OperationId.NewGuid(),
                serializedInterResult
            );

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task AddInterResult_OperationExist_Adds()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var interResult = FirstInterResultDefinition.Instance.NewInterResult(
            new FirstInterResultValue(Guid.NewGuid())
        );

        var serializedInterResult = FirstInterResultDefinition.Instance.Serialize(interResult);

        // Act
        await _storage.AddInterResult(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            serializedInterResult
        );

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            operation.Id
        );

        operationFromStorage.ShouldNotBeNull();
        operationFromStorage.SerializedInterResults.ShouldHaveSingleItem();
        operationFromStorage
            .SerializedInterResults.Single()
            .ShouldSatisfyAllConditions(
                resultFromStorage =>
                    resultFromStorage.Discriminator.ShouldBe(
                        FirstInterResultDefinition.Instance.Discriminator
                    ),
                resultFromStorage => resultFromStorage.Key.ShouldBeNull(),
                resultFromStorage => resultFromStorage.Value.ShouldBe(serializedInterResult.Value)
            );
    }

    [Test]
    public async Task AddInterResult_NotKeyedResult_Idempotent()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var interResult = FirstInterResultDefinition.Instance.NewInterResult(
            new FirstInterResultValue(Guid.NewGuid())
        );

        var serializedInterResult = FirstInterResultDefinition.Instance.Serialize(interResult);

        // Act
        await _storage.AddInterResult(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            serializedInterResult
        );

        await _storage.AddInterResult(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            serializedInterResult
        );

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            operation.Id
        );

        operationFromStorage.ShouldNotBeNull();
        operationFromStorage.SerializedInterResults.ShouldHaveSingleItem();
        operationFromStorage
            .SerializedInterResults.Single()
            .ShouldSatisfyAllConditions(
                resultFromStorage =>
                    resultFromStorage.Discriminator.ShouldBe(
                        FirstInterResultDefinition.Instance.Discriminator
                    ),
                resultFromStorage => resultFromStorage.Key.ShouldBeNull(),
                resultFromStorage => resultFromStorage.Value.ShouldBe(serializedInterResult.Value)
            );
    }

    [Test]
    public async Task AddInterResult_KeyedResult_Idempotent()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var interResult = ThirdInterResultDefinition.Instance.NewInterResult(10, Guid.NewGuid());

        var serializedInterResult = ThirdInterResultDefinition.Instance.Serialize(interResult);

        // Act
        await _storage.AddInterResult(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            serializedInterResult
        );

        await _storage.AddInterResult(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            serializedInterResult
        );

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            operation.Id
        );

        operationFromStorage.ShouldNotBeNull();
        operationFromStorage.SerializedInterResults.ShouldHaveSingleItem();
        operationFromStorage
            .SerializedInterResults.Single()
            .ShouldSatisfyAllConditions(
                resultFromStorage =>
                    resultFromStorage.Discriminator.ShouldBe(
                        ThirdInterResultDefinition.Instance.Discriminator
                    ),
                resultFromStorage => resultFromStorage.Key.ShouldBe(serializedInterResult.Key),
                resultFromStorage => resultFromStorage.Value.ShouldBe(serializedInterResult.Value)
            );
    }

    [Test]
    public async Task AddInterResult_ResultWithOtherDiscriminatorExists_Adds()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var firstInterResult = FirstInterResultDefinition.Instance.NewInterResult(
            new FirstInterResultValue(Guid.NewGuid())
        );

        var serializedFirstInterResult = FirstInterResultDefinition.Instance.Serialize(
            firstInterResult
        );

        await _storage.AddInterResult(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            serializedFirstInterResult
        );

        var secondInterResult = SecondInterResultDefinition.Instance.NewInterResult(199.125);
        var serializedSecondInterResult = SecondInterResultDefinition.Instance.Serialize(
            secondInterResult
        );

        // Act
        await _storage.AddInterResult(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            serializedSecondInterResult
        );

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            operation.Id
        );

        operationFromStorage.ShouldNotBeNull();
        operationFromStorage.SerializedInterResults.Count.ShouldBe(2);
        operationFromStorage
            .SerializedInterResults.Single(x => x.Discriminator == firstInterResult.Discriminator)
            .ShouldSatisfyAllConditions(
                resultFromStorage =>
                    resultFromStorage.Discriminator.ShouldBe(firstInterResult.Discriminator),
                resultFromStorage => resultFromStorage.Key.ShouldBeNull(),
                resultFromStorage =>
                    resultFromStorage.Value.ShouldBe(serializedFirstInterResult.Value)
            );
        operationFromStorage
            .SerializedInterResults.Single(x => x.Discriminator == secondInterResult.Discriminator)
            .ShouldSatisfyAllConditions(
                resultFromStorage =>
                    resultFromStorage.Discriminator.ShouldBe(secondInterResult.Discriminator),
                resultFromStorage => resultFromStorage.Key.ShouldBeNull(),
                resultFromStorage =>
                    resultFromStorage.Value.ShouldBe(serializedSecondInterResult.Value)
            );
    }

    [Test]
    public async Task AddInterResult_ResultWithOtherValueExists_Throws()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var interResult1 = FirstInterResultDefinition.Instance.NewInterResult(
            new FirstInterResultValue(Guid.NewGuid())
        );

        var serializedInterResult1 = FirstInterResultDefinition.Instance.Serialize(interResult1);

        await _storage.AddInterResult(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            serializedInterResult1
        );

        var interResult2 = FirstInterResultDefinition.Instance.NewInterResult(
            new FirstInterResultValue(Guid.NewGuid())
        );

        var serializedInterResult2 = FirstInterResultDefinition.Instance.Serialize(interResult2);

        // Act
        var action = () =>
            _storage.AddInterResult(
                TestOperationDefinition.Instance.Discriminator,
                operation.Id,
                serializedInterResult2
            );

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task AddInterResult_OperationIsFinished_Throws()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        operation = operation with
        {
            State = OperationState.FromResult(new TestOperationResult("Hello World!")),
        };

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var interResult = FirstInterResultDefinition.Instance.NewInterResult(
            new FirstInterResultValue(Guid.NewGuid())
        );

        var serializedInterResult = FirstInterResultDefinition.Instance.Serialize(interResult);

        // Act
        var action = () =>
            _storage.AddInterResult(
                TestOperationDefinition.Instance.Discriminator,
                operation.Id,
                serializedInterResult
            );

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task AddResult_OperationExists_Adds()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var result = new TestOperationResult(Guid.NewGuid().ToString());

        // Act
        await _storage.AddResult(TestOperationDefinition.Instance, operation.Id, result);

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            operation.Id
        );

        operationFromStorage.ShouldNotBeNull();
        var finishedState =
            operationFromStorage.State.ShouldBeOfType<OperationState<TestOperationResult>.Finished>();
        finishedState.Result.ShouldBe(result);
    }

    [Test]
    public async Task AddResult_OperationDoesntExist_Throws()
    {
        // Arrange
        var operationId = OperationId.NewGuid();
        var result = new TestOperationResult(Guid.NewGuid().ToString());

        // Act
        var action = () =>
            _storage.AddResult(TestOperationDefinition.Instance, operationId, result);

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task AddResult_Idempotent()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var result = new TestOperationResult(Guid.NewGuid().ToString());

        // Act
        await _storage.AddResult(TestOperationDefinition.Instance, operation.Id, result);
        await _storage.AddResult(TestOperationDefinition.Instance, operation.Id, result);

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            operation.Id
        );

        operationFromStorage.ShouldNotBeNull();
        var finishedState =
            operationFromStorage.State.ShouldBeOfType<OperationState<TestOperationResult>.Finished>();
        finishedState.Result.ShouldBe(result);
    }

    [Test]
    public async Task AddResult_OperationHasOtherResult_Throws()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var result1 = new TestOperationResult(Guid.NewGuid().ToString());

        await _storage.AddResult(TestOperationDefinition.Instance, operation.Id, result1);

        var result2 = new TestOperationResult(Guid.NewGuid().ToString());

        // Act
        var action = () =>
            _storage.AddResult(TestOperationDefinition.Instance, operation.Id, result2);

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();

        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance,
            operation.Id
        );
        operationFromStorage.ShouldNotBeNull();
        var finishedState =
            operationFromStorage.State.ShouldBeOfType<OperationState<TestOperationResult>.Finished>();
        finishedState.Result.ShouldBe(result1);
    }

    [Test]
    public async Task GetOrSetPollingScheduleId_Idempotent()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var scheduleId1 = new OperationPollingScheduleId(Guid.NewGuid().ToString());
        var scheduleId2 = new OperationPollingScheduleId(Guid.NewGuid().ToString());

        // Act
        var scheduleIdFromStorage = await _storage.GetOrSetPollingScheduleId(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            scheduleId1
        );

        scheduleIdFromStorage.ShouldBe(scheduleId1);

        await _storage.GetOrSetPollingScheduleId(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id,
            scheduleId2
        );

        scheduleIdFromStorage.ShouldBe(scheduleId1);

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id
        );

        operationFromStorage.ShouldNotBeNull();
        operationFromStorage.PollingScheduleId.ShouldNotBeNull();
        operationFromStorage.PollingScheduleId.ShouldBe(scheduleId1);
    }

    [Test]
    public async Task GetOrSetPollingScheduleId_OperationIsFinished_Throws()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        operation = operation with
        {
            State = OperationState.FromResult(new TestOperationResult("Hello World!")),
        };

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var scheduleId = new OperationPollingScheduleId(Guid.NewGuid().ToString());

        // Act
        var action = () =>
            _storage.GetOrSetPollingScheduleId(
                TestOperationDefinition.Instance.Discriminator,
                operation.Id,
                scheduleId
            );

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();

        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id
        );
        operationFromStorage.ShouldNotBeNull();
        operationFromStorage.PollingScheduleId.ShouldBeNull();
    }

    [Test]
    public async Task GetOrSetPollingScheduleId_OperationIsFinishedAndHasPollingScheduleId_Throws()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        operation = operation with
        {
            PollingScheduleId = new OperationPollingScheduleId(Guid.NewGuid().ToString()),
            State = OperationState.FromResult(new TestOperationResult("Hello World!")),
        };

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        var scheduleId = new OperationPollingScheduleId(Guid.NewGuid().ToString());

        // Act
        var action = () =>
            _storage.GetOrSetPollingScheduleId(
                TestOperationDefinition.Instance.Discriminator,
                operation.Id,
                scheduleId
            );

        // Assert
        await action.ShouldThrowAsync<InvalidOperationException>();

        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id
        );
        operationFromStorage.ShouldNotBeNull();
        operationFromStorage.PollingScheduleId.ShouldNotBeNull();
        operationFromStorage.PollingScheduleId.ShouldBe(operation.PollingScheduleId);
    }

    [Test]
    public async Task Delete_Idempotent()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        // Act
        await _storage.Delete(TestOperationDefinition.Instance.Discriminator, operation.Id);

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id
        );
        operationFromStorage.ShouldBeNull();

        // Act
        await _storage.Delete(TestOperationDefinition.Instance.Discriminator, operation.Id);
    }

    [Test]
    public async Task Delete_OperationDoesntExist_Ok()
    {
        // Act
        await _storage.Delete(
            TestOperationDefinition.Instance.Discriminator,
            OperationId.NewGuid()
        );
    }

    [Test]
    public async Task Delete_DoesntAffectOtherOperations()
    {
        // Arrange
        var operation = TestOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new TestOperationArgs(10),
            DateTime.UtcNow
        );

        await _storage.GetOrAdd(TestOperationDefinition.Instance, operation);

        // Act
        await _storage.Delete(
            TestOperationDefinition.Instance.Discriminator,
            OperationId.NewGuid()
        );

        // Assert
        var operationFromStorage = await _storage.GetByIdOrDefault(
            TestOperationDefinition.Instance.Discriminator,
            operation.Id
        );
        operationFromStorage.ShouldNotBeNull();
    }
}
