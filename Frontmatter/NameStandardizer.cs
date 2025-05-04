// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
		var standardProperties = StandardOrder.PropertyNames;

		// Create fuzzy matches for properties that don't match standard names
		Dictionary<string, object> standardizedFrontmatter = [];

		foreach (var property in frontmatter)
		{
			// Skip if it's already a standard property name
			if (Array.Exists(standardProperties, p => string.Equals(p, property.Key, StringComparison.OrdinalIgnoreCase)))
			{
				standardizedFrontmatter[property.Key.ToLowerInvariant()] = property.Value;
				continue;
			}

			// Check if we've already processed this property name before
			if (PropertyNameCache.TryGetValue(property.Key, out var mappedName))
			{
				// If the mapped name is the same as the original, it means we previously determined
				// there's no good match, so keep the original
				if (mappedName == property.Key)
				{
					standardizedFrontmatter[property.Key] = property.Value;
				}
				else
				{
					// Use the previously matched property name
					standardizedFrontmatter[mappedName] = property.Value;
				}

				continue;
			}

			// Try to find a match in known property mappings
			var knownMapping = FindKnownPropertyMapping(property.Key);
			if (knownMapping != null)
			{
				PropertyNameCache.TryAdd(property.Key, knownMapping);
				standardizedFrontmatter[knownMapping] = property.Value;
				continue;
			}

			// Try to find a match by removing common prefixes and suffixes
			var normalizedKey = NormalizePropertyName(property.Key);
			var standardMatch = FindStandardPropertyMatch(normalizedKey, standardProperties);
			if (standardMatch != null)
			{
				PropertyNameCache.TryAdd(property.Key, standardMatch);
				standardizedFrontmatter[standardMatch] = property.Value;
				continue;
			}

			// If no match found, preserve the original property name
			PropertyNameCache.TryAdd(property.Key, property.Key);
			standardizedFrontmatter[property.Key] = property.Value;
		}

		return standardizedFrontmatter;
	}

	private static string? FindKnownPropertyMapping(string key)
	{
		var mappings = PropertyMappings.All;
		return mappings.TryGetValue(key, out var value) ? value : null;
	}

	private static string? FindStandardPropertyMatch(string normalizedKey, string[] standardProperties)
	{
		// Try exact match first
		var exactMatch = standardProperties.FirstOrDefault(p =>
			string.Equals(NormalizePropertyName(p), normalizedKey, StringComparison.OrdinalIgnoreCase));
		if (exactMatch != null)
		{
			return exactMatch;
		}

		// Try partial matches
		foreach (var standardProperty in standardProperties)
		{
			var normalizedStandard = NormalizePropertyName(standardProperty);
			if (normalizedKey.Contains(normalizedStandard) || normalizedStandard.Contains(normalizedKey))
			{
				return standardProperty;
			}
		}

		return null;
	}

	private static string NormalizePropertyName(string key)
	{
		// Convert to lowercase
		key = key.ToLowerInvariant();

		// Remove common prefixes
		string[] prefixes = ["page_", "post_", "meta_", "custom_", "user_", "site_"];
		foreach (var prefix in prefixes)
		{
			if (key.StartsWith(prefix))
			{
				key = key[prefix.Length..];
				break;
			}
		}

		// Remove common suffixes
		string[] suffixes = ["_value", "_text", "_data", "_info", "_meta", "_field"];
		foreach (var suffix in suffixes)
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
}
