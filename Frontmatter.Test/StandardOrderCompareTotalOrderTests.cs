// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Names that differ only in case must not compare as equal, or a sorted collection
/// built on <see cref="StandardOrder.Compare"/> throws or drops keys (#197).
/// </summary>
[TestClass]
public class StandardOrderCompareTotalOrderTests
{
	private static readonly Comparer<string> Comparer = Comparer<string>.Create(StandardOrder.Compare);

	[TestMethod]
	[DataRow("Title", "title")]
	[DataRow("MyKey", "mykey")]
	[DataRow("TAGS", "Tags")]
	public void Compare_CaseVariants_AreNotEqualAndAntisymmetric(string a, string b)
	{
		int forward = StandardOrder.Compare(a, b);
		int backward = StandardOrder.Compare(b, a);

		Assert.AreNotEqual(0, forward, $"'{a}' and '{b}' are different strings and must not compare equal");
		Assert.AreEqual(-Math.Sign(forward), Math.Sign(backward), "Compare must be antisymmetric");
	}

	[TestMethod]
	public void Compare_SameString_IsZero()
	{
		Assert.AreEqual(0, StandardOrder.Compare("Title", "Title"));
		Assert.AreEqual(0, StandardOrder.Compare("mykey", "mykey"));
	}

	[TestMethod]
	public void SortedDictionary_KeepsCaseVariantKeys()
	{
		SortedDictionary<string, object> dictionary = new(Comparer)
		{
			{ "title", "a" },
			{ "Title", "b" },
		};

		Assert.HasCount(2, dictionary);
		Assert.AreEqual("a", dictionary["title"]);
		Assert.AreEqual("b", dictionary["Title"]);
	}

	[TestMethod]
	public void SortedSet_KeepsCaseVariantKeys()
	{
		SortedSet<string> set = new(["tags", "Tags", "myKey", "MyKey"], Comparer);

		Assert.HasCount(4, set);
	}

	[TestMethod]
	public void Compare_OrderAcrossPositionsIsUnchanged()
	{
		Assert.IsLessThan(0, StandardOrder.Compare("Title", "author"), "Known keys keep their standard order regardless of case");
		Assert.IsLessThan(0, StandardOrder.Compare("Tags", "zzz_unknown"), "Known keys come before unknown keys");
		Assert.IsLessThan(0, StandardOrder.Compare("Alpha", "beta"), "Unknown keys still sort case-insensitively");
		Assert.IsGreaterThan(0, StandardOrder.Compare("zzz_unknown", "TITLE"), "Unknown keys come after known keys");
	}
}
