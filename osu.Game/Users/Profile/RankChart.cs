﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Users.Profile
{
    public class RankChart : Container
    {
        private readonly SpriteText rankText, performanceText, relativeText;
        private readonly RankChartLineGraph graph;

        private int[] ranks, performances;
        private int rank, performance, countryRank;

        private readonly User user;

        public RankChart(User user)
        {
            this.user = user;
            Padding = new MarginPadding { Vertical = 10 };
            Children = new Drawable[]
            {
                rankText = new OsuSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Font = @"Exo2.0-RegularItalic",
                    TextSize = 25
                },
                relativeText = new OsuSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Font = @"Exo2.0-RegularItalic",
                    Y = 25,
                    TextSize = 13
                },
                performanceText = new OsuSpriteText
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Font = @"Exo2.0-RegularItalic",
                    TextSize = 13
                },
                graph = new RankChartLineGraph
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.X,
                    Y = -13,
                    DefaultValueCount = 90,
                    BallRelease = () =>
                    {
                        rankText.Text = $"#{rank:#,#}";
                        performanceText.Text = $"{performance:#,#}pp";
                        relativeText.Text = $"{this.user.Country?.FullName} #{countryRank:#,#}";
                    },
                    BallMove = index =>
                    {
                        rankText.Text = $"#{ranks[index]:#,#}";
                        performanceText.Text = $"{performances[index]:#,#}pp";
                        relativeText.Text = index == ranks.Length ? "Now" : $"{ranks.Length - index} days ago";
                        //plural should be handled in a general way
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            graph.Colour = colours.Yellow;
            Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.Sleep(1000);

                // put placeholder data here to show the transform
                rank = 12345;
                countryRank = 678;
                performance = 4567;
                ranks = Enumerable.Range(1234, 80).ToArray();
                performances = ranks.Select(x => 6000 - x).ToArray();
                // use logarithmic coordinates
                graph.Values = ranks.Select(x => -(float)Math.Log(x));
                graph.ResetBall();
            });
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.DrawSize) != 0)
            {
                graph.Height = DrawHeight - 71;
            }

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        private class RankChartLineGraph : LineGraph
        {
            private readonly CircularContainer ball;
            private bool ballShown;

            private const double transform_duration = 100;

            public Action<int> BallMove;
            public Action BallRelease;

            public RankChartLineGraph()
            {
                Add(ball = new CircularContainer
                {
                    Size = new Vector2(8),
                    Masking = true,
                    Origin = Anchor.Centre,
                    Alpha = 0,
                    RelativePositionAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both }
                    }
                });
            }

            public void ResetBall()
            {
                ball.MoveTo(new Vector2(1, ((ActualMaxValue - Values.Last()) / (ActualMaxValue - ActualMinValue)).Value), ballShown ? transform_duration : 0, EasingTypes.OutQuint);
                ball.Show();
                BallRelease();
                ballShown = true;
            }

            protected override bool OnMouseMove(InputState state)
            {
                if (ballShown)
                {
                    var values = Values as IList<float>;
                    var position = ToLocalSpace(state.Mouse.NativeState.Position);
                    int count = Math.Max(values.Count, DefaultValueCount);
                    int index = (int)Math.Round(position.X / DrawWidth * (count - 1));
                    if (index >= count - values.Count)
                    {
                        int i = index + values.Count - count;
                        float value = values[i];
                        float y = ((ActualMaxValue - value) / (ActualMaxValue - ActualMinValue)).Value;
                        if (Math.Abs(y * DrawHeight - position.Y) <= 8f)
                        {
                            ball.MoveTo(new Vector2(index / (float)(count - 1), y), transform_duration, EasingTypes.OutQuint);
                            BallMove(i);
                        }
                    }
                }
                return base.OnMouseMove(state);
            }

            protected override void OnHoverLost(InputState state)
            {
                if (ballShown)
                    ResetBall();
                base.OnHoverLost(state);
            }
        }
    }
}
