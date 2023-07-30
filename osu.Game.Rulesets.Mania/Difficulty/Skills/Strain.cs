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

            double dist = 0.0;

            if (current.DeltaTime > 30) chordCount = 1;
            chordCount++;

            for (int i = 0; i < chordCount; i++) dist += distOf(current, i, column);

            dist /= chordCount; // <-- if close to 1 we have a roll or chord, which are treated the same.

            calcKeyMode = Math.Max(calcKeyMode, 1 + Math.Abs(column - (maniaCurrent.Previous(0) == null ? column : ((ManiaDifficultyHitObject)(maniaCurrent.Previous(0))).BaseObject.Column)));

            double[] startTimesCopy = new HashSet<double>(startTimes).ToArray(); // Hashset to remove duplicates

            double priority = calcNotePriority(current, startTimesCopy);

            double chordDelta = calcChordDelta(startTimesCopy, maniaCurrent);

            // Decay and increase individualStrains in own column
            individualStrains[column] = applyDecay(individualStrains[column], Math.Max(1, Math.Pow(chordDelta, -(Math.Log(priority + chordCount - 1) / 52) + 1)), individual_decay_base);
            individualStrains[column] += Math.Max(0, calcKeyMode) / chordCount;

            // For notes at the same time (in a chord), the individualStrain should be the hardest individualStrain out of those columns
            individualStrain = current.DeltaTime <= 1 ? Math.Max(individualStrain, individualStrains[column]) : individualStrains[column];

            // Decay and increase overallStrain
            overallStrain = Math.Max(0, applyDecay(overallStrain * Math.Max(((Math.Log(Math.Max(0.1, 10 - chordCount)) / 100) + (1 / 1.0085)), 0.1), current.DeltaTime, overall_decay_base));
            overallStrain += (((1.5 - 1) * (Math.Tanh(4 - Math.Max(0, calcKeyMode)) + 1) / 2) + 1);

            // Update startTimes and endTimes arrays
            startTimes[column] = startTime;
            endTimes[column] = endTime;

            // By subtracting CurrentStrain, this skill effectively only considers the maximum strain of any one hitobject within each strain section.
            return individualStrain + overallStrain - CurrentStrain;
        }

        private double calcNotePriority(DifficultyHitObject m, double[] arr)
            => Math.Max(1, Math.Max(1, 1 + Array.IndexOf(arr, startTimes[((ManiaDifficultyHitObject)m).BaseObject.Column]))
                + (Math.Pow(((2 * Math.Tanh((m.DeltaTime - 40) / 12) + Math.Tanh((12 - m.DeltaTime) / 6) + 1) / 2) + 1, 2) - 2) / 2);


        private double calcChordDelta(double[] arr, DifficultyHitObject m)
        {
            Array.Sort(arr);

            double prevStartTime = -1;

            foreach (double d in arr)
            {
                if (d > prevStartTime && d < m.StartTime) prevStartTime = d;

                prevStartTime = prevStartTime < m.StartTime ? prevStartTime : m.StartTime;
            }

            return Math.Max(m.DeltaTime, m.StartTime - prevStartTime);
        }

        private double distOf(DifficultyHitObject m, int backwardsIndex, double col)
        {
            if (m == null) return 0;
            if (m.Previous(backwardsIndex) == null) return 0;
            if (m.StartTime - m.Previous(backwardsIndex).StartTime < 1) return 0; // Ignore chords
            return Math.Abs(((ManiaDifficultyHitObject)m.Previous(backwardsIndex)).BaseObject.Column - col);
        }

        protected override double CalculateInitialStrain(double offset, DifficultyHitObject current)
            => applyDecay(individualStrain, offset - current.Previous(0).StartTime, individual_decay_base)
               + applyDecay(overallStrain, offset - current.Previous(0).StartTime, overall_decay_base);

        private double applyDecay(double value, double deltaTime, double decayBase)
            => value * Math.Pow(decayBase, deltaTime / 1000);
    }
}
