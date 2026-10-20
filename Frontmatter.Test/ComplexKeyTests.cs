// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for complex (sequence or mapping) YAML keys, which used to be rewritten as the .NET
/// type name of the deserialized key.
/// </summary>
[TestClass]
public class ComplexKeyTests
{
	private const string SequenceKey = "---\n? [a, b]\n: c\ntitle: t\n---\nbody\n";
	private const string MappingKey = "---\n? {x: 1}\n: c\ntitle: t\n---\nbody\n";
	private const string NestedKey = "---\nmeta: { ? [1, 2] : pair }\ntitle: t\n---\nbody\n";

	[TestMethod]
	[DataRow("? [a, b]\n: c\ntitle: t")]
	[DataRow("? {x: 1}\n: c\ntitle: t")]
	[DataRow("meta: { ? [1, 2] : pair }\ntitle: t")]
	[DataRow("meta:\n  list:\n    - { ? [1] : deep }")]
	public void TryParseYamlObject_ComplexKey_ReturnsFalse(string yaml)
	{
		Assert.IsFalse(YamlSerializer.TryParseYamlObject(yaml, out Dictionary<string, object>? result));
		Assert.IsNull(result);
	}

	[TestMethod]
	public void TryParseYamlObject_ScalarKeysOfOtherTypes_StillParse()
	{
		Assert.IsTrue(YamlSerializer.TryParseYamlObject("1: one\ntrue: yes\nmeta: {2: two}", out Dictionary<string, object>? result));

		Assert.AreEqual("one", result["1"]);
		Assert.IsTrue(result.ContainsKey("true"));
		Assert.AreEqual("two", ((Dictionary<string, object>)result["meta"])["2"]);
	}

	[TestMethod]
	[DataRow(SequenceKey)]
	[DataRow(MappingKey)]
	[DataRow(NestedKey)]
	public void ExtractFrontmatter_ComplexKey_ReturnsNull(string input) =>
		Assert.IsNull(Frontmatter.ExtractFrontmatter(input));

	[TestMethod]
	[DataRow(SequenceKey)]
	[DataRow(MappingKey)]
	[DataRow(NestedKey)]
	public void CombineFrontmatter_ComplexKey_ReturnsTheDocumentUnchanged(string input)
	{
		Assert.AreEqual(input, Frontmatter.CombineFrontmatter(input));
		Assert.AreEqual(input, Frontmatter.CombineFrontmatter(input, FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None));
	}

	[TestMethod]
	[DataRow(SequenceKey)]
	[DataRow(MappingKey)]
	[DataRow(NestedKey)]
	public void AddFrontmatter_ComplexKey_ReturnsTheDocumentUnchanged(string input) =>
		Assert.AreEqual(input, Frontmatter.AddFrontmatter(input, new() { ["draft"] = true }));
}
