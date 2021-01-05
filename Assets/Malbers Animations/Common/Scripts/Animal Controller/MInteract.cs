using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using UnityEngine;

namespace MalbersAnimations.Utilities
{
    public class MInteract : UnityUtils, IInteractable
    {
        public IntReference m_InteractableID = new IntReference(0);
        [SerializeField] private BoolReference hasInteracted = new BoolReference(false);
        public IntEvent OnInteract = new IntEvent();

        public bool HasInteracted { get => hasInteracted.Value; set => hasInteracted.Value = value; }

        public void Interact(int value)
        {
            if (!HasInteracted)
            {
                if (m_InteractableID <= 0 || m_InteractableID == value)
                {
                    OnInteract.Invoke(value);
                    HasInteracted = true;
                }
            }
        }

        public void Interact()
        {
            if (!HasInteracted)
            {
                OnInteract.Invoke(0);
                HasInteracted = true;
            }
        }

        public void Interact(Component comp)
        {
            var interacter = comp.GetComponentInParent<IInteracter>();

            if (interacter != null)
            {
                Interact(interacter.InteracterID);
            }
            else
            Interact();
        }

        public virtual void ResetInteraction() => HasInteracted = false;
    }
}