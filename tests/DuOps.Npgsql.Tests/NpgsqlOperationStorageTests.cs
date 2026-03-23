using Dapper;
using DuOps.Core;
using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Storages;
using DuOps.Core.Tests.TestOperation;
using DuOps.Core.Tests.TestOperation.InnerResults.First;
using DuOps.Core.Tests.TestOperation.InnerResults.Second;
using DuOps.Npgsql.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using NSubstitute;
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
        _container = new PostgreSqlBuilder("postgres:15").Build();
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        _dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

        await using var connection = await _dataSource.OpenConnectionAsync();

        foreach (var migration in NpgsqlOperationStorageMigrations.GetMigrations())
            await connection.ExecuteAsync(migration);

        var optionsMonitor = Substitute.For<IOptionsMonitor<NpgsqlOperationStorageOptions>>();
        optionsMonitor.CurrentValue.Returns(new NpgsqlOperationStorageOptions());

        var connectionFactory = new NpgsqlDataSourceConnectionFactory(_dataSource);

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _storage = new NpgsqlOperationStorage(
            connectionFactory,
            optionsMonitor,
            timeProvider,
            new NullLogger<NpgsqlOperationStorage>(),
            "asd"
        );
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Test]
    public async Task ScheduleOperationAsync_OperationDoesntExist_AddsNewOperation()
    {
        // Arrange
        var expectedOperationId = Guid.CreateVersion7();
        var expectedQueueId = new OperationQueueId("any");
        var expectedOperationArgs = new TestOperationArgs(10);

        // Act
        await _storage.ScheduleOperationAsync(
            TestOperationDefinition.Instance,
            expectedQueueId,
            expectedOperationId,
            expectedOperationArgs
        );

        // Assert
        var operation = await _storage.GetByIdOrDefaultAsync(
            TestOperationDefinition.Instance,
            expectedOperationId
        );

        operation.ShouldNotBeNull();
        operation.Type.ShouldBe(TestOperationDefinition.Instance.Type);
        operation.Id.ShouldBe(expectedOperationId);
        operation.Args.ShouldBe(expectedOperationArgs);
        operation.QueueId.ShouldBe(expectedQueueId);
        operation.State.ShouldBeOfType<OperationState<TestOperationResult>.Active>();
        operation.RetryCount.ShouldBe(0);
    }

    [Test]
    public async Task ScheduleOperationAsync_OperationAlreadyExists_NothingHappens()
    {
        // Arrange
        var expectedOperationId = Guid.CreateVersion7();
        var expectedQueueId = new OperationQueueId("any");
        var expectedOperationArgs = new TestOperationArgs(10);

        await _storage.ScheduleOperationAsync(
            TestOperationDefinition.Instance,
            expectedQueueId,
            expectedOperationId,
            expectedOperationArgs
        );
        // Act
        await _storage.ScheduleOperationAsync(
            TestOperationDefinition.Instance,
            expectedQueueId,
            expectedOperationId,
            new TestOperationArgs(Arg1: 200)
        );

        // Assert
        var operation = await _storage.GetByIdOrDefaultAsync(
            TestOperationDefinition.Instance,
            expectedOperationId
        );

        operation.ShouldNotBeNull();
        operation.Type.ShouldBe(TestOperationDefinition.Instance.Type);
        operation.Id.ShouldBe(expectedOperationId);
        operation.Args.ShouldBe(expectedOperationArgs);
        operation.QueueId.ShouldBe(expectedQueueId);
        operation.State.ShouldBeOfType<OperationState<TestOperationResult>.Active>();
        operation.RetryCount.ShouldBe(0);
    }

    [Test]
    public async Task GetOrDefaultAsync_OperationDoesntExist_ReturnsNull()
    {
        // Act
        var returnedOperation = await _storage.GetByIdOrDefaultAsync(
            TestOperationDefinition.Instance,
            Guid.CreateVersion7()
        );

        // Assert
        returnedOperation.ShouldBeNull();
    }

    [Test]
    public async Task AddInnerResultsAsync_OperationExist_Adds()
    {
        // Arrange
        var operationType = TestOperationDefinition.Instance.Type;
        var expectedOperationId = Guid.CreateVersion7();
        var serializedOperationId = TestOperationDefinition.Instance.SerializeId(
            expectedOperationId
        );
        var expectedQueueId = new OperationQueueId("any");
        var expectedOperationArgs = new TestOperationArgs(10);

        await _storage.ScheduleOperationAsync(
            TestOperationDefinition.Instance,
            expectedQueueId,
            expectedOperationId,
            expectedOperationArgs
        );

        var innerResult = FirstInnerResultDefinition.Instance.NewInnerResult(
            new FirstInnerResultValue(Guid.NewGuid()),
            DateTime.UtcNow
        );

        var serializedInnerResult = FirstInnerResultDefinition.Instance.Serialize(innerResult);

        // Act
        await _storage.AddInnerResultsAsync(
            operationType,
            serializedOperationId,
            [serializedInnerResult]
        );

        // Assert
        var innerResultsFromStorage = await _storage.GetAllInnerResultsAsync(
            operationType,
            serializedOperationId
        );

        innerResultsFromStorage.ShouldNotBeNull();
        innerResultsFromStorage
            .Single()
            .ShouldSatisfyAllConditions(
                fromStorage => fromStorage.Type.ShouldBe(FirstInnerResultDefinition.Instance.Type),
                fromStorage => fromStorage.Id.ShouldBeNull(),
                fromStorage => fromStorage.Value.ShouldBe(serializedInnerResult.Value),
                fromStorage =>
                    (serializedInnerResult.CreatedAt - fromStorage.CreatedAt).ShouldBeLessThan(
                        TimeSpan.FromMicroseconds(1)
                    ),
                fromStorage => fromStorage.UpdatedAt.ShouldBeNull()
            );
    }

    [Test]
    public async Task AddInnerResultsAsync_ResultWithOtherTypeExists_Adds()
    {
        // Arrange
        var operationType = TestOperationDefinition.Instance.Type;
        var expectedOperationId = Guid.CreateVersion7();
        var serializedOperationId = TestOperationDefinition.Instance.SerializeId(
            expectedOperationId
        );
        var expectedQueueId = new OperationQueueId("any");
        var expectedOperationArgs = new TestOperationArgs(10);

        await _storage.ScheduleOperationAsync(
            TestOperationDefinition.Instance,
            expectedQueueId,
            expectedOperationId,
            expectedOperationArgs
        );

        var firstInnerResult = FirstInnerResultDefinition.Instance.NewInnerResult(
            new FirstInnerResultValue(Guid.NewGuid()),
            DateTime.UtcNow
        );

        var serializedFirstInnerResult = FirstInnerResultDefinition.Instance.Serialize(
            firstInnerResult
        );

        await _storage.AddInnerResultsAsync(
            operationType,
            serializedOperationId,
            [serializedFirstInnerResult]
        );

        var secondInnerResult = SecondInnerResultDefinition.Instance.NewInnerResult(
            199.125,
            DateTime.UtcNow
        );

        var serializedSecondInnerResult = SecondInnerResultDefinition.Instance.Serialize(
            secondInnerResult
        );

        // Act
        await _storage.AddInnerResultsAsync(
            operationType,
            serializedOperationId,
            [serializedSecondInnerResult]
        );

        // Assert
        var innerResultsFromStorage = await _storage.GetAllInnerResultsAsync(
            operationType,
            serializedOperationId
        );

        innerResultsFromStorage.ShouldNotBeNull();
        innerResultsFromStorage.Length.ShouldBe(2);
        innerResultsFromStorage
            .Single(x => x.Type == firstInnerResult.Type)
            .ShouldSatisfyAllConditions(
                fromStorage => fromStorage.Type.ShouldBe(firstInnerResult.Type),
                fromStorage => fromStorage.Id.ShouldBeNull(),
                fromStorage => fromStorage.Value.ShouldBe(serializedFirstInnerResult.Value)
            );
        innerResultsFromStorage
            .Single(x => x.Type == secondInnerResult.Type)
            .ShouldSatisfyAllConditions(
                fromStorage => fromStorage.Type.ShouldBe(secondInnerResult.Type),
                fromStorage => fromStorage.Id.ShouldBeNull(),
                fromStorage => fromStorage.Value.ShouldBe(serializedSecondInnerResult.Value)
            );
    }

    [Test]
    public async Task AddInnerResultsAsync_ResultExists_Throws()
    {
        // Arrange
        var operationType = TestOperationDefinition.Instance.Type;
        var expectedOperationId = Guid.CreateVersion7();
        var serializedOperationId = TestOperationDefinition.Instance.SerializeId(
            expectedOperationId
        );
        var expectedQueueId = new OperationQueueId("any");
        var expectedOperationArgs = new TestOperationArgs(10);

        await _storage.ScheduleOperationAsync(
            TestOperationDefinition.Instance,
            expectedQueueId,
            expectedOperationId,
            expectedOperationArgs
        );

        var firstInnerResult = FirstInnerResultDefinition.Instance.NewInnerResult(
            new FirstInnerResultValue(Guid.NewGuid()),
            DateTime.UtcNow
        );

        var serializedFirstInnerResult = FirstInnerResultDefinition.Instance.Serialize(
            firstInnerResult
        );

        await _storage.AddInnerResultsAsync(
            operationType,
            serializedOperationId,
            [serializedFirstInnerResult]
        );

        // Act
        var action = () =>
            _storage.AddInnerResultsAsync(
                TestOperationDefinition.Instance.Type,
                serializedOperationId,
                [serializedFirstInnerResult]
            );

        // Assert
        await action.ShouldThrowAsync<PostgresException>();
    }
}
