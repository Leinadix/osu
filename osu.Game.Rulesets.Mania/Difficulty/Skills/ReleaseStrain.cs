// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Utils;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.Difficulty.Skills
{
    public class ReleaseStrain : StrainDecaySkill
    {
        private const double ln_decay_base = 0.24;
        private const double overall_decay_base = 0.30;

        private double noodleBuff = 1.40;
        private double rollTreshhold = 0.1;
        private double keymodeScaling = 4.5;

        private readonly double[] lnStrains;

        private readonly ManiaDifficultyHitObject[] lnBuffNoteCache;
        private readonly int keymode;

        private double lnStrain;
        private double overallStrain;

        protected override double SkillMultiplier => 1;

        protected override double StrainDecayBase => 1;

        public ReleaseStrain(Mod[] mods, int totalColumns)
            : base(mods)
        {
            lnStrains = new double[totalColumns];
            lnBuffNoteCache = new ManiaDifficultyHitObject[totalColumns];
            overallStrain = 1;

            keymode = totalColumns;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var maniaCurrent = (ManiaDifficultyHitObject)current;
            int column = maniaCurrent.BaseObject.Column;

            lnStrain = noodleBuff * coolFunction(maniaCurrent);

            // Decay and increase lnStrain
            lnStrains[column] = applyDecay(lnStrain, current.DeltaTime, ln_decay_base);
            lnStrains[column] += lnStrain;

            lnBuffNoteCache[column] = maniaCurrent;

            // Decay and increase overallStrain
            overallStrain = applyDecay(overallStrain, current.DeltaTime, overall_decay_base);
            overallStrain += lnStrain * (isLN(maniaCurrent) ? 1 : 0);

            // By subtracting CurrentStrain, this skill effectively only considers the maximum strain of any one hitobject within each strain section.
            return lnStrain + overallStrain - CurrentStrain;
        }

        private bool isLN(DifficultyHitObject current)
        {
            var cc = (ManiaDifficultyHitObject)current;

            return cc.BaseObject is HoldNote;
        }

        protected override double CalculateInitialStrain(double offset, DifficultyHitObject current)
            //=> isLN(current) ? applyDecay(lnStrain, offset - current.Previous(0).StartTime, ln_decay_base) : 0;
            => applyDecay(lnStrain, offset - current.Previous(0).StartTime, ln_decay_base);

        private double applyDecay(double value, double deltaTime, double decayBase)
            => value * Math.Pow(decayBase, deltaTime / 1000);

        private double coolFunction(ManiaDifficultyHitObject obj)
        {
            double hand = getHand(obj.BaseObject.Column);
            double bonus = 0.0;

            int totalDistance = 0;

            // Amount of LN-Bodies that are on the same hand as this note that are being held while this note is played
            int concurrentNotesOnSameHand = 0;

            // Amount of LN-Bodies that are being held while this note is played
            int concurrentNotes = 0;

            for (int i = 0; i < lnBuffNoteCache.Length; i++)
            {
                ManiaDifficultyHitObject note = lnBuffNoteCache[i];

                if (note == null) continue;

                concurrentNotes++;

                if (isLN(note) && Precision.DefinitelyBigger(note.EndTime, obj.StartTime, 1) &&
                                    Precision.DefinitelyBigger(obj.EndTime, note.EndTime, 1) &&
                                    Precision.DefinitelyBigger(obj.StartTime, note.StartTime, 1))
                {
                    double noteHand = getHand(note.BaseObject.Column);

                    if (noteHand == hand || noteHand == 0.5 || hand == 0.5)
                    {
                        concurrentNotesOnSameHand++;
                        totalDistance += Math.Abs(obj.BaseObject.Column - note.BaseObject.Column);
                    }

                    //bonus = coolFunction(note) - lnBuffResults[i];
                }
            }


            // See https://www.desmos.com/calculator/wkvpncjv5z
            double averageDistance = (totalDistance / Math.Max(1.0, concurrentNotesOnSameHand));

            return (Math.Max(0, keymodeScaling / (1 + (1 / Math.Max(0, averageDistance - rollTreshhold)))) / Math.Max(1.0, concurrentNotes)) + bonus;
        }


        /// <summary>
        /// <para>Method that returns on what hand a column is expected to be played across all keymodes.</para>
        /// <para></para>
        /// <para>Examples:</para>
        /// <para>  4k 2nd column would return 0 (left hand).</para>
        /// <para>  4k 3rd column would return 1 (right hand).</para>
        /// <para>  9k 5th column would return 0.5 (both hands / special).</para>
        /// <para>  9k 2nd column would return 0 (left hand).</para>
        /// <para>  9k 9th column would return 1 (right hand).</para>
        /// </summary>
        /// <param name="column"></param>
        /// <returns>0 : left hand ; 1 : right hand ; 0.5 : both hands (aka special)</returns>
        private double getHand(int column)
            => ((column == Math.Ceiling(keymode / 2.0)) && (keymode % 2 == 1)) ? (0.5) : ((column < keymode / 2.0) ? 0 : 1);
    }
}
