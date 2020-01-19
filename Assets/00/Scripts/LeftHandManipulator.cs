using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class LeftHandEvent : UnityEvent { }

public class LeftHandManipulator : MonoBehaviour
{
    [SerializeField] private GameObject leftHand = null;

    [SerializeField] private float colorTransitionSpeed;

    [SerializeField] private Color leftIndexColor;
    [SerializeField] private Color leftMiddleColor;
    [SerializeField] private Color leftRingColor;
    [SerializeField] private Color leftPinkyColor;

    public LeftHandEvent OnLeftIndexTouch;
    public LeftHandEvent OnLeftMiddleTouch;
    public LeftHandEvent OnLeftRingTouch;
    public LeftHandEvent OnLeftPinkyTouch;

    public LeftHandEvent OnTouchEnd;

    private SkinnedMeshRenderer leftHandRenderer;
    private AudioSource audioSource;

    void Start()
    {
        leftHandRenderer = leftHand.GetComponentInChildren<SkinnedMeshRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Left Hand triggers
        if (other.tag == "lIndex")
        {
            OnLeftIndexTouch.Invoke();
            StopAllCoroutines();
            StartCoroutine(TransitionColor(leftIndexColor));
            //leftHandRenderer.material.SetColor("_InnerColor", leftIndexColor);
        }
        if (other.tag == "lMiddle")
        {
            OnLeftMiddleTouch.Invoke();
            StopAllCoroutines();
            StartCoroutine(TransitionColor(leftMiddleColor));
            //leftHandRenderer.material.SetColor("_InnerColor", leftMiddleColor);
        }
        if (other.tag == "lRing")
        {
            OnLeftRingTouch.Invoke();
            StopAllCoroutines();
            StartCoroutine(TransitionColor(leftRingColor));
            //leftHandRenderer.material.SetColor("_InnerColor", leftRingColor);
        }
        if (other.tag == "lPinky")
        {
            OnLeftPinkyTouch.Invoke();
            StopAllCoroutines();
            StartCoroutine(TransitionColor(leftPinkyColor));
            //leftHandRenderer.material.SetColor("_InnerColor", leftPinkyColor);
        }
/*
        // Right Hand triggers
        if (other.tag == "rIndex")
        {
            OnRightIndexTouch.Invoke();
            rightHandRenderer.material.SetColor("_InnerColor", rightIndexColor);
        }
        if (other.tag == "rMiddle")
        {
            OnRightMiddleTouch.Invoke();
            rightHandRenderer.material.SetColor("_InnerColor", rightMiddleColor);
        }
        if (other.tag == "rRing")
        {
            OnRightRingTouch.Invoke();
            rightHandRenderer.material.SetColor("_InnerColor", rightRingColor);
        }
        if (other.tag == "rPinky")
        {
            OnRightPinkyTouch.Invoke();
            rightHandRenderer.material.SetColor("_InnerColor", rightPinkyColor);
        }
*/
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "lIndex" || other.tag == "lMiddle" || other.tag == "lRing" || other.tag == "lPinky")
        {
            //leftHandRenderer.material.SetColor("_InnerColor", Color.white);
            StopAllCoroutines();
            StartCoroutine(TransitionColor(Color.white));
            OnTouchEnd.Invoke();
            audioSource.Stop();
        }
    }

    private IEnumerator TransitionColor(Color to)
    {
        while (leftHandRenderer.material.GetColor("_InnerColor") != to)
        {
            leftHandRenderer.material.SetColor("_InnerColor", Color.Lerp(leftHandRenderer.material.GetColor("_InnerColor"), to, colorTransitionSpeed * Time.deltaTime));
            yield return null;
        }
    }
}
