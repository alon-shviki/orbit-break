namespace OrbitBreak.Client.Game;

/// <summary>Procedural constellation generator: wells + block grid, seeded, one tier harder each call.</summary>
public static class Constellation
{
    // ponytail: grid-based placement, Poisson-disc scatter if constellations feel too regular
    public static (List<Well> Wells, List<Block> Blocks) Generate(Random rng, int tier, double w, double h)
    {
        var wells = new List<Well>();
        var wellCount = Math.Min(1 + tier / 2, 5);
        for (var attempts = 0; wells.Count < wellCount && attempts < 200; attempts++)
        {
            var x = w * (0.15 + rng.NextDouble() * 0.70);
            var y = h * (0.22 + rng.NextDouble() * 0.36);
            if (wells.Any(o => (o.X - x) * (o.X - x) + (o.Y - y) * (o.Y - y) < 140 * 140)) continue;
            wells.Add(new Well
            {
                X = x, Y = y,
                Core = 14 + rng.NextDouble() * 6,
                Influence = 130 + 10 * Math.Min(tier, 8),
                Strength = 6e6 + 5e5 * Math.Min(tier, 8), // sim-tuned: escape speed ~700 px/s from core — wells deflect, not devour (issue #2)
            });
        }

        var blocks = new List<Block>();
        const double bw = 54, bh = 22, cellW = 64, cellH = 32;
        var density = Math.Min(0.22 + 0.05 * tier, 0.65);
        var hazardChance = Math.Min(0.02 * tier, 0.12);
        var armoredChance = Math.Min(0.08 + 0.03 * tier, 0.28);
        // movers appear from tier 4, stair-stepped in gradually (issue #4); hazards never drift
        var moverChance = tier < 4 ? 0 : Math.Min(0.05 * (tier - 3), 0.30);
        var moverSpeed = 30 + 10 * Math.Min(tier, 8);

        var cols = (int)((w - 40) / cellW);
        var xOffset = (w - cols * cellW) / 2;
        for (var row = 0; ; row++)
        {
            var y = h * 0.06 + row * cellH;
            if (y + bh > h * 0.55) break;
            for (var col = 0; col < cols; col++)
            {
                if (rng.NextDouble() > density) continue;
                var x = xOffset + col * cellW + (cellW - bw) / 2;
                var cx = x + bw / 2; var cy = y + bh / 2;
                if (wells.Any(o => (o.X - cx) * (o.X - cx) + (o.Y - cy) * (o.Y - cy) < (o.Core + 40) * (o.Core + 40)))
                    continue;

                var roll = rng.NextDouble();
                var kind = roll < hazardChance                        ? BlockKind.Hazard
                         : roll < hazardChance + 0.08                 ? BlockKind.Explosive
                         : roll < hazardChance + 0.08 + armoredChance ? BlockKind.Armored
                         : BlockKind.Standard;

                blocks.Add(new Block
                {
                    X = x, Y = y, W = bw, H = bh,
                    Kind = kind,
                    Hp = kind == BlockKind.Armored ? 3 : 1,
                    Vx = kind != BlockKind.Hazard && rng.NextDouble() < moverChance
                        ? (rng.NextDouble() < 0.5 ? -moverSpeed : moverSpeed)
                        : 0,
                });
            }
        }

        return (wells, blocks);
    }
}
