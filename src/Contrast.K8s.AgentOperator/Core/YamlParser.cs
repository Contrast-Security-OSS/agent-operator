using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Contrast.K8s.AgentOperator.Core
{
    public interface IYamlParser
    {
        Dictionary<string, YamlSetting> Parse(Stream stream, out ParseResult parseResult);
        Dictionary<string, YamlSetting> Parse(string str, out ParseResult parseResult);
    }

    public class YamlParser : IYamlParser
    {
        // Basically copied from Contrast.Common, but made immutable. 

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Dictionary<string, YamlSetting> Parse(Stream stream, out ParseResult parseResult)
        {
            parseResult = new ParseResult(false);
            var configDictionary = new Dictionary<string, YamlSetting>();

            try
            {
                var yaml = new YamlStream();
                yaml.Load(new StreamReader(stream));

                if (yaml.Documents.Count > 0)
                {
                    if (yaml.Documents[0].RootNode is YamlMappingNode rootMappingNode)
                    {
                        RecurseYaml(rootMappingNode, new List<string?>(), configDictionary);
                    }
                }

                parseResult = new ParseResult(true);
            }
            catch (Exception ex)
            {
                if (ex is YamlException yex)
                {
                    parseResult = new ParseResult(
                        false,
                        yex.Start.Line,
                        yex.Start.Column,
                        yex.End.Column,
                        yex.Message
                    );

                    // the user probably introduced invalid YAML; we don't want to push this exception to telemetry
                    Logger.Warn($"Error parsing YAML stream at line:{yex.Start.Line}. Error: {ex.GetType().Name}: {ex.Message}");
                }
                else
                {
                    Logger.Warn(ex, "Error parsing YAML stream.");
                }
            }

            return configDictionary;
        }

        public Dictionary<string, YamlSetting> Parse(string str, out ParseResult parseResult)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;

            return Parse(stream, out parseResult);
        }

        private static void RecurseYaml(YamlNode? currentNode,
                                        List<string?> depthStack,
                                        IDictionary<string, YamlSetting> configDictionary)
        {
            if (currentNode is YamlMappingNode mappingNode)
            {
                foreach (var childNode in mappingNode.Children)
                {
                    if (childNode.Key is YamlScalarNode key)
                    {
                        depthStack.Add(key.Value);

                        if (childNode.Value is YamlScalarNode valueScalar)
                        {
                            string? value = null;
                            if (!string.IsNullOrEmpty(valueScalar.Value))
                            {
                                value = valueScalar.Value;
                            }

                            var settingName = string.Join(".", depthStack.ToArray());

                            configDictionary[settingName] = new YamlSetting(settingName, value, key.Start.Column, valueScalar.Start.Column, key.Start.Line);
                        }
                        else
                        {
                            if (childNode.Value is YamlMappingNode valueMapping)
                            {
                                RecurseYaml(valueMapping, depthStack, configDictionary);
                            }
                        }

                        if (depthStack.Count > 0)
                        {
                            depthStack.RemoveAt(depthStack.Count - 1);
                        }
                    }
                }
            }
        }
    }

    public record ParseResult(bool IsValid,
                              int LineNumber = -1,
                              int StartColumn = -1,
                              int EndColumn = -1,
                              string? Error = null);

    public record YamlSetting(string Key,
                              string? Value,
                              int Line,
                              int KeyColumn,
                              int ValueColumn);
}
