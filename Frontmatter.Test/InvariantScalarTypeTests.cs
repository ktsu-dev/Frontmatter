// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #217: <see cref="DateTimeOffset"/>, <see cref="DateOnly"/>, <see cref="TimeOnly"/>
/// and <see cref="Version"/> values are written as one invariant scalar under any culture, rather than as a
/// mapping of their properties.
/// </summary>
[TestClass]
public class InvariantScalarTypeTests
{
	private static readonly DateTimeOffset Date = new(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(2));

	private CultureInfo? originalCulture;

	[TestInitialize]
	public void UseANonInvariantCulture()
	{
		originalCulture = CultureInfo.CurrentCulture;
		// Built from the invariant culture so it works in globalization-invariant mode too, where named
		// cultures such as de-DE are not available.
		CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
		culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
		culture.DateTimeFormat.LongTimePattern = "HH.mm.ss";
		culture.DateTimeFormat.TimeSeparator = ".";
		culture.DateTimeFormat.DateSeparator = ".";
		culture.NumberFormat.NumberDecimalSeparator = ",";
		CultureInfo.CurrentCulture = culture;
	}

	[TestCleanup]
	public void RestoreTheCulture() => CultureInfo.CurrentCulture = originalCulture!;

	[TestMethod]
	public void AddFrontmatter_DateTimeOffset_IsWrittenAsAnIsoTimestamp()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["date"] = Date });

		Assert.AreEqual("---\ndate: 2024-01-15T10:30:00.0000000+02:00\n---\nBody\n", result.ReplaceLineEndings("\n"));
		Assert.AreEqual(Date, DateTimeOffset.Parse(Frontmatter.ExtractFrontmatter(result)!["date"].ToString()!, CultureInfo.InvariantCulture));
	}

	[TestMethod]
	public void AddFrontmatter_DateOnly_IsWrittenAsAnIsoDate()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["date"] = new DateOnly(2024, 1, 15) });

		Assert.AreEqual("---\ndate: 2024-01-15\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	[DataRow(10, 30, 0, 0, "10:30:00")]
	[DataRow(10, 30, 5, 250, "10:30:05.25")]
	public void AddFrontmatter_TimeOnly_IsWrittenAsAQuotedInvariantTime(int hour, int minute, int second, int millisecond, string expected)
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["time"] = new TimeOnly(hour, minute, second, millisecond) });

		Assert.AreEqual($"---\ntime: \"{expected}\"\n---\nBody\n", result.ReplaceLineEndings("\n"));
		Assert.AreEqual(expected, Frontmatter.ExtractFrontmatter(result)!["time"]);
	}

	[TestMethod]
	[DataRow(1, 2, 3, "version: 1.2.3")]
	[DataRow(1, 10, -1, "version: \"1.10\"")]
	public void AddFrontmatter_Version_IsWrittenAsItsText(int major, int minor, int build, string expected)
	{
		Version version = build < 0 ? new(major, minor) : new(major, minor, build);

		string result = Frontmatter.AddFrontmatter("Body\n", new() { ["version"] = version });

		Assert.AreEqual($"---\n{expected}\n---\nBody\n", result.ReplaceLineEndings("\n"));
		Assert.AreEqual(version.ToString(), Frontmatter.ExtractFrontmatter(result)!["version"]);
	}

	[TestMethod]
	public void AddFrontmatter_IntoAnExistingHeader_WritesTheValueAsAScalar()
	{
		string result = Frontmatter.AddFrontmatter("---\ntitle: Hello\n---\nBody\n", new() { ["date"] = Date });

		Assert.AreEqual("---\ntitle: Hello\ndate: 2024-01-15T10:30:00.0000000+02:00\n---\nBody\n", result.ReplaceLineEndings("\n"));
	}

	[TestMethod]
	public void SerializeYamlObject_NestedValues_AreWrittenAsScalars()
	{
		string yaml = YamlSerializer.SerializeYamlObject(new() { ["release"] = new Dictionary<string, object> { ["on"] = new DateOnly(2024, 1, 15), ["version"] = new Version(2, 0, 1) } });

		Assert.AreEqual("release:\n  on: 2024-01-15\n  version: 2.0.1\n", yaml.ReplaceLineEndings("\n"));
	}
}
