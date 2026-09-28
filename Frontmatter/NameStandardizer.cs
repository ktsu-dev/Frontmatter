// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

using System.Collections.Concurrent;
using System.Linq;

/// <summary>
/// Provides functionality for standardizing frontmatter property names.
/// </summary>
internal static class NameStandardizer
{
	/// <summary>
	/// Cache for fuzzy matched property names
	/// </summary>
	private static readonly ConcurrentDictionary<string, string> PropertyNameCache = new();

	/// <summary>
	/// Standardizes frontmatter property names by mapping non-standard names to standard ones using fuzzy matching.
	/// </summary>
	/// <param name="frontmatter">The frontmatter dictionary with potentially non-standard property names.</param>
	/// <returns>A new dictionary with standardized property names.</returns>
	internal static Dictionary<string, object> StandardizePropertyNames(Dictionary<string, object> frontmatter)
	{
		// Get the array of standard property names
		string[] standardProperties = StandardOrder.PropertyNames;

		// Create fuzzy matches for properties that don't match standard names
		Dictionary<string, object> standardizedFrontmatter = [];
		KeyValuePair<string, object>[] properties = [.. frontmatter];

		for (int i = 0; i < properties.Length; i++)
		{
			KeyValuePair<string, object> property = properties[i];
			string standardName = GetStandardName(property.Key, standardProperties);

			// Two keys can resolve to the same standard name (author + creator, Title + title). Only
			// rename a key when its standard name is free: not already written, and not the exact name
			// of a key still to come. Otherwise keep the original key, so neither value is lost.
			bool isFree = standardName == property.Key ||
				(!standardizedFrontmatter.ContainsKey(standardName) &&
				!properties.Skip(i + 1).Any(later => later.Key == standardName));

			standardizedFrontmatter[isFree ? standardName : property.Key] = property.Value;
		}

		return standardizedFrontmatter;
	}

	private static string GetStandardName(string key, string[] standardProperties)
	{
		// Already a standard property name, possibly in a different case
		if (Array.Exists(standardProperties, p => string.Equals(p, key, StringComparison.OrdinalIgnoreCase)))
		{
			return key.ToLowerInvariant();
		}

		// Check if we've already processed this property name before
		if (PropertyNameCache.TryGetValue(key, out string? mappedName))
		{
			return mappedName;
		}

		// Try to find a match in known property mappings, then by removing common prefixes and
		// suffixes. If no match is found, preserve the original property name.
		string standardName = FindKnownPropertyMapping(key)
			?? FindStandardPropertyMatch(NormalizePropertyName(key), standardProperties)
			?? key;

		PropertyNameCache.TryAdd(key, standardName);
		return standardName;
	}

	private static string? FindKnownPropertyMapping(string key)
	{
		IReadOnlyDictionary<string, string> mappings = PropertyMappings.All;
		return mappings.TryGetValue(key, out string? value) ? value : null;
	}

	private static string? FindStandardPropertyMatch(string normalizedKey, string[] standardProperties)
	{
		// A key whose normalized form is empty carries nothing to match on: every non-empty standard
		// property trivially contains the empty string, so without this guard such a key is admitted
		// by the containment test below against the whole standard set and silently renamed to
		// whichever one ranks first. Keys that normalize away entirely are decoration-only ("page_",
		// "_value", "_") or whitespace, and preserving them is the only safe answer for a library
		// whose job is round-tripping frontmatter.
		if (normalizedKey.Length == 0)
		{
			return null;
		}

		// Try exact match first
		string? exactMatch = standardProperties.FirstOrDefault(p =>
			string.Equals(NormalizePropertyName(p), normalizedKey, StringComparison.OrdinalIgnoreCase));
		if (exactMatch != null)
		{
			return exactMatch;
		}

		// Try partial matches. Containment only says a standard property is plausible, so the
		// candidates it admits are ranked by fuzzy similarity rather than returning whichever one
		// StandardOrder.PropertyNames happens to list first. Ties keep the earlier property, so the
		// standard order still decides when the scores cannot.
		string? bestProperty = null;
		int bestScore = int.MinValue;

		foreach (string standardProperty in standardProperties)
		{
			string normalizedStandard = NormalizePropertyName(standardProperty);
			if (!PropertyNameNormalizer.MayMatchByContainment(normalizedKey, normalizedStandard))
			{
				continue;
			}

			if (!normalizedKey.Contains(normalizedStandard) && !normalizedStandard.Contains(normalizedKey))
			{
				continue;
			}

			int score = FuzzyRanking.Score(normalizedKey, normalizedStandard);
			if (score > bestScore)
			{
				bestProperty = standardProperty;
				bestScore = score;
			}
		}

		return bestProperty;
	}

	private static string NormalizePropertyName(string key) => PropertyNameNormalizer.Normalize(key);
}
