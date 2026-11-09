using System;

namespace CarSales.Sim
{
    /// <summary>What the builders are doing on a site (append only: saved as a number).</summary>
    public enum ConstructionStage { None = 0, Clearing = 1, Site = 2, Frame = 3, Finished = 4 }

    /// <summary>
    /// Phase 14c: a rebuild is a real construction job that runs over several days, in stages you can see:
    /// the old building comes down to levelled ground (Clearing), machines and materials arrive and the foundations go in
    /// (Site), the new building's frame goes up (Frame - the "draft" of the building), and then it is Finished.
    /// </summary>
    public static class ConstructionSim
    {
        /// <summary>Nights each stage takes: a level 3 landmark needs an extra night on the frame.</summary>
        public static int[] StageNights(int toLevel) => toLevel >= 3 ? new[] { 1, 1, 2 } : new[] { 1, 1, 1 };

        public static int TotalNights(int toLevel)
        {
            int t = 0;
            foreach (var n in StageNights(toLevel)) t += n;
            return t;
        }

        /// <summary>The stage on <paramref name="today"/> for a job started on <paramref name="startDay"/>.</summary>
        public static ConstructionStage StageOn(int startDay, int today, int toLevel)
        {
            int d = today - startDay;
            if (d < 0) return ConstructionStage.None;
            var nights = StageNights(toLevel);
            int acc = 0;
            for (int i = 0; i < nights.Length; i++)
            {
                acc += nights[i];
                if (d < acc) return (ConstructionStage)(i + 1);
            }
            return ConstructionStage.Finished;
        }

        /// <summary>0..1 through the job.</summary>
        public static float Progress(int startDay, int today, int toLevel)
        {
            int total = TotalNights(toLevel);
            return total <= 0 ? 1f : Math.Max(0f, Math.Min(1f, (today - startDay) / (float)total));
        }

        public static int DaysLeft(int startDay, int today, int toLevel) => Math.Max(0, startDay + TotalNights(toLevel) - today);

        /// <summary>Jobs saved before stages existed only know their finishing day: work the start out from it.</summary>
        public static int StartDayFor(int savedStartDay, int readyDay, int toLevel) =>
            savedStartDay > 0 ? savedStartDay : readyDay - TotalNights(toLevel);

        public static string Name(ConstructionStage s)
        {
            switch (s)
            {
                case ConstructionStage.Clearing: return "Clearing the plot";
                case ConstructionStage.Site: return "Foundations";
                case ConstructionStage.Frame: return "Frame going up";
                case ConstructionStage.Finished: return "Finished";
                default: return "";
            }
        }

        public static string Describe(ConstructionStage s)
        {
            switch (s)
            {
                case ConstructionStage.Clearing: return "The old building is down; diggers are levelling the ground.";
                case ConstructionStage.Site: return "Cabins, crane and materials are on site; the foundations are being poured.";
                case ConstructionStage.Frame: return "The frame of the new building is up and the walls are going in.";
                case ConstructionStage.Finished: return "The new building is ready.";
                default: return "";
            }
        }
    }
}
