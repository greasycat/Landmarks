using System;
using System.Collections.Generic;
using System.IO;
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
        // public string Tag { get; set; }

        private Dictionary<string, string> _attributes;
        
        public string TagName { set; get; }

        public string Name
        {
            get => GetAttribute("name");
            set => SetAttribute("name", value);
        }
        

        private XmlNode(string tagName)
        {
            TagName = tagName;
            _attributes = new Dictionary<string, string>();
        }

        public XmlNode(string tagName, Dictionary<string, string> attributes)
        {
            TagName = tagName;
            _attributes = attributes;
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
            if (_attributes == null) _attributes = new Dictionary<string, string>();
            _attributes[key] = value;
        }

        private void SetAttributes(Dictionary<string, string> attributes)
        {
            _attributes = attributes;
        }

        public bool HasAttribute(string key)
        {
            return _attributes.ContainsKey(key);
        }

        public bool HasAttributeEqualTo(string key, string value)
        {
            return HasAttribute(key) && GetAttribute(key) == value;
        }
        
        public void RemoveAttribute(string key)
        {
            if (_attributes.ContainsKey(key))
            {
                _attributes.Remove(key);
            }
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

        public string GetSomeAttributesToString(int num)
        {
            return GetSomeAttributes(num).Aggregate("", (current, pair) => current + $"{pair.Key}={pair.Value} ");
        }

        public string HierarchyToString(int depth)
        {
            var str = new string(' ', depth * 4) + $"{Name} {GetSomeAttributesToString(3)}\n";
            return _children.Aggregate(str, (current, child) => current + child.HierarchyToString(depth + 1));
        }

        private static void ParseFromLine(string line, ref XmlNode parent)
        {
            var tagString = line.Substring(1, line.Length - 2);
            var firstSpace = tagString.IndexOf(' ');
            var tagName = tagString.Substring(0, firstSpace);

            var regex = new Regex(@"(\w+)\s*=\s*""([^""]*)""");
            var matches = regex.Matches(line);

            var attributes = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                // Groups[0] is the entire match, Groups[1] is the key, Groups[2] is the value
                if (match.Groups.Count != 3) continue;

                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                attributes[key] = value;
            }

            // parent.Tag = tagName;
            parent.SetAttributes(attributes);
        }

        public static XmlNode ParseFromLines(List<string> lines, Dictionary<string, XmlNode> lookup = null, string key = null)
        {
            if (lines.Count == 0) return new XmlNode("NA");

            var root = new XmlNode("Root");
            root.SetAttribute("name", "Root");
            var parent = root;

            foreach (var tagLine in lines.Select(line => line.Trim()))
            {
                // determine if it's an opening tag or a closing tag using regex
                var openingTag = Regex.Match(tagLine, @"^<[^/]+>$");
                var closingTag = Regex.Match(tagLine, @"^</[^/]+>$");

                // check if it's an opening tag

                if (openingTag.Success)
                {
                    // if it's an opening tag, create a new node and add it to the parent
                    var newChild = new XmlNode("new")
                    {
                        Parent = parent
                    };

                    parent.AddChildTask(newChild);
                    parent = newChild;

                    // parse the tag
                    ParseFromLine(tagLine, ref parent);
                    
                    if (!string.IsNullOrEmpty(key) && lookup != null)
                        lookup.Add(parent.GetAttribute(key), parent);
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

        public static string BuildOpeningString(string tagName, Dictionary<string, string> attributes, int depth)
        {
            var indent = new string(' ', depth*4);
            var attributeString = string.Join(" ", attributes.Select(attr => $"{attr.Key}=\"{attr.Value}\""));

            return $"{indent}<{tagName} {attributeString}>";
        }


        public static string BuildOpeningString(XmlNode node)
        {
            var depth = int.Parse(node.GetAttribute("depth"));
            return BuildOpeningString(node.TagName, node._attributes, depth);
        }

        public static string BuildClosingString(string tagName, int depth)
        {
            return IndentText($"</{tagName}>", depth);
        }

        private static string IndentText(string text, int depth)
        {
            return new string(' ', depth*4) + text;
        }

        public static void UpdateAttribute(string file, XmlNode node, string key, string value)
        {
            var lines = File.ReadAllLines(file).ToList();
            try
            {
                var lineNumber = int.Parse(node.GetAttribute("line"));
                var originalLine = lines[lineNumber-1];
                ParseFromLine(originalLine, ref node);
                node.SetAttribute(key, value);
                var newLine = BuildOpeningString(node);
                lines[lineNumber-1] = newLine;
                File.WriteAllLines(file, lines);
            }
            catch (Exception e)
            {
                // ignored
                LM_Debug.Instance.LogError("Fail to update attribute");
            }
        }
        
        public static void UpdateAttributes(string file, XmlNode node, Dictionary<string, string> attributes)
        {
            var lines = File.ReadAllLines(file).ToList();
            try
            {
                var lineNumber = int.Parse(node.GetAttribute("line"));
                var originalLine = lines[lineNumber-1];
                ParseFromLine(originalLine, ref node);
                
                foreach (var pair in attributes)
                {
                    node.SetAttribute(pair.Key, pair.Value);
                }
                
                var newLine = BuildOpeningString(node);
                lines[lineNumber-1] = newLine;
                File.WriteAllLines(file, lines);
            }
            catch (Exception e)
            {
                // ignored
                LM_Debug.Instance.LogError("Fail to update attribute");
            }
        }
    }
}
