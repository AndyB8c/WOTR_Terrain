using UnityEngine;
namespace MalbersAnimations.Controller
{
    /// <summary>
    /// This is used for all the components that use OnAnimator Move... it breaks the Timeline edition
    /// </summary>
    [ExecuteInEditMode]
    public class AnimatorMoveTimelineFixer : MonoBehaviour
    {
        public Animator anim;
        void Start()
        {
            if (Application.isEditor && Application.isPlaying) Destroy(this);
        }

        private void OnAnimatorMove() 
        { if (anim!= null)  anim.ApplyBuiltinRootMotion(); }

        private void Reset()
        { anim = GetComponent<Animator>(); }
    }
}