using System;

namespace PKHeX.Core;

/// <summary>
/// Modifications using <see cref="BatchInfo"/> legality.
/// </summary>
internal static class BatchModifications
{
    private static bool IsAll(ReadOnlySpan<char> p) => p.EndsWith("All", StringComparison.OrdinalIgnoreCase);
    private static bool IsNone(ReadOnlySpan<char> p) => p.EndsWith("None", StringComparison.OrdinalIgnoreCase);

    public static ModifyResult SetSuggestedMoveset(BatchInfo info, bool random = false)
    {
        Span<ushort> moves = stackalloc ushort[4];
        info.Legality.GetMoveSet(moves, random);
        return SetMoves(info.Entity, moves);
    }

    public static ModifyResult SetSuggestedRelearnData(BatchInfo info, ReadOnlySpan<char> propValue)
    {
        var pk = info.Entity;
        if (pk is ITechRecord t)
        {
            t.ClearRecordFlags();
            if (IsAll(propValue))
            {
                t.SetRecordFlags(); // all
            }
            else if (!IsNone(propValue))
            {
                Span<ushort> moves = stackalloc ushort[4];
                pk.GetMoves(moves);
                t.SetRecordFlags(moves); // whatever fit the current moves
            }
        }

        pk.SetRelearnMoves(info.Legality);
        return ModifyResult.Modified;
    }

    public static ModifyResult SetSuggestedMasteryData(BatchInfo info, ReadOnlySpan<char> propValue)
    {
        var pk = info.Entity;
        if (pk is not IMoveShop8Mastery t)
            return ModifyResult.Invalid;

        t.ClearMoveShopFlags();
        if (IsNone(propValue))
            return ModifyResult.Modified;

        var e = info.Legality.EncounterMatch;
        if (e is IMasteryInitialMoveShop8 enc)
            enc.SetInitialMastery(pk);
        if (IsAll(propValue))
            t.SetMoveShopFlagsAll(pk);
        else
            t.SetMoveShopFlags(pk);
        return ModifyResult.Modified;
    }

    public static ModifyResult SetSuggestedRibbons(BatchInfo info, ReadOnlySpan<char> value)
    {
        if (IsNone(value))
            RibbonApplicator.RemoveAllValidRibbons(info.Legality);
        else // All
            RibbonApplicator.SetAllValidRibbons(info.Legality);
        return ModifyResult.Modified;
    }

    public static ModifyResult SetSuggestedMetData(BatchInfo info)
    {
        var pk = info.Entity;
        var encounter = EncounterSuggestion.GetSuggestedMetInfo(pk);
        if (encounter == null)
            return ModifyResult.Error;

        int level = encounter.LevelMin;
        int location = encounter.Location;
        int minimumLevel = EncounterSuggestion.GetLowestLevel(pk, encounter.LevelMin);

        pk.Met_Level = level;
        pk.Met_Location = location;
        pk.CurrentLevel = Math.Max(minimumLevel, level);

        return ModifyResult.Modified;
    }

    public static ModifyResult SetMinimumCurrentLevel(BatchInfo info)
    {
        var result = EncounterSuggestion.IterateMinimumCurrentLevel(info.Entity, info.Legal);
        return result ? ModifyResult.Modified : ModifyResult.Filtered;
    }

    /// <summary>
    /// Sets the provided moves in a random order.
    /// </summary>
    /// <param name="pk">Pokémon to modify.</param>
    /// <param name="moves">Moves to apply.</param>
    public static ModifyResult SetMoves(PKM pk, ReadOnlySpan<ushort> moves)
    {
        pk.SetMoves(moves);
        pk.HealPP();
        return ModifyResult.Modified;
    }

    public static ModifyResult SetEVs(PKM pk)
    {
        Span<int> evs = stackalloc int[6];
        EffortValues.SetMax(evs, pk);
        pk.SetEVs(evs);
        return ModifyResult.Modified;
    }

    /// <summary>
    /// Sets the contests stats as requested.
    /// </summary>
    /// <param name="pk">Pokémon to modify.</param>
    /// <param name="la">Legality Information matched to.</param>
    /// <param name="option">Option to apply with</param>
    public static ModifyResult SetContestStats(PKM pk, LegalityAnalysis la, ReadOnlySpan<char> option)
    {
        if (option.Length != 0 && option[BatchEditing.CONST_SUGGEST.Length..] is not "0")
            pk.SetMaxContestStats(la.EncounterMatch, la.Info.EvoChainsAllGens);
        else
            pk.SetSuggestedContestStats(la.EncounterMatch, la.Info.EvoChainsAllGens);
        return ModifyResult.Modified;
    }
}
