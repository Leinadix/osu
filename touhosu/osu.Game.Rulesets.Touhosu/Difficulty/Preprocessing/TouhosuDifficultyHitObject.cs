using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Touhosu.Difficulty.Preprocessing
{
    public class TouhosuDifficultyHitObject : DifficultyHitObject
    {
        public TouhosuDifficultyHitObject(HitObject hitObject, HitObject lastObject, double clockRate, List<DifficultyHitObject> objects, int index) : base(hitObject, lastObject, clockRate, objects, index)
        {
        }
    }
}
