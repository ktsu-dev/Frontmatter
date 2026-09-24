// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="PropertyNameNormalizer"/>, the single normalization used by both
/// <see cref="NameStandardizer"/> and <see cref="PropertyMerger"/>.
/// </summary>
[TestClass]
public class PropertyNameNormalizerTests
{
	private static readonly string[] reviewStatusWords = ["review", "status"];
	private static readonly string[] noWords = [];

	[TestMethod]
	public void Normalize_LowercasesAndCollapsesSeparators()
	{
		Assert.AreEqual("my_key", PropertyNameNormalizer.Normalize("My-Key"));
		Assert.AreEqual("my_key", PropertyNameNormalizer.Normalize("my  key"));
		Assert.AreEqual("my_key", PropertyNameNormalizer.Normalize("my__key"));
		Assert.AreEqual("my_key", PropertyNameNormalizer.Normalize("_my_key_"));
	}

	[TestMethod]
	public void Normalize_StripsOneDecorativePrefixAndSuffix()
	{
		Assert.AreEqual("title", PropertyNameNormalizer.Normalize("page_title"));
		Assert.AreEqual("note", PropertyNameNormalizer.Normalize("note_value"));
		Assert.AreEqual("note", PropertyNameNormalizer.Normalize("custom_note_field"));
	}

	/// <summary>
	/// Whitespace is removed before the prefix and suffix strip, so a padded key normalizes to the
	/// same thing as its unpadded form. The standardizer's former copy of this logic lower-cased
	/// without trimming and replaced only the space character, so a padded — and especially a
	/// tab-padded — key kept its padding and missed the strip entirely.
	/// </summary>
	[TestMethod]
	public void Normalize_TrimsAllWhitespaceBeforeStrippingAffixes()
	{
		foreach (string padded in new[] { " page_title ", "\tpage_title\t", "\npage_title\n", "  page_title  " })
		{
			Assert.AreEqual("title", PropertyNameNormalizer.Normalize(padded), $"for [{padded}]");
		}

		Assert.AreEqual("name", PropertyNameNormalizer.Normalize("\tuser_name_value\t"));
		Assert.AreEqual("headline", PropertyNameNormalizer.Normalize("  post_headline  "));
	}

	/// <summary>
	/// A key made only of decoration normalizes to the empty string. Both callers must treat that as
	/// "nothing to match on" rather than as a value to compare, because every string contains the
	/// empty string.
	/// </summary>
	[TestMethod]
	public void Normalize_DecorationOnlyKeysNormalizeToEmpty()
	{
		foreach (string decoration in new[] { "page_", "_value", "_", "-", " ", "\t", "", "page__value" })
		{
			Assert.AreEqual(string.Empty, PropertyNameNormalizer.Normalize(decoration), $"for [{decoration}]");
		}
	}

	[TestMethod]
	public void NormalizeToWords_SplitsNormalizedForm()
	{
		Assert.AreSequenceEqual(reviewStatusWords, PropertyNameNormalizer.NormalizeToWords(" page_Review-Status "));
		Assert.AreSequenceEqual(noWords, PropertyNameNormalizer.NormalizeToWords("page_"));
	}
}
