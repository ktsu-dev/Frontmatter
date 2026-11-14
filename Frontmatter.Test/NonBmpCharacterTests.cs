// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for #250: characters outside the Basic Multilingual Plane, such as emoji, must be written
/// as the characters themselves, not as <c>\UXXXXXXXX</c> escapes, and must still read back unchanged.
/// </summary>
[TestClass]
public class NonBmpCharacterTests
{
	private const string Rocket = "\U0001F680";
	private const string Party = "\U0001F389";
	private const string Document = "---\ntitle: Launch day " + Rocket + "\ntags: [news, " + Party + "]\n---\nBody\n";

	[TestMethod]
	public void AddFrontmatter_NoHeader_WritesEmojiAsIs()
	{
		string result = Frontmatter.AddFrontmatter("Body\n", new Dictionary<string, object> { ["title"] = "Launch day " + Rocket });

		AssertLiteral(result, "title", "Launch day " + Rocket);
	}

	[TestMethod]
	public void AddFrontmatter_IntoHeader_WritesEmojiAsIs()
	{
		string result = Frontmatter.AddFrontmatter(Document, new Dictionary<string, object> { ["description"] = "We shipped " + Party });

		AssertLiteral(result, "description", "We shipped " + Party);
	}

	[TestMethod]
	public void ReplaceFrontmatter_WritesEmojiAsIs()
	{
		string result = Frontmatter.ReplaceFrontmatter(Document, new Dictionary<string, object> { ["title"] = "Launch day " + Rocket, ["draft"] = false });

		AssertLiteral(result, "title", "Launch day " + Rocket);
	}

	[TestMethod]
	public void CombineFrontmatter_WritesEmojiAsIs()
	{
		string result = Frontmatter.CombineFrontmatter(Document);

		AssertLiteral(result, "title", "Launch day " + Rocket);
		Assert.IsTrue(result.Contains("- " + Party), result);
	}

	[TestMethod]
	public void SerializeFrontmatter_WritesEmojiAsIs()
	{
		string yaml = Frontmatter.SerializeFrontmatter(new Dictionary<string, object> { ["title"] = "Launch day " + Rocket });

		Assert.IsFalse(yaml.Contains("\\U"), yaml);
		Assert.IsTrue(yaml.Contains(Rocket), yaml);
		Assert.IsTrue(YamlSerializer.TryParseYamlObject(yaml, out Dictionary<string, object>? parsed));
		Assert.AreEqual("Launch day " + Rocket, parsed["title"]);
	}

	[TestMethod]
	public void SerializeYamlObject_EmojiKeyAndNestedValue_WritesThemAsIs()
	{
		Dictionary<string, object> input = new()
		{
			[Rocket] = new List<object> { Party, new Dictionary<string, object> { ["k"] = "x" + Rocket + "y" } },
		};

		string yaml = YamlSerializer.SerializeYamlObject(input);

		Assert.IsFalse(yaml.Contains("\\U"), yaml);
		Assert.IsTrue(YamlSerializer.TryParseYamlObject(yaml, out Dictionary<string, object>? parsed), yaml);
		List<object> list = (List<object>)parsed[Rocket];
		Assert.AreEqual(Party, list[0]);
		Assert.AreEqual("x" + Rocket + "y", ((Dictionary<string, object>)list[1])["k"]);
	}

	[TestMethod]
	[DataRow("﷐", DisplayName = "Noncharacter used by the encoding")]
	[DataRow("﷐﷑", DisplayName = "Text that looks like an encoded character")]
	[DataRow("﷐﷑", DisplayName = "Text that looks like an encoded noncharacter")]
	[DataRow("a\\U0001F680b", DisplayName = "A literal backslash-U sequence")]
	[DataRow("\U0010FFFF", DisplayName = "Highest code point")]
	[DataRow("\U00010000", DisplayName = "Lowest supplementary code point")]
	[DataRow("\uD83D", DisplayName = "Lone high surrogate")]
	[DataRow("x\uDE80", DisplayName = "Lone low surrogate")]
	public void SerializeYamlObject_UnusualText_RoundTrips(string value)
	{
		string yaml = YamlSerializer.SerializeYamlObject(new Dictionary<string, object> { ["k"] = value });

		Assert.IsTrue(YamlSerializer.TryParseYamlObject(yaml, out Dictionary<string, object>? parsed), yaml);
		Assert.AreEqual(value, parsed["k"]);
	}

	[TestMethod]
	public void SerializeYamlObject_LongTextWithEmoji_IsNotSplitInsideACharacter()
	{
		string words = string.Concat(System.Linq.Enumerable.Repeat("word " + Rocket + " ", 40));
		string run = string.Concat(System.Linq.Enumerable.Repeat(Rocket + Party, 60));
		string quoted = " " + words;

		foreach (string value in new[] { words, run, quoted, words + "\n" + run + "\n" })
		{
			string yaml = YamlSerializer.SerializeYamlObject(new Dictionary<string, object> { ["k"] = value });

			Assert.IsFalse(yaml.Contains("\\U"), yaml);
			Assert.IsTrue(YamlSerializer.TryParseYamlObject(yaml, out Dictionary<string, object>? parsed), yaml);
			Assert.AreEqual(value, parsed["k"]);
		}
	}

	private static void AssertLiteral(string document, string key, string expected)
	{
		Assert.IsFalse(document.Contains("\\U"), document);
		Assert.IsTrue(document.Contains(expected), document);
		Assert.AreEqual(expected, Frontmatter.ExtractFrontmatter(document)![key]);
	}
}
