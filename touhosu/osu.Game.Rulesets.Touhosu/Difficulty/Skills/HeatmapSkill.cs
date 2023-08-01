using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Touhosu.Objects;

namespace osu.Game.Rulesets.Touhosu.Difficulty.Skills
{
    public class HeatmapSkill : Skill
    {
        private double[,] heatmapSamples;

        private double clockrate;

        private int objectCount;

        public HeatmapSkill(Mod[] mods, double _clockrate, int _objectCount) : base(mods)
        {
            heatmapSamples = new double[307, 384];
            clockrate = _clockrate;
            objectCount = _objectCount;
        }

        public override double DifficultyValue()
        {
            double avg = 0.0;
            for (int x = 0; x < 307; x++)
            {
                for (int y = 0; y < 384; y++)
                {
                    avg += heatmapSamples[x, y];
                    if (heatmapSamples[x, y] / objectCount < 0.5){
                        avg -= 0.75;
                    }
                }
            }
            return Math.Max(0, avg) / 117888;
        }

        public override void Process(DifficultyHitObject current)
        {
            TouhosuHitObject obj = (TouhosuHitObject)(current.BaseObject);
            if (obj is AngeledProjectile p)
            {
                double pos_X = p.Position.X;
                double pos_Y = p.Position.Y;

                double angle = p.Angle;
                double speed = p.SpeedMultiplier;

                double move_X = Math.Cos(angle);
                double move_Y = Math.Sin(angle);

                while (pos_X >= 0 && pos_X < 307 && pos_Y >= 0 && pos_Y < 384 && speed > 0)
                {
                    heatmapSamples[(int)Math.Floor(pos_X), (int)Math.Floor(pos_Y)] += clockrate;
                    pos_X += move_X * speed;
                    pos_Y += move_Y * speed;
                }
            }
        }
    }
}
