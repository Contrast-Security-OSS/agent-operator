// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility
{
    public static class JavaArgumentParser
    {
        /* https://docs.oracle.com/en/java/javase/17/docs/specs/jvmti.html#tooloptions
         * JAVA_TOOL_OPTIONS variable will be broken into options at white-space boundaries.
         * White-space characters include space, tab, carriage-return, new-line, vertical-tab, and form-feed.
         * Sequences of white-space characters are considered equivalent to a single white-space character.
         * No white-space is included in the options unless quoted.
         * Quoting is as follows:
         *  
         *  All characters enclosed between a pair of single quote marks (''), except a single quote, are quoted.
         *  Double quote characters have no special meaning inside a pair of single quote marks.
         *  All characters enclosed between a pair of double quote marks (""), except a double quote, are quoted.
         *  Single quote characters have no special meaning inside a pair of double quote marks.
         *  A quoted part can start or end anywhere in the variable.
         *  White-space characters have no special meaning when quoted -- they are included in the option like any other character and do not mark white-space boundaries.
         *  The pair of quote marks is not included in the option.
         */

        public static IEnumerable<string> ParseArguments(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                yield break;
            }

            var state = State.Unknown;
            var wordStart = 0;
            var wordLength = 0;
            var currentQuote = '\0';

            var workspace = input.ToCharArray();

            var i = 0;
            while (i < input.Length)
            {
                var c = workspace[i];

                switch (state)
                {
                    case State.InBoundary:
                    case State.Unknown:
                        if (IsQuote(c))
                        {
                            wordStart = i;
                            wordLength = 0;
                            currentQuote = c;
                            state = State.InQuotedWord;
                        }
                        else if (IsWhitespace(c))
                        {
                            state = State.InBoundary;
                        }
                        else
                        {
                            wordStart = i;
                            wordLength = 0;
                            state = State.InWord;
                        }

                        break;
                    case State.InQuotedWord:
                        wordLength++;
                        if (c == currentQuote)
                        {
                            state = State.InWord;
                        }

                        break;
                    case State.InWord:
                        wordLength++;
                        if (IsQuote(c))
                        {
                            currentQuote = c;
                            state = State.InQuotedWord;
                        }
                        else if (IsWhitespace(c))
                        {
                            state = State.InBoundary;

                            yield return input.Substring(wordStart, wordLength);
                        }

                        break;
                    case State.EndOfInput:
                        wordLength++;
                        if (!IsWhitespace(c))
                        {
                            yield return input.Substring(wordStart, wordLength);
                        }

                        yield break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (i + 1 == input.Length)
                {
                    if (state == State.InQuotedWord)
                    {
                        throw new JavaArgumentParserException("Unmatched quote in arguments");
                    }
                    state = State.EndOfInput;
                }
                else
                {
                    i++;
                }
            }
        }

        private static bool IsQuote(char c)
        {
            return c == '"' || c == '\'';
        }

        private static bool IsWhitespace(char c)
        {
            return (c >= 0x9 && c <= 0xD) || c == 0x20; //(tab, carriage-return, new-line, vertical-tab, and form-feed) or space
        }

        private enum State
        {
            Unknown,
            InBoundary,
            InQuotedWord,
            InWord,
            EndOfInput
        }


    }
    public class JavaArgumentParserException : Exception
    {
        public JavaArgumentParserException()
        {
        }

        public JavaArgumentParserException(string message)
            : base(message)
        {
        }

        public JavaArgumentParserException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

}
