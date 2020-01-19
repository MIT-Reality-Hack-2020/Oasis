using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class microphoneInput : MonoBehaviour
{
    string OculusLinkMicName = "Headset Microphone (Oculus Virtual Audio Device)";
    string LaptopMic = "Microphone (Realtek Audio)";

    // Start recording with built-in Microphone and play the recorded audio right away
    public void turnOn()
    {
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(LaptopMic, true, 10, 44100);
        audioSource.Play();

        void Update()
    {

        }
    }
}