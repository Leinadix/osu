// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Mania.Difficulty.Utils
{
    public struct DiffHitWindows
    {
        public double HMax;
        public double H300;
        public double H200;
        public double H100;
        public double H50;

        public DiffHitWindows(Mod[] mods, double overallDifficulty)
        {
            if (mods.Any(m => m is ManiaModClassic))
                getClassicHitWindows(mods, overallDifficulty, false);
            else
                getLazerHitWindows(mods, overallDifficulty);
        }

        private void getLazerHitWindows(Mod[] mods, double overallDifficulty)
        {
            double windowMultiplier = 1;

            if (mods.Any(m => m is ModHardRock))
                windowMultiplier *= 1 / 1.4;
            else if (mods.Any(m => m is ModEasy))
                windowMultiplier *= 1.4;

            if (overallDifficulty < 5)
                HMax = (22.4 - 0.6 * overallDifficulty) * windowMultiplier;
            else
                HMax = (24.9 - 1.1 * overallDifficulty) * windowMultiplier;
            H300 = (64 - 3 * overallDifficulty) * windowMultiplier;
            H200 = (97 - 3 * overallDifficulty) * windowMultiplier;
            H100 = (127 - 3 * overallDifficulty) * windowMultiplier;
            H50 = (151 - 3 * overallDifficulty) * windowMultiplier;
        }

        private void getClassicHitWindows(Mod[] mods, double overallDifficulty, bool isConvert)
        {
            double greatWindowLeniency = 0;
            double goodWindowLeniency = 0;

            // When converting beatmaps to osu!mania in stable, the resulting hit window sizes are dependent on whether the beatmap's OD is above or below 4.
            if (isConvert)
            {
                if (overallDifficulty <= 4)
                {
                    greatWindowLeniency = 13;
                    goodWindowLeniency = 10;
                }

                overallDifficulty = 10;
            }

            double windowMultiplier = 1;

            if (mods.Any(m => m is ModHardRock))
                windowMultiplier *= 1 / 1.4;
            else if (mods.Any(m => m is ModEasy))
                windowMultiplier *= 1.4;

            HMax = Math.Floor(16 * windowMultiplier);
            H300 = Math.Floor((64 - 3 * overallDifficulty + greatWindowLeniency) * windowMultiplier);
            H200 = Math.Floor((97 - 3 * overallDifficulty + goodWindowLeniency) * windowMultiplier);
            H100 = Math.Floor((127 - 3 * overallDifficulty) * windowMultiplier);
            H50 = Math.Floor((151 - 3 * overallDifficulty) * windowMultiplier);
        }

        public double HitProbability(double window, double deviation) => SpecialFunctions.Erf(window / (deviation * Math.Sqrt(2)));

        public double HitProbabilityLn(double window, double headDeviation, double tailDeviation)
        {
            double root2 = Math.Sqrt(2);

            double pHead = SpecialFunctions.Erf(window / (headDeviation * root2));

            // Calculate the expected value of the distance from 0 of the head hit, given it lands within the current window.
            // We'll subtract this from the tail window to approximate the difficulty of landing both hits within 2x the current window.
            double beta = window / headDeviation;
            double z = SpecialFunctions.NormalCdf(0, 1, beta) - 0.5;
            double expectedValue = headDeviation * (SpecialFunctions.NormalPdf(0, 1, 0) - SpecialFunctions.NormalPdf(0, 1, beta)) / z;

            double pTail = SpecialFunctions.Erf((2 * window - expectedValue) / (tailDeviation * root2));

            return pHead * pTail;
        }
    }
}
