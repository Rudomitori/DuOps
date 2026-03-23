using Shouldly;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core.Tests.Operations.InnerResults;

public sealed class InnerResultTypeTests
{
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("a ")]
    [TestCase(" a")]
    [TestCase(" a ")]
    [TestCase(" \n\t ")]
    [TestCase("-")]
    [TestCase(null)]
    public void Ctor_InvalidValue_Throws(string? value)
    {
        Should.Throw<ArgumentException>(() => new InnerResultType(value!));
    }

    [TestCase("a")]
    [TestCase("2")]
    [TestCase("a2a")]
    [TestCase("a2Z")]
    [TestCase("a2_")]
    [TestCase("_123_asd__ZHN_")]
    public void Ctor_ValidValue_Ok(string value)
    {
        Should.NotThrow(() => new InnerResultType(value));
    }
}
