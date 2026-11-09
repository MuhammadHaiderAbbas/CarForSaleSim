using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Sim;
using NUnit.Framework;

namespace CarSales.Tests
{
    /// <summary>Phase 14f: the town's road meshes (carriageway, kerbs, pavements, junctions, roundabouts, white lines).</summary>
    public class RoadMeshTests
    {
        static RoadMeshSet town;
        static RoadMeshSet Town => town ?? (town = RoadMesher.BuildTown());
        static Coverage asphalt;
        static Coverage Asphalt => asphalt ?? (asphalt = new Coverage(Town, RoadSurface.Asphalt));

        /// <summary>Which ground points are covered by triangles of one surface (a 10 m grid of triangles for speed).</summary>
        sealed class Coverage
        {
            const float Cell = 10f;
            readonly Dictionary<(int, int), List<float[]>> grid = new Dictionary<(int, int), List<float[]>>();

            public Coverage(RoadMeshSet set, RoadSurface s)
            {
                foreach (var ch in set.Chunks)
                    foreach (var (a, b, c) in Tris(ch, s))
                    {
                        var t = new[] { a.X, a.Z, b.X, b.Z, c.X, c.Z };
                        int x0 = (int)Math.Floor(Math.Min(a.X, Math.Min(b.X, c.X)) / Cell), x1 = (int)Math.Floor(Math.Max(a.X, Math.Max(b.X, c.X)) / Cell);
                        int z0 = (int)Math.Floor(Math.Min(a.Z, Math.Min(b.Z, c.Z)) / Cell), z1 = (int)Math.Floor(Math.Max(a.Z, Math.Max(b.Z, c.Z)) / Cell);
                        for (int x = x0; x <= x1; x++)
                            for (int z = z0; z <= z1; z++)
                            {
                                if (!grid.TryGetValue((x, z), out var l)) grid[(x, z)] = l = new List<float[]>();
                                l.Add(t);
                            }
                    }
            }

            static bool In(float[] t, float px, float pz)
            {
                float d1 = (px - t[2]) * (t[1] - t[3]) - (t[0] - t[2]) * (pz - t[3]);
                float d2 = (px - t[4]) * (t[3] - t[5]) - (t[2] - t[4]) * (pz - t[5]);
                float d3 = (px - t[0]) * (t[5] - t[1]) - (t[4] - t[0]) * (pz - t[1]);
                bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                return !(neg && pos);
            }

            public bool Covers(V2 p) =>
                grid.TryGetValue(((int)Math.Floor(p.x / Cell), (int)Math.Floor(p.z / Cell)), out var l) && l.Any(t => In(t, p.x, p.z));

            /// <summary>Covered within <paramref name="tolerance"/> metres.</summary>
            public bool Near(V2 p, float tolerance)
            {
                if (Covers(p)) return true;
                for (int k = 0; k < 8; k++)
                {
                    double a = k * Math.PI / 4;
                    if (Covers(p + new V2((float)Math.Cos(a), (float)Math.Sin(a)) * tolerance)) return true;
                }
                return false;
            }
        }

        static IEnumerable<(RoadVertex, RoadVertex, RoadVertex)> Tris(RoadChunk ch, RoadSurface s)
        {
            var t = ch.Triangles[(int)s];
            for (int i = 0; i + 2 < t.Count; i += 3) yield return (ch.Vertices[t[i]], ch.Vertices[t[i + 1]], ch.Vertices[t[i + 2]]);
        }

        static V2 Centroid(RoadVertex a, RoadVertex b, RoadVertex c) => new V2((a.X + b.X + c.X) / 3f, (a.Z + b.Z + c.Z) / 3f);

        [Test]
        public void Meshes_AreValid()
        {
            Assert.Greater(Town.Chunks.Count, 30);
            foreach (var ch in Town.Chunks)
            {
                Assert.Less(ch.Vertices.Count, 65000, ch.Name + " fits a 16-bit mesh");
                foreach (var v in ch.Vertices)
                {
                    Assert.IsFalse(float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z) || float.IsNaN(v.U) || float.IsNaN(v.V), ch.Name);
                    Assert.That(v.Y, Is.InRange(-0.001f, 0.2f), ch.Name);
                }
                foreach (var list in ch.Triangles)
                {
                    Assert.AreEqual(0, list.Count % 3, ch.Name);
                    Assert.IsTrue(list.All(i => i >= 0 && i < ch.Vertices.Count), ch.Name);
                }
            }
            Assert.Greater(Town.Chunks.Sum(c => c.TriangleCount(RoadSurface.Pavement)), 1000, "pavements");
            Assert.Greater(Town.Chunks.Sum(c => c.TriangleCount(RoadSurface.Kerb)), 1000, "kerbs");
            Assert.Greater(Town.Chunks.Sum(c => c.TriangleCount(RoadSurface.Verge)), 100, "verges on the loop");
            Assert.Greater(Town.Chunks.Sum(c => c.TriangleCount(RoadSurface.Island)), 100, "roundabout islands");
        }

        [Test]
        public void FlatSurfaces_FaceUp()
        {
            foreach (var ch in Town.Chunks)
                foreach (var s in new[] { RoadSurface.Asphalt, RoadSurface.Pavement, RoadSurface.Marking, RoadSurface.Verge, RoadSurface.Island })
                    foreach (var (a, b, c) in Tris(ch, s))
                    {
                        // Unity's front face: Vector3.Cross(b - a, c - a) points at the viewer.
                        float y = (b.Z - a.Z) * (c.X - a.X) - (b.X - a.X) * (c.Z - a.Z);
                        Assert.Greater(y, 0f, $"{ch.Name} {s} triangle faces down");
                    }
        }

        [Test]
        public void EveryRoad_IsSurfaced_IncludingJunctionsAndRoundabouts()
        {
            foreach (var r in TownPlan.Roads)
                for (int i = 0; i < r.Points.Length - 1; i++)
                {
                    V2 a = r.Points[i], b = r.Points[i + 1];
                    var d = (b - a).Normalized; var n = new V2(-d.z, d.x);
                    float len = V2.Distance(a, b);
                    for (float s = 0f; s <= len; s += 5f)
                    {
                        var p = a + d * s;
                        if (TownPlan.Roundabouts.Any(rb => V2.Distance(rb.Centre, p) < rb.Radius - RoadMesher.IslandInset + 0.5f)) continue;   // the island
                        Assert.IsTrue(Asphalt.Near(p, 0.05f), $"{r.Name}: centre {p.x:0.0},{p.z:0.0}");
                        foreach (float side in new[] { -1f, 1f })
                        {
                            var q = p + n * (side * (r.Width / 2f - RoadMesher.MaxBendOffset - 0.2f));
                            if (TownPlan.Roundabouts.Any(rb => V2.Distance(rb.Centre, q) < rb.Radius - RoadMesher.IslandInset + 0.5f)) continue;
                            Assert.IsTrue(Asphalt.Near(q, 0.05f), $"{r.Name}: edge {q.x:0.0},{q.z:0.0}");
                        }
                    }
                }
            foreach (var rb in TownPlan.Roundabouts)
                for (int k = 0; k < 12; k++)
                {
                    double ang = k * Math.PI / 6;
                    Assert.IsTrue(Asphalt.Covers(rb.Centre + new V2((float)Math.Cos(ang), (float)Math.Sin(ang)) * rb.Radius), "the ring");
                }
        }

        [Test]
        public void TrafficLanes_StayOnTheTarmac()
        {
            var lanes = RoadGraph.Build(TownPlan.Segments(), 9f, 6f, TownPlan.LightNodes);
            int checkedPoints = 0;
            foreach (var l in lanes)
                for (float s = 0f; s <= l.Length; s += 3f, checkedPoints++)
                    Assert.IsTrue(Asphalt.Near(l.PointAt(s), 0.3f), $"lane {l.id} at {l.PointAt(s).x:0.0},{l.PointAt(s).z:0.0}");
            Assert.Greater(checkedPoints, 1000);
        }

        [Test]
        public void Carriageway_IsAsWideAsTheRoad()
        {
            foreach (var run in Town.Runs)
            {
                var ch = Town.Chunks.First(c => c.Name.StartsWith(run.Road + " ") && Math.Abs(c.Area(RoadSurface.Asphalt) - run.Length * run.Width) < run.Length * run.Width * 0.03f + 1f);
                Assert.IsNotNull(ch, run.Road);
            }
            foreach (var rb in TownPlan.Roundabouts)
            {
                var ch = Town.Chunks.First(c => c.Name == $"Roundabout {rb.Centre.x:0},{rb.Centre.z:0}");
                float ri = rb.Radius - RoadMesher.IslandInset, ro = rb.Radius + RoadMesher.RingOuter;
                Assert.AreEqual(Math.PI * (ro * ro - ri * ri), ch.Area(RoadSurface.Asphalt), Math.PI * (ro * ro - ri * ri) * 0.02, ch.Name);
            }
        }

        [Test]
        public void WhiteLines_AreOnTheCarriageway_PavementsAreOff_It()
        {
            foreach (var ch in Town.Chunks)
            {
                foreach (var (a, b, c) in Tris(ch, RoadSurface.Marking))
                    Assert.IsTrue(Asphalt.Near(Centroid(a, b, c), 0.05f), $"{ch.Name}: a white line at {Centroid(a, b, c).x:0.0},{Centroid(a, b, c).z:0.0} is off the road");
                foreach (var (a, b, c) in Tris(ch, RoadSurface.Pavement))
                {
                    var p = Centroid(a, b, c);
                    Assert.IsFalse(Asphalt.Covers(p), $"{ch.Name}: pavement on the carriageway at {p.x:0.0},{p.z:0.0}");
                }
            }
        }

        [Test]
        public void Bends_AreSmooth()
        {
            foreach (var run in Town.Runs)
                for (int i = 1; i < run.Centre.Count - 1; i++)
                {
                    var d1 = (run.Centre[i] - run.Centre[i - 1]).Normalized; var d2 = (run.Centre[i + 1] - run.Centre[i]).Normalized;
                    if (V2.Distance(run.Centre[i - 1], run.Centre[i]) < 0.2f || V2.Distance(run.Centre[i], run.Centre[i + 1]) < 0.2f) continue;
                    double turn = Math.Acos(Math.Max(-1, Math.Min(1, V2.Dot(d1, d2)))) * 180 / Math.PI;
                    Assert.Less(turn, 15.0, $"{run.Road} at {run.Centre[i].x:0},{run.Centre[i].z:0}");
                }
        }

        [Test]
        public void SiteEntrances_HaveDroppedKerbs()
        {
            foreach (var id in new[] { "autoworld", "FuelStation", "MechanicWorkshop" })
            {
                var s = TownPlan.Site(id);
                var e = s.ToWorld((s.FX0 + s.FX1) / 2f, s.FZ0 - 2f);
                bool dropped = Town.Chunks.Any(ch => Tris(ch, RoadSurface.Pavement).Any(t =>
                    V2.Distance(Centroid(t.Item1, t.Item2, t.Item3), e) < 9f && Math.Max(t.Item1.Y, Math.Max(t.Item2.Y, t.Item3.Y)) < RoadMesher.RoadY + 0.03f));
                Assert.IsTrue(dropped, id + ": the pavement is lowered at the entrance");
            }
            // Away from entrances the kerb is full height.
            Assert.IsTrue(Town.Chunks.SelectMany(ch => Tris(ch, RoadSurface.Pavement)).Count(t => Math.Abs(t.Item1.Y - (RoadMesher.RoadY + RoadMesher.KerbHeight)) < 1e-4f) > 1000);
        }

        [Test]
        public void Junctions_HaveTheRightLines()
        {
            var f = Town.Features;
            int ringArms = TownPlan.Roundabouts.Sum(rb => TownPlan.RawSegments().Count(s => V2.Distance(s.a, rb.Centre) < 1f || V2.Distance(s.b, rb.Centre) < 1f));
            Assert.AreEqual(ringArms, f.Count(x => x.Kind == RoadFeatureKind.RoundaboutEntry), "a give-way line at every roundabout entry");
            int lightArms = TownPlan.LightNodes.Sum(n => TownPlan.RawSegments().Count(s => V2.Distance(s.a, n) < 1f || V2.Distance(s.b, n) < 1f));
            Assert.AreEqual(lightArms, f.Count(x => x.Kind == RoadFeatureKind.StopLine), "stop lines at the traffic lights");
            Assert.GreaterOrEqual(f.Count(x => x.Kind == RoadFeatureKind.GiveWay), 6, "give-way lines where side roads meet main roads");
            Assert.AreEqual(RoadMesher.TownCrossings.Length, f.Count(x => x.Kind == RoadFeatureKind.Zebra), "every zebra crossing found its road");
            foreach (var x in f)
            {
                Assert.IsTrue(Asphalt.Near(x.Position, 0.1f), $"{x.Kind} at {x.Position.x:0},{x.Position.z:0}");
                Assert.AreEqual(1f, x.Facing.Length, 1e-3f);
            }
        }
    }
}
