using osu.Framework.Localisation;
using osu.Game.Rulesets.Touhosu.Objects.Drawables;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;

namespace osu.Game.Rulesets.Touhosu.Mods
{
    public class TouhosuModFlashlight : ModHidden
    {
        public override double ScoreMultiplier => 1.06;
        public override string Name => "Flashlight";
        public override string Acronym => "FL";
        public override IconUsage? Icon => OsuIcon.ModFlashlight;
        public override LocalisableString Description => "Bullets will become visible near you.";

        public override void ApplyToDrawableHitObject(DrawableHitObject dho)
        {
            base.ApplyToDrawableHitObject(dho);

            if (dho is DrawableAngeledProjectile p)
            {
                p.HiddenApplied = true;
                p.Flashlight = true;
            }
        }

        protected override void ApplyIncreasedVisibilityState(DrawableHitObject hitObject, ArmedState state)
        {
        }

        protected override void ApplyNormalVisibilityState(DrawableHitObject hitObject, ArmedState state)
        {
        }
    }
}
