using System;
using System.Linq;
using System.Security;
using System.Text;
using Newtonsoft.Json;

namespace Landmarks.Scripts.Progress
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class Serializer
    {
        public static Dictionary<string, GameObject> ConvertToLookupDictionary(IEnumerable<GameObject> gameObjects)
        {
            var lookup = new Dictionary<string, GameObject>();
            foreach (var gameObject in gameObjects)
            {
                var key = Uid.TryGetUid(gameObject, out var uid) ? $"{uid}" : gameObject.name;
                if (!lookup.ContainsKey(key))
                {
                    lookup.Add(key, gameObject);
                }
            }

            return lookup;
        }


        public static Dictionary<string, GameObject> ConvertToLookupDictionary(IEnumerable<IEnumerable<GameObject>> gameObjects)
        {
            var flattenList = gameObjects.SelectMany(objs => objs);
            return ConvertToLookupDictionary(flattenList);
        }


        public static List<Dictionary<string, string>> ConvertToDictionaryList(IEnumerable<GameObject> gameObjects)
        {
            return gameObjects.Where(obj => obj != null).Select(obj =>
            {

                if (Uid.TryGetUid(obj, out var uid))
                {
                    return new Dictionary<string, string>()
                    {
                        {"uid", $"{uid}"},
                        {"name", obj.name}
                    };
                }

                return new Dictionary<string, string>()
                {
                    {"name", obj.name}
                };
            }).ToList();
        }

        public static List<List<Dictionary<string, string>>> ConvertToDictionaryList(
            IEnumerable<IEnumerable<GameObject>> gameObjects)
        {
            return gameObjects.Select(ConvertToDictionaryList).ToList();
        }

        public static List<GameObject> ConvertToGameObjectList(IEnumerable<Dictionary<string, string>> gameObjects,
            IDictionary<string, GameObject> gameObjectLookup)
        {
            return gameObjects.Select(obj =>
            {
                var key = "";

                // First getting the uid if not fallback to name
                if (obj.TryGetValue("uid", out var uidString))
                {
                    key = uidString;
                }
                else if (obj.TryGetValue("name", out var name))
                {
                    key = name;
                }
                else
                {
                    return default;
                }

                if (gameObjectLookup.TryGetValue(key, out var gameObject)) return gameObject;

                Debug.LogError($"Cannot find object with key {key}");
                return default;

            }).ToList();
        }

        public static string Serialize(object obj)
        {
            var unformatted = JsonConvert.SerializeObject(obj);
            return SecurityElement.Escape(unformatted);
        }

        public static T Deserialize<T>(string json)
        {
            var unescaped = new SecurityElement("", json).Text;
            Debug.Log("unescaped: " + unescaped);
            return JsonConvert.DeserializeObject<T>(unescaped);
        }

        public static List<List<GameObject>> ConvertToGameObjectList(
            IEnumerable<IEnumerable<Dictionary<string, string>>> gameObjects,
            IDictionary<string, GameObject> gameObjectLookup)
        {
            return gameObjects.Select(objs => ConvertToGameObjectList(objs, gameObjectLookup)).ToList();
        }
    }
}
