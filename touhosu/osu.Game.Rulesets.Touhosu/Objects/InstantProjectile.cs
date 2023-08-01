﻿using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Judgements;

namespace osu.Game.Rulesets.Touhosu.Objects
{
    public class InstantProjectile : Projectile
    {
        protected override void ApplyDefaultsToSelf(ControlPointInfo controlPointInfo, IBeatmapDifficultyInfo difficulty)
        {
            base.ApplyDefaultsToSelf(controlPointInfo, difficulty);
            TimePreempt = 800;
        }
    }
}
