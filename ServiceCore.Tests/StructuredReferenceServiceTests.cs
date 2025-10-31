using System;
using System.Reflection;
using ServiceCore.Invoicing.Pdf;
using Xunit;

namespace ServiceCore.Tests;

public class StructuredReferenceServiceTests
{
    [Fact]
    public void CreateOgm_ComputesCheckDigits()
    {
        var service = new StructuredReferenceService();
        var result = service.CreateOgm("1234567890");
        Assert.Equal("+++123/4567/89095+++", result);
    }

    [Fact]
    public void CreateRf_ComputesIsoReference()
    {
        var service = new StructuredReferenceService();
        var result = service.CreateRf("123456789012345");
        Assert.Equal("RF68123456789012345", result);
    }

    [Fact]
    public void EpcPayload_BuildsExpectedStructure()
    {
        var payload = InvokePayload(
            creditorName: "Group LN",
            iban: "BE12 3456 7890 1234",
            bic: "KREDBEBB",
            amount: 1234.56m,
            remittance: "+ + +123/4567/89095+++"
        );

        var expected = string.Join("\n", new[]
        {
            "BCD",
            "001",
            "1",
            "SCT",
            "KREDBEBB",
            "Group LN",
            "BE12345678901234",
            "EUR1234.56",
            string.Empty,
            "+ + +123/4567/89095+++"
        });

        Assert.Equal(expected + "\n", payload);
    }

    private static string InvokePayload(string creditorName, string iban, string bic, decimal amount, string remittance)
    {
        var method = typeof(EpcQrService).GetMethod("BuildPayload", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Payload builder not found");
        return (string)method.Invoke(null, new object[] { creditorName, iban, bic, amount, remittance })!;
    }
}