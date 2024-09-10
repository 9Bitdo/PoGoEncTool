using PKHeX.Core;
using System.Collections.Generic;
using System.Linq;
using static PKHeX.Core.Species;
using static PoGoEncTool.Core.PogoShiny;

// ReSharper disable RedundantEmptyObjectOrCollectionInitializer
// ReSharper disable CollectionNeverUpdated.Local

namespace PoGoEncTool.Core;

#if DEBUG
public static class BulkActions
{
    public static BossType Type { get; set; } = BossType.Normal;
    public static string Season { get; set; } = "Max Out";

    public static void AddRaidBosses(PogoEncounterList list)
    {
        var bosses = new List<(ushort Species, byte Form, PogoShiny Shiny, byte Tier)>
        {
            new((int)Bulbasaur, 0, Random, 1),
        };

        foreach (var enc in bosses)
        {
            var pkm = list.GetDetails(enc.Species, enc.Form);
            var boss = Type switch
            {
                BossType.Shadow => "Shadow Raid Boss",
                BossType.PowerSpot => "Power Spot Boss",
                _ => "Raid Boss"
            };

            var type = Type switch
            {
                BossType.Shadow => PogoType.RaidS,
                BossType.PowerSpot => PogoType.MaxBattle,
                _ => PogoType.Raid,
            };

            var stars = GetRaidBossTier(enc.Tier);
            var entry = new PogoEntry
            {
                Start = new PogoDate(),
                End   = new PogoDate(),
                Type = type,
                LocalizedStart = true,
                NoEndTolerance = false,
                Comment = $"{stars}-Star {boss}",
                Shiny = enc.Shiny,
            };

            // set species as available if this encounter is its debut
            if (!pkm.Available)
                pkm.Available = true;

            // set its evolutions as available as well
            var evos = EvoUtil.GetEvoSpecForms(enc.Species, enc.Form)
                .Where(z => EvoUtil.IsAllowedEvolution(enc.Species, enc.Form, z.Species, z.Form)).ToArray();

            foreach ((ushort s, byte f) in evos)
            {
                var parent = list.GetDetails(s, f);
                if (!parent.Available)
                    parent.Available = true;
            }

            pkm.Add(entry); // add the entry!
        }
    }

    private static string GetRaidBossTier(byte tier) => tier switch
    {
        1 => "One",
        2 => "Two",
        3 => "Three",
        4 => "Four",
        5 => "Five",
        _ => string.Empty,
    };

    public static void AddMonthlyRaidBosses(PogoEncounterList list)
    {
        var bosses = new List<(ushort Species, byte Form, PogoShiny Shiny, PogoDate Start, PogoDate End, bool IsMega, byte MegaForm)>
        {
            // Five-Star
            new((int)Bulbasaur, 0, Random, new PogoDate(), new PogoDate(), false, 0),

            // Mega
            new((int)Bulbasaur, 0, Random, new PogoDate(), new PogoDate(), true, 0),
        };

        foreach (var enc in bosses)
        {
            var pkm = list.GetDetails(enc.Species, enc.Form);
            var comment = "Five-Star Raid Boss";
            if (enc.IsMega)
            {
                comment = enc.MegaForm switch
                {
                    0 when enc.Species is (int)Charizard or (int)Mewtwo => $"Mega Raid Boss (Mega {(Species)enc.Species} X)",
                    1 when enc.Species is (int)Charizard or (int)Mewtwo => $"Mega Raid Boss (Mega {(Species)enc.Species} Y)",
                    _ => "Mega Raid Boss",
                };
            }

            var type = enc.Species switch
            {
                (int)Meltan or (int)Melmetal => PogoType.Raid, // only Mythicals that can be traded
                _ when SpeciesCategory.IsMythical(enc.Species) => PogoType.RaidM,
                _ => PogoType.Raid,
            };

            var entry = new PogoEntry
            {
                Start = enc.Start,
                End = enc.End,
                Type = type,
                LocalizedStart = true,
                NoEndTolerance = false,
                Comment = comment,
                Shiny = enc.Shiny,
            };

            // set species as available if this encounter is its debut
            if (!pkm.Available)
                pkm.Available = true;

            pkm.Add(entry); // add the raid entry!

            // add an accompanying GBL encounter if it has not appeared in research before, or continues to appear in the wild
            if ((!enc.IsMega) && !pkm.Data.Any(z => z.Type is PogoType.Wild or PogoType.Research or PogoType.ResearchM && z.Shiny == enc.Shiny && z.End == null))
            {
                // some Legendary and Mythical Pokmon are exempt because one of their forms have been in research, and they revert or can be changed upon transfer to HOME
                if (enc.Species is (int)Giratina or (int)Genesect)
                    continue;
                AddEncounterGBL(list, enc.Species, enc.Form, enc.Shiny, enc.Start);
            }
        }
    }

    private static void AddEncounterGBL(PogoEncounterList list, ushort species, byte form, PogoShiny shiny, PogoDate start)
    {
        var end = new PogoDate(2024, 12, 03);
        var pkm = list.GetDetails(species, form);
        var type = SpeciesCategory.IsMythical(species) ? PogoType.GBLM : PogoType.GBL;
        var entry = new PogoEntry
        {
            Start = start,
            End = end,
            Type = type,
            LocalizedStart = true,
            NoEndTolerance = false,
            Comment = $"Reward Encounter (Pokémon GO: {Season})",
            Shiny = shiny,
        };

        pkm.Add(entry); // add the GBL entry!
    }

    public static void AddNewShadows(PogoEncounterList list)
    {
        var removed = new List<(ushort Species, byte Form)>
        {
            new((int)Bulbasaur, 0),
        };

        var added = new List<(ushort Species, byte Form, PogoShiny Shiny)>
        {
            new((int)Bulbasaur, 0, Never),
        };

        // add end dates for Shadows that have been removed
        foreach ((ushort s, byte f) in removed)
        {
            var pkm = list.GetDetails(s, f);
            var entries = pkm.Data;

            foreach (var entry in entries)
            {
                if (entry is { Type: PogoType.Shadow, End: null })
                    entry.End = new PogoDate();
            }
        }

        // add new Shadows
        foreach ((ushort s, byte f, PogoShiny shiny) in added)
        {
            var pkm = list.GetDetails(s, f);
            var entry = new PogoEntry
            {
                Start = new PogoDate(),
                Shiny = shiny,
                Type = PogoType.Shadow,
                LocalizedStart = true,
                NoEndTolerance = false,
                Comment = "Team GO Rocket Grunt",
            };

            pkm.Add(entry);
        }
    }

    public enum BossType : byte
    {
        Normal = 0,
        Shadow = 1,
        PowerSpot = 2,
    }
}
#endif
