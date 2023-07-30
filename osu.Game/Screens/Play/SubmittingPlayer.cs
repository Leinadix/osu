// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Game.Online.Spectator;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// A player instance which supports submitting scores to an online store.
    /// </summary>
    public abstract partial class SubmittingPlayer : Player
    {
        /// <summary>
        /// The token to be used for the current submission. This is fetched via a request created by <see cref="CreateTokenRequest"/>.
        /// </summary>
        private long? token;

        [Resolved]
        private IAPIProvider api { get; set; }

        [Resolved]
        private SpectatorClient spectatorClient { get; set; }

        private TaskCompletionSource<bool> scoreSubmissionSource;

        protected SubmittingPlayer(PlayerConfiguration configuration = null)
            : base(configuration)
        {
        }

        protected override void LoadAsyncComplete()
        {
            base.LoadAsyncComplete();
            handleTokenRetrieval();
        }

        private bool handleTokenRetrieval()
        {
            // Token request construction should happen post-load to allow derived classes to potentially prepare DI backings that are used to create the request.
            var tcs = new TaskCompletionSource<bool>();

            if (Mods.Value.Any(m => !m.UserPlayable))
            {
                handleTokenFailure(new InvalidOperationException("Non-user playable mod selected."));
                return false;
            }

            if (!api.IsLoggedIn)
            {
                handleTokenFailure(new InvalidOperationException("API is not online."));
                return false;
            }

            var req = CreateTokenRequest();

            if (req == null)
            {
                handleTokenFailure(new InvalidOperationException("Request could not be constructed."));
                return false;
            }

            req.Success += r =>
            {
                Logger.Log($"Score submission token retrieved ({r.ID})");
                token = r.ID;
                tcs.SetResult(true);
            };
            req.Failure += handleTokenFailure;

            api.Queue(req);

            // Generally a timeout would not happen here as APIAccess will timeout first.
            if (!tcs.Task.Wait(30000))
                req.TriggerFailure(new InvalidOperationException("Token retrieval timed out (request never run)"));

            return true;

            void handleTokenFailure(Exception exception)
            {
                tcs.SetResult(false);

                if (HandleTokenRetrievalFailure(exception))
                {
                    if (string.IsNullOrEmpty(exception.Message))
                        Logger.Error(exception, "Failed to retrieve a score submission token.");
                    else
                        Logger.Log($"You are not able to submit a score: {exception.Message}", level: LogLevel.Important);

                    Schedule(() =>
                    {
                        ValidForResume = false;
                        this.Exit();
                    });
                }
                else
                {
                    // Gameplay is allowed to continue, but we still should keep track of the error.
                    // In the future, this should be visible to the user in some way.
                    Logger.Log($"Score submission token retrieval failed ({exception.Message})");
                }
            }
        }

        /// <summary>
        /// Called when a token could not be retrieved for submission.
        /// </summary>
        /// <param name="exception">The error causing the failure.</param>
        /// <returns>Whether gameplay should be immediately exited as a result. Returning false allows the gameplay session to continue. Defaults to true.</returns>
        protected virtual bool HandleTokenRetrievalFailure(Exception exception) => true;

        protected override async Task PrepareScoreForResultsAsync(Score score)
        {
            await base.PrepareScoreForResultsAsync(score).ConfigureAwait(false);

            score.ScoreInfo.Date = DateTimeOffset.Now;

            await submitScore(score).ConfigureAwait(false);
            spectatorClient.EndPlaying(GameplayState);
        }

        [Resolved]
        private RealmAccess realm { get; set; }

        protected override void StartGameplay()
        {
            base.StartGameplay();

            // User expectation is that last played should be updated when entering the gameplay loop
            // from multiplayer / playlists / solo.
            realm.WriteAsync(r =>
            {
                var realmBeatmap = r.Find<BeatmapInfo>(Beatmap.Value.BeatmapInfo.ID);
                if (realmBeatmap != null)
                    realmBeatmap.LastPlayed = DateTimeOffset.Now;
            });

            spectatorClient.BeginPlaying(token, GameplayState, Score);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            bool exiting = base.OnExiting(e);

            if (LoadedBeatmapSuccessfully)
            {
                Task.Run(async () =>
                {
                    await submitScore(Score.DeepClone()).ConfigureAwait(false);
                    spectatorClient.EndPlaying(GameplayState);
                }).FireAndForget();
            }

            return exiting;
        }

        /// <summary>
        /// Construct a request to be used for retrieval of the score token.
        /// Can return null, at which point <see cref="HandleTokenRetrievalFailure"/> will be fired.
        /// </summary>
        [CanBeNull]
        protected abstract APIRequest<APIScoreToken> CreateTokenRequest();

        /// <summary>
        /// Construct a request to submit the score.
        /// Will only be invoked if the request constructed via <see cref="CreateTokenRequest"/> was successful.
        /// </summary>
        /// <param name="score">The score to be submitted.</param>
        /// <param name="token">The submission token.</param>
        protected abstract APIRequest<MultiplayerScore> CreateSubmissionRequest(Score score, long token);

        private Task submitScore(Score score)
        {
            if (score.ScoreInfo.Ruleset.ShortName == "Touhosu")
            {
                string username = score.ScoreInfo.User.Username;
                int mapID = score.ScoreInfo.BeatmapInfo.OnlineID;
                int objectCount = score.ScoreInfo.MaximumStatistics.GetValueOrDefault(HitResult.Perfect);
                int missCount = score.ScoreInfo.Statistics.GetValueOrDefault(HitResult.Miss);
                int hitCount = missCount + score.ScoreInfo.Statistics.GetValueOrDefault(HitResult.Perfect);
                string mods = "+";
                double clockRate = 1.0;
                foreach (Mod m in score.ScoreInfo.Mods)
                {
                    mods += m.Acronym + " ";

                    if (m.Acronym == "DT" || m.Acronym == "NC" || m.Acronym == "HT" || m.Acronym == "DC")
                    {
                        clockRate = ((ModRateAdjust)m).SpeedChange.Value;
                        mods += $"({clockRate.ToString()}x) ";
                    }

                    if (m.Acronym == "NF")
                    {
                        objectCount *= 50; // Dont submit score
                    }

                }
                string starRating = (objectCount / 15 / (score.ScoreInfo.BeatmapInfo.Length / 1000 / clockRate)).ToString().Replace('.', ',');

                if (hitCount >= objectCount)
                {


                    string url = $"https://docs.google.com/forms/d/e/1FAIpQLSe_XnonydP9dDVW8GmP9uXofyjgFp8FJoOM7cwRej-4mno8qA/formResponse?&submit=Submit?usp=pp_url&entry.1540326648={username}&entry.472573208={mapID}&entry.1373202477={objectCount}&entry.2076057473={missCount}&entry.829082443={starRating}&entry.1748110803={mods}";

                    using (var client = new HttpClient())
                    {
                        var response = client.GetAsync(url).GetAwaiter().GetResult();
                    }
                }
            }

            // token may be null if the request failed but gameplay was still allowed (see HandleTokenRetrievalFailure).
            if (token == null)
                return Task.CompletedTask;

            if (scoreSubmissionSource != null)
                return scoreSubmissionSource.Task;

            // if the user never hit anything, this score should not be counted in any way.
            if (!score.ScoreInfo.Statistics.Any(s => s.Key.IsHit() && s.Value > 0))
                return Task.CompletedTask;

            scoreSubmissionSource = new TaskCompletionSource<bool>();
            var request = CreateSubmissionRequest(score, token.Value);

            request.Success += s =>
            {
                score.ScoreInfo.OnlineID = s.ID;
                score.ScoreInfo.Position = s.Position;

                scoreSubmissionSource.SetResult(true);
            };

            request.Failure += e =>
            {
                Logger.Error(e, $"Failed to submit score ({e.Message})");
                scoreSubmissionSource.SetResult(false);
            };

            api.Queue(request);
            return scoreSubmissionSource.Task;
        }
    }
}
