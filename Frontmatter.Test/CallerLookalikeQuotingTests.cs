// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #228 and #230: a string a caller supplies that YAML 1.2 reads as a float, such as
/// <c>1e3</c>, or that PyYAML reads as YAML 1.1's value or merge indicator (<c>=</c>, <c>&lt;&lt;</c>), is
/// written quoted and reads back as the same string.
/// </summary>
[TestClass]
public class CallerLookalikeQuotingTests
{
	private static readonly string[] Lookalikes = ["1e3", "1E3", "1e+3", "-1e3", "2e10", "1.5e3", ".5", "=", "<<"];

	private static Dictionary<string, object> LookalikeProperties()
	{
		Dictionary<string, object> properties = [];
		for (int i = 0; i < Lookalikes.Length; i++)
		{
			properties[$"k{i}"] = Lookalikes[i];
		}

		return properties;
	}

	private static void AssertEachLookalikeQuoted(string result)
	{
		for (int i = 0; i < Lookalikes.Length; i++)
		{
			string quoted = $"k{i}: [\"']{Regex.Escape(Lookalikes[i])}[\"']";
			Assert.IsTrue(Regex.IsMatch(result, quoted, RegexOptions.Multiline), $"k{i} = {Lookalikes[i]} is not quoted in:{Environment.NewLine}{result}");
		}
	}

	private static string[] ExtractedValues(string document)
	{
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter(document)!;
		string[] values = new string[Lookalikes.Length];
		for (int i = 0; i < Lookalikes.Length; i++)
		{
			Assert.IsInstanceOfType<string>(header[$"k{i}"], $"k{i} = {Lookalikes[i]} read back as {header[$"k{i}"]?.GetType()}");
			values[i] = (string)header[$"k{i}"];
		}

		return values;
	}

	[TestMethod]
	public void AddFrontmatter_NoHeader_QuotesLookalikes()
	{
		string result = Frontmatter.AddFrontmatter("Body", LookalikeProperties());

		AssertEachLookalikeQuoted(result);
		CollectionAssert.AreEqual(Lookalikes, ExtractedValues(result));
	}

	[TestMethod]
	public void AddFrontmatter_IntoExistingHeader_KeepsHeaderAndQuotesLookalikes()
	{
		string result = Frontmatter.AddFrontmatter("---\ntitle: Launch # keep me\n---\nBody\n", LookalikeProperties());

		// The read-back check succeeds, so the header is not replaced and keeps its comment.
		StringAssert.StartsWith(result, "---\ntitle: Launch # keep me\n");
		AssertEachLookalikeQuoted(result);
		CollectionAssert.AreEqual(Lookalikes, ExtractedValues(result));
	}

	[TestMethod]
	public void ReplaceFrontmatter_QuotesLookalikes()
	{
		string result = Frontmatter.ReplaceFrontmatter("---\ntitle: Old\n---\nBody\n", LookalikeProperties());

		AssertEachLookalikeQuoted(result);
		CollectionAssert.AreEqual(Lookalikes, ExtractedValues(result));
	}

	[TestMethod]
	public void SerializeFrontmatter_QuotesLookalikes() =>
		AssertEachLookalikeQuoted(Frontmatter.SerializeFrontmatter(LookalikeProperties()));

	[TestMethod]
	public void AddFrontmatter_Numbers_StayBare()
	{
		string result = Frontmatter.AddFrontmatter("Body", new Dictionary<string, object> { ["count"] = 1000, ["ratio"] = 1.5, ["title"] = "1e3 things" });

		StringAssert.Contains(result, "count: 1000");
		StringAssert.Contains(result, "ratio: 1.5");
		StringAssert.Contains(result, "title: 1e3 things");
	}
}
