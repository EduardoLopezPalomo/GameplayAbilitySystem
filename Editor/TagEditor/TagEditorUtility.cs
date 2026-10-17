using System;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.EditorTools
{
    internal static class TagEditorUtility
    {
        /// <summary>
        /// Order of tags. Parents go before children, and siblings are ordered alphabetically.
        /// </summary>
        public static int Compare(string a, string b)
        {
            string[] pa = a.Split('.');
            string[] pb = b.Split('.');
            int shared = Math.Min(pa.Length, pb.Length);

            for (int i = 0; i < shared; i++)
            {
                int c = string.Compare(pa[i], pb[i], StringComparison.OrdinalIgnoreCase);
                if (c != 0)
                    return c;
            }

            if (pa.Length != pb.Length)
                return pa.Length - pb.Length;

            return string.CompareOrdinal(a, b);
        }

        /// <summary>
        /// Returns true if the tag exists in the settings. This does not consider hierarchy.
        /// </summary>
        public static bool Exists(string name)
        {
            var settings = GameplayTagSettings.Load();
            if (settings == null)
                return false;

            var tags = settings.Tags;
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == name)
                    return true;
            }
            return false;
        }
    }
}
