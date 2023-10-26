using System;
using System.Collections;
using System.Collections.Generic;

namespace Landmarks.Scripts.Progress
{
    public class SaveNode
    {
        public string TaskName { get; set; }

        public bool Interrupted { get; set; }
        public string FieldString { get; set; }
        
        public List<SaveNode> children;
        
        public void AddChildTask(SaveNode child)
        {
            children.Add(child);
        }

        public static SaveNode ParseFromLines(List<string> lines)
        {
            if (lines.Count == 0) return null;
            
            var root = new SaveNode();
            var parent = root;

            for (var i = 0; i < lines.Count; ++i)
            {
                var token = lines[i].Trim();

                if (token.StartsWith("("))
                {
                    // substring the [1:] to remove (
                    
                    root.children.Add(ParseFromStartToken(token));
                }

                
            }

            
            return root;
        }

        public static SaveNode ParseFromStartToken(string token)
        {
                    var split = token.Substring(1).Split(':');
                    var fieldString = "{}";
                    var name = split[0];
                    if (split.Length < 2)
                    {
                        fieldString = split[1];
                    }
                    
                    return new SaveNode
                    {
                        TaskName = name,
                        FieldString = fieldString,
                        Interrupted = true
                    };
        }
    }
}