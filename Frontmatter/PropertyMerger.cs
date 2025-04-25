namespace ktsu.Frontmatter;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides functionality for merging similar frontmatter properties.
/// </summary>
internal static class PropertyMerger
{
	/// <summary>
	/// Cache for property merge mappings
	/// </summary>
	private static readonly ConcurrentDictionary<string, string> PropertyMergeCache = new();

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
		var propertyMappings = new Dictionary<string, string>();
		string[] frontmatterKeys = [.. frontmatter.Keys];

		// Initialize with known mappings
		foreach (string? key in frontmatterKeys)
		{
			propertyMappings[key] = GetCanonicalName(key, strategy, frontmatterKeys);
		}

		// Create a new dictionary to hold the merged properties
		var mergedFrontmatter = new Dictionary<string, object>();

		// Group properties by their canonical names and merge values
		foreach (var group in propertyMappings.GroupBy(x => x.Value, x => x.Key))
		{
			string canonicalKey = group.Key;
			var originalKeys = group.ToList();

			MergePropertyGroup(frontmatter, mergedFrontmatter, canonicalKey, originalKeys);
		}

		return mergedFrontmatter;
	}

	private static string GetCanonicalName(string key, FrontmatterMergeStrategy strategy, string[] frontmatterKeys)
	{
		// Try to find in cache first
		if (PropertyMergeCache.TryGetValue(key, out string? cachedName))
		{
			return cachedName;
		}

		string canonicalName = strategy switch
		{
			FrontmatterMergeStrategy.Conservative => GetConservativeCanonicalName(key),
			FrontmatterMergeStrategy.Aggressive => GetAggressiveCanonicalName(key, frontmatterKeys),
			FrontmatterMergeStrategy.Maximum => FindSemanticCanonicalName(key, frontmatterKeys),
			FrontmatterMergeStrategy.None => key,
			_ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Invalid merge strategy")
		};

		PropertyMergeCache.TryAdd(key, canonicalName);
		return canonicalName;
	}

	private static string GetConservativeCanonicalName(string key) =>
		PropertyMappings.All.TryGetValue(key, out string? knownName) ? knownName : key;

	private static string GetAggressiveCanonicalName(string key, string[] frontmatterKeys)
	{
		string canonicalName = FindBasicCanonicalName(key, frontmatterKeys);

		bool isKnownMapping = PropertyMappings.All.ContainsKey(key);
		bool hasExactMatch = frontmatterKeys.Any(k => k != key &&
			string.Equals(NormalizePropertyName(k), NormalizePropertyName(key), StringComparison.OrdinalIgnoreCase));
		bool isInSameCategory = IsInSameCategory(key, canonicalName);

		return !isKnownMapping && !hasExactMatch && !isInSameCategory
			? key
			: PropertyMappings.All.TryGetValue(canonicalName, out string? knownName) ? knownName : canonicalName;
	}

	private static bool IsInSameCategory(string key, string canonicalName)
	{
		var categoryMappings = new[]
		{
			PropertyMappings.Title,
			PropertyMappings.Author,
			PropertyMappings.Date,
			PropertyMappings.Tags,
			PropertyMappings.Categories,
			PropertyMappings.Description,
			PropertyMappings.Modified,
			PropertyMappings.Layout,
			PropertyMappings.Permalink
		};

		return categoryMappings.Any(mapping => mapping.ContainsKey(key) || mapping.ContainsKey(canonicalName));
	}

	private static void MergePropertyGroup(
		Dictionary<string, object> source,
		Dictionary<string, object> target,
		string canonicalKey,
		List<string> originalKeys)
	{
		// Get the first value to determine the type
		string firstKey = originalKeys[0];
		object firstValue = source[firstKey];
		if (firstValue == null)
		{
			return;
		}

		var firstType = firstValue.GetType();

		// Check if all values have the same type
		bool allSameType = originalKeys.All(k => source[k]?.GetType() == firstType);

		if (!allSameType)
		{
			// If types are different, keep all properties separate
			foreach (string key in originalKeys)
			{
				target[key] = source[key] ?? throw new InvalidOperationException($"Value for key {key} is null");
			}

			return;
		}

		// Handle array/list types specially
		if (firstValue is IList<object> || firstValue is object[])
		{
			MergeArrayValues(source, target, canonicalKey, originalKeys);
		}
		else
		{
			// For all other types, use the first value
			target[canonicalKey] = firstValue;
		}
	}

	private static void MergeArrayValues(
		Dictionary<string, object> source,
		Dictionary<string, object> target,
		string canonicalKey,
		List<string> originalKeys)
	{
		HashSet<object> mergedList = [];
		foreach (string key in originalKeys)
		{
			object? value = source[key];
			if (value is IList<object> list)
			{
				mergedList.UnionWith(list);
			}
			else if (value is object[] array)
			{
				mergedList.UnionWith(array);
			}
		}

		target[canonicalKey] = mergedList.ToArray();
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
		if (PropertyMappings.All.TryGetValue(key, out string? canonicalName))
		{
			return canonicalName;
		}

		// Remove common prefixes/suffixes and special characters
		string normalizedKey = NormalizePropertyName(key);

		// Look for exact matches after normalization
		foreach (string existingKey in existingKeys)
		{
			if (existingKey == key)
			{
				continue;
			}

			string normalizedExisting = NormalizePropertyName(existingKey);
			if (string.Equals(normalizedKey, normalizedExisting, StringComparison.OrdinalIgnoreCase))
			{
				// If the existing key is a known property, use its canonical name
				return PropertyMappings.All.TryGetValue(existingKey, out string? knownName) ? knownName : existingKey;
			}
		}

		// Look for partial matches
		foreach (string existingKey in existingKeys)
		{
			if (existingKey == key)
			{
				continue;
			}

			string normalizedExisting = NormalizePropertyName(existingKey);
			if (normalizedKey.Contains(normalizedExisting, StringComparison.OrdinalIgnoreCase) ||
				normalizedExisting.Contains(normalizedKey, StringComparison.OrdinalIgnoreCase))
			{
				// If the existing key is a known property, use its canonical name
				return PropertyMappings.All.TryGetValue(existingKey, out string? knownName) ? knownName : existingKey;
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
		// First try basic matching (which includes checking PropertyMappings.All)
		string basicMatch = FindBasicCanonicalName(key, existingKeys);
		if (basicMatch != key)
		{
			return basicMatch;
		}

		// Then try more aggressive matching using word similarity
		string[] keyWords = NormalizePropertyName(key).Split(['-', ' ', '_'], StringSplitOptions.RemoveEmptyEntries);

		// Find best match among existing keys
		var bestMatch = existingKeys
			.Select(existingKey => new
			{
				Key = existingKey,
				Score = CalculateWordMatchScore(
					keyWords,
					NormalizePropertyName(existingKey).Split(['-', ' ', '_'], StringSplitOptions.RemoveEmptyEntries)
				)
			})
			.Where(match => match.Score > 0)
			.OrderByDescending(match => match.Score)
			.FirstOrDefault();

		return bestMatch?.Key ?? key;
	}

	private static string NormalizePropertyName(string key)
	{
		// Convert to lowercase and trim
		key = key.Trim().ToLowerInvariant();

		// Common prefixes and suffixes to remove
		string[] prefixes = ["page_", "post_", "meta_", "custom_", "user_", "site_"];
		string[] suffixes = ["_value", "_text", "_data", "_info", "_meta", "_field"];

		// Remove prefixes
		foreach (string prefix in prefixes)
		{
			if (key.StartsWith(prefix, StringComparison.Ordinal))
			{
				key = key[prefix.Length..];
				break;
			}
		}

		// Remove suffixes
		foreach (string suffix in suffixes)
		{
			if (key.EndsWith(suffix, StringComparison.Ordinal))
			{
				key = key[..^suffix.Length];
				break;
			}
		}

		// Replace special characters with underscores and remove duplicates
		return string.Join("_", key.Split(['-', ' ', '_'], StringSplitOptions.RemoveEmptyEntries));
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
