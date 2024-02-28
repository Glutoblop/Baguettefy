namespace Baguettefy.Core
{
    public static class RateLimit
    {
        public enum EDiscordCall
        {
            SendMessage,
            DeleteMessage,
            Reaction,
            EditMember,
            EditMemberNickname,
            EditUsername,
            EditChannel,
            CreateChannel,
            DeleteChannel,

            EditMessage,
            EditPermission,

            GetChannel,
            GetMessage,
            GetUser,
            GetGuild
        }

        private class RateLimitTracker
        {
            /// <summary>How many calls you can make in the given seconds.</summary>
            public int CallLimit { get; }
            /// <summary>The seconds you are allowed to perform the limited calls in.</summary>
            public int RateLimit { get; }


            /// <summary>How many times this has been called, reset when it hits Limit.</summary>
            public int BatchCount;

            /// <summary>The first time you call after the BatchCount is 0, mark this timestamp.
            /// If you are about to hit the limit, you need to know how long it took you to do that limit, if its outside the Seconds bounds
            /// you need to wait the remaining time before making that call.</summary>
            public DateTime FirstCalledThisSet;

            public RateLimitTracker(int limit, int rateLimit)
            {
                CallLimit = limit;
                RateLimit = rateLimit;

                FirstCalledThisSet = DateTime.UtcNow;
            }
        }

        private static readonly Dictionary<EDiscordCall, RateLimitTracker> _RateLimits = new()
        {
            { EDiscordCall.SendMessage , new RateLimitTracker(5,5)},
            { EDiscordCall.DeleteMessage , new RateLimitTracker(5,5)},
            { EDiscordCall.Reaction , new RateLimitTracker(300,300)},
            { EDiscordCall.EditMember , new RateLimitTracker(10,10)},
            { EDiscordCall.EditMemberNickname , new RateLimitTracker(1,1)},
            { EDiscordCall.EditUsername , new RateLimitTracker(2,3600)},
            { EDiscordCall.EditChannel , new RateLimitTracker(2,10)},
            { EDiscordCall.CreateChannel , new RateLimitTracker(2,10)},
            { EDiscordCall.DeleteChannel , new RateLimitTracker(2,10)},

            { EDiscordCall.EditMessage , new RateLimitTracker(5,10)},
            { EDiscordCall.EditPermission , new RateLimitTracker(5,10)},

            { EDiscordCall.GetChannel , new RateLimitTracker(10,10)},
            { EDiscordCall.GetMessage , new RateLimitTracker(10,10)},
            { EDiscordCall.GetUser , new RateLimitTracker(10,10)},
            { EDiscordCall.GetGuild, new RateLimitTracker(10,10)}
        };

        public static async Task Check(EDiscordCall callType)
        {
            if (_RateLimits[callType].BatchCount == 0)
            {
                _RateLimits[callType].FirstCalledThisSet = DateTime.UtcNow;
            }

            _RateLimits[callType].BatchCount++;

            //if you have hit the limit of calls, then you need to see how long it took you to hit this limit
            var secondsSinceLimitHit = (DateTime.UtcNow - _RateLimits[callType].FirstCalledThisSet).TotalSeconds;

            //if you haven't hit the limit yet, you don't care
            if (_RateLimits[callType].BatchCount >= _RateLimits[callType].CallLimit)
            {
                if (secondsSinceLimitHit <= _RateLimits[callType].CallLimit)
                {
                    var remainingBatchTime = _RateLimits[callType].CallLimit - secondsSinceLimitHit;

                    Console.WriteLine($"[RateLimit] Rate limit reached for {Enum.GetName(callType)}. {_RateLimits[callType].BatchCount} hits in {secondsSinceLimitHit} seconds. " +
                                      $"Waiting {remainingBatchTime + _RateLimits[callType].RateLimit} seconds to start the new batch.");
                    await Task.Delay(TimeSpan.FromSeconds(remainingBatchTime + _RateLimits[callType].RateLimit));

                    _RateLimits[callType].BatchCount = 0;
                }
                else
                {
                    Console.WriteLine($"[RateLimit] Rate limit missed for {Enum.GetName(callType)}. {_RateLimits[callType].BatchCount} hits in {secondsSinceLimitHit} seconds.");
                }
            }

            if (secondsSinceLimitHit > _RateLimits[callType].RateLimit)
            {
                _RateLimits[callType].BatchCount = 0;
                //Console.WriteLine($"[RateLimit] reset {Enum.GetName(callType)} batch count. {secondsSinceLimitHit} seconds past, batches are in {_RateLimits[callType].RateLimit}.");
            }

            //If its been longer than the rate limit to hit the call limit, then you need to wait the remainder of time before you enter the next limit.
        }
    }
}
