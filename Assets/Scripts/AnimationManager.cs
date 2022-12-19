using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager: MonoBehaviour
{
    private Animator animator;
    private string currentState;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    //start playing a new animation
    public void ChangeAnimationState(string newState) {
        if (newState == currentState) return;

        animator.Play(newState);
        currentState = newState;
    }

    //Check if a specific animation is playing
    //parameter named "0" is the animation layer (should always be 0 I think for me right now) 
    public bool IsAnimationPlaying(string stateName) {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName) &&
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f) { //this line is checking how far along the animation is between 0 and 1
            return true;
        } else {
            return false;
        }
    }
}
