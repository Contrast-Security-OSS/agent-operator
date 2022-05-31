using System.Collections.Concurrent;
using GlobExpressions;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions
{
    public interface IGlobMatcher
    {
        bool Matches(string pattern, string value);
    }

    public class GlobMatcher : IGlobMatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<string, Glob> _cache = new();

        public bool Matches(string pattern, string value)
        {
            var glob = _cache.GetOrAdd(pattern, s =>
            {
                Logger.Trace($"Compiling glob pattern '{s}'.");
                return new Glob(s, GlobOptions.CaseInsensitive | GlobOptions.Compiled);
            });
            return glob.IsMatch(value);
        }
    }
}
