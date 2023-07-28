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
        private double chordCount = 1.0;
        private int calcKeyMode = 1;
        private int keymode = 1;

        public Strain(Mod[] mods, int totalColumns)
            : base(mods)
        {
            startTimes = new double[totalColumns];
            endTimes = new double[totalColumns];
            individualStrains = new double[totalColumns];
            overallStrain = 1;

            keymode = totalColumns;

        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var maniaCurrent = (ManiaDifficultyHitObject)current;
            double startTime = maniaCurrent.StartTime;
            double endTime = maniaCurrent.EndTime;
            int column = maniaCurrent.BaseObject.Column;

            calcKeyMode = Math.Max(calcKeyMode, 1 + Math.Abs(column - (maniaCurrent.Previous(0) == null ? column : ((ManiaDifficultyHitObject)(maniaCurrent.Previous(0))).BaseObject.Column)));

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

            double chordDelta = Math.Max(maniaCurrent.DeltaTime, maniaCurrent.StartTime - prevStartTime);

            if (current.DeltaTime > 30) chordCount = 1;
            chordCount++;

            priority = Math.Max(1, priority + (Math.Pow(((2 * Math.Tanh((maniaCurrent.DeltaTime - 40) / 12) + Math.Tanh((12 - maniaCurrent.DeltaTime) / 6) + 1) / 2) + 1, 2) - 2) / 2);

            // Decay and increase individualStrains in own column
            individualStrains[column] = applyDecay(individualStrains[column], Math.Max(1, Math.Pow(chordDelta, -(Math.Log(priority + chordCount - 1) / 52) + 1)), individual_decay_base);
            individualStrains[column] += calcKeyMode / chordCount;

            // For notes at the same time (in a chord), the individualStrain should be the hardest individualStrain out of those columns
            individualStrain = current.DeltaTime <= 1 ? Math.Max(individualStrain, individualStrains[column]) : individualStrains[column];

            // Decay and increase overallStrain
            overallStrain = Math.Max(0, applyDecay(overallStrain * Math.Max(((Math.Log(Math.Max(1, 10 - chordCount)) / 100) + (1 / 1.0065)), 0.1), current.DeltaTime, overall_decay_base));
            overallStrain += (((1.5 - 1) * (Math.Tanh(4 - calcKeyMode) + 1) / 2) + 1);

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
