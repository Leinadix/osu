// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Mania.Difficulty.Evaluators
{
    public class SpeedEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            ManiaDifficultyHitObject mCurrent = (ManiaDifficultyHitObject)current;

            double SpeedFactor = 0.16;

            double GraceNoteTolerance = 6;
            double noteDelta = mCurrent.StartTime - mCurrent.PrevHitObjects.ToList().ConvertAll(obj => (obj?.StartTime ?? double.PositiveInfinity) + GraceNoteTolerance).Min();
            if (noteDelta == 0) noteDelta = double.PositiveInfinity;

            // BPM as in 1/2th Notes because that is what players usually use to refer to speed BPM
            double speedBpm = 15000.0 / noteDelta;

            double streamCount = 0;

            ManiaDifficultyHitObject? prevChord = GetPreviousChord(mCurrent);

            // Find amount of notes in stream (allow single jacks)
            while (streamCount < mCurrent.Index)
            {
                if (prevChord is null) break;
                if (ChordEvaluator.FindJackCountInChord(prevChord, noteDelta, GraceNoteTolerance) > 1) break;
                prevChord = GetPreviousChord(prevChord);
                streamCount++;
            }

            double streamStamina = Math.Min(3000, streamCount + (streamCount * streamCount / 1000.0));

            double StaminaBonus = 1 + Math.Pow(streamStamina, 0.12);

            return SpeedFactor * speedBpmScale(speedBpm) * StaminaBonus;
        }

        public static ManiaDifficultyHitObject? GetPreviousChord(ManiaDifficultyHitObject note)
        {
            var t = note.PrevHitObjects.ToList().Where(obj => obj is not null);
            if (t.Count() > 0)
            {
                return t.Last();
            }
            return null;
        }

        private static double speedBpmScale(double bpm) => bpm * Math.Pow(bpm / 380, 1.2);
    }
}
