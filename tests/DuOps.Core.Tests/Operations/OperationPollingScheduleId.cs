using DuOps.Core.Operations;
using Shouldly;

namespace DuOps.Core.Tests.Operations;

public sealed class OperationPollingScheduleIdTests
{
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(" a")]
    [TestCase("a ")]
    [TestCase("- ")]
    [TestCase(" + ")]
    [TestCase(null)]
    public void Ctor_InvalidValue_Throws(string? value)
    {
        Should.Throw<ArgumentException>(() => new OperationPollingScheduleId(value));
    }

    [TestCase("A")]
    [TestCase("A-")]
    [TestCase("FE8E61C1-AEFE-4676-9518-16F655347464")]
    [TestCase("Привет, Мир!")]
    [TestCase("123123")]
    public void Ctor_ValidValue_Ok(string value)
    {
        Should.NotThrow(() => new OperationPollingScheduleId(value));
    }
}
