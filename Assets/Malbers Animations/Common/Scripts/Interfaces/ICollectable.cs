using UnityEngine;

namespace MalbersAnimations
{
    /// <summary>Interface to Identify Collectables Item</summary>
    public interface ICollectable
    {
        /// <summary>Applies the Item Dropped Logic</summary>
        void Drop();

        /// <summary>Applies the Item Picked Logic</summary>
        void Pick();

        /// <summary> Is the Item Picked?</summary>
        bool IsPicked { get; set; }
    }
}