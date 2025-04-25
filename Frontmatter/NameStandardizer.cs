namespace ktsu.Frontmatter;

using System.Collections.Concurrent;

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

		foreach (var property in frontmatter)
		{
			// Skip if it's already a standard property name
			if (Array.Exists(standardProperties, p => string.Equals(p, property.Key, StringComparison.OrdinalIgnoreCase)))
			{
				standardizedFrontmatter[property.Key] = property.Value;
				continue;
			}

			// Check if we've already processed this property name before
			if (PropertyNameCache.TryGetValue(property.Key, out string? mappedName))
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

			// Find the closest matching standard property name using simple similarity
			string bestMatch = FindMostSimilarPropertyName(property.Key, standardProperties);

			PropertyNameCache.TryAdd(property.Key, bestMatch);
			standardizedFrontmatter[bestMatch] = property.Value;
		}

		return standardizedFrontmatter;
	}

	/// <summary>
	/// Finds the most similar property name from a list of standard properties
	/// </summary>
	private static string FindMostSimilarPropertyName(string propertyName, string[] standardProperties)
	{
		string bestMatch = propertyName;
		int bestScore = int.MinValue;

		foreach (string standardProperty in standardProperties)
		{
			int score = CalculateSimilarity(propertyName, standardProperty);
			if (score > bestScore)
			{
				bestScore = score;
				bestMatch = standardProperty;
			}
		}

		// Only use best match if it's similar enough
		if (bestScore > 3)
		{
			return bestMatch;
		}

		// Fall back to original property name
		return propertyName;
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
}
