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

    /// <summary>Puts a single ball in flight at the given position/velocity.</summary>
    private static Ball Fly(Engine e, double x, double y, double vx, double vy)
    {
        var ball = new Ball { X = x, Y = y, Vx = vx, Vy = vy };
        e.FlightBalls.Add(ball);
        return ball;
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
        e.Wells = new List<Well> { new() { X = 400, Y = 300, Core = 15, Influence = 200, Strength = 6e6 } };
        e.Blocks.Clear();
        var ball = Fly(e, 300, 300, 0, 0);

        e.Tick(1.0 / 60);

        Assert.True(ball.Vx > 0, "ball should accelerate toward the well on its right");
        Assert.Equal(1, e.Combo);
    }

    [Fact]
    public void BallHit_BreaksBlock_AndScores()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks = new List<Block> { new() { X = 100, Y = 100, W = 54, H = 22, Kind = BlockKind.Standard, Hp = 1 } };
        Fly(e, 95, 111, 100, 0);

        e.Tick(1.0 / 60);

        Assert.Equal(1, e.BlocksBroken);
        Assert.Equal(10, e.Score); // base value × 1.0 multiplier
        // that was the last block — full clear advances the tier mid-flight, Block Breaker style
        Assert.Equal(2, e.Tier);
        Assert.Contains(e.Blocks, b => b.Alive);
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
            Fly(e, 50, 599, 0, 0); // already past the paddle line
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
        var ball = Fly(e, 400, e.PaddleY - 1, 0, 200); // falling into paddle center

        e.Tick(1.0 / 60);

        Assert.True(e.InFlight, "a paddle hit should keep the flight going, not end it");
        Assert.True(ball.Vy < 0, "ball should now be moving upward");
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
        // ~26px paddle band, so a post-move position check would never see the paddle
        var ball = Fly(e, 400, e.PaddleY - 45, 0, Engine.MaxBallSpeed);

        e.Tick(1.0 / 30);

        Assert.True(e.InFlight, "ball crossed the paddle band this tick and must bounce, not tunnel");
        Assert.True(ball.Vy < 0, "ball should be moving upward after the bounce");
        Assert.Equal(Engine.StartingBalls, e.Balls);
    }

    [Fact]
    public void BallSpeed_IsCappedAtMaxBallSpeed()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        var ball = Fly(e, 400, 300, 0, -Engine.MaxBallSpeed * 3);

        e.Tick(1.0 / 60);

        var speed = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
        Assert.True(speed <= Engine.MaxBallSpeed * 1.001, $"speed {speed} exceeds cap");
    }

    [Fact]
    public void PaddleBounce_ResetsOrbitTrapRecallTimer()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 400;
        Fly(e, 400, e.PaddleY - 1, 0, 200);
        e.FlightTime = Engine.MaxFlightSeconds - 1; // one second from forced recall

        e.Tick(1.0 / 60);

        Assert.True(e.InFlight);
        Assert.True(e.FlightTime < 1, "paddle contact should restart the recall timer — only genuinely trapped orbits get recalled");
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
        Fly(e, 400, e.PaddleY - 1, 0, 200);

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
        var ball = Fly(e, 495, e.PaddleY - 1, 0, 200);

        e.Tick(1.0 / 60);

        Assert.True(e.InFlight);
        Assert.True(ball.Vy < 0, "widened paddle should have caught a ball the normal paddle misses");
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
        Fly(e, 395, 305, 100, 0);

        e.Tick(1.0 / 60);

        Assert.NotEmpty(e.PowerUps);
    }

    [Fact]
    public void SplitPickup_MidFlight_ForksEveryBall()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 400;
        Fly(e, 400, 100, 0, -10); // one ball high up, out of the way
        e.PowerUps.Add(new PowerUp { X = 400, Y = e.PaddleY - 20, Kind = PowerUpKind.SplitBall });

        for (var i = 0; i < 30 && e.PowerUps.Count > 0; i++) e.Tick(1.0 / 60);

        Assert.Empty(e.PowerUps);
        Assert.Equal(2, e.FlightBalls.Count);
    }

    [Fact]
    public void LosingOneOfSeveralBalls_KeepsTheFlightAlive_AndCostsNoLife()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks.Clear();
        e.PaddleX = 100; // far away — the falling ball will be missed
        Fly(e, 700, 599, 0, 50);  // about to be lost
        Fly(e, 400, 100, 0, -10); // safely up top

        e.Tick(1.0 / 60);

        Assert.True(e.InFlight, "one ball is still flying");
        Assert.Single(e.FlightBalls);
        Assert.Equal(Engine.StartingBalls, e.Balls); // only losing the LAST ball costs a life
    }

    [Fact]
    public void HeavyBall_OneShotsArmored_AndPlowsThroughWithoutBouncing()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks = new List<Block>
        {
            new() { X = 100, Y = 100, W = 54, H = 22, Kind = BlockKind.Armored, Hp = 3 },
            new() { X = 300, Y = 300, W = 54, H = 22, Kind = BlockKind.Standard, Hp = 1 }, // keeps the board non-empty
        };
        e.Variant = BallVariant.Heavy;
        var ball = Fly(e, 95, 111, 100, 0);

        e.Tick(1.0 / 60);

        Assert.False(e.Blocks[0].Alive, "heavy ball should one-shot a 3hp armored block");
        Assert.True(ball.Vx > 0, "heavy ball plows through instead of bouncing back");
    }

    [Fact]
    public void PhaseBall_IgnoresGravityWells_AndEarnsNoCombo()
    {
        var e = NewEngine();
        e.Wells = new List<Well> { new() { X = 400, Y = 300, Core = 15, Influence = 200, Strength = 6e6 } };
        e.Blocks.Clear();
        e.Variant = BallVariant.Phase;
        var ball = Fly(e, 300, 300, 0, -100);

        e.Tick(1.0 / 60);

        Assert.Equal(0, ball.Vx); // no sideways pull
        Assert.Equal(0, e.Combo);
    }

    [Fact]
    public void Generator_SpawnsMoversOnlyFromTierFour()
    {
        for (var seed = 1; seed <= 10; seed++)
        {
            var (_, lowTier) = Constellation.Generate(new Random(seed), 3, 800, 600);
            Assert.DoesNotContain(lowTier, b => b.Vx != 0);
        }
        // over several seeds tier 6 (15% mover chance) should produce at least one mover
        var found = false;
        for (var seed = 1; seed <= 10 && !found; seed++)
        {
            var (_, highTier) = Constellation.Generate(new Random(seed), 6, 800, 600);
            found = highTier.Any(b => b.Vx != 0);
            Assert.DoesNotContain(highTier, b => b.Vx != 0 && b.Kind == BlockKind.Hazard);
        }
        Assert.True(found, "tier 6 constellations should contain moving blocks");
    }

    [Fact]
    public void MovingBlock_DriftsAndBouncesOffTheSideWall()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks = new List<Block>
        {
            new() { X = 30, Y = 100, W = 54, H = 22, Kind = BlockKind.Standard, Hp = 1, Vx = -100 },
            new() { X = 300, Y = 300, W = 54, H = 22, Kind = BlockKind.Standard, Hp = 1 },
        };

        for (var i = 0; i < 30; i++) e.Tick(1.0 / 60); // half a second, drifting left into the wall

        Assert.True(e.Blocks[0].Vx > 0, "block should have bounced off the left wall and reversed");
        Assert.True(e.Blocks[0].X >= 20);
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
        Fly(e, 400, 300, 0, 50);
        e.FlightTime = Engine.MaxFlightSeconds + 1;

        e.Tick(1.0 / 60);

        Assert.True(e.GameOver);
        Assert.Contains("hazard", e.GameOverReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KillingABlock_SpawnsScorePopup_ThatRisesAndExpires()
    {
        var e = NewEngine();
        e.Wells.Clear();
        e.Blocks = new List<Block>
        {
            new() { X = 100, Y = 100, W = 54, H = 22, Kind = BlockKind.Standard, Hp = 1 },
            new() { X = 700, Y = 100, W = 54, H = 22, Kind = BlockKind.Standard, Hp = 3 }, // keeps the tier from clearing
        };
        Fly(e, 95, 111, 100, 0);

        e.Tick(1.0 / 60);

        var popup = Assert.Single(e.Popups);
        Assert.Equal("+10", popup.Text); // base value × 1.0 multiplier
        var y0 = popup.Y;

        e.Tick(1.0 / 60);
        Assert.True(popup.Y < y0, "popup should rise");

        for (var i = 0; i < 90; i++) e.Tick(1.0 / 60); // 1.5s > 1s life
        Assert.Empty(e.Popups);
    }

    [Fact]
    public void EnteringAWell_SpawnsComboCallout()
    {
        var e = NewEngine();
        e.Wells = new List<Well> { new() { X = 400, Y = 300, Core = 15, Influence = 200, Strength = 6e6 } };
        e.Blocks.Clear();
        Fly(e, 300, 300, 0, 0);

        e.Tick(1.0 / 60);

        var popup = Assert.Single(e.Popups, p => p.Big);
        Assert.Equal("COMBO x1.5", popup.Text);
    }

    [Fact]
    public void ZenMode_LostBall_CostsNothing_AndNeverEndsTheRun()
    {
        var e = new Engine { Width = 800, Height = 600 };
        e.Reset(1, GameMode.Zen);
        e.Wells.Clear();
        Fly(e, 400, e.PaddleY + 30, 0, 400); // already past the paddle, heading out

        e.Tick(1.0 / 60);

        Assert.False(e.GameOver);
        Assert.Equal(Engine.StartingBalls, e.Balls); // no life lost
        Assert.Equal(1, e.Launches);                 // flight still ended and counted
    }

    [Fact]
    public void ZenMode_HazardAtPaddleLine_ParksInsteadOfEndingRun()
    {
        var e = new Engine { Width = 800, Height = 600 };
        e.Reset(1, GameMode.Zen);
        e.Wells.Clear();
        e.Blocks = new List<Block>
        {
            new() { X = 300, Y = e.PaddleY - 40, W = 54, H = 22, Kind = BlockKind.Hazard, Hp = 1 },
        };
        Fly(e, 400, 300, 0, 50);
        e.FlightTime = Engine.MaxFlightSeconds + 1; // recall ends the flight, advancing hazards

        e.Tick(1.0 / 60);

        Assert.False(e.GameOver);
        Assert.True(e.Blocks[0].Y + e.Blocks[0].H < e.PaddleY - 12 + 1, "hazard should park at the line");
    }

    [Fact]
    public void TimeAttack_EndsWhenTheClockRunsOut_NotWhenBallsAreLost()
    {
        var e = new Engine { Width = 800, Height = 600 };
        e.Reset(1, GameMode.TimeAttack);
        e.Wells.Clear();
        Fly(e, 400, e.PaddleY + 30, 0, 400);

        e.Tick(1.0 / 60);
        Assert.False(e.GameOver); // lost ball is free in time attack

        e.TimeLeft = 0.001;
        e.Tick(1.0 / 60);

        Assert.True(e.GameOver);
        Assert.Contains("time", e.GameOverReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, e.TimeLeft);
    }

    [Fact]
    public void NormalMode_IsTheDefault_AndTimerDoesNotTick()
    {
        var e = NewEngine();
        Assert.Equal(GameMode.Normal, e.Mode);

        var before = e.TimeLeft;
        e.Tick(1.0 / 60);
        Assert.Equal(before, e.TimeLeft);
    }
}
