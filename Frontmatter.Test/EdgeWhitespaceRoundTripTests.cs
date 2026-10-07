// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #183: a string that starts or ends with whitespace must be written so that the
/// next read gets the same string back. YAML strips that whitespace from a plain scalar.
/// </summary>
[TestClass]
public class EdgeWhitespaceRoundTripTests
{
	[TestMethod]
	[DataRow("tab\t", DisplayName = "Trailing tab")]
	[DataRow("x \t", DisplayName = "Trailing space and tab")]
	[DataRow("x\t ", DisplayName = "Trailing tab and space")]
	[DataRow("sp ", DisplayName = "Trailing space")]
	[DataRow("\ttab", DisplayName = "Leading tab")]
	[DataRow(" sp", DisplayName = "Leading space")]
	public void AddFrontmatter_ValueWithEdgeWhitespace_RoundTrips(string value)
	{
		string document = Frontmatter.AddFrontmatter("Body", new Dictionary<string, object> { ["k"] = value, ["z"] = "1" });

		Assert.AreEqual(value, Frontmatter.ExtractFrontmatter(document)!["k"]);
	}

	[TestMethod]
	public void CombineFrontmatter_QuotedValueEndingInTab_StaysQuoted()
	{
		string input = "---\nk: \"tab\\t\"\nz: 1\n---\nBody\n";

		string combined = Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		Assert.AreEqual("tab\t", Frontmatter.ExtractFrontmatter(combined)!["k"]);
	}

	[TestMethod]
	public void SerializeYamlObject_ValueEndingInTab_ParsesBackUnchanged()
	{
		string yaml = YamlSerializer.SerializeYamlObject(new Dictionary<string, object> { ["k"] = "tab\t", ["z"] = "1" });

		Assert.IsTrue(YamlSerializer.TryParseYamlObject(yaml, out Dictionary<string, object>? parsed));
		Assert.AreEqual("tab\t", parsed["k"]);
	}

	[TestMethod]
	public void SerializeYamlObject_PlainValue_StaysPlain()
	{
		string yaml = YamlSerializer.SerializeYamlObject(new Dictionary<string, object> { ["k"] = "plain value" });

		Assert.AreEqual("k: plain value", yaml.Trim());
	}
}
