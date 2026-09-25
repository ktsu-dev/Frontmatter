// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

/// <summary>
/// Normalizes frontmatter property names into a common form so that keys which differ only by
/// casing, separators, surrounding whitespace or a decorative prefix/suffix compare equal.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NameStandardizer"/> and <see cref="PropertyMerger"/> each carried their own copy of
/// this logic. The two had drifted apart: the standardizer lower-cased without trimming, stripped
/// its prefix and suffix with culture-sensitive comparisons, and replaced only the space character,
/// so a key carrying leading or trailing whitespace — a tab especially — kept it and missed the
/// prefix/suffix strip entirely. This single implementation takes the merger's behaviour, which is
/// the more robust of the two on every point of difference.
/// </para>
/// <para>
/// Trimming happens before the prefix and suffix strip so that <c>" page_title "</c> normalizes the
/// same as <c>"page_title"</c>, and <see cref="string.Trim()"/> covers every whitespace character
/// rather than the space alone. The prefix and suffix comparisons are ordinal because these are
/// fixed ASCII tokens, matched against an already invariantly-lowercased key.
/// </para>
/// </remarks>
internal static class PropertyNameNormalizer
{
	/// <summary>
	/// Decorative leading tokens that carry no meaning for matching purposes.
	/// </summary>
	private static readonly string[] Prefixes = ["page_", "post_", "meta_", "custom_", "user_", "site_"];

	/// <summary>
	/// Decorative trailing tokens that carry no meaning for matching purposes.
	/// </summary>
	private static readonly string[] Suffixes = ["_value", "_text", "_data", "_info", "_meta", "_field"];

	/// <summary>
	/// Characters treated as word separators within a property name.
	/// </summary>
	private static readonly char[] Separators = ['-', ' ', '_'];

	/// <summary>
	/// Normalizes a property name for comparison.
	/// </summary>
	/// <param name="key">The property name to normalize.</param>
	/// <returns>
	/// The name lower-cased and trimmed, with at most one decorative prefix and one decorative
	/// suffix removed, and all separator runs collapsed to a single underscore. Leading and
	/// trailing separators are dropped, so the result never begins or ends with an underscore.
	/// </returns>
	internal static string Normalize(string key)
	{
		key = key.Trim().ToLowerInvariant();

		// At most one prefix and one suffix are removed, so this is find-first-then-act rather than
		// a filter: the captured key is reassigned between the two steps.
		string? matchedPrefix = Array.Find(Prefixes, prefix => key.StartsWith(prefix, StringComparison.Ordinal));
		if (matchedPrefix is not null)
		{
			key = key[matchedPrefix.Length..];
		}

		string? matchedSuffix = Array.Find(Suffixes, suffix => key.EndsWith(suffix, StringComparison.Ordinal));
		if (matchedSuffix is not null)
		{
			key = key[..^matchedSuffix.Length];
		}

		return string.Join("_", key.Split(Separators, StringSplitOptions.RemoveEmptyEntries));
	}

	/// <summary>
	/// The shortest normalized name a containment test is allowed to match on.
	/// </summary>
	/// <remarks>
	/// Two is the smallest value that changes nothing legitimate: the shortest names anywhere in
	/// <see cref="StandardOrder.PropertyNames"/> or <see cref="PropertyMappings"/> are <c>by</c>,
	/// <c>tag</c> and <c>url</c>, and no standard property or mapping name normalizes to fewer than
	/// two characters, so nothing real is matched by containment on a single character.
	/// </remarks>
	private const int MinimumMatchableLength = 2;

	/// <summary>
	/// Whether two normalized names carry enough content to be compared by containment.
	/// </summary>
	/// <param name="first">One normalized name.</param>
	/// <param name="second">The other normalized name.</param>
	/// <returns><see langword="true"/> when a containment match between the two would be meaningful.</returns>
	/// <remarks>
	/// <para>
	/// Containment is tested in both directions, so the short side is what makes a match meaningless
	/// regardless of which argument it is. A one-character fragment is contained in every candidate
	/// that happens to use that letter, so it is admitted on an incidental letter rather than on a
	/// shared word: <c>meta_x_field</c> normalizes to <c>x</c>, and <c>next</c> contains <c>x</c>.
	/// </para>
	/// <para>
	/// This is the same failure the empty-string guards in <see cref="NameStandardizer"/> and
	/// <see cref="PropertyMerger"/> already document, one character further along — a value silently
	/// attributed to an unrelated property, and in the merger's case dropped outright. It applies to
	/// containment only: exact matching on a one-character normalized form stays available, so a
	/// genuine single-character key still pairs with another key that normalizes to the same thing.
	/// </para>
	/// </remarks>
	internal static bool MayMatchByContainment(string first, string second) =>
		first.Length >= MinimumMatchableLength && second.Length >= MinimumMatchableLength;

	/// <summary>
	/// Normalizes a property name and splits it into its constituent words.
	/// </summary>
	/// <param name="key">The property name to normalize and split.</param>
	/// <returns>The normalized name's words, with empty entries removed.</returns>
	internal static string[] NormalizeToWords(string key) =>
		Normalize(key).Split(Separators, StringSplitOptions.RemoveEmptyEntries);
}
