// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Provides per-type serialization and deserialization support for <see cref="HybridCache"/>.
/// </summary>
/// <typeparam name="T">The type being serialized/deserialized.</typeparam>
public interface IHybridCacheSerializer<T>
{
    /// <summary>
    /// Deserializes a <typeparamref name="T"/> value from the provided <paramref name="source"/>.
    /// </summary>
    T Deserialize(ReadOnlySequence<byte> source);

    /// <summary>
    /// Serializes <paramref name="value"/> to the provided <paramref name="target"/>.
    /// </summary>
    void Serialize(T value, IBufferWriter<byte> target);
}
