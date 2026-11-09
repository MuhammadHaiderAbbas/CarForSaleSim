using System;
using System.Collections.Generic;
using System.Linq;

namespace CarSales.Sim
{
    /// <summary>What a road triangle is made of (one material each). Append only.</summary>
    public enum RoadSurface { Asphalt = 0, Kerb = 1, Pavement = 2, Marking = 3, Verge = 4, Island = 5 }

    public enum RoadFeatureKind { GiveWay = 0, StopLine = 1, Zebra = 2, RoundaboutEntry = 3 }

    public struct RoadVertex
    {
        public float X, Y, Z, U, V;
        public RoadVertex(float x, float y, float z, float u, float v) { X = x; Y = y; Z = z; U = u; V = v; }
    }

    /// <summary>One mesh: a stretch of road between junctions, a junction or a roundabout.</summary>
    public sealed class RoadChunk
    {
        public const int SurfaceCount = 6;
        public string Name;
        public readonly List<RoadVertex> Vertices = new List<RoadVertex>();
        public readonly List<int>[] Triangles = Enumerable.Range(0, SurfaceCount).Select(_ => new List<int>()).ToArray();

        public int TriangleCount(RoadSurface s) => Triangles[(int)s].Count / 3;

        int Add(RoadVertex v) { Vertices.Add(v); return Vertices.Count - 1; }

        /// <summary>
        /// Adds a triangle facing <paramref name="normal"/> (0,1,0 = up). Unity draws clockwise triangles, which is the
        /// same as Vector3.Cross(b - a, c - a) pointing along the normal; the corners are swapped when needed.
        /// </summary>
        public void Tri(RoadSurface s, RoadVertex a, RoadVertex b, RoadVertex c, float nx = 0f, float ny = 1f, float nz = 0f)
        {
            float ux = b.X - a.X, uy = b.Y - a.Y, uz = b.Z - a.Z, vx = c.X - a.X, vy = c.Y - a.Y, vz = c.Z - a.Z;
            float cx = uy * vz - uz * vy, cy = uz * vx - ux * vz, cz = ux * vy - uy * vx;
            if (cx * cx + cy * cy + cz * cz < 1e-10f) return;   // degenerate
            var t = Triangles[(int)s];
            int ia = Add(a), ib = Add(b), ic = Add(c);
            if (cx * nx + cy * ny + cz * nz >= 0f) { t.Add(ia); t.Add(ib); t.Add(ic); }
            else { t.Add(ia); t.Add(ic); t.Add(ib); }
        }

        public void Quad(RoadSurface s, RoadVertex a, RoadVertex b, RoadVertex c, RoadVertex d, float nx = 0f, float ny = 1f, float nz = 0f)
        {
            Tri(s, a, b, c, nx, ny, nz);
            Tri(s, a, c, d, nx, ny, nz);
        }

        /// <summary>Plan area of one surface (for tests).</summary>
        public float Area(RoadSurface s)
        {
            var t = Triangles[(int)s];
            double sum = 0;
            for (int i = 0; i < t.Count; i += 3)
            {
                var a = Vertices[t[i]]; var b = Vertices[t[i + 1]]; var c = Vertices[t[i + 2]];
                sum += Math.Abs((b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X)) / 2.0;
            }
            return (float)sum;
        }
    }

    /// <summary>Something the builder puts a sign or a beacon at: a give-way line, a stop line, a zebra crossing.</summary>
    public struct RoadFeature
    {
        public RoadFeatureKind Kind;
        public V2 Position;      // the middle of the line (or of the crossing)
        public V2 Facing;        // the direction traffic arrives from the line's point of view (towards the junction)
        public V2 Kerb;          // the nearside kerb point (where a sign or beacon stands)
    }

    /// <summary>A road to mesh: a centre line (corners are rounded), its width and whether it has pavements.</summary>
    public sealed class RoadInput
    {
        public string Name;
        public V2[] Points;
        public float Width = 9f;
        public bool Pavements = true;
        public bool Closed => Points != null && Points.Length > 2 && V2.Distance(Points[0], Points[Points.Length - 1]) < 1f;
    }

    /// <summary>A stretch between two junctions after rounding the bends and trimming back from the junctions.</summary>
    public sealed class RoadRun
    {
        public string Road;
        public float Width;
        public bool Pavements;
        public readonly List<V2> Centre = new List<V2>();
        public readonly List<float> Distance = new List<float>();
        public float Length => Distance.Count > 0 ? Distance[Distance.Count - 1] : 0f;
    }

    public sealed class RoadMeshSet
    {
        public readonly List<RoadChunk> Chunks = new List<RoadChunk>();
        public readonly List<RoadFeature> Features = new List<RoadFeature>();
        public readonly List<RoadRun> Runs = new List<RoadRun>();
    }

    /// <summary>
    /// Phase 14f: proper road geometry instead of boxes - a carriageway with rounded bends, kerbs and raised pavements
    /// (dropped kerbs at every site entrance), gravel verges on the country loop, junctions with rounded kerb corners,
    /// roundabouts with a kerbed island, and the white lines: centre lines (hazard lines before junctions), edge lines,
    /// give-way and stop lines, and zebra crossings. Engine-free and tested; the editor turns the chunks into meshes
    /// and keeps each road's line as a Unity Splines spline you can edit and rebuild from.
    /// </summary>
    public static class RoadMesher
    {
        public const float RoadY = 0.02f, MarkY = 0.026f, KerbHeight = 0.12f, PavementWidth = 2.4f, VergeWidth = 1.2f;
        public const float Step = 2f, CornerRadius = 6f, MaxBendOffset = 1f, TownBendRadius = 30f, LoopBendRadius = 60f;
        public const float IslandInset = 5f, RingOuter = 5.5f, RingLift = 0.003f, DropLength = 4f, DropRamp = 2f, DroppedHeight = 0.018f;
        public const float HazardLength = 30f;

        // ------------------------------------------------------------------ TOWN
        public static List<RoadInput> TownRoads() =>
            TownPlan.Roads.Select(r => new RoadInput { Name = r.Name, Points = r.Points, Width = r.Width, Pavements = r.Pavements }).ToList();

        /// <summary>Where cars drive in from the road: the front of every site (dropped kerbs there).</summary>
        public static List<V2> TownEntrances() =>
            TownPlan.Sites.Select(s => s.ToWorld((s.FX0 + s.FX1) / 2f, s.FZ0 - 2f)).ToList();

        /// <summary>Zebra crossings: the town centre, outside AUTO WORLD and by the Service Centre.</summary>
        public static readonly V2[] TownCrossings = { new V2(-215, -130), new V2(-400, -107), new V2(30, -45), new V2(-78, 86), new V2(-250, -45) };

        public static RoadMeshSet BuildTown() =>
            Build(TownRoads(), TownPlan.Roundabouts, TownPlan.LightNodes, TownEntrances(), TownCrossings);

        // ------------------------------------------------------------------ GRAPH
        sealed class Arm
        {
            public RoadInput Road;
            public V2 Dir;
            public float Half;
            public bool EndsHere;
            public float Trim;
            public bool HasEnd;
            public V2 EndP, Away;   // where its run actually ends, and the run's direction there (away from the node)
        }

        sealed class Node
        {
            public V2 P;
            public readonly List<Arm> Arms = new List<Arm>();
            public bool Roundabout, Lights;
            public float Radius;
            public bool IsJunction => Roundabout || Arms.Count >= 3 || (Arms.Count == 2 && Arms[0].Road != Arms[1].Road);
        }

        static float Cross(V2 a, V2 b) => a.x * b.z - a.z * b.x;
        static V2 Left(V2 d) => new V2(-d.z, d.x);   // counter-clockwise side seen from above (x east, z north)
        static float AngleOf(V2 d) { var a = (float)Math.Atan2(d.z, d.x); return a < 0 ? a + (float)(2 * Math.PI) : a; }

        public static RoadMeshSet Build(IList<RoadInput> roads, IList<PlanRoundabout> roundabouts, IList<V2> lightNodes, IList<V2> entrances, IList<V2> crossings)
        {
            var set = new RoadMeshSet();
            var nodes = new List<Node>();
            Node NodeAt(V2 p)
            {
                var n = nodes.Find(x => V2.Distance(x.P, p) < 1f);
                if (n == null) { n = new Node { P = p }; nodes.Add(n); }
                return n;
            }
            foreach (var r in roads)
            {
                if (r.Points == null || r.Points.Length < 2) continue;
                int last = r.Points.Length - 1;
                for (int i = 0; i < last; i++)
                {
                    V2 a = r.Points[i], b = r.Points[i + 1];
                    if (V2.Distance(a, b) < 0.5f) continue;
                    NodeAt(a).Arms.Add(new Arm { Road = r, Dir = (b - a).Normalized, Half = r.Width / 2f, EndsHere = i == 0 && !r.Closed });
                    NodeAt(b).Arms.Add(new Arm { Road = r, Dir = (a - b).Normalized, Half = r.Width / 2f, EndsHere = i + 1 == last && !r.Closed });
                }
            }
            foreach (var rb in roundabouts ?? new PlanRoundabout[0])
            {
                var n = nodes.Find(x => V2.Distance(x.P, rb.Centre) < 1f);
                if (n == null) continue;
                n.Roundabout = true;
                n.Radius = rb.Radius;
            }
            foreach (var l in lightNodes ?? new V2[0]) { var n = nodes.Find(x => V2.Distance(x.P, l) < 1f); if (n != null) n.Lights = true; }

            // How far each arm stops short of its junction.
            foreach (var n in nodes.Where(n => n.IsJunction))
                foreach (var a in n.Arms)
                {
                    if (n.Roundabout) { float ro = n.Radius + RingOuter; a.Trim = (float)Math.Sqrt(Math.Max(0f, ro * ro - a.Half * a.Half)); continue; }
                    float t = a.Half + 1f;
                    foreach (var o in n.Arms)
                    {
                        if (o == a || V2.Dot(a.Dir, o.Dir) < -0.85f) continue;   // the same road carrying straight on
                        float sin = Math.Abs(Cross(a.Dir, o.Dir));
                        t = Math.Max(t, Math.Min(40f, (o.Half + CornerRadius) / Math.Max(0.25f, sin)));
                    }
                    a.Trim = t;
                }

            Node Find(V2 p) => nodes.Find(x => V2.Distance(x.P, p) < 1f);
            Arm ArmOf(Node n, RoadInput r, V2 dir) => n?.Arms.Where(a => a.Road == r).OrderByDescending(a => V2.Dot(a.Dir, dir)).FirstOrDefault();

            // Runs: each road split at its junctions.
            foreach (var r in roads)
            {
                if (r.Points == null || r.Points.Length < 2) continue;
                var pts = r.Points.ToList();
                if (r.Closed)
                {
                    pts.RemoveAt(pts.Count - 1);
                    int start = pts.FindIndex(p => Find(p)?.IsJunction == true);
                    if (start < 0) start = 0;
                    pts = pts.Skip(start).Concat(pts.Take(start)).ToList();
                    pts.Add(pts[0]);
                }
                var breaks = new List<int>();
                for (int i = 0; i < pts.Count; i++)
                    if (i == 0 || i == pts.Count - 1 || Find(pts[i])?.IsJunction == true) breaks.Add(i);
                for (int k = 0; k < breaks.Count - 1; k++)
                {
                    var raw = pts.GetRange(breaks[k], breaks[k + 1] - breaks[k] + 1);
                    var na = Find(raw[0]); var nb = Find(raw[raw.Count - 1]);
                    var armA = na != null && na.IsJunction ? ArmOf(na, r, (raw[1] - raw[0]).Normalized) : null;
                    var armB = nb != null && nb.IsJunction ? ArmOf(nb, r, (raw[raw.Count - 2] - raw[raw.Count - 1]).Normalized) : null;
                    var run = MakeRun(r, raw, armA?.Trim ?? 0f, armB?.Trim ?? 0f);
                    if (run == null) continue;
                    if (armA != null) { var (p, t, _) = Frame(run, 0f); armA.HasEnd = true; armA.EndP = p; armA.Away = t; }
                    if (armB != null) { var (p, t, _) = Frame(run, run.Length); armB.HasEnd = true; armB.EndP = p; armB.Away = t * -1f; }
                    set.Runs.Add(run);
                    var chunk = new RoadChunk { Name = $"{r.Name} {k + 1}" };
                    MeshRun(chunk, run, Crossovers(run, entrances));
                    MarkRun(chunk, set, run, na, armA, nb, armB, crossings);
                    set.Chunks.Add(chunk);
                }
            }

            foreach (var n in nodes.Where(n => n.IsJunction))
            {
                var chunk = new RoadChunk { Name = n.Roundabout ? $"Roundabout {n.P.x:0},{n.P.z:0}" : $"Junction {n.P.x:0},{n.P.z:0}" };
                if (n.Roundabout) MeshRoundabout(chunk, n);
                else MeshJunction(chunk, n);
                set.Chunks.Add(chunk);
            }
            return set;
        }

        // ------------------------------------------------------------------ CENTRE LINES
        /// <summary>Rounds the bends (a curve no more than <see cref="MaxBendOffset"/> inside the corner), samples it every <see cref="Step"/> and trims both ends.</summary>
        static RoadRun MakeRun(RoadInput r, List<V2> raw, float trimStart, float trimEnd)
        {
            float pref = r.Pavements ? TownBendRadius : LoopBendRadius;
            var dense = new List<V2> { raw[0] };
            V2 cursor = raw[0];
            for (int i = 1; i < raw.Count; i++)
            {
                V2 corner = raw[i];
                V2 next = corner;
                float L = 0f;
                if (i < raw.Count - 1)
                {
                    V2 d1 = (corner - raw[i - 1]).Normalized, d2 = (raw[i + 1] - corner).Normalized;
                    float cos = Math.Max(-1f, Math.Min(1f, V2.Dot(d1, d2)));
                    float theta = (float)Math.Acos(cos);
                    if (theta > 0.02f)
                    {
                        float half = theta / 2f;
                        L = Math.Min(pref * (float)Math.Tan(half), 2f * MaxBendOffset / Math.Max(1e-3f, (float)Math.Sin(half)));
                        L = Math.Min(L, Math.Min(0.45f * V2.Distance(raw[i - 1], corner), 0.45f * V2.Distance(corner, raw[i + 1])));
                    }
                }
                V2 dirIn = (corner - raw[i - 1]).Normalized;
                V2 lineEnd = corner - dirIn * L;
                Subdivide(dense, cursor, lineEnd);
                if (L > 0f)
                {
                    V2 dirOut = (raw[i + 1] - corner).Normalized;
                    V2 b = corner + dirOut * L;
                    int n = Math.Max(2, (int)Math.Ceiling(2f * L / Step));
                    for (int k = 1; k <= n; k++)
                    {
                        float t = k / (float)n, u = 1f - t;
                        dense.Add(lineEnd * (u * u) + corner * (2f * u * t) + b * (t * t));
                    }
                    cursor = b;
                }
                else cursor = corner;
            }
            // Trim from both ends.
            var dist = Cumulative(dense);
            float total = dist[dist.Count - 1];
            if (total - trimStart - trimEnd < 1f) return null;
            var run = new RoadRun { Road = r.Name, Width = r.Width, Pavements = r.Pavements };
            run.Centre.Add(PointAt(dense, dist, trimStart));
            for (int i = 0; i < dense.Count; i++)
                if (dist[i] > trimStart + 0.05f && dist[i] < total - trimEnd - 0.05f) run.Centre.Add(dense[i]);
            run.Centre.Add(PointAt(dense, dist, total - trimEnd));
            run.Distance.AddRange(Cumulative(run.Centre));
            return run;
        }

        static void Subdivide(List<V2> into, V2 a, V2 b)
        {
            float len = V2.Distance(a, b);
            int n = Math.Max(1, (int)Math.Ceiling(len / Step));
            for (int k = 1; k <= n; k++) into.Add(a + (b - a) * (k / (float)n));
        }

        static List<float> Cumulative(List<V2> p)
        {
            var d = new List<float> { 0f };
            for (int i = 1; i < p.Count; i++) d.Add(d[i - 1] + V2.Distance(p[i - 1], p[i]));
            return d;
        }

        static V2 PointAt(List<V2> p, List<float> d, float s)
        {
            if (s <= 0f) return p[0];
            for (int i = 1; i < p.Count; i++)
                if (d[i] >= s) { float seg = d[i] - d[i - 1]; return seg < 1e-5f ? p[i] : p[i - 1] + (p[i] - p[i - 1]) * ((s - d[i - 1]) / seg); }
            return p[p.Count - 1];
        }

        static V2 TangentAt(RoadRun run, int i)
        {
            var c = run.Centre;
            int a = Math.Max(0, i - 1), b = Math.Min(c.Count - 1, i + 1);
            return (c[b] - c[a]).Normalized;
        }

        /// <summary>The point, tangent and left normal at distance <paramref name="s"/> along a run.</summary>
        static (V2 p, V2 t, V2 n) Frame(RoadRun run, float s)
        {
            var c = run.Centre; var d = run.Distance;
            s = Math.Max(0f, Math.Min(run.Length, s));
            int i = 1;
            while (i < c.Count - 1 && d[i] < s) i++;
            float seg = d[i] - d[i - 1];
            float f = seg < 1e-5f ? 0f : (s - d[i - 1]) / seg;
            var p = c[i - 1] + (c[i] - c[i - 1]) * f;
            var t = ((TangentAt(run, i - 1) * (1f - f)) + (TangentAt(run, i) * f)).Normalized;
            return (p, t, Left(t));
        }

        // ------------------------------------------------------------------ STRIPS
        static RoadVertex Vx(V2 p, float y, float u, float v) => new RoadVertex(p.x, y, p.z, u, v);

        /// <summary>Dropped kerbs: (side, distance) where a site entrance meets the pavement.</summary>
        static List<(int side, float s)> Crossovers(RoadRun run, IList<V2> entrances)
        {
            var list = new List<(int, float)>();
            if (!run.Pavements || entrances == null) return list;
            float h = run.Width / 2f;
            foreach (var e in entrances)
            {
                int best = -1; float bd = float.MaxValue;
                for (int i = 0; i < run.Centre.Count; i++) { float d = V2.Distance(run.Centre[i], e); if (d < bd) { bd = d; best = i; } }
                if (best < 0 || bd > h + PavementWidth + 14f || bd < h - 1f) continue;
                float s = run.Distance[best];
                if (s < DropLength + DropRamp || s > run.Length - DropLength - DropRamp) continue;
                var (p, _, n) = Frame(run, s);
                list.Add((V2.Dot(e - p, n) >= 0f ? 1 : -1, s));
            }
            return list;
        }

        static float KerbAt(List<(int side, float s)> drops, int side, float s)
        {
            float k = 1f;
            foreach (var (ds, dsS) in drops)
            {
                if (ds != side) continue;
                float d = Math.Abs(s - dsS);
                float f = d <= DropLength ? 0f : d >= DropLength + DropRamp ? 1f : (d - DropLength) / DropRamp;
                k = Math.Min(k, f);
            }
            float dropped = DroppedHeight / KerbHeight;
            return dropped + (1f - dropped) * k;
        }

        static void MeshRun(RoadChunk ch, RoadRun run, List<(int side, float s)> drops)
        {
            float h = run.Width / 2f;
            var c = run.Centre; var d = run.Distance;
            // Extra samples at the dropped kerbs' ramps so the slopes are clean.
            var ss = new SortedSet<float>(d);
            foreach (var (_, s) in drops)
                foreach (float o in new[] { -DropLength - DropRamp, -DropLength, DropLength, DropLength + DropRamp })
                    if (s + o > 0f && s + o < run.Length) ss.Add(s + o);
            var samples = ss.ToList();
            var frames = samples.Select(s => Frame(run, s)).ToList();
            for (int i = 0; i < samples.Count - 1; i++)
            {
                var (p0, _, n0) = frames[i]; var (p1, _, n1) = frames[i + 1];
                float s0 = samples[i], s1 = samples[i + 1];
                ch.Quad(RoadSurface.Asphalt, Vx(p0 + n0 * h, RoadY, -h, s0), Vx(p1 + n1 * h, RoadY, -h, s1), Vx(p1 - n1 * h, RoadY, h, s1), Vx(p0 - n0 * h, RoadY, h, s0));
                foreach (int side in new[] { -1, 1 })
                {
                    var a0 = p0 + n0 * (h * side); var a1 = p1 + n1 * (h * side);
                    if (run.Pavements)
                    {
                        float y0 = RoadY + KerbHeight * KerbAt(drops, side, s0), y1 = RoadY + KerbHeight * KerbAt(drops, side, s1);
                        var b0 = p0 + n0 * ((h + PavementWidth) * side); var b1 = p1 + n1 * ((h + PavementWidth) * side);
                        var toRoad = (n0 * -side);
                        ch.Quad(RoadSurface.Kerb, Vx(a0, RoadY, s0, 0f), Vx(a1, RoadY, s1, 0f), Vx(a1, y1, s1, y1 - RoadY), Vx(a0, y0, s0, y0 - RoadY), toRoad.x, 0f, toRoad.z);
                        ch.Quad(RoadSurface.Pavement, Vx(a0, y0, 0f, s0), Vx(a1, y1, 0f, s1), Vx(b1, y1, PavementWidth, s1), Vx(b0, y0, PavementWidth, s0));
                        ch.Quad(RoadSurface.Kerb, Vx(b0, 0f, s0, 0f), Vx(b1, 0f, s1, 0f), Vx(b1, y1, s1, y1), Vx(b0, y0, s0, y0), -toRoad.x, 0f, -toRoad.z);
                    }
                    else
                    {
                        var b0 = p0 + n0 * ((h + VergeWidth) * side); var b1 = p1 + n1 * ((h + VergeWidth) * side);
                        ch.Quad(RoadSurface.Verge, Vx(a0, RoadY - 0.004f, 0f, s0), Vx(a1, RoadY - 0.004f, 0f, s1), Vx(b1, RoadY - 0.006f, VergeWidth, s1), Vx(b0, RoadY - 0.006f, VergeWidth, s0));
                    }
                }
            }
        }

        // ------------------------------------------------------------------ MARKINGS
        /// <summary>A line along the road between two distances, at a lateral offset (+ = left of travel from the run's start).</summary>
        static void MarkAlong(RoadChunk ch, RoadRun run, float s0, float s1, float lateral, float width)
        {
            if (s1 - s0 < 0.05f) return;
            var pts = new List<float> { s0 };
            pts.AddRange(run.Distance.Where(x => x > s0 && x < s1));
            pts.Add(s1);
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var (p0, _, n0) = Frame(run, pts[i]); var (p1, _, n1) = Frame(run, pts[i + 1]);
                float w = width / 2f;
                ch.Quad(RoadSurface.Marking, Vx(p0 + n0 * (lateral + w), MarkY, 0f, pts[i]), Vx(p1 + n1 * (lateral + w), MarkY, 0f, pts[i + 1]),
                    Vx(p1 + n1 * (lateral - w), MarkY, 1f, pts[i + 1]), Vx(p0 + n0 * (lateral - w), MarkY, 1f, pts[i]));
            }
        }

        static void Dashes(RoadChunk ch, RoadRun run, float s0, float s1, float lateral, float width, float dash, float gap)
        {
            for (float s = s0; s < s1; s += dash + gap) MarkAlong(ch, run, s, Math.Min(s1, s + dash), lateral, width);
        }

        /// <summary>A line across the road at distance s, from lateral l0 to l1, <paramref name="depth"/> deep along the road (dashed if dash &gt; 0).</summary>
        static void MarkAcross(RoadChunk ch, RoadRun run, float s, float l0, float l1, float depth, float dash = 0f, float gap = 0f)
        {
            var (p, _, n) = Frame(run, s);
            var t = Left(n) * -1f;   // the tangent
            float a = Math.Min(l0, l1), b = Math.Max(l0, l1);
            for (float x = a; x < b - 0.01f; x += dash > 0f ? dash + gap : b - a)
            {
                float x1 = dash > 0f ? Math.Min(b, x + dash) : b;
                var c0 = p + n * x; var c1 = p + n * x1;
                float hd = depth / 2f;
                ch.Quad(RoadSurface.Marking, Vx(c0 - t * hd, MarkY, 0f, 0f), Vx(c0 + t * hd, MarkY, 0f, 1f), Vx(c1 + t * hd, MarkY, 1f, 1f), Vx(c1 - t * hd, MarkY, 1f, 0f));
            }
        }

        static void MarkRun(RoadChunk ch, RoadMeshSet set, RoadRun run, Node na, Arm armA, Node nb, Arm armB, IList<V2> crossings)
        {
            float h = run.Width / 2f, len = run.Length;
            bool loop = !run.Pavements;
            // Centre line: hazard lines (long dashes) on the approach to a junction, normal lane lines elsewhere.
            float ha = na != null && na.IsJunction ? Math.Min(HazardLength, len / 2f) : 0f;
            float hb = nb != null && nb.IsJunction ? Math.Min(HazardLength, len / 2f) : 0f;
            float lineStart = armA != null ? 1.5f : 0f, lineEnd = len - (armB != null ? 1.5f : 0f);
            Dashes(ch, run, lineStart, lineStart + ha, 0f, 0.12f, 6f, 2f);
            if (loop) Dashes(ch, run, lineStart + ha, lineEnd - hb, 0f, 0.12f, 3f, 9f);
            else Dashes(ch, run, lineStart + ha, lineEnd - hb, 0f, 0.1f, 4f, 5f);
            float back = lineEnd - hb;
            for (float s = back; s < lineEnd; s += 8f) MarkAlong(ch, run, s, Math.Min(lineEnd, s + 6f), 0f, 0.12f);
            if (loop)
                foreach (int side in new[] { -1, 1 }) MarkAlong(ch, run, 0f, len, side * (h - 0.3f), 0.1f);

            // The end of the road at each junction: give way, stop, or give way to the roundabout.
            foreach (var (node, arm, atEnd) in new[] { (na, armA, false), (nb, armB, true) })
            {
                if (node == null || arm == null) continue;
                // Arriving at the node you drive on the left: the approach lane is +left at the end, -left at the start.
                float s = atEnd ? len - 0.6f : 0.6f;
                float sign = atEnd ? 1f : -1f;
                var (p, t, n) = Frame(run, s);
                var facing = atEnd ? t : t * -1f;
                RoadFeatureKind kind;
                if (node.Roundabout) { kind = RoadFeatureKind.RoundaboutEntry; MarkAcross(ch, run, s, 0f, sign * (h - 0.25f), 0.2f, 0.6f, 0.3f); }
                else if (node.Lights) { kind = RoadFeatureKind.StopLine; MarkAcross(ch, run, s - sign * 0.9f, 0f, sign * (h - 0.25f), 0.3f); }
                else if (arm.EndsHere && node.Arms.Count >= 3)
                {
                    kind = RoadFeatureKind.GiveWay;
                    MarkAcross(ch, run, s, 0f, sign * (h - 0.25f), 0.2f, 0.6f, 0.3f);
                    MarkAcross(ch, run, s - sign * 0.5f, 0f, sign * (h - 0.25f), 0.2f, 0.6f, 0.3f);
                    GiveWayTriangle(ch, run, s - sign * 12f, sign * h / 2f, atEnd);
                }
                else continue;
                set.Features.Add(new RoadFeature { Kind = kind, Position = p + n * (sign * h / 2f), Facing = facing, Kerb = p + n * (sign * (h + 0.6f)) });
            }

            // Zebra crossings on town roads, away from the junctions.
            if (!loop && crossings != null)
                foreach (var z in crossings)
                {
                    int best = -1; float bd = float.MaxValue;
                    for (int i = 0; i < run.Centre.Count; i++) { float d = V2.Distance(run.Centre[i], z); if (d < bd) { bd = d; best = i; } }
                    if (best < 0 || bd > 4f) continue;
                    float s = run.Distance[best];
                    if (s < 18f || s > len - 18f) continue;
                    for (float l = -h + 0.4f; l < h - 0.4f; l += 1f) MarkAcross(ch, run, s, l, Math.Min(h - 0.4f, l + 0.5f), 3f);
                    foreach (float side in new[] { -1f, 1f }) MarkAcross(ch, run, s + side * 2.2f, -h + 0.25f, h - 0.25f, 0.1f, 0.3f, 0.3f);
                    var (p, t, n) = Frame(run, s);
                    set.Features.Add(new RoadFeature { Kind = RoadFeatureKind.Zebra, Position = p, Facing = t, Kerb = p + n * (h + 0.6f) });
                }
        }

        /// <summary>The give-way triangle painted on the approach: base towards the junction, point towards oncoming drivers.</summary>
        static void GiveWayTriangle(RoadChunk ch, RoadRun run, float s, float lateral, bool towardsEnd)
        {
            if (s < 1f || s > run.Length - 1f) return;
            var (p, t, n) = Frame(run, s);
            var fwd = towardsEnd ? t : t * -1f;
            var c = p + n * lateral;
            var tip = c - fwd * 2.5f; var l = c + fwd * 1.5f + n * 0.9f; var r = c + fwd * 1.5f - n * 0.9f;
            foreach (var (a, b) in new[] { (tip, l), (l, r), (r, tip) })
            {
                var dir = (b - a).Normalized; var side = Left(dir) * 0.08f;
                ch.Quad(RoadSurface.Marking, Vx(a - side, MarkY, 0f, 0f), Vx(b - side, MarkY, 1f, 0f), Vx(b + side, MarkY, 1f, 1f), Vx(a + side, MarkY, 0f, 1f));
            }
        }

        // ------------------------------------------------------------------ JUNCTIONS
        static V2? Intersect(V2 p, V2 d, V2 q, V2 e)
        {
            float den = Cross(d, e);
            if (Math.Abs(den) < 1e-4f) return null;
            float t = Cross(q - p, e) / den;
            return p + d * t;
        }

        /// <summary>The kerb line between two neighbouring arms: a curve round the corner, or straight on for a through road.</summary>
        static V2 EndOf(V2 node, Arm a) => a.HasEnd ? a.EndP : node + a.Dir * a.Trim;
        static V2 AwayOf(Arm a) => a.HasEnd ? a.Away : a.Dir;

        static List<V2> CornerCurve(V2 node, Arm a, Arm b)
        {
            V2 da = AwayOf(a), db = AwayOf(b);
            var from = EndOf(node, a) + Left(da) * a.Half;
            var to = EndOf(node, b) - Left(db) * b.Half;
            var pts = new List<V2> { from };
            float sweep = AngleOf(db) - AngleOf(da);
            if (sweep < 0) sweep += (float)(2 * Math.PI);
            // A road that bends as it passes the junction: its outer edge goes out to the corner point (no cut corner).
            bool outerBend = sweep > Math.PI + 0.03f;
            var ctrl = sweep < 2.6f || outerBend ? Intersect(from, da, to, db) : null;
            bool ok = ctrl.HasValue && V2.Dot(ctrl.Value - from, da) < 0f && V2.Dot(ctrl.Value - to, db) < 0f;
            if (ok && outerBend) pts.Add(ctrl.Value);
            else if (ok)
            {
                const int n = 10;
                for (int k = 1; k < n; k++)
                {
                    float t = k / (float)n, u = 1f - t;
                    pts.Add(from * (u * u) + ctrl.Value * (2f * u * t) + to * (t * t));
                }
            }
            pts.Add(to);
            return pts;
        }

        static void MeshJunction(RoadChunk ch, Node n)
        {
            var arms = n.Arms.OrderBy(a => AngleOf(a.Dir)).ToList();
            var outline = new List<V2>();
            var corners = new List<(List<V2> curve, bool pavement)>();
            for (int i = 0; i < arms.Count; i++)
            {
                var a = arms[i]; var b = arms[(i + 1) % arms.Count];
                outline.Add(EndOf(n.P, a) - Left(AwayOf(a)) * a.Half);
                var curve = CornerCurve(n.P, a, b);
                outline.AddRange(curve.Take(curve.Count - 1));
                corners.Add((curve, a.Road.Pavements && b.Road.Pavements));
            }
            var centre = Vx(n.P, RoadY, n.P.x, n.P.z);
            for (int i = 0; i < outline.Count; i++)
            {
                var p = outline[i]; var q = outline[(i + 1) % outline.Count];
                ch.Tri(RoadSurface.Asphalt, centre, Vx(p, RoadY, p.x, p.z), Vx(q, RoadY, q.x, q.z));
            }
            // Pavements round the corners (outside the curve).
            foreach (var (curve, pavement) in corners)
            {
                if (!pavement || curve.Count < 2) continue;
                float y = RoadY + KerbHeight;
                float s = 0f;
                for (int k = 0; k < curve.Count - 1; k++)
                {
                    var p0 = curve[k]; var p1 = curve[k + 1];
                    var t0 = (curve[Math.Min(curve.Count - 1, k + 1)] - curve[Math.Max(0, k - 1)]).Normalized;
                    var t1 = (curve[Math.Min(curve.Count - 1, k + 2)] - curve[k]).Normalized;
                    var o0 = new V2(t0.z, -t0.x); var o1 = new V2(t1.z, -t1.x);   // right of the counter-clockwise outline = away from the road
                    float s1 = s + V2.Distance(p0, p1);
                    ch.Quad(RoadSurface.Kerb, Vx(p0, RoadY, s, 0f), Vx(p1, RoadY, s1, 0f), Vx(p1, y, s1, KerbHeight), Vx(p0, y, s, KerbHeight), -o0.x, 0f, -o0.z);
                    ch.Quad(RoadSurface.Pavement, Vx(p0, y, 0f, s), Vx(p1, y, 0f, s1), Vx(p1 + o1 * PavementWidth, y, PavementWidth, s1), Vx(p0 + o0 * PavementWidth, y, PavementWidth, s));
                    ch.Quad(RoadSurface.Kerb, Vx(p0 + o0 * PavementWidth, 0f, s, 0f), Vx(p1 + o1 * PavementWidth, 0f, s1, 0f), Vx(p1 + o1 * PavementWidth, y, s1, y), Vx(p0 + o0 * PavementWidth, y, s, y), o0.x, 0f, o0.z);
                    s = s1;
                }
            }
        }

        // ------------------------------------------------------------------ ROUNDABOUTS
        static void MeshRoundabout(RoadChunk ch, Node n)
        {
            const int seg = 72;
            float ri = n.Radius - IslandInset, ro = n.Radius + RingOuter, y = RoadY + RingLift, top = RoadY + 0.15f;
            V2 At(float ang, float r) => n.P + new V2((float)Math.Cos(ang), (float)Math.Sin(ang)) * r;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * 2f * (float)Math.PI / seg, a1 = (i + 1) * 2f * (float)Math.PI / seg;
                V2 i0 = At(a0, ri), i1 = At(a1, ri), o0 = At(a0, ro), o1 = At(a1, ro);
                ch.Quad(RoadSurface.Asphalt, Vx(i0, y, i0.x, i0.z), Vx(i1, y, i1.x, i1.z), Vx(o1, y, o1.x, o1.z), Vx(o0, y, o0.x, o0.z));
                // The island: a kerb, a paved edge and grass.
                V2 k0 = At(a0, ri - 0.5f), k1 = At(a1, ri - 0.5f);
                var outA = (i0 - n.P).Normalized;
                ch.Quad(RoadSurface.Kerb, Vx(i0, y, a0 * ri, 0f), Vx(i1, y, a1 * ri, 0f), Vx(i1, top, a1 * ri, 0.15f), Vx(i0, top, a0 * ri, 0.15f), outA.x, 0f, outA.z);
                ch.Quad(RoadSurface.Kerb, Vx(i0, top, 0f, a0 * ri), Vx(i1, top, 0f, a1 * ri), Vx(k1, top, 0.5f, a1 * ri), Vx(k0, top, 0.5f, a0 * ri));
                ch.Tri(RoadSurface.Island, Vx(n.P, top, n.P.x, n.P.z), Vx(k0, top, k0.x, k0.z), Vx(k1, top, k1.x, k1.z));
            }
            // The outer kerb and pavement, with gaps where the roads come in.
            bool pavements = n.Arms.Any(a => a.Road.Pavements);
            var gaps = n.Arms.Select(a => (ang: AngleOf(a.Dir), half: (float)Math.Asin(Math.Min(0.99f, (a.Half + 0.3f) / ro)))).ToList();
            bool InGap(float ang) => gaps.Any(g => { float d = Math.Abs(ang - g.ang) % (2f * (float)Math.PI); if (d > Math.PI) d = 2f * (float)Math.PI - d; return d < g.half; });
            float py = RoadY + KerbHeight, w = pavements ? PavementWidth : VergeWidth;
            for (int i = 0; i < seg * 2; i++)
            {
                float a0 = i * (float)Math.PI / seg, a1 = (i + 1) * (float)Math.PI / seg;
                if (InGap(a0) || InGap(a1)) continue;
                V2 o0 = At(a0, ro), o1 = At(a1, ro), p0 = At(a0, ro + w), p1 = At(a1, ro + w);
                var inA = (n.P - o0).Normalized;
                if (pavements)
                {
                    ch.Quad(RoadSurface.Kerb, Vx(o0, RoadY, a0 * ro, 0f), Vx(o1, RoadY, a1 * ro, 0f), Vx(o1, py, a1 * ro, KerbHeight), Vx(o0, py, a0 * ro, KerbHeight), inA.x, 0f, inA.z);
                    ch.Quad(RoadSurface.Pavement, Vx(o0, py, 0f, a0 * ro), Vx(o1, py, 0f, a1 * ro), Vx(p1, py, w, a1 * ro), Vx(p0, py, w, a0 * ro));
                }
                else ch.Quad(RoadSurface.Verge, Vx(o0, RoadY - 0.004f, 0f, a0 * ro), Vx(o1, RoadY - 0.004f, 0f, a1 * ro), Vx(p1, RoadY - 0.006f, w, a1 * ro), Vx(p0, RoadY - 0.006f, w, a0 * ro));
            }
            // Lane line round the ring (dashed) halfway across.
            float rm = n.Radius;
            for (int i = 0; i < seg; i += 2)
            {
                float a0 = i * 2f * (float)Math.PI / seg, a1 = (i + 1) * 2f * (float)Math.PI / seg;
                V2 m0 = At(a0, rm - 0.05f), m1 = At(a1, rm - 0.05f), q0 = At(a0, rm + 0.05f), q1 = At(a1, rm + 0.05f);
                ch.Quad(RoadSurface.Marking, Vx(m0, y + 0.006f, 0f, 0f), Vx(m1, y + 0.006f, 1f, 0f), Vx(q1, y + 0.006f, 1f, 1f), Vx(q0, y + 0.006f, 0f, 1f));
            }
        }
    }
}
