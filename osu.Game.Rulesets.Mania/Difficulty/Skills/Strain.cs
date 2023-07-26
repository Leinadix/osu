// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.Difficulty.Skills
{
    public class Strain : StrainDecaySkill
    {
        private const double individual_decay_base = 0.125;
        private const double overall_decay_base = 0.30;

        protected override double SkillMultiplier => 1;
        protected override double StrainDecayBase => 1;

        private readonly double[] startTimes;
        private readonly double[] endTimes;
        private readonly double[] individualStrains;

        private double individualStrain;
        private double overallStrain;
        private int chordCount = 1;
        private int keyMode = 1;

        public Strain(Mod[] mods, int totalColumns)
            : base(mods)
        {
            startTimes = new double[totalColumns];
            endTimes = new double[totalColumns];
            individualStrains = new double[totalColumns];
            overallStrain = 1;
            keyMode = totalColumns;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var maniaCurrent = (ManiaDifficultyHitObject)current;
            double startTime = maniaCurrent.StartTime;
            double endTime = maniaCurrent.EndTime;
            int column = maniaCurrent.BaseObject.Column;

            double priority = 1;

            double[] startTimesCopy = new HashSet<double>(startTimes).ToArray();
            Array.Sort(startTimesCopy);
            priority = Math.Max(1, priority + Array.IndexOf(startTimesCopy, startTimes[column]));

            double prevStartTime = -1;

            foreach (double d in startTimesCopy)
            {
                if (d > prevStartTime && d < startTime) prevStartTime = d;
            }

            prevStartTime = prevStartTime < startTime ? prevStartTime : startTime;

            double chordDelta = (maniaCurrent.StartTime - prevStartTime);
            double deltaTimeIgnoreReleases = (maniaCurrent.Previous(0) == null) ? 1 : (maniaCurrent.StartTime - maniaCurrent.Previous(0).StartTime);

            if (deltaTimeIgnoreReleases > 1) chordCount = 1;

            // Decay and increase individualStrains in own column
            individualStrains[column] = applyDecay(individualStrains[column], chordDelta /* / priority*/, individual_decay_base);
            individualStrains[column] += 2;

            // For notes at the same time (in a chord), the individualStrain should be the hardest individualStrain out of those columns
            individualStrain = deltaTimeIgnoreReleases <= 1 ? Math.Max(individualStrain, individualStrains[column]) : individualStrains[column];

            // Decay and increase overallStrain
            overallStrain = applyDecay(overallStrain * Math.Max(((Math.Log(10 - chordCount) / 100) + (1 / 1.01)), 0.1), deltaTimeIgnoreReleases, overall_decay_base);
            overallStrain += 1;

            chordCount++;

            // Update startTimes and endTimes arrays
            startTimes[column] = startTime;
            endTimes[column] = endTime;

            // By subtracting CurrentStrain, this skill effectively only considers the maximum strain of any one hitobject within each strain section.
            return individualStrain + overallStrain - CurrentStrain;
        }

        protected override double CalculateInitialStrain(double offset, DifficultyHitObject current)
            => applyDecay(individualStrain, offset - current.Previous(0).StartTime, individual_decay_base)
               + applyDecay(overallStrain, offset - current.Previous(0).StartTime, overall_decay_base);

        private double applyDecay(double value, double deltaTime, double decayBase)
            => value * Math.Pow(decayBase, deltaTime / 1000);
    }
}
