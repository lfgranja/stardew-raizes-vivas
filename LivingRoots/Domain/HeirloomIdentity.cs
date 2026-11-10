using System;
using System.Collections.Generic;

namespace LivingRoots.Domain;

/// <summary>
/// Pure crioulo identity predicates (spec 020): which item IDs and tags belong
/// to the "Sementes Crioulas" surface. No SMAPI dependency — callers do the
/// game-side lookups and pass already-resolved values.
/// </summary>
public static class HeirloomIdentity
{
    /// <summary>True for the heirloom seed item, qualified or unqualified.</summary>
    public static bool IsHeirloomSeedId(string? itemId)
    {
        return IsId(itemId, ModConstants.HeirloomItemId);
    }

    /// <summary>True for the heirloom produce item, qualified or unqualified.</summary>
    public static bool IsHeirloomProduceId(string? itemId)
    {
        return IsId(itemId, ModConstants.HeirloomProduceItemId);
    }

    /// <summary>True for the heirloom seed context tags (heirloom_seed).</summary>
    public static bool IsHeirloomSeedTags(IReadOnlyList<string>? tags)
    {
        return HasTag(tags, ModConstants.HeirloomSeedTag);
    }

    /// <summary>True for the heirloom produce context tags (heirloom_produce).</summary>
    public static bool IsHeirloomProduceTags(IReadOnlyList<string>? tags)
    {
        return HasTag(tags, ModConstants.HeirloomProduceTag);
    }

    private static bool IsId(string? itemId, string bareId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (string.Equals(itemId, bareId, StringComparison.Ordinal))
        {
            return true;
        }

        // Qualified form: "(O)LivingRoots.HeirloomParsnipSeeds" etc.
        var close = itemId.IndexOf(')');
        if (close >= 0 && close < itemId.Length - 1)
        {
            return string.Equals(itemId[(close + 1)..], bareId, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool HasTag(IReadOnlyList<string>? tags, string tag)
    {
        if (tags == null)
        {
            return false;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            if (string.Equals(tags[i], tag, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
