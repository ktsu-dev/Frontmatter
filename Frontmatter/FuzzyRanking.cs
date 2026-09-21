// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

using ktsu.FuzzySearch;

/// <summary>
/// Ranks candidate property names by approximate similarity, using <see cref="Fuzzy"/>.
/// </summary>
/// <remarks>
/// Both property-name matchers in this assembly select a candidate out of a set that a coarse
/// containment test has already admitted. Containment says only whether a candidate is plausible,
/// not how good it is, so without a score the winner is decided by iteration order -- which is why
/// <c>subtitles_track</c> used to standardize to <c>title</c> rather than <c>subtitle</c>.
/// <para>
/// <see cref="Fuzzy.Contains(System.ReadOnlySpan{char}, System.ReadOnlySpan{char}, out int)"/>
/// scores a match by rewarding consecutive characters, matches after a separator and matches on a
/// camelCase boundary, and by penalizing characters the pattern never matched. That is the ranking
/// both call sites were approximating by hand.
/// </para>
/// </remarks>
internal static class FuzzyRanking
{
	/// <summary>
	/// Scores how well two normalized property names match, ignoring which of them is longer.
	/// </summary>
	/// <param name="first">The first normalized property name.</param>
	/// <param name="second">The second normalized property name.</param>
	/// <returns>
	/// The better of the two directional scores, or <see cref="int.MinValue"/> if neither name is a
	/// subsequence of the other.
	/// </returns>
	/// <remarks>
	/// Both directions are tried because the callers' containment gates are themselves
	/// bidirectional: a candidate qualifies whether it contains the key or is contained by it.
	/// Scoring only one direction would return no score at all for half of the candidates that the
	/// gate admitted.
	/// </remarks>
	internal static int Score(string first, string second)
	{
		int best = int.MinValue;

		if (Fuzzy.Contains(first.AsSpan(), second.AsSpan(), out int forward))
		{
			best = forward;
		}

		if (Fuzzy.Contains(second.AsSpan(), first.AsSpan(), out int reverse) && reverse > best)
		{
			best = reverse;
		}

		return best;
	}
}
