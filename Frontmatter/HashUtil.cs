namespace ktsu.Frontmatter;

using HashDepot;

/// <summary>
/// Provides utility methods for computing hash values consistently across the application.
/// </summary>
internal static class HashUtil
{
	/// <summary>
	/// The FNV-1a prime value used in the hash algorithm.
	/// </summary>
	private const uint Fnv1aPrime = 16777619;

	/// <summary>
	/// The FNV-1a offset basis (starting value) used in the hash algorithm.
	/// </summary>
	private const uint Fnv1aOffsetBasis = 2166136261;

	/// <summary>
	/// Computes an FNV-1a hash of the specified string content.
	/// </summary>
	/// <param name="content">The string content to hash.</param>
	/// <returns>A 32-bit FNV-1a hash value.</returns>
	internal static uint ComputeHash(string content)
	{
		// FNV-1a is chosen for its efficiency, good distribution, and low collision rate for typical content
		byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
		return Fnv1a.Hash32(contentBytes);
	}

	/// <summary>
	/// Combines multiple hash values into a single hash value.
	/// </summary>
	/// <param name="hashes">The hash values to combine.</param>
	/// <returns>A combined 32-bit hash value.</returns>
	internal static uint CombineHashes(params uint[] hashes)
	{
		if (hashes.Length == 0)
		{
			return 0;
		}

		// Start with the basis value for FNV-1a
		uint result = Fnv1aOffsetBasis;

		// Combine using FNV-1a algorithm multiplication and XOR
		foreach (uint hash in hashes)
		{
			// Convert hash to bytes
			byte[] bytes = BitConverter.GetBytes(hash);

			// Apply FNV-1a to each byte
			foreach (byte b in bytes)
			{
				result = (result * Fnv1aPrime) ^ b;
			}
		}

		return result;
	}

	/// <summary>
	/// Creates a cache key by combining a base content hash with option flags.
	/// </summary>
	/// <param name="content">The main content to hash.</param>
	/// <param name="options">Option values to combine with the content hash.</param>
	/// <returns>A combined cache key.</returns>
	internal static uint CreateCacheKey(string content, params uint[] options)
	{
		uint contentHash = ComputeHash(content);
		return CombineHashes([contentHash, .. options]);
	}
}
