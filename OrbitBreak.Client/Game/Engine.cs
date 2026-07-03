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

public enum PowerUpKind { WidePaddle, SlowBall, ExtraBall, Sticky, HeavyBall, SplitBall, PhaseBall }

public class PowerUp
{
    public double X, Y;
    public PowerUpKind Kind;
}

public class Ball
{
    public double X, Y, Vx, Vy;
}

/// <summary>Flight-scoped ball mode from a variant pickup (issue #3); cleared when the flight ends.</summary>
public enum BallVariant { None, Heavy, Phase }

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
    public const double PowerUpChance = 0.15;   // drop roll per killed block
    public const double PowerUpFallSpeed = 130; // px/s straight down — must be caught with the paddle
    public const double PowerUpDuration = 10;   // seconds for the timed effects (wide, slow)
    public const double PowerUpR = 11;          // pickup half-size for catch/draw

    public double Width, Height;
    public double PaddleX;
    public double PaddleY => Height - 36;

    public List<Well> Wells = new();
    public List<Block> Blocks = new();
    public List<Particle> Particles = new();
    public List<PowerUp> PowerUps = new();
    public Queue<(double X, double Y)> Trail = new();

    // active power-up effects
    public double WidePaddleTime, SlowBallTime;
    public int StickyCharges;
    public double PaddleHalfWidthNow => WidePaddleTime > 0 ? PaddleHalfWidth * 1.5 : PaddleHalfWidth;

    public List<Ball> FlightBalls = new();          // all balls currently in flight (split can multiply them)
    public bool InFlight => FlightBalls.Count > 0;
    public double FlightTime;
    public BallVariant Variant;                     // heavy/phase mode, applies to every flight ball
    public bool PendingSplit;                       // split caught between flights → next launch starts doubled
    public const int MaxFlightBalls = 6;

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
        PowerUps.Clear();
        WidePaddleTime = SlowBallTime = 0;
        StickyCharges = 0;
        Trail.Clear();
        _wellsThisFlight.Clear();
        FlightBalls.Clear();
        Variant = BallVariant.None;
        PendingSplit = false;
        PaddleX = Width / 2;
        Regenerate();
    }

    public void Regenerate() => (Wells, Blocks) = Constellation.Generate(_rng, Tier, Width, Height);

    public void Launch(double vx, double vy)
    {
        if (InFlight || GameOver) return;
        FlightTime = 0;
        FlightBalls.Add(new Ball { X = PaddleX, Y = PaddleY, Vx = vx, Vy = vy });
        if (PendingSplit) { PendingSplit = false; SplitAll(); }
        _wellsThisFlight.Clear();
        Trail.Clear();
    }

    /// <summary>Every flight ball forks into two (±20° spread), capped at MaxFlightBalls.</summary>
    public void SplitAll()
    {
        const double a = 0.35;
        var (cos, sin) = (Math.Cos(a), Math.Sin(a));
        foreach (var b in FlightBalls.ToList())
        {
            if (FlightBalls.Count >= MaxFlightBalls) break;
            FlightBalls.Add(new Ball { X = b.X, Y = b.Y, Vx = b.Vx * cos - b.Vy * sin, Vy = b.Vx * sin + b.Vy * cos });
            (b.Vx, b.Vy) = (b.Vx * cos + b.Vy * sin, -b.Vx * sin + b.Vy * cos);
        }
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

        PaddleX = Math.Clamp(PaddleX + paddleAxis * PaddleSpeed * dt, PaddleHalfWidthNow, Width - PaddleHalfWidthNow);

        if (WidePaddleTime > 0) WidePaddleTime -= dt;
        if (SlowBallTime > 0) SlowBallTime -= dt;

        // power-ups fall straight down and must be caught with the paddle — even between flights
        for (var i = PowerUps.Count - 1; i >= 0; i--)
        {
            var pu = PowerUps[i];
            pu.Y += PowerUpFallSpeed * dt;
            if (pu.Y - PowerUpR > Height) { PowerUps.RemoveAt(i); continue; }
            if (pu.Y + PowerUpR >= PaddleY - PaddleHeight / 2 && pu.Y - PowerUpR <= PaddleY + PaddleHeight / 2
                && pu.X + PowerUpR >= PaddleX - PaddleHalfWidthNow && pu.X - PowerUpR <= PaddleX + PaddleHalfWidthNow)
            {
                Apply(pu.Kind);
                Emit(pu.X, pu.Y, "#facc15", 8);
                PowerUps.RemoveAt(i);
            }
        }

        if (!InFlight) return; // ball rests on the paddle (drawn at PaddleX/PaddleY), ready to launch
        FlightTime += dt;

        // an active slow-ball pickup halves the cap, which actively brakes a fast ball
        var speedCap = SlowBallTime > 0 ? MaxBallSpeed * 0.5 : MaxBallSpeed;
        var paddleTop = PaddleY - PaddleHeight / 2;

        for (var bi = FlightBalls.Count - 1; bi >= 0; bi--)
        {
            var ball = FlightBalls[bi];

            // gravity wells: inverse-square pull inside influence radius, elastic bumper at the core.
            // A phase ball ignores wells entirely — straight lines, but no well combos either.
            if (Variant != BallVariant.Phase)
                foreach (var w in Wells)
                {
                    var dx = w.X - ball.X; var dy = w.Y - ball.Y;
                    var d2 = dx * dx + dy * dy;
                    var d = Math.Sqrt(d2);
                    if (d > w.Influence || d == 0) continue;

                    if (_wellsThisFlight.Add(w)) Combo++;

                    var a = w.Strength / Math.Max(d2, 900); // floor stops the pull exploding near the core
                    ball.Vx += a * dx / d * dt;
                    ball.Vy += a * dy / d * dt;

                    if (d < w.Core + BallR)
                    {
                        var nx = -dx / d; var ny = -dy / d;
                        var dot = ball.Vx * nx + ball.Vy * ny;
                        if (dot < 0) { ball.Vx -= 2 * dot * nx; ball.Vy -= 2 * dot * ny; }
                        ball.X = w.X + nx * (w.Core + BallR + 0.5);
                        ball.Y = w.Y + ny * (w.Core + BallR + 0.5);
                    }
                }

            // cap speed: wells add velocity every frame with no natural limit (issues #10, #11)
            var ballSpeed = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
            if (ballSpeed > speedCap)
            {
                ball.Vx *= speedCap / ballSpeed;
                ball.Vy *= speedCap / ballSpeed;
            }

            var prevX = ball.X; var prevY = ball.Y;
            ball.X += ball.Vx * dt;
            ball.Y += ball.Vy * dt;

            if (bi == 0)
            {
                Trail.Enqueue((ball.X, ball.Y));
                if (Trail.Count > 24) Trail.Dequeue();
            }

            // walls bounce (left/right/top)
            if (ball.X < BallR)         { ball.X = BallR;         ball.Vx = Math.Abs(ball.Vx) * 0.98; }
            if (ball.X > Width - BallR) { ball.X = Width - BallR; ball.Vx = -Math.Abs(ball.Vx) * 0.98; }
            if (ball.Y < BallR)         { ball.Y = BallR;         ball.Vy = Math.Abs(ball.Vy) * 0.98; }

            // paddle: reflects the ball back into play — hit offset from center steers the bounce angle,
            // like classic Breakout/Arkanoid, instead of silently ending the flight.
            // Swept check: a fast ball can cross the whole ~26px paddle band in one tick, so test the
            // path travelled this frame (prev → current), not just the final position (issue #11).
            if (ball.Vy > 0
                && prevY + BallR <= PaddleY + PaddleHeight / 2  // started at/above the band's bottom
                && ball.Y + BallR >= paddleTop)                 // ended at/below the band's top
            {
                // X position at the moment the ball's bottom edge crossed the paddle's top
                var t = Math.Clamp((paddleTop - (prevY + BallR)) / Math.Max(ball.Y - prevY, 1e-9), 0, 1);
                var xAtCross = prevX + (ball.X - prevX) * t;
                if (xAtCross + BallR >= PaddleX - PaddleHalfWidthNow && xAtCross - BallR <= PaddleX + PaddleHalfWidthNow)
                {
                    if (StickyCharges > 0)
                    {
                        // sticky pickup: this contact is a guaranteed catch — the ball is absorbed;
                        // if it was the last one, the flight ends ready for a fresh aimed launch
                        StickyCharges--;
                        FlightBalls.RemoveAt(bi);
                        if (!InFlight) { EndFlight(caught: true); return; }
                        continue;
                    }
                    var offset = Math.Clamp((xAtCross - PaddleX) / PaddleHalfWidthNow, -1, 1);
                    var speed = Math.Sqrt(ball.Vx * ball.Vx + ball.Vy * ball.Vy);
                    ball.Vx = offset * speed;
                    ball.Vy = -Math.Sqrt(Math.Max(speed * speed - ball.Vx * ball.Vx, speed * speed * 0.25));
                    ball.X = xAtCross;
                    ball.Y = paddleTop - BallR - 0.5;
                    FlightTime = 0; // paddle contact proves the flight isn't orbit-trapped — recall timer restarts
                }
            }

            // past the paddle with nothing to stop it = lost; only losing the LAST ball costs a life
            if (ball.Y - BallR > PaddleY + PaddleHeight / 2)
            {
                FlightBalls.RemoveAt(bi);
                if (!InFlight) { EndFlight(caught: false); return; }
                continue;
            }

            // block collision: circle vs AABB, reflect on the deep axis, one block per ball per tick.
            // A heavy ball deals 3 damage and plows straight through without bouncing.
            foreach (var b in Blocks)
            {
                if (!b.Alive) continue;
                var cx = Math.Clamp(ball.X, b.X, b.X + b.W);
                var cy = Math.Clamp(ball.Y, b.Y, b.Y + b.H);
                var dx = ball.X - cx; var dy = ball.Y - cy;
                if (dx * dx + dy * dy > BallR * BallR) continue;

                HitBlock(b, Variant == BallVariant.Heavy ? 3 : 1);
                if (Variant != BallVariant.Heavy)
                {
                    if (Math.Abs(dx) > Math.Abs(dy))
                        ball.Vx = (dx >= 0 ? 1 : -1) * Math.Abs(ball.Vx);
                    else
                        ball.Vy = (dy >= 0 ? 1 : -1) * Math.Abs(ball.Vy);
                }
                break;
            }
        }

        if (FlightTime > MaxFlightSeconds) { EndFlight(caught: true); return; }

        // full clear advances the tier immediately, Block Breaker style — no waiting out the
        // flight in an empty arena; the balls stay in flight into the new constellation
        if (Blocks.Count > 0 && Blocks.All(b => !b.Alive))
        {
            Tier++;
            Regenerate();
        }
    }

    private void HitBlock(Block b, int damage = 1)
    {
        b.HitFlash = 0.12;
        b.Hp -= damage;
        if (b.Hp > 0) return;
        KillBlock(b);
    }

    private void KillBlock(Block b)
    {
        if (!b.Alive) return;
        b.Alive = false;
        BlocksBroken++;
        Score += (int)(b.Value * Multiplier);
        Emit(b.CenterX, b.CenterY, ColorOf(b.Kind), 10);

        if (_rng.NextDouble() < PowerUpChance)
            PowerUps.Add(new PowerUp
            {
                X = b.CenterX, Y = b.CenterY,
                Kind = (PowerUpKind)_rng.Next(7), // 4 classic power-ups + 3 ball variants (issue #3)
            });

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

    private void Apply(PowerUpKind kind)
    {
        switch (kind)
        {
            case PowerUpKind.WidePaddle: WidePaddleTime = PowerUpDuration; break;
            case PowerUpKind.SlowBall:   SlowBallTime = PowerUpDuration; break;
            case PowerUpKind.ExtraBall:  Balls++; break;
            case PowerUpKind.Sticky:     StickyCharges++; break;
            // ball variants (issue #3): heavy/phase last until the current (or next) flight ends
            case PowerUpKind.HeavyBall:  Variant = BallVariant.Heavy; break;
            case PowerUpKind.PhaseBall:  Variant = BallVariant.Phase; break;
            case PowerUpKind.SplitBall:
                if (InFlight) SplitAll();
                else PendingSplit = true; // caught between flights → next launch starts doubled
                break;
        }
    }

    private void EndFlight(bool caught)
    {
        FlightBalls.Clear();
        Variant = BallVariant.None;
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
