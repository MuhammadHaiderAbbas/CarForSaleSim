using System;
using System.Linq;
using CarSales.Foundation;
using NUnit.Framework;

namespace CarSales.Tests
{
    /// <summary>Phase 12b: every interface icon parses and draws something sensible.</summary>
    public class Phase12bTests
    {
        static int Filled(byte[] a) => a.Count(v => v > 127);

        [Test]
        public void EveryIcon_Rasterises_ToAVisibleShape()
        {
            foreach (UIIcon icon in Enum.GetValues(typeof(UIIcon)))
            {
                if (icon == UIIcon.None) continue;
                Assert.IsTrue(IconLibrary.Svg.ContainsKey(icon), icon + " has SVG data");
                var a = VectorIcon.Rasterize(IconLibrary.Svg[icon], 48);
                int filled = Filled(a);
                Assert.Greater(filled, 48 * 48 / 40, $"{icon}: almost nothing drawn ({filled} px)");
                Assert.Less(filled, 48 * 48 * 9 / 10, $"{icon}: nearly the whole square is filled ({filled} px) - the background box was drawn");
            }
        }

        [Test]
        public void Rectangle_FillsExactlyItsArea()
        {
            var a = VectorIcon.Rasterize("<svg viewBox=\"0 0 24 24\"><path d=\"M0 0h24v24H0z\" fill=\"none\"/><path d=\"M6 6h12v12H6z\"/></svg>", 24, 4, 0f);
            Assert.AreEqual(144, Filled(a), "12x12 square");
            Assert.AreEqual(255, a[12 * 24 + 12]);
            Assert.AreEqual(0, a[2 * 24 + 2]);
        }

        [Test]
        public void Holes_UseTheNonZeroRule_AndArcsMakeCircles()
        {
            // A ring: outer square clockwise, inner square counter-clockwise = a hole.
            var ring = VectorIcon.Rasterize("<svg viewBox=\"0 0 24 24\"><path d=\"M2 2h20v20H2z M8 8v8h8V8z\"/></svg>", 24, 4, 0f);
            Assert.AreEqual(0, ring[12 * 24 + 12], "hole in the middle");
            Assert.AreEqual(255, ring[4 * 24 + 4]);
            var circle = VectorIcon.Rasterize("<svg viewBox=\"0 0 24 24\"><path d=\"M12 2a10 10 0 1 0 0.001 0z\"/></svg>", 96, 4, 0f);
            double area = Filled(circle) / (96.0 * 96.0) * 576.0;   // in SVG units
            Assert.AreEqual(Math.PI * 100, area, 12, "a circle of radius 10");
        }

        [Test]
        public void Relative_Commands_AndCurves()
        {
            var polys = VectorIcon.PathPolys("m4 4 l4 0 l0 4 l-4 0 z M10 10 c0 2 2 4 4 4 s4 -2 4 -4 z");
            Assert.AreEqual(2, polys.Count);
            Assert.AreEqual(8f, polys[0].Max(p => p.X), 1e-4);
            Assert.AreEqual(14f, polys[1].Max(p => p.Y), 0.2f);
        }
    }
}
