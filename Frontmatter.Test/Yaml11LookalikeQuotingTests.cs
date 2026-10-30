// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #188: a string a caller supplies that YAML 1.1 reads as a boolean or a number,
/// such as <c>NO</c> or <c>12:30</c>, is written quoted so Jekyll, PyYAML and other YAML 1.1 readers keep
/// it a string.
/// </summary>
[TestClass]
public class Yaml11LookalikeQuotingTests
{
	private static readonly string[] Lookalikes = ["NO", "yes", "on", "Off", "y", "n", "12:30", "1_000", "0b101"];

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

	[TestMethod]
	public void AddFrontmatter_NoHeader_QuotesYaml11Lookalikes()
	{
		string result = Frontmatter.AddFrontmatter("Body", LookalikeProperties());

		AssertEachLookalikeQuoted(result);
		CollectionAssert.AreEqual(Lookalikes, ExtractedValues(result));
	}

	[TestMethod]
	public void AddFrontmatter_IntoExistingHeader_QuotesYaml11Lookalikes()
	{
		string result = Frontmatter.AddFrontmatter("---\ntitle: Launch # keep me\n---\nBody\n", LookalikeProperties());

		StringAssert.StartsWith(result, "---\ntitle: Launch # keep me\n");
		AssertEachLookalikeQuoted(result);
		CollectionAssert.AreEqual(Lookalikes, ExtractedValues(result));
	}

	[TestMethod]
	public void ReplaceFrontmatter_QuotesYaml11Lookalikes()
	{
		string result = Frontmatter.ReplaceFrontmatter("---\ntitle: Old\n---\nBody\n", LookalikeProperties());

		AssertEachLookalikeQuoted(result);
		CollectionAssert.AreEqual(Lookalikes, ExtractedValues(result));
	}

	[TestMethod]
	public void SerializeFrontmatter_QuotesYaml11Lookalikes() =>
		AssertEachLookalikeQuoted(Frontmatter.SerializeFrontmatter(LookalikeProperties()));

	[TestMethod]
	public void AddFrontmatter_BooleansAndPlainStrings_StayBare()
	{
		string result = Frontmatter.AddFrontmatter("Body", new Dictionary<string, object> { ["draft"] = false, ["title"] = "hello" });

		StringAssert.Contains(result, "draft: false");
		StringAssert.Contains(result, "title: hello");
	}

	[TestMethod]
	public void CombineFrontmatter_PlainYes_StaysPlain()
	{
		// A plain value from the document keeps its spelling, so a YAML 1.1 reader still sees the boolean
		// the author wrote. Only values a caller supplies are quoted.
		string result = Frontmatter.CombineFrontmatter("---\npublished: yes\ndraft: false\n---\nBody\n", FrontmatterNaming.AsIs, FrontmatterOrder.AsIs, FrontmatterMergeStrategy.None);

		StringAssert.Contains(result, "published: yes");
		StringAssert.Contains(result, "draft: false");
	}

	private static string[] ExtractedValues(string document)
	{
		Dictionary<string, object> header = Frontmatter.ExtractFrontmatter(document)!;
		string[] values = new string[Lookalikes.Length];
		for (int i = 0; i < Lookalikes.Length; i++)
		{
			values[i] = (string)header[$"k{i}"];
		}

		return values;
	}
}
