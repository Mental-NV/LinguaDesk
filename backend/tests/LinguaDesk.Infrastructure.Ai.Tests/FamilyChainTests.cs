using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class FamilyChainTests
{
    [TestMethod]
    public void EvaluationSnapshotResolvesPrimaryPlusDistinctFallbackForBothFamilies()
    {
        var translation = FamilyChain.EvaluationDefault(ChainFamily.Translation);
        var rewriting = FamilyChain.EvaluationDefault(ChainFamily.Rewriting);

        Assert.AreEqual(ChainFamily.Translation, translation.Family);
        Assert.AreEqual(ChainFamily.Rewriting, rewriting.Family);

        foreach (var chain in new[] { translation, rewriting })
        {
            Assert.AreEqual(FamilyChain.EvaluationPrimaryId, chain.Primary.CandidateId);
            Assert.IsNotNull(chain.Fallback);
            Assert.AreEqual(FamilyChain.EvaluationFallbackId, chain.Fallback.CandidateId);
            Assert.AreNotEqual(chain.Primary, chain.Fallback);
            CollectionAssert.AreEqual(
                new[] { chain.Primary.CandidateId, chain.Fallback!.CandidateId },
                chain.CandidatesInOrder().Select(static profile => profile.CandidateId).ToArray());
        }
    }

    [TestMethod]
    public void PrimaryOnlyChainHoldsOneCandidate()
    {
        var chain = FamilyChain.Create(
            ChainFamily.Translation,
            CandidateRegistry.Default,
            FamilyChain.EvaluationPrimaryId);

        Assert.IsNull(chain.Fallback);
        Assert.HasCount(1, chain.CandidatesInOrder().ToArray());
    }

    [TestMethod]
    public void UnknownPrimaryIdFailsValidation()
    {
        var exception = Assert.ThrowsExactly<CandidateProfileException>(
            () => FamilyChain.Create(
                ChainFamily.Translation,
                CandidateRegistry.Default,
                "No-Such-Candidate"));

        StringAssert.Contains(exception.Message, "Unknown candidate ID");
    }

    [TestMethod]
    public void UnknownFallbackIdFailsValidation()
    {
        var exception = Assert.ThrowsExactly<CandidateProfileException>(
            () => FamilyChain.Create(
                ChainFamily.Rewriting,
                CandidateRegistry.Default,
                FamilyChain.EvaluationPrimaryId,
                "No-Such-Candidate"));

        StringAssert.Contains(exception.Message, "Unknown candidate ID");
    }

    [TestMethod]
    public void IdenticalPrimaryAndFallbackProfilesAreRejectedAsAHiddenRetry()
    {
        var exception = Assert.ThrowsExactly<CandidateProfileException>(
            () => FamilyChain.Create(
                ChainFamily.Translation,
                CandidateRegistry.Default,
                FamilyChain.EvaluationPrimaryId,
                FamilyChain.EvaluationPrimaryId));

        StringAssert.Contains(exception.Message, "hidden retry");
    }

    [TestMethod]
    public void ChainSelectionIsByCandidateIdOnlyWithNoRouteOrModeKey()
    {
        var translation = FamilyChain.EvaluationDefault(ChainFamily.Translation);

        var properties = typeof(FamilyChain).GetProperties().Select(static property => property.Name).ToArray();

        CollectionAssert.DoesNotContain(properties, "Route");
        CollectionAssert.DoesNotContain(properties, "Mode");
        CollectionAssert.DoesNotContain(properties, "SourceLanguage");
        CollectionAssert.DoesNotContain(properties, "TargetLanguage");

        var factoryParameters = typeof(FamilyChain)
            .GetMethods()
            .Where(static method => string.Equals(method.Name, nameof(FamilyChain.Create), StringComparison.Ordinal))
            .SelectMany(static method => method.GetParameters())
            .Select(static parameter => parameter.Name)
            .ToArray();

        CollectionAssert.DoesNotContain(factoryParameters, "route");
        CollectionAssert.DoesNotContain(factoryParameters, "mode");
        Assert.AreEqual(translation.Primary.CandidateId, translation.CandidatesInOrder().First().CandidateId);
    }

    [TestMethod]
    public void ValidationMakesZeroDispatchesWithoutAnyClient()
    {
        var factoryParameters = typeof(FamilyChain)
            .GetMethods()
            .Where(static method => string.Equals(method.Name, nameof(FamilyChain.Create), StringComparison.Ordinal))
            .SelectMany(static method => method.GetParameters())
            .Select(static parameter => parameter.ParameterType)
            .ToArray();

        Assert.IsFalse(factoryParameters.Any(static type =>
            type.Name.Contains("ChatClient", StringComparison.Ordinal)));
    }
}
