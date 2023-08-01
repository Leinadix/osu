using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Touhosu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Touhosu.Difficulty.Skills;

namespace osu.Game.Rulesets.Touhosu.Difficulty
{
    public class TouhosuDifficultyCalculator : DifficultyCalculator
    {
        public TouhosuDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            double sqSk = Math.Sqrt(skills[0].DifficultyValue());
            return new TouhosuDifficultyAttributes
            {
                StarRating = sqSk,
                Mods = mods,
                MaxCombo = beatmap.GetMaxCombo()
            };
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            List<DifficultyHitObject> objects = new List<DifficultyHitObject>();

            for (int i = 1; i < beatmap.HitObjects.Count; i++)
            {
                objects.Add(new TouhosuDifficultyHitObject(beatmap.HitObjects[i], beatmap.HitObjects[i - 1], clockRate, objects, objects.Count));
            }

            return objects;
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            return new Skill[]
            {
                new HeatmapSkill(mods, clockRate, beatmap.HitObjects.Count)
            };
        }
    }
}
