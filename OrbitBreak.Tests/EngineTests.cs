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

        Assert.Equal(1, e.BlocksBroken);
        Assert.Equal(10, e.Score); // base value × 1.0 multiplier
        // that was the last block — full clear advances the tier mid-flight, Block Breaker style
        Assert.Equal(2, e.Tier);
        Assert.Contains(e.Blocks, b => b.Alive);
    }

    [Fact]
    public void PaddleBounce_ResetsOrbitTrapRecallTimer()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 400;
        e.BallX = 400; e.BallY = e.PaddleY - 1; e.BallVx = 0; e.BallVy = 200;
        e.FlightTime = Engine.MaxFlightSeconds - 1; // one second from forced recall
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.True(e.InFlight);
        Assert.True(e.FlightTime < 1, "paddle contact should restart the recall timer — only genuinely trapped orbits get recalled");
    }

    [Fact]
    public void BallMissesPaddle_ConsumesBalls_ThenGameOver()
    {
        var e = NewEngine();
        for (var i = 0; i < Engine.StartingBalls; i++)
        {
            Assert.False(e.GameOver);
            e.Wells.Clear();
            e.Blocks.Clear();
            e.BallX = 50; e.BallY = 599; e.BallVx = 0; e.BallVy = 0; // already past the paddle line
            e.InFlight = true;
            e.Tick(1.0 / 60);
            Assert.Equal(Engine.StartingBalls - 1 - i, e.Balls);
        }
        Assert.True(e.GameOver);
        Assert.Contains("lost", e.GameOverReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BallBouncesOffPaddle_ReflectsUpward_WithoutLosingABall()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 400;
        e.BallX = 400; e.BallY = e.PaddleY - 1; e.BallVx = 0; e.BallVy = 200; // falling into paddle center
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.True(e.InFlight, "a paddle hit should keep the flight going, not end it");
        Assert.True(e.BallVy < 0, "ball should now be moving upward");
        Assert.Equal(Engine.StartingBalls, e.Balls);
    }

    [Fact]
    public void FastBall_StillBouncesOffPaddle_InsteadOfTunneling()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 400;
        // at the dt clamp ceiling (1/30s) a max-speed ball moves 40px — more than the whole
        // ~26px paddle band, so the old post-move position check never saw the paddle
        e.BallX = 400; e.BallY = e.PaddleY - 45; e.BallVx = 0; e.BallVy = Engine.MaxBallSpeed;
        e.InFlight = true;

        e.Tick(1.0 / 30);

        Assert.True(e.InFlight, "ball crossed the paddle band this tick and must bounce, not tunnel");
        Assert.True(e.BallVy < 0, "ball should be moving upward after the bounce");
        Assert.Equal(Engine.StartingBalls, e.Balls);
    }

    [Fact]
    public void BallSpeed_IsCappedAtMaxBallSpeed()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.BallX = 400; e.BallY = 300; e.BallVx = 0; e.BallVy = -Engine.MaxBallSpeed * 3;
        e.InFlight = true;

        e.Tick(1.0 / 60);

        var speed = Math.Sqrt(e.BallVx * e.BallVx + e.BallVy * e.BallVy);
        Assert.True(speed <= Engine.MaxBallSpeed * 1.001, $"speed {speed} exceeds cap");
    }

    [Fact]
    public void PowerUp_CaughtByPaddle_AppliesEffect()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 400;
        e.PowerUps.Add(new PowerUp { X = 400, Y = e.PaddleY - 20, Kind = PowerUpKind.ExtraBall });

        for (var i = 0; i < 30 && e.PowerUps.Count > 0; i++) e.Tick(1.0 / 60);

        Assert.Empty(e.PowerUps);
        Assert.Equal(Engine.StartingBalls + 1, e.Balls);
    }

    [Fact]
    public void PowerUp_MissedByPaddle_FallsOffScreen()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 100; // far from the drop
        e.PowerUps.Add(new PowerUp { X = 700, Y = e.PaddleY - 20, Kind = PowerUpKind.ExtraBall });

        for (var i = 0; i < 120; i++) e.Tick(1.0 / 60);

        Assert.Empty(e.PowerUps);
        Assert.Equal(Engine.StartingBalls, e.Balls); // nothing applied
    }

    [Fact]
    public void StickyPaddle_TurnsNextContactIntoACatch()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 400;
        e.StickyCharges = 1;
        e.BallX = 400; e.BallY = e.PaddleY - 1; e.BallVx = 0; e.BallVy = 200;
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.False(e.InFlight, "sticky contact should end the flight as a catch, ready to re-aim");
        Assert.Equal(Engine.StartingBalls, e.Balls); // a catch, not a loss
        Assert.Equal(0, e.StickyCharges);
    }

    [Fact]
    public void WidePaddle_WidensTheCatchZone()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 400;
        e.WidePaddleTime = Engine.PowerUpDuration;
        // outside the normal ±70 half-width, inside the widened ±105
        e.BallX = 495; e.BallY = e.PaddleY - 1; e.BallVx = 0; e.BallVy = 200;
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.True(e.InFlight);
        Assert.True(e.BallVy < 0, "widened paddle should have caught a ball the normal paddle misses");
    }

    [Fact]
    public void KillingManyBlocks_DropsAtLeastOnePowerUp()
    {
        var e = NewEngine(seed: 1);
        e.Wells.Clear();
        // explosive at the center of a tight cluster — the chain kills all of them, rolling
        // the seeded 15% drop chance ~30 times (P(no drop) ≈ 0.85^30 < 1%)
        e.Blocks = new List<Block> { new() { X = 400, Y = 300, W = 10, H = 10, Kind = BlockKind.Explosive, Hp = 1 } };
        for (var i = 0; i < 30; i++)
            e.Blocks.Add(new Block { X = 380 + i, Y = 305, W = 8, H = 8, Kind = BlockKind.Standard, Hp = 1 });
        e.BallX = 395; e.BallY = 305; e.BallVx = 100; e.BallVy = 0;
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.NotEmpty(e.PowerUps);
    }

    [Fact]
    public void HazardReachingPaddleLine_EndsRun()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks = new List<Block>
        {
            new() { X = 300, Y = e.PaddleY - 40, W = 54, H = 22, Kind = BlockKind.Hazard, Hp = 1 },
        };
        // flight-timeout recall (caught, no ball lost) still advances hazards, same as a real return
        e.BallX = 400; e.BallY = 300; e.BallVx = 0; e.BallVy = 50;
        e.FlightTime = Engine.MaxFlightSeconds + 1;
        e.InFlight = true;

        e.Tick(1.0 / 60);

        Assert.True(e.GameOver);
        Assert.Contains("hazard", e.GameOverReason, StringComparison.OrdinalIgnoreCase);
    }
}
