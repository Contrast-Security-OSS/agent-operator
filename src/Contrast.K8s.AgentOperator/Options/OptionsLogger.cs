// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using NLog;

namespace Contrast.K8s.AgentOperator.Options;

public interface IOptionsLogger
{
    void LogOptionValue(string key, string defaultValue, string actualValue);
    void LogOptionValue(string key, long defaultValue, long actualValue);
    void LogOptionValue(string key, bool defaultValue, bool actualValue);
}

[UsedImplicitly]
public class OptionsLogger : IOptionsLogger
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void LogOptionValue(string key, string defaultValue, string actualValue)
    {
        if (!string.Equals(defaultValue, actualValue, StringComparison.Ordinal))
        {
            WriteLog(key, defaultValue, actualValue);
        }
    }

    public void LogOptionValue(string key, long defaultValue, long actualValue)
    {
        if (defaultValue != actualValue)
        {
            WriteLog(key, defaultValue, actualValue);
        }
    }

    public void LogOptionValue(string key, bool defaultValue, bool actualValue)
    {
        if (defaultValue != actualValue)
        {
            WriteLog(key, defaultValue, actualValue);
        }
    }

    private static void WriteLog<T>(string key, T defaultValue, T actualValue)
    {
        Logger.Info($"Option '{key}' was changed from '{defaultValue}' (default) -> '{actualValue}'.");
    }
}
