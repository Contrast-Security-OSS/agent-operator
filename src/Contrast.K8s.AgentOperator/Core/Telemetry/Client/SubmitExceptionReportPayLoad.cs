// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Client;

public class SubmitExceptionReportPayload
{
    [JsonProperty("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonProperty("instance")]
    public string Instance { get; set; }

    [JsonProperty("tags")]
    public IReadOnlyDictionary<string, string> Tags { get; set; }

    [JsonProperty("logger")]
    public string? Logger { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("occurrences")]
    public int Occurrences { get; set; }

    /// <summary>
    /// The exception, or a list of exceptions if nested - ordered by oldest to newest.
    /// Exceptions in some languages can be tree structures, in which case, use *reverse* depth-first traversal.
    /// (don't google that, I might have made that up)
    /// The deepest node within a branch should be first, with branches ordered from left to right, recursively.
    /// </summary>
    [JsonProperty("exceptions")]
    public IReadOnlyList<Exception> Exceptions { get; set; }

    public SubmitExceptionReportPayload(DateTimeOffset timestamp,
                                        string instance,
                                        IReadOnlyDictionary<string, string> tags,
                                        IReadOnlyList<Exception> exceptions,
                                        string? message = null,
                                        string? logger = null,
                                        int occurrences = 1)
    {
        Timestamp = timestamp;
        Instance = instance;
        Tags = tags;
        Message = message;
        Exceptions = exceptions;
        Logger = logger;
        Occurrences = occurrences;
    }

    public class Exception
    {
        /// <summary>
        /// The name of the type of exception. In .NET this would be the full type name including namespace (e.g. System.Exception).
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// The name of the module that the exception exists in. In .NET this would be the assembly full name (e.g. mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089).
        /// </summary>
        [JsonProperty("module")]
        public string? Module { get; set; }

        /// <summary>
        /// An optional value from the exception. Under .NET this is the exception message.
        /// </summary>
        [JsonProperty("value")]
        public string? Value { get; set; }

        /// <summary>
        /// Stack frames in the order of oldest to newest. Under .NET, the native frames must be reversed.
        /// </summary>
        [JsonProperty("stackFrames")]
        public IReadOnlyList<StackFrame> StackFrames { get; set; }

        public Exception(string type, IReadOnlyList<StackFrame> stackFrames, string? value = null, string? module = null)
        {
            Type = type;
            StackFrames = stackFrames;
            Value = value;
            Module = module;
        }
    }

    public class StackFrame
    {
        /// <summary>
        /// The name of the function that the frame executed within. In .NET this would be the method name excluding type information.
        /// </summary>
        [JsonProperty("function")]
        public string Function { get; set; }

        /// <summary>
        /// The name of the type that the frame executed within. In .NET this would be the full type name including namespace (e.g. System.String).
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// The name of the assembly that the frame execute within. In .NET this would be the assembly full name (e.g. mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089).
        /// </summary>
        [JsonProperty("module")]
        public string? Module { get; set; }

        /// <summary>
        /// False when in customer code or within system code (base libraries or contrast shipped libraries).
        /// Should only be true in Contrast first-party code.
        /// </summary>
        [JsonProperty("inContrast")]
        public bool InContrast { get; set; }

        public StackFrame(string function, string type, bool inContrast, string? module = null)
        {
            Function = function;
            Type = type;
            InContrast = inContrast;
            Module = module;
        }
    }
}
