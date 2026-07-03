namespace OrbitBreak.Client.Game;

public enum BlockKind { Standard, Armored, Explosive, Hazard }

public class Well
{
    public double X, Y;
    public double Core;       // solid bumper radius — ball bounces off it
    public double Influence;  // pull radius
    public double Strength;   // gravity constant, accel = Strength / dist²
}

public class Block
{
    public double X, Y, W, H; // top-left
    public BlockKind Kind;
    public int Hp;
    public bool Alive = true;
    public double HitFlash;
    public double CenterX => X + W / 2;
    public double CenterY => Y + H / 2;
    public int Value => Kind switch
    {
        BlockKind.Armored   => 30,
        BlockKind.Explosive => 20,
        BlockKind.Hazard    => 50,
        _                   => 10,
    };
}

public class Particle
{
    public double X, Y, Vx, Vy, Life;
    public string Color = "#fff";
}

/// <summary>Whole simulation: ball, wells, blocks, combo, difficulty. No rendering, no interop — testable head-less.</summary>
public class Engine
{
    // ── tuning knobs (all need playtesting — see Notes/Design/Core Loop.md open questions) ──
    public const double BallR = 7;
    public const double PaddleHalfWidth = 70;   // half-width of the controllable paddle (sim-tuned, issue #2)
    public const double PaddleHeight = 12;
    public const double PaddleSpeed = 760;      // px/s, A/D or ←/→ (sim-tuned, issue #2)
    public const int    LaunchesPerTier = 5;    // constellation regenerates one tier harder every N launches
    public const int    StartingBalls = 3;
    public const double MaxFlightSeconds = 25;  // trapped-orbit recall, counts as a catch
    public const double MaxBallSpeed = 1200;    // wells can slingshot above launch speed (max 1000) but not runaway
    public const double HazardStep = 30;        // px a hazard block descends per launch
    public const double ExplosionRadius = 80;

    public double Width, Height;
    public double PaddleX;
    public double PaddleY => Height - 36;

    public List<Well> Wells = new();
    public List<Block> Blocks = new();
    public List<Particle> Particles = new();
    public Queue<(double X, double Y)> Trail = new();

    public double BallX, BallY, BallVx, BallVy;
    public bool InFlight;
    public double FlightTime;

    public int Score, BlocksBroken, Combo, Tier, Balls, Launches;
    public bool GameOver;
    public string GameOverReason = "";
    public double Shake;

    private Random _rng = new();
    private readonly HashSet<Well> _wellsThisFlight = new();

    // ponytail: multiplier = distinct wells entered this flight; approximates "per well-assisted
    // deflection" from the design doc — refine to actual deflection detection after feel-testing
    public double Multiplier => 1 + 0.5 * Combo;

    public void Reset(int seed)
    {
        _rng = new Random(seed); // seeded per run — enables a future daily-seed mode
        Score = BlocksBroken = Combo = Launches = 0;
        Tier = 1;
        Balls = StartingBalls;
        GameOver = false;
        GameOverReason = "";
        Shake = 0;
        Particles.Clear();
        Trail.Clear();
        _wellsThisFlight.Clear();
        InFlight = false;
        PaddleX = Width / 2;
        BallX = PaddleX; BallY = PaddleY; BallVx = BallVy = 0;
        Regenerate();
    }

    public void Regenerate() => (Wells, Blocks) = Constellation.Generate(_rng, Tier, Width, Height);

    public void Launch(double vx, double vy)
    {
        if (InFlight || GameOver) return;
        InFlight = true;
        FlightTime = 0;
        BallVx = vx; BallVy = vy;
        _wellsThisFlight.Clear();
        Trail.Clear();
    }

    public void Tick(double dt, double paddleAxis = 0)
    {
        dt = Math.Min(dt, 1 / 30.0); // clamp tab-switch spikes

        if (Shake > 0) Shake -= dt;

        for (var i = Particles.Count - 1; i >= 0; i--)
        {
            var p = Particles[i];
            p.Life -= dt * 2;
            if (p.Life <= 0) { Particles.RemoveAt(i); continue; }
            p.X += p.Vx * dt; p.Y += p.Vy * dt;
        }

        foreach (var b in Blocks)
            if (b.HitFlash > 0) b.HitFlash -= dt;

        if (GameOver) return;

        PaddleX = Math.Clamp(PaddleX + paddleAxis * PaddleSpeed * dt, PaddleHalfWidth, Width - PaddleHalfWidth);

        if (!InFlight)
        {
            // ball waits on the paddle, ready to launch, and slides with it
            BallX = PaddleX;
            BallY = PaddleY;
            return;
        }
        FlightTime += dt;

        // gravity wells: inverse-square pull inside influence radius, elastic bumper at the core
        foreach (var w in Wells)
        {
            var dx = w.X - BallX; var dy = w.Y - BallY;
            var d2 = dx * dx + dy * dy;
            var d = Math.Sqrt(d2);
            if (d > w.Influence || d == 0) continue;

            if (_wellsThisFlight.Add(w)) Combo++;

            var a = w.Strength / Math.Max(d2, 900); // floor stops the pull exploding near the core
            BallVx += a * dx / d * dt;
            BallVy += a * dy / d * dt;

            if (d < w.Core + BallR)
            {
                var nx = -dx / d; var ny = -dy / d;
                var dot = BallVx * nx + BallVy * ny;
                if (dot < 0) { BallVx -= 2 * dot * nx; BallVy -= 2 * dot * ny; }
                BallX = w.X + nx * (w.Core + BallR + 0.5);
                BallY = w.Y + ny * (w.Core + BallR + 0.5);
            }
        }

        // cap speed: wells add velocity every frame with no natural limit (issues #10, #11)
        var ballSpeed = Math.Sqrt(BallVx * BallVx + BallVy * BallVy);
        if (ballSpeed > MaxBallSpeed)
        {
            BallVx *= MaxBallSpeed / ballSpeed;
            BallVy *= MaxBallSpeed / ballSpeed;
        }

        var prevX = BallX; var prevY = BallY;
        BallX += BallVx * dt;
        BallY += BallVy * dt;

        Trail.Enqueue((BallX, BallY));
        if (Trail.Count > 24) Trail.Dequeue();

        // walls bounce (left/right/top)
        if (BallX < BallR)          { BallX = BallR;          BallVx = Math.Abs(BallVx) * 0.98; }
        if (BallX > Width - BallR)  { BallX = Width - BallR;  BallVx = -Math.Abs(BallVx) * 0.98; }
        if (BallY < BallR)          { BallY = BallR;          BallVy = Math.Abs(BallVy) * 0.98; }

        // paddle: reflects the ball back into play — hit offset from center steers the bounce angle,
        // like classic Breakout/Arkanoid, instead of silently ending the flight.
        // Swept check: a fast ball can cross the whole ~26px paddle band in one tick, so test the
        // path travelled this frame (prev → current), not just the final position (issue #11).
        var paddleTop = PaddleY - PaddleHeight / 2;
        if (BallVy > 0
            && prevY + BallR <= PaddleY + PaddleHeight / 2  // started at/above the band's bottom
            && BallY + BallR >= paddleTop)                  // ended at/below the band's top
        {
            // X position at the moment the ball's bottom edge crossed the paddle's top
            var t = Math.Clamp((paddleTop - (prevY + BallR)) / Math.Max(BallY - prevY, 1e-9), 0, 1);
            var xAtCross = prevX + (BallX - prevX) * t;
            if (xAtCross + BallR >= PaddleX - PaddleHalfWidth && xAtCross - BallR <= PaddleX + PaddleHalfWidth)
            {
                var offset = Math.Clamp((xAtCross - PaddleX) / PaddleHalfWidth, -1, 1);
                var speed = Math.Sqrt(BallVx * BallVx + BallVy * BallVy);
                BallVx = offset * speed;
                BallVy = -Math.Sqrt(Math.Max(speed * speed - BallVx * BallVx, speed * speed * 0.25));
                BallX = xAtCross;
                BallY = paddleTop - BallR - 0.5;
                FlightTime = 0; // paddle contact proves the ball isn't orbit-trapped — recall timer restarts
            }
        }

        // past the paddle with nothing to stop it = lost
        if (BallY - BallR > PaddleY + PaddleHeight / 2)
        {
            EndFlight(caught: false);
            return;
        }

        if (FlightTime > MaxFlightSeconds) { EndFlight(caught: true); return; }

        // block collision: circle vs AABB, reflect on the deep axis, one block per tick
        foreach (var b in Blocks)
        {
            if (!b.Alive) continue;
            var cx = Math.Clamp(BallX, b.X, b.X + b.W);
            var cy = Math.Clamp(BallY, b.Y, b.Y + b.H);
            var dx = BallX - cx; var dy = BallY - cy;
            if (dx * dx + dy * dy > BallR * BallR) continue;

            HitBlock(b);
            if (Math.Abs(dx) > Math.Abs(dy))
                BallVx = (dx >= 0 ? 1 : -1) * Math.Abs(BallVx);
            else
                BallVy = (dy >= 0 ? 1 : -1) * Math.Abs(BallVy);
            break;
        }

        // full clear advances the tier immediately, Block Breaker style — no waiting out the
        // flight in an empty arena; the ball stays in flight into the new constellation
        if (Blocks.Count > 0 && Blocks.All(b => !b.Alive))
        {
            Tier++;
            Regenerate();
        }
    }

    private void HitBlock(Block b)
    {
        b.HitFlash = 0.12;
        if (--b.Hp > 0) return;
        KillBlock(b);
    }

    private void KillBlock(Block b)
    {
        if (!b.Alive) return;
        b.Alive = false;
        BlocksBroken++;
        Score += (int)(b.Value * Multiplier);
        Emit(b.CenterX, b.CenterY, ColorOf(b.Kind), 10);

        if (b.Kind == BlockKind.Explosive)
        {
            Shake = 0.25;
            Emit(b.CenterX, b.CenterY, "#f97316", 18);
            foreach (var o in Blocks) // recursion chains explosions; Alive flag stops cycles
            {
                if (!o.Alive || o == b) continue;
                var dx = o.CenterX - b.CenterX; var dy = o.CenterY - b.CenterY;
                if (dx * dx + dy * dy < ExplosionRadius * ExplosionRadius) KillBlock(o);
            }
        }
    }

    private void EndFlight(bool caught)
    {
        InFlight = false;
        BallX = PaddleX; BallY = PaddleY; BallVx = BallVy = 0;
        Combo = 0;
        _wellsThisFlight.Clear();
        Trail.Clear();

        if (!caught)
        {
            Balls--;
            if (Balls <= 0)
            {
                GameOver = true;
                GameOverReason = "Ball missed the paddle and was lost";
                return;
            }
        }

        Launches++;
        if (Launches % LaunchesPerTier == 0 || Blocks.All(b => !b.Alive))
        {
            Tier++;
            Regenerate();
            return;
        }

        // hazards creep toward the paddle line every launch
        foreach (var b in Blocks)
        {
            if (!b.Alive || b.Kind != BlockKind.Hazard) continue;
            b.Y += HazardStep;
            if (b.Y + b.H >= PaddleY - 12)
            {
                GameOver = true;
                GameOverReason = "Hazard breached the paddle line";
            }
        }
    }

    private void Emit(double x, double y, string color, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var a = _rng.NextDouble() * Math.PI * 2;
            var s = 60 + _rng.NextDouble() * 160;
            Particles.Add(new Particle
            {
                X = x, Y = y,
                Vx = Math.Cos(a) * s, Vy = Math.Sin(a) * s,
                Life = 0.5 + _rng.NextDouble() * 0.5,
                Color = color,
            });
        }
    }

    public static string ColorOf(BlockKind k) => k switch
    {
        BlockKind.Armored   => "#7c3aed",
        BlockKind.Explosive => "#f97316",
        BlockKind.Hazard    => "#ef4444",
        _                   => "#38bdf8",
    };
}
