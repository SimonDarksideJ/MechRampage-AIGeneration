using System;
using System.Collections.Generic;
using MechRampage.Core.Entities;

namespace MechRampage.Core
{
    public class Upgrade
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public int Level { get; set; }
        public int BaseCost { get; init; }
        public float CostGrowth { get; init; } = 1.6f;
        public Action<Mech> ApplyEffect { get; init; }
        public string Description { get; init; }
        public int CurrentCost => (int)MathF.Ceiling(BaseCost * MathF.Pow(CostGrowth, Level));
    }

    public static class UpgradeCatalog
    {
        public static List<Upgrade> CreateDefaults() => new()
        {
            new Upgrade
            {
                Id = "dmg", Name = "Damage Boost", BaseCost = 100, Description = "+5 Attack Damage",
                ApplyEffect = mech => mech.AttackDamage += 5
            },
            new Upgrade
            {
                Id = "spd", Name = "Speed Boost", BaseCost = 80, Description = "+10% Move Speed",
                ApplyEffect = mech => mech.MoveSpeed *= 1.10f
            },
            new Upgrade
            {
                Id = "rng", Name = "Range Boost", BaseCost = 120, Description = "+1 Attack Range",
                ApplyEffect = mech => mech.AttackRange += 1f
            }
        };
    }
}
