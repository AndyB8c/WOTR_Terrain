using UnityEngine;

namespace MalbersAnimations
{
    /// <summary>Used for identify Interactables</summary>
    public interface IInteractable
    {
        /// <summary>Reset the Interactable </summary>
        void ResetInteraction();


        /// <summary>Applies the Interaction Logic</summary>
        void Interact(int InteracterID);

        /// <summary>Applies the Interaction Logic</summary>
        bool HasInteracted { get; } 
    }

    public interface IInteracter
    {
        int InteracterID { get; }
    }


    /// <summary>Used for Play Animations on a Character, in case of the Animal Controller are the Modes</summary>
    public interface ICharacterAction
    {
        /// <summary>Play an Animation Action on a Character and returns True if it can play it</summary>
        bool PlayAction(int Set, int Index);

        /// <summary>Force  an Animation Action on a Character and returns True if it can play it</summary>
        bool ForceAction(int Set, int Index);

        /// <summary>Is the Character playing an Action Animation </summary>
        bool IsPlayingAction { get; }
    }

    public interface IDestination { }
}