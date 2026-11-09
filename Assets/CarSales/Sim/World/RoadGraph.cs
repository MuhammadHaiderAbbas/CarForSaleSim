using System;
using System.Collections.Generic;

namespace CarSales.Sim
{
    [Serializable]
    public struct V2
    {
        public float x, z;
        public V2(float x, float z) { this.x = x; this.z = z; }
        public static V2 operator +(V2 a, V2 b) => new V2(a.x + b.x, a.z + b.z);
        public static V2 operator -(V2 a, V2 b) => new V2(a.x - b.x, a.z - b.z);
        public static V2 operator *(V2 a, float k) => new V2(a.x * k, a.z * k);
        public float Length => (float)Math.Sqrt(x * x + z * z);
        public V2 Normalized { get { float l = Length; return l > 1e-5f ? new V2(x / l, z / l) : new V2(0f, 0f); } }
        public static float Dot(V2 a, V2 b) => a.x * b.x + a.z * b.z;
        public static float Distance(V2 a, V2 b) => (a - b).Length;
    }

    /// <summary>A straight piece of road between two junctions or ends.</summary>
    [Serializable]
    public struct RoadSegment { public V2 a, b; public float speedLimit; public RoadSegment(V2 a, V2 b, float limit = 13.4f) { this.a = a; this.b = b; speedLimit = limit; } }

    public sealed class Lane
    {
        public int id;
        public V2 start, end;
        public float speedLimit;
        public bool alongX;                       // for the traffic-light phase
        public bool endsAtJunction;
        public readonly List<int> next = new List<int>();
        public float Length => V2.Distance(start, end);
        public V2 Dir => (end - start).Normalized;
        public V2 PointAt(float d) => start + Dir * Math.Max(0f, Math.Min(Length, d));
    }

    /// <summary>
    /// Builds the traffic lane graph from road segments (spec §6.13): two lanes per segment, driving on the LEFT
    /// (UK), offset by a quarter of the road width; lanes that end where another segment starts are joined
    /// (straight on, left, right - never a U-turn); a node shared by three or more segments is a junction.
    /// </summary>
    public static class RoadGraph
    {
        public static List<Lane> Build(IList<RoadSegment> segments, float roadWidth = 9f, float joinRadius = 6f)
        {
            var lanes = new List<Lane>();
            float off = roadWidth / 4f;
            foreach (var s in segments)
                foreach (bool forward in new[] { true, false })
                {
                    V2 a = forward ? s.a : s.b, b = forward ? s.b : s.a;
                    V2 dir = (b - a).Normalized;
                    var left = new V2(-dir.z, dir.x);      // left of travel
                    lanes.Add(new Lane
                    {
                        id = lanes.Count, start = a + left * off, end = b + left * off, speedLimit = s.speedLimit,
                        alongX = Math.Abs(dir.x) >= Math.Abs(dir.z),
                    });
                }
            // Junction nodes: road ends shared by 3+ segment ends.
            var ends = new List<V2>();
            foreach (var s in segments) { ends.Add(s.a); ends.Add(s.b); }
            bool IsJunction(V2 p)
            {
                int n = 0;
                foreach (var e in ends) if (V2.Distance(e, p) < joinRadius) n++;
                return n >= 3;
            }
            foreach (var l in lanes)
            {
                // The road node this lane ends at (lane end minus the side offset).
                var dir = l.Dir;
                var node = l.end - new V2(-dir.z, dir.x) * off;
                l.endsAtJunction = IsJunction(node);
                foreach (var m in lanes)
                {
                    if (m == l) continue;
                    var mdir = m.Dir;
                    var mNode = m.start - new V2(-mdir.z, mdir.x) * off;
                    if (V2.Distance(node, mNode) > joinRadius) continue;
                    if (V2.Dot(dir, mdir) < -0.5f) continue;   // no U-turns
                    l.next.Add(m.id);
                }
            }
            return lanes;
        }

        /// <summary>The default v4 town: two long roads (z = -27 and 95) and two cross roads (x = ±70).</summary>
        public static List<RoadSegment> DefaultTown()
        {
            var list = new List<RoadSegment>();
            foreach (float z in new[] { -27f, 95f })
            {
                list.Add(new RoadSegment(new V2(-250f, z), new V2(-70f, z)));
                list.Add(new RoadSegment(new V2(-70f, z), new V2(70f, z)));
                list.Add(new RoadSegment(new V2(70f, z), new V2(250f, z)));
            }
            foreach (float x in new[] { -70f, 70f }) list.Add(new RoadSegment(new V2(x, -27f), new V2(x, 95f), 11f));
            return list;
        }
    }
}
