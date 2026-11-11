// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #247. An object the caller passes under two keys used to be written as a YAML
/// anchor and alias (<c>tags: &amp;o0</c> / <c>keywords: *o0</c>), which Ruby's <c>YAML.safe_load</c> rejects.
/// </summary>
[TestClass]
public class SharedValueAliasTests
{
	private const string SharedListHeader = "tags:\n- a\n- b\nkeywords:\n- a\n- b";

	private static Dictionary<string, object> SharedList()
	{
		List<object> list = ["a", "b"];
		return new() { ["tags"] = list, ["keywords"] = list };
	}

	private static Dictionary<string, object> SharedMapping()
	{
		Dictionary<string, object> mapping = new() { ["x"] = 1 };
		return new() { ["a"] = mapping, ["b"] = mapping };
	}

	private static void AssertNoAliases(string yaml)
	{
		Assert.IsFalse(yaml.Contains('&', StringComparison.Ordinal), yaml);
		Assert.IsFalse(yaml.Contains('*', StringComparison.Ordinal), yaml);
	}

	[TestMethod]
	public void AddFrontmatter_SharedList_WritesTheListUnderEachKey()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", SharedList());

		Assert.AreEqual($"---\n{SharedListHeader}\n---\nBody\n", result);
	}

	[TestMethod]
	public void AddFrontmatter_SharedListIntoAnExistingHeader_WritesTheListUnderEachKey()
	{
		string result = Frontmatter.AddFrontmatter("---\ntitle: x # c\n---\nBody\n", SharedList());

		AssertNoAliases(result);
		StringAssert.Contains(result, SharedListHeader);
		StringAssert.Contains(result, "title: x # c");
	}

	[TestMethod]
	public void ReplaceFrontmatter_SharedList_WritesTheListUnderEachKey()
	{
		string result = Frontmatter.ReplaceFrontmatter("---\ntitle: x\n---\nBody\n", SharedList());

		Assert.AreEqual($"---\n{SharedListHeader}\n---\nBody\n", result);
	}

	[TestMethod]
	public void SerializeFrontmatter_SharedList_WritesTheListUnderEachKey() =>
		Assert.AreEqual(SharedListHeader, Frontmatter.SerializeFrontmatter(SharedList()).ReplaceLineEndings("\n"));

	[TestMethod]
	public void SerializeFrontmatter_SharedMapping_WritesTheMappingUnderEachKey() =>
		Assert.AreEqual("a:\n  x: 1\nb:\n  x: 1", Frontmatter.SerializeFrontmatter(SharedMapping()).ReplaceLineEndings("\n"));

	[TestMethod]
	public void SerializeFrontmatter_SharedListWrittenOut_ReadsBackAsTwoLists()
	{
		Dictionary<string, object>? read = Frontmatter.ExtractFrontmatter(Frontmatter.AddFrontmatter("Body\n", SharedList()));

		Assert.IsNotNull(read);
		CollectionAssert.AreEqual(new object[] { "a", "b" }, ((List<object>)read["tags"]).ToArray());
		CollectionAssert.AreEqual(new object[] { "a", "b" }, ((List<object>)read["keywords"]).ToArray());
	}

	[TestMethod]
	public void SerializeFrontmatter_ValueContainsItself_ThrowsArgumentExceptionNamingTheKey()
	{
		List<object> cycle = [];
		cycle.Add(cycle);

		ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
			Frontmatter.SerializeFrontmatter(new() { ["title"] = "x", ["c"] = cycle }));

		StringAssert.Contains(ex.Message, "'c'");
	}
}
