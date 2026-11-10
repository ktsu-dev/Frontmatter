// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #225: a <see cref="TimeSpan"/> is written double-quoted, since YAML 1.1 reads a plain
/// <c>10:30:00</c> as the base-60 integer 37800, and a <see cref="double"/> or <see cref="float"/> written in
/// exponent form keeps a <c>.</c> in its mantissa, since YAML 1.1 reads <c>1E+20</c> as a string.
/// </summary>
[TestClass]
public class Yaml11NumberAndDurationTests
{
	/// <summary>
	/// YAML 1.1's float pattern, which PyYAML and Jekyll use. Its mantissa needs a <c>.</c>.
	/// </summary>
	private static readonly Regex Yaml11Float = new(@"^[-+]?([0-9][0-9_]*)?\.[0-9.]*([eE][-+][0-9]+)?$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

	private CultureInfo? originalCulture;

	[TestInitialize]
	public void UseANonInvariantCulture()
	{
		originalCulture = CultureInfo.CurrentCulture;
		CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		culture.NumberFormat.NumberDecimalSeparator = ",";
		culture.DateTimeFormat.TimeSeparator = ".";
		CultureInfo.CurrentCulture = culture;
	}

	[TestCleanup]
	public void RestoreTheCulture() => CultureInfo.CurrentCulture = originalCulture!;

	[TestMethod]
	[DataRow(0, 10, 30, 0, 0, "10:30:00")]
	[DataRow(0, 1, 30, 0, 0, "01:30:00")]
	[DataRow(1, 2, 3, 4, 500, "1.02:03:04.5000000")]
	[DataRow(0, 0, -5, 0, 0, "-00:05:00")]
	public void AddFrontmatter_TimeSpan_IsWrittenAsAQuotedInvariantDuration(int days, int hours, int minutes, int seconds, int milliseconds, string expected)
	{
		TimeSpan duration = new(days, hours, minutes, seconds, milliseconds);

		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["duration"] = duration });

		Assert.AreEqual($"---\nduration: \"{expected}\"\n---\nBody\n", result.ReplaceLineEndings("\n"));
		Assert.AreEqual(duration, TimeSpan.Parse((string)Frontmatter.ExtractFrontmatter(result)!["duration"], CultureInfo.InvariantCulture));
	}

	[TestMethod]
	public void AddFrontmatter_TimeSpanIntoAnExistingHeader_IsQuoted()
	{
		string result = Frontmatter.AddFrontmatter("---\ntitle: Hello\n---\nBody\n", new() { ["duration"] = new TimeSpan(10, 30, 0) });

		Assert.AreEqual("---\ntitle: Hello\nduration: \"10:30:00\"\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	[DataRow(1e20, "1.0E+20")]
	[DataRow(-1e20, "-1.0E+20")]
	[DataRow(1e-5, "1.0E-05")]
	[DataRow(1.5e300, "1.5E+300")]
	[DataRow(1.5, "1.5")]
	[DataRow(51.507351, "51.507351")]
	public void AddFrontmatter_Double_IsWrittenAsAFloatForYaml11AndYaml12(double value, string expected)
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["d"] = value });

		Assert.AreEqual($"---\nd: {expected}\n---\nBody\n", result.ReplaceLineEndings("\n"));
		Assert.IsTrue(Yaml11Float.IsMatch(expected), $"{expected} is not a YAML 1.1 float");
		Assert.IsNotInstanceOfType<string>(Frontmatter.ExtractFrontmatter(result)!["d"]);
	}

	[TestMethod]
	public void AddFrontmatter_Float_IsWrittenAsAFloatForYaml11AndYaml12()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["f"] = 1e20f });

		Assert.AreEqual("---\nf: 1.0E+20\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void AddFrontmatter_DoubleIntoAnExistingHeader_KeepsTheMantissaDot()
	{
		string result = Frontmatter.AddFrontmatter("---\ntitle: Hello\n---\nBody\n", new() { ["d"] = 1e20 });

		Assert.AreEqual("---\ntitle: Hello\nd: 1.0E+20\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void AddFrontmatter_ValuesInAList_AreWrittenForYaml11()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["values"] = new List<object> { new TimeSpan(10, 30, 0), 1e20 } });

		Assert.AreEqual("---\nvalues:\n- \"10:30:00\"\n- 1.0E+20\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	[DataRow(double.NaN, ".nan")]
	[DataRow(double.PositiveInfinity, ".inf")]
	[DataRow(double.NegativeInfinity, "-.inf")]
	public void AddFrontmatter_NonFiniteDouble_IsWrittenAsYamlSpelling(double value, string expected)
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["d"] = value });

		Assert.AreEqual($"---\nd: {expected}\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}
}
