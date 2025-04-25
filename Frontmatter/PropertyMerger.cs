namespace ktsu.Frontmatter;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides functionality for merging similar frontmatter properties.
/// </summary>
internal static partial class PropertyMerger
{
	/// <summary>
	/// Cache for property merge mappings
	/// </summary>
	private static readonly ConcurrentDictionary<string, string> PropertyMergeCache = new();

	/// <summary>
	/// Dictionary of known property mappings for merging
	/// </summary>
	private static readonly Dictionary<string, string> KnownPropertyMappings = new(StringComparer.OrdinalIgnoreCase)
	{
		// Title variants
		{ "title", "title" },
		{ "name", "title" },
		{ "heading", "title" },
		{ "subject", "title" },
		{ "post-title", "title" },
		{ "pagetitle", "title" },
		{ "page-title", "title" },
		{ "headline", "title" },

		// Author variants
		{ "author", "author" },
		{ "authors", "author" },
		{ "creator", "author" },
		{ "contributor", "author" },
		{ "contributors", "author" },
		{ "by", "author" },
		{ "written-by", "author" },
		{ "writer", "author" },

		// Date variants
		{ "date", "date" },
		{ "created", "date" },
		{ "creation_date", "date" },
		{ "creation-date", "date" },
		{ "creationdate", "date" },
		{ "published", "date" },
		{ "publish_date", "date" },
		{ "publish-date", "date" },
		{ "publishdate", "date" },
		{ "post-date", "date" },
		{ "posting-date", "date" },
		{ "pubdate", "date" },

		// Tags variants
		{ "tags", "tags" },
		{ "tag", "tags" },
		{ "keywords", "tags" },
		{ "keyword", "tags" },
		{ "topics", "tags" },
		{ "topic", "tags" },

		// Categories variants
		{ "categories", "categories" },
		{ "category", "categories" },
		{ "section", "categories" },
		{ "sections", "categories" },
		{ "group", "categories" },
		{ "groups", "categories" },

		// Description variants
		{ "description", "description" },
		{ "summary", "description" },
		{ "abstract", "description" },
		{ "excerpt", "description" },
		{ "desc", "description" },
		{ "overview", "description" },
		{ "snippet", "description" },

		// Last modified variants
		{ "modified", "modified" },
		{ "last_modified", "modified" },
		{ "last-modified", "modified" },
		{ "lastmodified", "modified" },
		{ "updated", "modified" },
		{ "update_date", "modified" },
		{ "update-date", "modified" },
		{ "updatedate", "modified" },
		{ "revision-date", "modified" },
		{ "last-update", "modified" },

		// Layout variants
		{ "layout", "layout" },
		{ "template", "layout" },
		{ "page-layout", "layout" },
		{ "type", "layout" },
		{ "page-type", "layout" },

		// URL/permalink variants
		{ "permalink", "permalink" },
		{ "url", "permalink" },
		{ "link", "permalink" },
		{ "slug", "permalink" },
		{ "path", "permalink" }
	};

	/// <summary>
	/// Merges properties that capture redundant information based on the specified strategy.
	/// </summary>
	/// <param name="frontmatter">The frontmatter dictionary to process.</param>
	/// <param name="strategy">The merge strategy to use.</param>
	/// <returns>A new dictionary with merged properties.</returns>
	internal static Dictionary<string, object> MergeSimilarProperties(Dictionary<string, object> frontmatter, FrontmatterMergeStrategy strategy)
	{
		// For None strategy, just return the original dictionary
		if (strategy == FrontmatterMergeStrategy.None)
		{
			return new Dictionary<string, object>(frontmatter);
		}

		// Keep track of which properties map to canonical names
		var propertyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// Initialize with known mappings
		foreach (string key in frontmatter.Keys)
		{
			// Try to find in cache first
			if (PropertyMergeCache.TryGetValue(key, out string? canonicalName))
			{
				propertyMappings[key] = canonicalName;
				continue;
			}

			// For Conservative strategy, only use the predefined mappings
			if (strategy == FrontmatterMergeStrategy.Conservative)
			{
				// Check known mappings dictionary
				if (KnownPropertyMappings.TryGetValue(key, out canonicalName))
				{
					PropertyMergeCache[key] = canonicalName;
					propertyMappings[key] = canonicalName;
				}
				else
				{
					// No mapping found, use the key itself
					PropertyMergeCache[key] = key;
					propertyMappings[key] = key;
				}

				continue;
			}

			// For Aggressive and Maximum strategies, try to find similar keys
			canonicalName = strategy switch
			{
				FrontmatterMergeStrategy.Aggressive => FindBasicCanonicalName(key, [.. frontmatter.Keys]),
				FrontmatterMergeStrategy.Maximum => FindSemanticCanonicalName(key, [.. frontmatter.Keys]),
				FrontmatterMergeStrategy.Conservative => key, // Should never reach here
				FrontmatterMergeStrategy.None => key, // Should never reach here
				_ => key // Fallback for any other case
			};

			PropertyMergeCache[key] = canonicalName;
			propertyMappings[key] = canonicalName;
		}

		// Create a new dictionary to hold the merged properties
		var mergedFrontmatter = new Dictionary<string, object>();

		// Group properties by their canonical names
		var canonicalGroups = new Dictionary<string, List<string>>();

		foreach (var mapping in propertyMappings)
		{
			string originalKey = mapping.Key;
			string canonicalKey = mapping.Value;

			if (canonicalGroups.TryGetValue(canonicalKey, out var group))
			{
				group.Add(originalKey);
			}
			else
			{
				group = [];
				canonicalGroups[canonicalKey] = group;
				group.Add(originalKey);
			}
		}

		foreach (var group in canonicalGroups)
		{
			string canonicalKey = group.Key;
			var originalKeys = group.Value;

			// Get the first value to determine the type
			object firstValue = frontmatter[originalKeys[0]];
			if (firstValue == null)
			{
				continue;
			}

			var firstType = firstValue.GetType();

			// Check if all values have the same type
			bool allSameType = originalKeys.All(k => frontmatter[k]?.GetType() == firstType);

			if (!allSameType)
			{
				// If types are different, keep all properties separate
				foreach (string key in originalKeys)
				{
					mergedFrontmatter[key] = frontmatter[key] ?? throw new InvalidOperationException($"Value for key {key} is null");
				}

				continue;
			}

			// Handle array/list types specially
			if (firstValue is IList<object> firstList)
			{
				// If the first value is a list, merge all lists into one
				List<object> mergedList = [];
				foreach (string key in originalKeys)
				{
					if (frontmatter[key] is IList<object> list)
					{
						mergedList.AddRange(list);
					}
				}

				mergedFrontmatter[canonicalKey] = mergedList.Distinct().ToList();
			}
			else
			{
				// For all other types, use the first value
				mergedFrontmatter[canonicalKey] = firstValue;
			}
		}

		return mergedFrontmatter;
	}

	/// <summary>
	/// Attempts to find a canonical name for a property key using basic analysis.
	/// </summary>
	/// <param name="key">The key to analyze.</param>
	/// <param name="existingKeys">All existing keys in the frontmatter.</param>
	/// <returns>The canonical name for the key.</returns>
	private static string FindBasicCanonicalName(string key, string[] existingKeys)
	{
		// First check if it's a known property
		if (KnownPropertyMappings.TryGetValue(key, out string? canonicalName))
		{
			return canonicalName;
		}

		// Remove common prefixes/suffixes and special characters
		string normalizedKey = NormalizePropertyName(key);

		// Look for exact matches after normalization
		foreach (string existingKey in existingKeys)
		{
			string normalizedExisting = NormalizePropertyName(existingKey);
			if (string.Equals(normalizedKey, normalizedExisting, StringComparison.OrdinalIgnoreCase))
			{
				return existingKey;
			}
		}

		// Look for partial matches
		foreach (string existingKey in existingKeys)
		{
			string normalizedExisting = NormalizePropertyName(existingKey);
			if (normalizedKey.Contains(normalizedExisting, StringComparison.OrdinalIgnoreCase) ||
				normalizedExisting.Contains(normalizedKey, StringComparison.OrdinalIgnoreCase))
			{
				return existingKey;
			}
		}

		return key;
	}

	/// <summary>
	/// Attempts to find a canonical name for a property key using semantic analysis.
	/// </summary>
	/// <param name="key">The key to analyze.</param>
	/// <param name="existingKeys">All existing keys in the frontmatter.</param>
	/// <returns>The canonical name for the key.</returns>
	private static string FindSemanticCanonicalName(string key, string[] existingKeys)
	{
		// First try basic matching
		string basicMatch = FindBasicCanonicalName(key, existingKeys);
		if (basicMatch != key)
		{
			return basicMatch;
		}

		// Then try more aggressive matching using word similarity
		string[] keyWords = NormalizePropertyName(key).Split('_', ' ', '-');
		var matches = new Dictionary<string, int>();

		foreach (string existingKey in existingKeys)
		{
			string[] existingWords = NormalizePropertyName(existingKey).Split('_', ' ', '-');
			int matchScore = CalculateWordMatchScore(keyWords, existingWords);
			if (matchScore > 0)
			{
				matches[existingKey] = matchScore;
			}
		}

		return matches.Count > 0 ? matches.OrderByDescending(m => m.Value).First().Key : key;
	}

	private static string NormalizePropertyName(string key)
	{
		// Convert to lowercase
		key = key.ToLowerInvariant();

		// Remove common prefixes
		string[] prefixes = ["page_", "post_", "meta_", "custom_", "user_", "site_"];
		foreach (string? prefix in prefixes)
		{
			if (key.StartsWith(prefix))
			{
				key = key[prefix.Length..];
				break;
			}
		}

		// Remove common suffixes
		string[] suffixes = ["_value", "_text", "_data", "_info", "_meta", "_field"];
		foreach (string? suffix in suffixes)
		{
			if (key.EndsWith(suffix))
			{
				key = key[..^suffix.Length];
				break;
			}
		}

		// Replace special characters with underscores
		key = key.Replace('-', '_').Replace(' ', '_');

		// Remove duplicate underscores
		while (key.Contains("__"))
		{
			key = key.Replace("__", "_");
		}

		return key.Trim('_');
	}

	private static int CalculateWordMatchScore(string[] words1, string[] words2)
	{
		int score = 0;
		foreach (string word1 in words1)
		{
			foreach (string word2 in words2)
			{
				if (word1 == word2)
				{
					score += 2;
				}
				else if (word1.Contains(word2) || word2.Contains(word1))
				{
					score += 1;
				}
			}
		}

		return score;
	}
}
