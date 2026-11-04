// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Globalization;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #235: <see cref="Int128"/>, <see cref="UInt128"/>, <see cref="Half"/>,
/// <see cref="BigInteger"/>, <c>nint</c> and <c>nuint</c> values are written as one invariant scalar,
/// rather than as <c>{}</c> or a mapping of their properties.
/// </summary>
[TestClass]
public class NumericScalarTypeTests
{
	private CultureInfo? originalCulture;

	public static IEnumerable<object[]> Values =>
	[
		[Int128.MaxValue, "170141183460469231731687303715884105727"],
		[Int128.MinValue, "-170141183460469231731687303715884105728"],
		[UInt128.MaxValue, "340282366920938463463374607431768211455"],
		[(Half)1.5, "1.5"],
		[(Half)(-0.1), "-0.1"],
		[Half.Epsilon, "6.0E-08"],
		[Half.MaxValue, "65500"],
		[Half.PositiveInfinity, ".inf"],
		[Half.NegativeInfinity, "-.inf"],
		[Half.NaN, ".nan"],
		[BigInteger.Parse("123456789012345678901234567890", CultureInfo.InvariantCulture), "123456789012345678901234567890"],
		[BigInteger.Parse("-98765432109876543210", CultureInfo.InvariantCulture), "-98765432109876543210"],
		[(nint)5, "5"],
		[(nint)(-5), "-5"],
		[(nuint)7, "7"],
	];

	[TestInitialize]
	public void UseANonInvariantCulture()
	{
		originalCulture = CultureInfo.CurrentCulture;
		// Built from the invariant culture so it works in globalization-invariant mode too.
		CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		culture.NumberFormat.NumberDecimalSeparator = ",";
		culture.NumberFormat.NegativeSign = "~";
		CultureInfo.CurrentCulture = culture;
	}

	[TestCleanup]
	public void RestoreTheCulture() => CultureInfo.CurrentCulture = originalCulture!;

	[TestMethod]
	[DynamicData(nameof(Values))]
	public void AddFrontmatter_Number_IsWrittenAsOneInvariantScalar(object value, string expected)
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["value"] = value });

		Assert.AreEqual($"---\nvalue: {expected}\n---\nBody\n", result.ReplaceLineEndings("\n"));
		Assert.IsNotInstanceOfType<IDictionary<object, object>>(Frontmatter.ExtractFrontmatter(result)!["value"]);
	}

	[TestMethod]
	[DynamicData(nameof(Values))]
	public void SerializeFrontmatter_Number_IsWrittenAsOneInvariantScalar(object value, string expected)
	{
		string yaml = Frontmatter.SerializeFrontmatter(new() { ["value"] = value });

		Assert.AreEqual($"value: {expected}", yaml.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	[DynamicData(nameof(Values))]
	public void SerializeYamlObject_NestedNumber_IsWrittenAsOneInvariantScalar(object value, string expected)
	{
		string yaml = YamlSerializer.SerializeYamlObject(new() { ["outer"] = new Dictionary<string, object> { ["value"] = value } });

		Assert.AreEqual($"outer:\n  value: {expected}\n", yaml.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void AddFrontmatter_IntoAnExistingHeader_WritesTheNumberAsAScalar()
	{
		string result = Frontmatter.AddFrontmatter("---\ntitle: Hello\n---\nBody\n", new() { ["id"] = Int128.MaxValue });

		Assert.AreEqual("---\ntitle: Hello\nid: 170141183460469231731687303715884105727\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void AddFrontmatter_NumbersInAList_AreWrittenAsScalars()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["ids"] = new List<object> { (Half)1.5, new BigInteger(42), (nint)3, UInt128.One } });

		Assert.AreEqual("---\nids:\n- 1.5\n- 42\n- 3\n- 1\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}
}
