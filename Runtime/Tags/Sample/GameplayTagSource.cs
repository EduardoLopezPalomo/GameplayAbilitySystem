using System.Collections.Generic;
using UnityEngine;

namespace supercachola.AbilitySystem.Tags
{
    /// <summary>
    /// A list of tags that comes with a sample or an add-on. When it is imported, its tags are added
    /// to the tag settings of the project. It never removes or changes the tags that already exist.
    /// </summary>
    //[CreateAssetMenu(menuName = "Ability System/Tag Source", fileName = "NewTagSource")]
    public sealed class GameplayTagSource : ScriptableObject
    {
        [SerializeField] private List<string> tags = new List<string>();

        public IReadOnlyList<string> Tags => tags;
    }
}
