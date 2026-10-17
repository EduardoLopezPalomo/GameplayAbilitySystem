using System;
using System.Collections.Generic;

namespace supercachola.AbilitySystem.Tags
{
    /// <summary>
    /// A registry for gameplay tags and their hierarchy.
    /// Every tag is an interer number. Number 0 means "no tag".
    /// </summary>
    
    internal static class GameplayTagRegistry
    {
       private static readonly List<string> Names = new List<string> {string.Empty};
       private static readonly List<int> Parents = new List<int> {0};
       private static readonly Dictionary<string, int> IdByName = new Dictionary<string, int>();
       private static readonly object registryLock = new object();
        /// <summary>
        /// If the tag is already registered, returns its id. Otherwise, registers it and returns the new id.
        /// </summary>
        /// <param name="name"> The name of the tag</param>
        /// <returns>THe Id of the tag</returns>
       public static int GetOrRegister(string name)
       {
           Validate(name);

           lock (registryLock)
           {
               if (IdByName.TryGetValue(name, out int id))
                   return id;
           
               int parentId = 0;
               int lastDot = name.LastIndexOf('.');
               // If the tag has a parent, register it first
               if (lastDot > 0)
                   parentId = GetOrRegister(name.Substring(0, lastDot));
           
               id = Names.Count;
               Names.Add(name);
               Parents.Add(parentId);
               IdByName.Add(name, id);
               return id;   
           }
       }
       /// <summary>
       /// Returns the name of the tag with the given id. If the id is invalid, returns an empty string.
       /// </summary>
       public static string GetName(int id)
       {
           return (id >= 0 && id < Names.Count) ? Names[id] : string.Empty;
       }
       
       /// <summary>
       /// Returns the id of the tag with the given name. If the name is invalid, returns false and id is 0.
       /// </summary>
       
       internal static bool TryGetId(string name, out int id)
       {
           if (string.IsNullOrEmpty(name))
           {
               id = 0;
               return false;
           }
           lock (registryLock)
           {
               return IdByName.TryGetValue(name, out id);
           }
       }
       
       /// <summary>
       /// Returns the parent id of the tag with the given id. If the id is invalid, returns 0.
       /// </summary>
       internal static int GetParentId(int id)
       {
           return (id > 0 && id < Parents.Count) ? Parents[id] : 0;
       }
       /// <summary>
       /// Returns true if the id is a child of the ancestor id, or if they are equal.
       /// </summary>
       public static bool IsChildOrEqual(int id, int ancestor)
       {
           while (id != 0)
           {
               if (id == ancestor) return true;
               id = Parents[id];
           }
           return false;
       }
       
       internal static bool IsValidName(string name)
       {
           return !string.IsNullOrEmpty(name)
                  && name[0] != '.'
                  && name[name.Length - 1] != '.'
                  && !name.Contains("..")
                  && !name.Contains(" ");
       }

       private static void Validate(string name)
       {
           if (!IsValidName(name))
               throw new ArgumentException(
                   $"Invalid name: '{name}'. Can not be empty, have spaces, double points or start/end in a point.");
       }
    }
    
}