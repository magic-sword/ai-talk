using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioController : MonoBehaviour
{
    AudioSource source;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void PlayVoice(AudioClip clip)
    {
        // 音声を二重再生させないようにする
        if (source.isPlaying)
        {
            return;
        }
        source.PlayOneShot(clip);
    }
}
