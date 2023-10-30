using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Landmarks.Scripts.Debugging;
using UnityEngine;

namespace Landmarks.Scripts.Progress
{
    public class XmlNode
    {
        public int Index { get; private set; }

        public XmlNode Parent { get; private set; }
        private readonly List<XmlNode> _children = new List<XmlNode>();
        public string Tag { get; set; }

        private Dictionary<string, string> _attributes;

        public string Name => GetAttribute("name");

        private XmlNode(string tag)
        {
            Tag = tag;
            _attributes = new Dictionary<string, string>();
        }

        /************************************************************************
         * Child-related Methods
         * *********************************************************************/
        public XmlNode this[int key]
        {
            get => _children[key];
            set => _children[key] = value;
        }

        private void AddChildTask(XmlNode child)
        {
            _children.Add(child);
            child.Index = _children.Count - 1;
        }

        private void RemoveChildTask(int index)
        {
            for (var i = index + 1; i < _children.Count; ++i)
            {
                _children[i].Index--;
            }

            _children.RemoveAt(index);
        }

        public int GetChildCount()
        {
            return _children.Count;
        }

        public IEnumerable<XmlNode> GetAllChildren()
        {
            return _children;
        }

        /************************************************************************
         * Task-related Methods
         * *********************************************************************/


        /// <summary>
        /// Set the current node to the next node, not including the children
        /// </summary>
        public static void SkipToNextNode(ref XmlNode node)
        {
            if (node == null) return;

            while (node.Parent != null)
            {
                var index = node.Index;
                if (index < node.Parent.GetChildCount() - 1)
                {
                    LM_Debug.Instance.Log(
                        "Change to next node in child: " + node.Name + " To " + node.Parent[index+1].Name, 1);
                    node = node.Parent[index + 1];
                    return;
                }

                node = node.Parent; // Move one level up
            }
            LM_Debug.Instance.Log("Change to parent node" + node.Name, 1);
        }

        /// <summary>
        /// Set the current node to the next node, including the children
        /// </summary>
        public static void MoveToNextNode(ref XmlNode node)
        {
            if (node == null) return;

            if (node.GetChildCount() == 0)
            {
                SkipToNextNode(ref node);
                return;
            }

            LM_Debug.Instance.Log(
                "Change to next node in child: " + node.Name + " To " + node[0].Name, 1);
            node = node[0];
        }

        /************************************************************************
         * Attribute-related Methods
         * *********************************************************************/
        public string GetAttribute(string key)
        {
            _attributes.TryGetValue(key, out var attribute);
            return attribute;
        }

        private void SetAttribute(string key, string value)
        {
            _attributes[key] = value;
        }

        private void SetAttributes(Dictionary<string, string> attributes)
        {
            _attributes = attributes;
        }

        private bool HasAttribute(string key)
        {
            return _attributes.ContainsKey(key);
        }

        public bool HasAttributeEqualTo(string key, string value)
        {
            return HasAttribute(key) && GetAttribute(key) == value;
        }

        private List<KeyValuePair<string, string>> AttributesToList()
        {
            return _attributes.ToList();
        }

        private IEnumerable<KeyValuePair<string, string>> GetSomeAttributes(int num)
        {
            if (num > _attributes.Count)
            {
                num = _attributes.Count;
            }
            else
            {
                return new List<KeyValuePair<string, string>>();
            }


            return AttributesToList().GetRange(0, num);
        }

        private string GetSomeAttributesToString(int num)
        {
            return GetSomeAttributes(num).Aggregate("", (current, pair) => current + $"{pair.Key}={pair.Value} ");
        }

        public string HierarchyToString(int depth)
        {
            var str = new string(' ', depth * 4) + $"{Tag} {GetSomeAttributesToString(3)}\n";
            return _children.Aggregate(str, (current, child) => current + child.HierarchyToString(depth + 1));
        }

        public static XmlNode ParseFromLines(List<string> lines)
        {
            if (lines.Count == 0) return null;

            var root = new XmlNode("Root");
            root.SetAttribute("name", "Root");
            var parent = root;

            foreach (var tag in lines.Select(line => line.Trim()))
            {
                // determine if it's an opening tag or a closing tag using regex
                var openingTag = Regex.Match(tag, @"^<[^/]+>$");
                var closingTag = Regex.Match(tag, @"^</[^/]+>$");

                // check if it's an opening tag

                if (openingTag.Success)
                {
                    // if it's an opening tag, create a new node and add it to the parent
                    var newChild = new XmlNode("")
                    {
                        Parent = parent
                    };

                    parent.AddChildTask(newChild);
                    parent = newChild;

                    // parse the tag
                    var tagString = tag.Substring(1, tag.Length - 2);
                    var firstSpace = tagString.IndexOf(' ');
                    var tagName = tagString.Substring(0, firstSpace);

                    var regex = new Regex(@"(\w+)\s*=\s*""([^""]*)""");
                    var matches = regex.Matches(tag);

                    var attributes = new Dictionary<string, string>();
                    foreach (Match match in matches)
                    {
                        // Groups[0] is the entire match, Groups[1] is the key, Groups[2] is the value
                        if (match.Groups.Count != 3) continue;

                        var key = match.Groups[1].Value;
                        var value = match.Groups[2].Value;
                        attributes[key] = value;
                    }

                    parent.Tag = tagName;
                    parent.SetAttributes(attributes);
                }
                else if (closingTag.Success)
                {
                    // if it's a closing tag, go back to the parent
                    parent.SetAttribute("completed", "true");
                    if (parent.Parent != null)
                        parent = parent.Parent;
                }
            }


            return root;
        }


        /************************************************************************
         * Helper Methods
         * *********************************************************************/

        // public string ToOpeningString(int depth)
        // {
        //     var str = "";
        //     for (var i = 0; i < depth; ++i)
        //     {
        //         str += "\t";
        //     }
        //
        //     str += $"<{ReplaceSpaceWithUnderscore(Tag)}";
        //     str = _attributes.Aggregate(str,
        //         (current, attribute) => current + $" {attribute.Key}=\"{attribute.Value}\"");
        //     str += ">";
        //     return str;
        // }
        //
        // public string ToClosingString()
        // {
        //     return $"</{ReplaceSpaceWithUnderscore(Tag)}>";
        // }

        public static string BuildOpeningString(string tagName, Dictionary<string, string> attributes, int depth)
        {
            var str = "";
            for (var i = 0; i < depth; ++i)
            {
                str += "\t";
            }

            str += $"<{tagName}";
            str = attributes.Aggregate(str,
                (current, attribute) => current + $" {attribute.Key}=\"{attribute.Value}\"");
            str += ">";
            return str;
        }

        public static string BuildOpeningString(string tagName, string name, int line, int depth)
        {
            return IndentText($"<{tagName} name=\"{name}\" line=\"{line}\">", depth);
        }

        public static string BuildClosingString(string tagName, int depth)
        {
            return IndentText($"</{tagName}>", depth);
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