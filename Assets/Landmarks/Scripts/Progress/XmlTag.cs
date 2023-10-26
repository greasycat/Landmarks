using System.Collections.Generic;
using System.Linq;

namespace Landmarks.Scripts.Progress
{
    public class XmlTag
    {
        private string Name { get; set; }
        private Dictionary<string, string> Attributes { get; set; }

        public XmlTag(string name)
        {
            Name = name;
            Attributes = new Dictionary<string, string>();
        }

        public XmlTag(string name, Dictionary<string, string> attributes)
        {
            Name = name;
            Attributes = attributes;
        }

        public void AddAttribute(string key, string value)
        {
            Attributes.Add(key, value);
        }

        public string ToOpeningString(int depth)
        {
            var str = "";
            for (var i = 0; i < depth; ++i)
            {
                str += "\t";
            }

            str += $"<{Name}";
            str = Attributes.Aggregate(str,
                (current, attribute) => current + $" {attribute.Key}=\"{attribute.Value}\"");
            str += ">";
            return str;
        }

        public string ToClosingString()
        {
            return $"</{Name}>";
        }

        // public static string BuildOpeningString(string name, Dictionary<string, string> attributes, int depth)
        // {
        //     var str = "";
        //     for (var i = 0; i < depth; ++i)
        //     {
        //         str += "\t";
        //     }
        //
        //     str += $"<{name}";
        //     str = attributes.Aggregate(str,
        //         (current, attribute) => current + $" {attribute.Key}=\"{attribute.Value}\"");
        //     str += ">";
        //     return str;
        // }

        public static string BuildOpeningString(string name, int depth)
        {
            return IndentText($"<{name}>", depth);
        }

        public static string BuildClosingString(string name, int depth)
        {
            return IndentText($"</{name}>", depth);
        }

        private static string IndentText(string text, int depth)
        {
            var indent = "";
            for (var i = 0; i < depth; i++)
            {
                indent += "\t";
            }

            return indent + text;
        }
    }
}