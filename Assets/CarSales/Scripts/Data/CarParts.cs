using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CarSales
{
    public enum CarPart { Engine, Brakes, Tires, Body, Interior, Electronics }

    /// <summary>
    /// Per-part condition. A car's overall condition = weighted average of its parts.
    /// Weights add up to 1, so repairing every part costs the same as the old single "repair" did.
    /// </summary>
    public static class CarParts
    {
        public static readonly CarPart[] All = (CarPart[])Enum.GetValues(typeof(CarPart));
        static readonly float[] Weights = { 0.30f, 0.15f, 0.10f, 0.20f, 0.15f, 0.10f };

        public static float Weight(CarPart p) => Weights[(int)p];

        public static string DisplayName(CarPart p)
        {
            switch (p)
            {
                case CarPart.Engine: return "Engine";
                case CarPart.Brakes: return "Brakes";
                case CarPart.Tires: return "Tires";
                case CarPart.Body: return "Body";
                case CarPart.Interior: return "Interior";
                default: return "Electronics";
            }
        }

        public static string RepairName(CarPart p)
        {
            switch (p)
            {
                case CarPart.Engine: return "Engine Repair";
                case CarPart.Brakes: return "Brake Service";
                case CarPart.Tires: return "New Tires";
                case CarPart.Body: return "Body Repair";
                case CarPart.Interior: return "Interior Refurbish";
                default: return "Electronics Repair";
            }
        }

        public static string Glyph(CarPart p)
        {
            switch (p)
            {
                case CarPart.Engine: return "E";
                case CarPart.Brakes: return "B";
                case CarPart.Tires: return "T";
                case CarPart.Body: return "Bo";
                case CarPart.Interior: return "In";
                default: return "El";
            }
        }

        public static void EnsureParts(CarInstance c)
        {
            if (c.parts != null && c.parts.Length == All.Length) return;
            c.parts = new float[All.Length];
            for (int i = 0; i < c.parts.Length; i++) c.parts[i] = c.condition;
        }

        public static float Get(CarInstance c, CarPart p) { EnsureParts(c); return c.parts[(int)p]; }

        public static void Set(CarInstance c, CarPart p, float value)
        {
            EnsureParts(c);
            c.parts[(int)p] = Mathf.Clamp(value, 0f, 100f);
            Recalculate(c);
        }

        public static void Recalculate(CarInstance c)
        {
            EnsureParts(c);
            float sum = 0f;
            for (int i = 0; i < c.parts.Length; i++) sum += c.parts[i] * Weights[i];
            c.condition = Mathf.Clamp(sum, 0f, 100f);
        }

        /// <summary>Creates varied part conditions whose weighted average is close to `overall`.</summary>
        public static void Randomize(CarInstance c, float overall)
        {
            c.parts = new float[All.Length];
            for (int i = 0; i < c.parts.Length; i++) c.parts[i] = Mathf.Clamp(overall + Random.Range(-28f, 22f), 5f, 100f);
            for (int pass = 0; pass < 3; pass++)
            {
                Recalculate(c);
                float diff = overall - c.condition;
                for (int i = 0; i < c.parts.Length; i++) c.parts[i] = Mathf.Clamp(c.parts[i] + diff, 5f, 100f);
            }
            Recalculate(c);
        }

        /// <summary>Crash damage: mostly body, some electronics and tires.</summary>
        public static void Damage(CarInstance c, float amount)
        {
            EnsureParts(c);
            c.parts[(int)CarPart.Body] = Mathf.Max(0f, c.parts[(int)CarPart.Body] - amount * 2.5f);
            c.parts[(int)CarPart.Electronics] = Mathf.Max(0f, c.parts[(int)CarPart.Electronics] - amount * 1.2f);
            c.parts[(int)CarPart.Tires] = Mathf.Max(0f, c.parts[(int)CarPart.Tires] - amount);
            Recalculate(c);
        }

        public static void RepairAll(CarInstance c)
        {
            EnsureParts(c);
            for (int i = 0; i < c.parts.Length; i++) c.parts[i] = 100f;
            Recalculate(c);
        }

        public static string Label(float v) => v >= 85f ? "Excellent" : v >= 65f ? "Good" : v >= 40f ? "Fair" : "Poor";
        public static Color LabelColor(float v) => v >= 65f ? UIFactory.Good : v >= 40f ? UIFactory.Warn : UIFactory.Bad;
    }
}
