// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for keys whose normalized form is a single character, such as
/// <c>meta_x_field</c> — a decorative prefix and suffix around one letter.
/// </summary>
/// <remarks>
/// <para>
/// A one-character fragment is contained in any candidate that happens to use that letter, so the
/// bidirectional containment tests in the standardizer and the merger admit it on an incidental
/// letter rather than a shared word. <c>meta_x_field</c> normalizes to <c>x</c>, and <c>next</c>
/// contains <c>x</c>.
/// </para>
/// <para>
/// This is the same failure <see cref="DecorationOnlyKeyTests"/> pins for the empty normalized
/// form, one character further along: the value is silently attributed to an unrelated property,
/// and in the merger's case a third property is dropped outright. These tests pin the guards.
/// </para>
/// </remarks>
[TestClass]
public class SingleCharacterKeyTests
{
	[TestInitialize]
	public void ClearCaches()
	{
		ClearCache(typeof(NameStandardizer), "PropertyNameCache");
		ClearCache(typeof(PropertyMerger), "PropertyMergeCache");
	}

	private static void ClearCache(Type type, string fieldName)
	{
		FieldInfo? field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
		if (field?.GetValue(null) is ConcurrentDictionary<string, string> cache)
		{
			cache.Clear();
		}
	}

	/// <summary>
	/// Each of these normalizes to one character and was renamed to an unrelated standard property:
	/// <c>x</c> to <c>next</c>, <c>a</c> to <c>area</c>, <c>e</c> to <c>editor</c>, <c>s</c> to
	/// <c>slug</c> and <c>t</c> to <c>toc</c>.
	/// </summary>
	[TestMethod]
	public void StandardizePropertyNames_PreservesKeysNormalizingToOneCharacter()
	{
		foreach (string key in new[] { "meta_x_field", "page_a_value", "meta_e_field", "custom_s_data", "page_x_text", "page_t_data" })
		{
			ClearCaches();

			Dictionary<string, object> result =
				NameStandardizer.StandardizePropertyNames(new Dictionary<string, object> { [key] = "V" });

			Assert.IsTrue(result.ContainsKey(key), $"[{key}] should be preserved, got [{string.Join(",", result.Keys)}]");
			Assert.AreEqual("V", result[key]);
		}
	}

	/// <summary>
	/// The merger cross-referenced mutually — <c>meta_x_field</c> to <c>next</c> and <c>next</c> back
	/// to <c>meta_x_field</c>, since containment is tested in both directions — and a third key that
	/// also contained the letter was dropped entirely rather than merged.
	/// </summary>
	[TestMethod]
	public void MergeSimilarProperties_DoesNotMergeOrDropOnASingleCharacterFragment()
	{
		Dictionary<string, object> frontmatter = new()
		{
			["meta_x_field"] = "A",
			["next"] = "B",
			["text"] = "C",
		};

		Dictionary<string, object> result =
			PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Maximum);

		Assert.HasCount(3, result, $"nothing should be dropped, got [{string.Join(",", result.Keys)}]");
		Assert.AreEqual("A", result["meta_x_field"]);
		Assert.AreEqual("B", result["next"]);
		Assert.AreEqual("C", result["text"]);
	}

	/// <summary>
	/// The rule is about containment specifically, and it is symmetric: a one-character name is
	/// unmatchable whichever side it appears on, because containment is tested in both directions.
	/// </summary>
	[TestMethod]
	public void MayMatchByContainment_RejectsAOneCharacterNameOnEitherSide()
	{
		Assert.IsFalse(PropertyNameNormalizer.MayMatchByContainment("x", "next"), "short first");
		Assert.IsFalse(PropertyNameNormalizer.MayMatchByContainment("next", "x"), "short second");
		Assert.IsFalse(PropertyNameNormalizer.MayMatchByContainment("x", "y"), "both short");
		Assert.IsFalse(PropertyNameNormalizer.MayMatchByContainment("", "next"), "empty is also below the floor");

		Assert.IsTrue(PropertyNameNormalizer.MayMatchByContainment("by", "author"), "two characters is the floor");
		Assert.IsTrue(PropertyNameNormalizer.MayMatchByContainment("tag", "tags"));
		Assert.IsTrue(PropertyNameNormalizer.MayMatchByContainment("url", "canonical_url"));
	}

	/// <summary>
	/// The floor is two characters rather than something larger, so a key normalizing to a
	/// two-character fragment still matches by containment exactly as it did before. <c>ag</c> is
	/// contained in <c>tags</c>, <c>stage</c> and <c>image</c>.
	/// </summary>
	[TestMethod]
	public void StandardizePropertyNames_StillMatchesATwoCharacterFragment()
	{
		ClearCaches();

		Dictionary<string, object> result =
			NameStandardizer.StandardizePropertyNames(new Dictionary<string, object> { ["meta_ag_field"] = "V" });

		Assert.IsFalse(result.ContainsKey("meta_ag_field"),
			$"[ag] is at the floor and should still be standardized, got [{string.Join(",", result.Keys)}]");
	}
}
