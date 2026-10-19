// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #204: a string holding a carriage return or NEL must be written so that the next
/// read gets the same string back. YAML turns every line break inside a block scalar into <c>\n</c>.
/// </summary>
[TestClass]
public class LineBreakRoundTripTests
{
	[TestMethod]
	[DataRow("a\r\nb", DisplayName = "CRLF")]
	[DataRow("a\rb", DisplayName = "Lone CR")]
	[DataRow("a\u0085b", DisplayName = "NEL")]
	[DataRow("Line one\r\nLine two\r\n", DisplayName = "Trailing CRLF")]
	public void AddFrontmatter_ValueWithLineBreak_RoundTrips(string value)
	{
		string document = Frontmatter.AddFrontmatter("Body\r\n", new Dictionary<string, object> { ["k"] = value, ["z"] = "1" });

		Assert.AreEqual(value, Frontmatter.ExtractFrontmatter(document)!["k"]);
	}

	[TestMethod]
	[DataRow("a\r\nb", DisplayName = "CRLF")]
	[DataRow("a\rb", DisplayName = "Lone CR")]
	[DataRow("a\u0085b", DisplayName = "NEL")]
	public void AddFrontmatter_KeyWithLineBreak_RoundTrips(string key)
	{
		string document = Frontmatter.AddFrontmatter("Body", new Dictionary<string, object> { [key] = "v" });

		Assert.AreEqual("v", Frontmatter.ExtractFrontmatter(document)![key]);
	}

	[TestMethod]
	public void CombineFrontmatter_QuotedValueWithCrlf_KeepsIt()
	{
		string input = "---\ntitle: T\nnote: \"a\\r\\nb\"\n---\nBody\n";

		string combined = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		Assert.AreEqual("a\r\nb", Frontmatter.ExtractFrontmatter(combined)!["note"]);
	}

	[TestMethod]
	public void SerializeYamlObject_MultiLineValueWithLfOnly_IsNotQuoted()
	{
		string yaml = YamlSerializer.SerializeYamlObject(new Dictionary<string, object> { ["k"] = "a\nb" });

		Assert.IsFalse(yaml.Contains('"'), yaml);
	}
}
