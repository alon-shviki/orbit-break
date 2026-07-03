using OrbitBreak.Client.Game;

namespace OrbitBreak.Tests;

public class EngineTests
{
    private static Engine NewEngine(int seed = 1)
    {
        var e = new Engine { Width = 800, Height = 600 };
        e.Reset(seed);
        return e;
    }

    [Fact]
    public void Generator_IsDeterministicPerSeed()
    {
        var (wellsA, blocksA) = Constellation.Generate(new Random(7), 3, 800, 600);
        var (wellsB, blocksB) = Constellation.Generate(new Random(7), 3, 800, 600);

        Assert.Equal(wellsA.Count, wellsB.Count);
        Assert.Equal(blocksA.Count, blocksB.Count);
        for (var i = 0; i < wellsA.Count; i++)
        {
            Assert.Equal(wellsA[i].X, wellsB[i].X);
            Assert.Equal(wellsA[i].Y, wellsB[i].Y);
        }
        for (var i = 0; i < blocksA.Count; i++)
        {
            Assert.Equal(blocksA[i].X, blocksB[i].X);
            Assert.Equal(blocksA[i].Kind, blocksB[i].Kind);
        }
    }

    [Fact]
    public void GravityWell_PullsBallTowardIt_AndCreditsCombo()
    {
        var e = NewEngine();
        e.Wells = new List<Well> { new() { X = 400, Y = 300, Core = 15, Influence = 200, Strength = 2.6e7 } };
        e.Blocks.Clear();
        e.BallX = 300; e.BallY = 300; e.BallVx = 0; e.BallVy = 0;
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.True(e.BallVx > 0, "ball should accelerate toward the well on its right");
        Assert.Equal(1, e.Combo);
    }

    [Fact]
    public void BallHit_BreaksBlock_AndScores()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks = new List<Block> { new() { X = 100, Y = 100, W = 54, H = 22, Kind = BlockKind.Standard, Hp = 1 } };
        e.BallX = 95; e.BallY = 111; e.BallVx = 100; e.BallVy = 0;
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.False(e.Blocks[0].Alive);
        Assert.Equal(1, e.BlocksBroken);
        Assert.Equal(10, e.Score); // base value × 1.0 multiplier
    }

    [Fact]
    public void BallLostOffCatch_ConsumesBalls_ThenGameOver()
    {
        var e = NewEngine();
        for (var i = 0; i < Engine.StartingBalls; i++)
        {
            Assert.False(e.GameOver);
            e.Wells.Clear();
            e.Blocks.Clear();
            e.BallX = 50; e.BallY = 599; e.BallVx = 0; e.BallVy = 0; // far outside the catch zone
            e.InFlight = true;
            e.Tick(1.0 / 60);
            Assert.Equal(Engine.StartingBalls - 1 - i, e.Balls);
        }
        Assert.True(e.GameOver);
        Assert.Contains("lost", e.GameOverReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HazardReachingLauncherLine_EndsRun()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks = new List<Block>
        {
            new() { X = 300, Y = e.LauncherY - 40, W = 54, H = 22, Kind = BlockKind.Hazard, Hp = 1 },
        };
        e.BallX = e.LauncherX; e.BallY = 599; e.BallVx = 0; e.BallVy = 0; // caught → launch ends → hazard steps down
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.True(e.GameOver);
        Assert.Contains("hazard", e.GameOverReason, StringComparison.OrdinalIgnoreCase);
    }
}
