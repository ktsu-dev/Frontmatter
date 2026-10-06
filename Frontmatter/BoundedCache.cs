// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Frontmatter;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A thread-safe cache that holds at most <see cref="Capacity"/> entries. Once it is full, the next
/// addition empties it first, so a long-running host keeps only its recent working set rather than
/// every document it has ever processed.
/// </summary>
/// <typeparam name="TKey">The type of the cache keys.</typeparam>
/// <typeparam name="TValue">The type of the cached values.</typeparam>
internal sealed class BoundedCache<TKey, TValue>(int capacity, IEqualityComparer<TKey>? comparer = null)
	where TKey : notnull
{
	private readonly ConcurrentDictionary<TKey, TValue> entries = new(comparer ?? EqualityComparer<TKey>.Default);

	/// <summary>
	/// Gets the most entries the cache holds.
	/// </summary>
	internal int Capacity { get; } = capacity;

	/// <summary>
	/// Gets the number of entries the cache holds now.
	/// </summary>
	internal int Count => entries.Count;

	/// <summary>
	/// Gets the value cached for a key.
	/// </summary>
	/// <param name="key">The key to look up.</param>
	/// <param name="value">The cached value, if there is one.</param>
	/// <returns>true if the key is cached; otherwise, false.</returns>
	internal bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => entries.TryGetValue(key, out value);

	/// <summary>
	/// Caches a value for a key, emptying the cache first if it is full.
	/// </summary>
	/// <param name="key">The key to cache the value under.</param>
	/// <param name="value">The value to cache.</param>
	internal void Add(TKey key, TValue value)
	{
		if (entries.Count >= Capacity)
		{
			entries.Clear();
		}

		entries.TryAdd(key, value);
	}
}
