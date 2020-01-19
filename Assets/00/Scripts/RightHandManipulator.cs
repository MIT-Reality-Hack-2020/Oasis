using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RightHandEvent : UnityEvent { }

public class RightHandManipulator : MonoBehaviour
{
    [SerializeField] private GameObject rightHand = null;

    [SerializeField] private float colorTransitionSpeed;

    [SerializeField] private Color rightIndexColor;
    [SerializeField] private Color rightMiddleColor;
    [SerializeField] private Color rightRingColor;
    [SerializeField] private Color rightPinkyColor;

    public RightHandEvent OnRightIndexTouch;
    public RightHandEvent OnRightMiddleTouch;
    public RightHandEvent OnRightRingTouch;
    public RightHandEvent OnRightPinkyTouch;

    public LeftHandEvent OnTouchEnd;

    private SkinnedMeshRenderer rightHandRenderer;
    private AudioSource audioSource;

    void Start()
    {
        rightHandRenderer = rightHand.GetComponentInChildren<SkinnedMeshRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
/*
        // Left Hand triggers
        if (other.tag == "lIndex")
        {
            OnLeftIndexTouch.Invoke();
            leftHandRenderer.material.SetColor("_InnerColor", leftIndexColor);
        }
        if (other.tag == "lMiddle")
        {
            OnLeftMiddleTouch.Invoke();
            leftHandRenderer.material.SetColor("_InnerColor", leftMiddleColor);
        }
        if (other.tag == "lRing")
        {
            OnLeftRingTouch.Invoke();
            leftHandRenderer.material.SetColor("_InnerColor", leftRingColor);
        }
        if (other.tag == "lPinky")
        {
            OnLeftPinkyTouch.Invoke();
            leftHandRenderer.material.SetColor("_InnerColor", leftPinkyColor);
        }
*/
        // Right Hand triggers
        if (other.tag == "rIndex")
        {
            OnRightIndexTouch.Invoke();
            StopAllCoroutines();
            StartCoroutine(TransitionColor(rightIndexColor));
            //rightHandRenderer.material.SetColor("_InnerColor", rightIndexColor);
        }
        if (other.tag == "rMiddle")
        {
            OnRightMiddleTouch.Invoke();
            StopAllCoroutines();
            StartCoroutine(TransitionColor(rightMiddleColor));
            //rightHandRenderer.material.SetColor("_InnerColor", rightMiddleColor);
        }
        if (other.tag == "rRing")
        {
            OnRightRingTouch.Invoke();
            StopAllCoroutines();
            StartCoroutine(TransitionColor(rightRingColor));
            //rightHandRenderer.material.SetColor("_InnerColor", rightRingColor);
        }
        if (other.tag == "rPinky")
        {
            OnRightPinkyTouch.Invoke();
            StopAllCoroutines();
            StartCoroutine(TransitionColor(rightPinkyColor));
            //rightHandRenderer.material.SetColor("_InnerColor", rightPinkyColor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "rIndex" || other.tag == "rMiddle" || other.tag == "rRing" || other.tag == "rPinky")
        {
            //rightHandRenderer.material.SetColor("_InnerColor", Color.white);
            StopAllCoroutines();
            StartCoroutine(TransitionColor(Color.white));
            OnTouchEnd.Invoke();
            audioSource.Stop();
        }
    }

    private IEnumerator TransitionColor(Color to)
    {
        while (rightHandRenderer.material.GetColor("_InnerColor") != to)
        {
            rightHandRenderer.material.SetColor("_InnerColor", Color.Lerp(rightHandRenderer.material.GetColor("_InnerColor"), to, colorTransitionSpeed * Time.deltaTime));
            yield return null;
        }
    }
}
