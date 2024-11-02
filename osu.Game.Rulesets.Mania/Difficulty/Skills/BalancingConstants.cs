// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Mania.Difficulty.Skills
{
    public struct BalancingConstants
    {
        public const double COLUMN = 1.5;
        public const double SPEED = 1.6;
        public const double CHORD = 1.1;
        public const double HOLD = 1.2;

        public const double STRAIN = 1.5;

        // Lower increases the influence of accuracy, and vice versa
        public const double ACC = 2;
    }
}