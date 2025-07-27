using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations.InterResults.Definitions;
using Shouldly;

namespace DuOps.Core.Tests.OperationDefinitions;

public sealed class OperationDiscriminatorTests
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
        Should.Throw<ArgumentException>(() => new OperationDiscriminator(value));
    }

    [TestCase("a")]
    [TestCase("2")]
    [TestCase("a2a")]
    [TestCase("a2Z")]
    [TestCase("a2_")]
    [TestCase("_123_asd__ZHN_")]
    public void Ctor_ValidValue_Ok(string value)
    {
        Should.NotThrow(() => new OperationDiscriminator(value));
    }
}
