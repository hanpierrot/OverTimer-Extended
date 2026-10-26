using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The menu self-tests (Tools/OverTimer/.../Run Self-Tests) as Test Runner tests, so one run covers them and
/// `unity test` can too. They still work from the menu; this only reports their failed checks as a test failure.
/// </summary>
public class SelfTestRunnerTests
{
    [Test]
    public void BilliardsSelfTests_AllChecksPass()
    {
        var failedLines = new List<string>();
        int failed = BilliardsSelfTests.RunAll(line => { if (line.StartsWith("FAIL")) failedLines.Add(line); });

        Assert.AreEqual(0, failed, "Billiards self-tests failed:\n" + string.Join("\n", failedLines));
    }

    [Test]
    public void PhoneShopSelfTests_AllChecksPass()
    {
        var failed = PhoneShopSelfTests.RunAll();

        Assert.IsEmpty(failed, "PhoneShop self-tests failed:\n" + string.Join("\n", failed));
    }
}
