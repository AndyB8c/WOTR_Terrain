using System.Collections;
using UnityEngine;

namespace MalbersAnimations.Controller.Reactions
{
    /// <summary> Reaction Script for Making the Animal do something </summary>
    [System.Serializable]
    public abstract class MReaction : ScriptableObject
    {
        [HelpBox]
        public string description;
        public bool active = true;
        public float delay = 0f;
        [HideInInspector] public string fullName;
        

        /// <summary>Instant Reaction ... without considering Active or Delay parameters</summary>
        protected abstract void _React(MAnimal animal);

        /// <summary>Instant Reaction ... without considering Active or Delay parameters</summary>
        protected abstract bool _TryReact(MAnimal animal);

        public void React(Component animal) => React(animal.FindComponent<MAnimal>());

        public void React(GameObject animal)
        {
            var go = animal.FindComponent<MAnimal>();
            React(go);
        }

        public void React(MAnimal animal)
        {
            if (animal && active)
            {
                if (delay > 0)
                    animal.StartCoroutine(DelayedReaction(animal));
                else
                    _React(animal);
            }
        }


        public bool TryReact(Component animal) => TryReact(animal.FindComponent<MAnimal>());

        public bool TryReact(GameObject animal)
        {
            var go = animal.FindComponent<MAnimal>();
            return TryReact(go);
        }

        public bool TryReact(MAnimal animal)
        {
            if (animal && active)
                return _TryReact(animal);

            return false;
        }


        private IEnumerator DelayedReaction(MAnimal an)
        {
            yield return new WaitForSeconds(delay);
            _React(an);
        }
    }
}