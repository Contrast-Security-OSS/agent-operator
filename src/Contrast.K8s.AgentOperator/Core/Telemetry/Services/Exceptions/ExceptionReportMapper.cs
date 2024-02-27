// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Contrast.K8s.AgentOperator.Core.Telemetry.Client;
using Contrast.K8s.AgentOperator.Core.Telemetry.Models;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Services.Exceptions;

public class ExceptionReportMapper
{
    public SubmitExceptionReportPayload CreateFrom(ExceptionReport report,
                                                   IReadOnlyDictionary<string, string> defaultTags,
                                                   string machineId,
                                                   int occurrences)
    {
        return new(
            report.Timestamp,
            machineId,
            defaultTags,
            CreateFromException(report.Exception).ToList(),
            report.LogMessage,
            report.LoggerName,
            occurrences
        );
    }

    public IEnumerable<SubmitExceptionReportPayload.Exception> CreateFromException(Exception exception)
    {
        if (exception is AggregateException ae)
        {
            foreach (var inner in ae.InnerExceptions.SelectMany(CreateFromException))
            {
                yield return inner;
            }
        }
        else if (exception.InnerException != null)
        {
            foreach (var inner in CreateFromException(exception.InnerException))
            {
                yield return inner;
            }
        }

        var stackTrace = new StackTrace(exception, false).GetFrames()?.Select(CreateFromStackFrame)
                         ?? Enumerable.Empty<SubmitExceptionReportPayload.StackFrame>();

        yield return new SubmitExceptionReportPayload.Exception(
            exception.GetType().FullName ?? "(unknown)",
            stackTrace.Reverse().ToList(),
            RenderExceptionMessage(exception),
            exception.GetType()?.Assembly.FullName
        );
    }

    public SubmitExceptionReportPayload.StackFrame CreateFromStackFrame(StackFrame stackFrame)
    {
        var method = stackFrame.GetMethod();
        var type = method!.DeclaringType;
        var assembly = type?.Assembly;

        var safe = IsAllowed(type, assembly);

        var methodName = safe
            ? method.Name
            : HashIdentifier(method.Name);

        string? typeName;
        if (type?.FullName != null)
        {
            typeName = safe ? type.FullName : HashIdentifier(type.FullName);
        }
        else
        {
            typeName = "(unknown)";
        }

        string? assemblyName = null;
        if (assembly != null)
        {
            if (safe)
            {
                assemblyName = assembly.FullName;
            }
            else
            {
                // mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
                var name = assembly.GetName();
                assemblyName = $"{HashIdentifier(name.Name!)}, Version={name.Version}, Culture=neutral, PublicKeyToken=null";
            }
        }

        (typeName, methodName) = DemangleAsyncFunctionName(typeName, methodName);

        return new SubmitExceptionReportPayload.StackFrame(
            methodName,
            typeName,
            typeName.StartsWith("Contrast.", StringComparison.OrdinalIgnoreCase),
            assemblyName
        );
    }

    private bool IsAllowed(Type? type, Assembly? assembly)
    {
        return true;
    }

    private static string HashIdentifier(string text, int length = 8)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        var hashString = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            hashString.AppendFormat("{0:x2}", hash[i]);
        }

        return hashString.ToString();
    }

    private static string RenderExceptionMessage(Exception exception)
    {
        return exception switch
        {
            IOException e when e.GetType().Name == "ZLibException" =>
                $"{e.Message} (HResult: {e.HResult}, HResultMessage: '{GetHResultMessage(e.HResult)}')",
            IOException e =>
                $"IOException message possibly tainted and was suppressed. (HResult: {e.HResult}, HResultMessage: '{GetHResultMessage(e.HResult)}')",
            UnauthorizedAccessException e =>
                $"UnauthorizedAccessException message possibly tainted and was suppressed. (HResult: {e.HResult}, HResultMessage: '{GetHResultMessage(e.HResult)}')",
            _ => exception.Message
        };
    }

    private static string GetHResultMessage(int errorCode)
    {
        return Marshal.GetExceptionForHR(errorCode)?.Message ?? "<none>";
    }

    private static FunctionName DemangleAsyncFunctionName(string type, string method)
    {
        // Sentry.Extensibility.SentryStackTraceFactory.cs

        if (method != "MoveNext")
        {
            return new FunctionName(type, method);
        }

        // Search for the function name in angle brackets followed by d__<digits>.
        //
        // Change:
        //   RemotePrinterService+<UpdateNotification>d__24 in MoveNext at line 457:13
        // to:
        //   RemotePrinterService in UpdateNotification at line 457:13

        // Modified this, nested classes appear to use . over +.
        var match = Regex.Match(type, @"^(.*)[\+\.]<(\w*)>d__\d*$");
        if (match.Success && match.Groups.Count == 3)
        {
            return new FunctionName(match.Groups[1].Value, match.Groups[2].Value);
        }

        return new FunctionName(type, method);
    }

    private record FunctionName(string Type, string Method);
}
