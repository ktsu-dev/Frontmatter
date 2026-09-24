// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter.Test;

using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for keys that normalize away to nothing — names made only of a decorative
/// prefix, suffix or separator, such as <c>page_</c> or <c>_value</c>.
/// </summary>
/// <remarks>
/// Every non-empty string contains the empty string, so a key whose normalized form is empty is
/// admitted by the containment tests in both the standardizer and the merger against the entire
/// candidate set, and is then renamed to, or merged into, whichever candidate ranks first. The
/// value is silently attributed to an unrelated property. These tests pin the guards that stop it.
/// </remarks>
[TestClass]
public class DecorationOnlyKeyTests
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

	[TestMethod]
	public void StandardizePropertyNames_PreservesDecorationOnlyKeys()
	{
		foreach (string key in new[] { "page_", "_value", "_", "page__value", "meta_", "custom_" })
		{
			Dictionary<string, object> result =
				NameStandardizer.StandardizePropertyNames(new Dictionary<string, object> { [key] = "V" });

			Assert.IsTrue(result.ContainsKey(key), $"[{key}] should be preserved, got [{string.Join(",", result.Keys)}]");
			Assert.AreEqual("V", result[key]);
		}
	}

	[TestMethod]
	public void MergeSimilarProperties_DoesNotMergeDecorationOnlyKeyIntoAnother()
	{
		Dictionary<string, object> frontmatter = new()
		{
			["page_"] = "Decoration",
			["description"] = "Real",
		};

		Dictionary<string, object> result =
			PropertyMerger.MergeSimilarProperties(frontmatter, FrontmatterMergeStrategy.Maximum);

		Assert.IsTrue(result.ContainsKey("page_"), $"[page_] should survive, got [{string.Join(",", result.Keys)}]");
		Assert.AreEqual("Decoration", result["page_"]);
		Assert.AreEqual("Real", result["description"]);
	}

	/// <summary>
	/// A padded key and its unpadded form must reach the same decision: either both match the same
	/// standard property, or neither matches and each is preserved as written. They did not before —
	/// the standardizer lower-cased without trimming and replaced only the space character, so
	/// padding blocked the prefix/suffix strip and sent the two forms down different paths.
	/// </summary>
	/// <remarks>
	/// An unmatched key is preserved verbatim, padding included, which is the correct behaviour for
	/// a round-tripping library — so the invariant is over the decision, not the literal spelling.
	/// </remarks>
	[TestMethod]
	public void StandardizePropertyNames_TreatsPaddedAndUnpaddedKeysAlike()
	{
		foreach ((string padded, string bare) in new[]
		{
			("\tmeta_x_field", "meta_x_field"),
			(" page_title ", "page_title"),
			("\tuser_name_value\t", "user_name_value"),
			("  post_headline  ", "post_headline"),
		})
		{
			ClearCaches();
			string paddedResult = Single(NameStandardizer.StandardizePropertyNames(new Dictionary<string, object> { [padded] = "V" }));
			ClearCaches();
			string bareResult = Single(NameStandardizer.StandardizePropertyNames(new Dictionary<string, object> { [bare] = "V" }));

			// When the bare key went unmatched it comes back as itself; the padded key should then
			// come back as itself too. Otherwise both should land on the same standard property.
			string expected = string.Equals(bareResult, bare, StringComparison.Ordinal) ? padded : bareResult;

			Assert.AreEqual(expected, paddedResult, $"padded [{padded}] and bare [{bare}] disagree");
		}
	}

	private static string Single(Dictionary<string, object> result)
	{
		Assert.HasCount(1, result);
		return result.Keys.First();
	}
}
