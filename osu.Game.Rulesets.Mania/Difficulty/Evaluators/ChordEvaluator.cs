// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Mania.Difficulty.Evaluators
{
    public class ChordEvaluator
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            ManiaDifficultyHitObject mCurrent = (ManiaDifficultyHitObject)current;

            double ChordScaleFactor = 0.03478260;

            double GraceNoteTolerance = 0;
            int jackCount = 0;

            double chordDelta = mCurrent.StartTime - mCurrent.PrevHitObjects.ToList().ConvertAll(obj => (obj?.StartTime ?? double.PositiveInfinity) + GraceNoteTolerance).Min();
            if (chordDelta <= 0) chordDelta = double.PositiveInfinity;

            int chordSize = mCurrent.CurrHitObjects.Count(obj => obj is not null);

            // Find amount of jacks
            jackCount = FindJackCountInChord(mCurrent, chordDelta, GraceNoteTolerance);

            // BPM as in 1/2th Notes because that is what players usually use to refer to jack BPM
            double chordBpm = 15000.0 / chordDelta;

            double scaledChordBpmFactor = chordBpmScale(chordBpm);
            double scaledTrillBpmFactor = 0.1 * chordBpmScale(chordBpm);

            // 1 means all jacks =>  no trills
            // 0 means  no jacks => all trills
            double jackDensity = jackCount / (double)chordSize;

            double Jackval = jackDensity * jackCount * scaledChordBpmFactor;
            double TrillVal = 0*(1 - jackDensity) * (chordSize - jackCount) * scaledTrillBpmFactor;

            return ChordScaleFactor * (Jackval + TrillVal);
        }

        public static int FindJackCountInChord(ManiaDifficultyHitObject note, double deltaTime, double tolerance)
            => note.CurrHitObjects.ToList()
                .Where(note => note is not null)
                .Where(note => note!.PrevInColumn(0) is not null)
                .Where(note => note!.StartTime - note.PrevInColumn(0)!.StartTime <= deltaTime + tolerance).Count();
        private static double chordBpmScale(double bpm) => bpm * Math.Pow(bpm / 240, 0.16);
    }
}
