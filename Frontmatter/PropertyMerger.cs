namespace ktsu.Frontmatter;

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
		Dictionary<string, string> propertyMappings = new(StringComparer.OrdinalIgnoreCase);

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
				FrontmatterMergeStrategy.Aggressive => FindBasicCanonicalName(key, frontmatter.Keys),
				FrontmatterMergeStrategy.Maximum => FindSemanticCanonicalName(key, frontmatter.Keys),
				FrontmatterMergeStrategy.Conservative => key, // Should never reach here
				FrontmatterMergeStrategy.None => key, // Should never reach here
				_ => key // Fallback for any other case
			};

			PropertyMergeCache[key] = canonicalName;
			propertyMappings[key] = canonicalName;
		}

		// Create a new dictionary to hold the merged properties
		Dictionary<string, object> mergedFrontmatter = [];

		// Group properties by their canonical names
		Dictionary<string, List<string>> canonicalGroups = [];
		foreach (var mapping in propertyMappings)
		{
			string originalKey = mapping.Key;
			string canonicalKey = mapping.Value;

			if (!canonicalGroups.TryGetValue(canonicalKey, out var group))
			{
				group = [];
				canonicalGroups[canonicalKey] = group;
			}

			group.Add(originalKey);
		}

		// Merge properties within each canonical group
		foreach (var group in canonicalGroups)
		{
			string canonicalKey = group.Key;
			var keys = group.Value;

			if (keys.Count == 1)
			{
				// No merging needed for single items
				mergedFrontmatter[keys[0]] = frontmatter[keys[0]];
				continue;
			}

			// Find the highest priority key (usually the canonical one)
			string primaryKey = keys.FirstOrDefault(k => string.Equals(k, canonicalKey, StringComparison.OrdinalIgnoreCase)) ?? keys.First();
			object mergedValue = MergePropertyValues(keys.Select(k => new KeyValuePair<string, object>(k, frontmatter[k])));

			mergedFrontmatter[primaryKey] = mergedValue;
		}

		return mergedFrontmatter;
	}

	/// <summary>
	/// Attempts to find a canonical name for a property key using basic analysis.
	/// </summary>
	/// <param name="key">The key to analyze.</param>
	/// <param name="existingKeys">All existing keys in the frontmatter.</param>
	/// <returns>The canonical name for the key.</returns>
	private static string FindBasicCanonicalName(string key, IEnumerable<string> existingKeys)
	{
		// Check for plural/singular forms
		string singularForm = key.TrimEnd('s');
		string pluralForm = key + "s";

		string[] existingKeysArray = [.. existingKeys];
		// If both forms exist, prefer the plural form
		if (existingKeysArray.Any(k => string.Equals(k, pluralForm, StringComparison.OrdinalIgnoreCase)))
		{
			return pluralForm;
		}

		// If singular form (without 's') exists and is not the same as the key, use it
		if (singularForm != key && existingKeysArray.Any(k => string.Equals(k, singularForm, StringComparison.OrdinalIgnoreCase)))
		{
			return singularForm;
		}

		// Check for common prefixes and suffixes
		var datePattern = DatePatternRegex();
		if (datePattern.IsMatch(key))
		{
			return "date";
		}

		var authorPattern = AuthorPatternRegex();
		if (authorPattern.IsMatch(key))
		{
			return "author";
		}

		// No transformation found, use the original key
		return key;
	}

	/// <summary>
	/// Attempts to find a canonical name for a property key using semantic analysis.
	/// </summary>
	/// <param name="key">The key to analyze.</param>
	/// <param name="existingKeys">All existing keys in the frontmatter.</param>
	/// <returns>The canonical name for the key.</returns>
	private static string FindSemanticCanonicalName(string key, IEnumerable<string> existingKeys)
	{
		// First try basic pattern matching
		string basicResult = FindBasicCanonicalName(key, existingKeys);
		if (basicResult != key)
		{
			return basicResult;
		}

		// Check if the key is in known mappings
		if (KnownPropertyMappings.TryGetValue(key, out string? canonicalName))
		{
			return canonicalName;
		}

		// Try basic similarity - we can't use the FuzzyMatcher here since we would need
		// to take a dependency on the full FuzzySearch library
		string[] knownPropertyNames = [.. KnownPropertyMappings.Values.Distinct()];

		// Simple distance-based matching
		string bestMatch = key;
		int bestScore = int.MinValue;

		foreach (string propertyName in knownPropertyNames)
		{
			int score = CalculateSimilarity(key, propertyName);
			if (score > bestScore)
			{
				bestScore = score;
				bestMatch = propertyName;
			}
		}

		// Only use the best match if it's similar enough
		if (bestScore > 3)
		{
			return bestMatch;
		}

		// No match found with sufficient confidence
		return key;
	}

	/// <summary>
	/// Calculates a simple similarity score between two strings.
	/// Higher scores indicate greater similarity.
	/// </summary>
	private static int CalculateSimilarity(string a, string b)
	{
		// Convert to lowercase for case-insensitive comparison
		a = a.ToLowerInvariant();
		b = b.ToLowerInvariant();

		// If one string contains the other, high similarity
		if (a.Contains(b) || b.Contains(a))
		{
			return 10;
		}

		// Count matching characters at start of string
		int prefixMatch = 0;
		int minLength = Math.Min(a.Length, b.Length);
		for (int i = 0; i < minLength; i++)
		{
			if (a[i] == b[i])
			{
				prefixMatch++;
			}
			else
			{
				break;
			}
		}

		// Count matching characters at end of string
		int suffixMatch = 0;
		for (int i = 1; i <= minLength; i++)
		{
			if (a[^i] == b[^i])
			{
				suffixMatch++;
			}
			else
			{
				break;
			}
		}

		// Count shared characters
		int sharedChars = a.Intersect(b).Count();

		// Calculate overall score
		return (prefixMatch * 2) + suffixMatch + sharedChars;
	}

	/// <summary>
	/// Merges values from multiple properties into a single value.
	/// </summary>
	/// <param name="properties">The properties to merge.</param>
	/// <returns>The merged value.</returns>
	private static object MergePropertyValues(IEnumerable<KeyValuePair<string, object>> properties)
	{
		List<KeyValuePair<string, object>> propertiesList = [.. properties];
		if (propertiesList.Count == 0)
		{
			return string.Empty;
		}

		if (propertiesList.Count == 1)
		{
			return propertiesList[0].Value;
		}

		// Check if all values are lists/arrays
		bool allLists = propertiesList.All(p => (p.Value is IEnumerable && p.Value is not string) || (p.Value is string s && s.Contains(',')) || p.Value is Array);

		if (allLists)
		{
			// Merge lists
			HashSet<object> mergedSet = [];

			foreach (var property in propertiesList)
			{
				if (property.Value is IEnumerable list and not string)
				{
					foreach (object item in list)
					{
						mergedSet.Add(item);
					}
				}
				else if (property.Value is Array array)
				{
					foreach (object item in array)
					{
						mergedSet.Add(item);
					}
				}
				else if (property.Value is string s && s.Contains(','))
				{
					// Split comma-separated string
					foreach (string item in s.Split(',').Select(x => x.Trim()))
					{
						if (!string.IsNullOrWhiteSpace(item))
						{
							mergedSet.Add(item);
						}
					}
				}
				else
				{
					mergedSet.Add(property.Value);
				}
			}

			return mergedSet.ToArray();
		}

		// If not all lists, take the most specific (non-null, non-empty) value
		foreach (var property in propertiesList)
		{
			if (property.Value != null &&
				(property.Value is not string s || !string.IsNullOrWhiteSpace(s)))
			{
				return property.Value;
			}
		}

		// If all values are null or empty, return the first one
		return propertiesList[0].Value;
	}

	[GeneratedRegex("(date|time|when)([-_].*)?", RegexOptions.IgnoreCase)] private static partial Regex DatePatternRegex();
	[GeneratedRegex("(author|by|creator|contributor)([-_].*)?", RegexOptions.IgnoreCase)] private static partial Regex AuthorPatternRegex();
}
