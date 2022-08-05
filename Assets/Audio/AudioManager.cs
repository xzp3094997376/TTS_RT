using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixerSnapshot[] m_audioSnapShots;
    public AudioMixer audioMixer;    // 进行控制的Mixer变量

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("123");
            m_audioSnapShots[0].TransitionTo(2);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("456");
            m_audioSnapShots[1].TransitionTo(2);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("789");
            m_audioSnapShots[2].TransitionTo(2);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("101");
            m_audioSnapShots[3].TransitionTo(2);
        }
    }

    public void SetMasterVolume(float volume)    // 控制主音量的函数
    {
        audioMixer.SetFloat("MasterVolume", volume);
        // MasterVolume为我们暴露出来的Master的参数
    }

    public void SetMusicVolume(float volume)    // 控制背景音乐音量的函数
    {
        audioMixer.SetFloat("MusicVolume", volume);
        // MusicVolume为我们暴露出来的Music的参数
    }

    public void SetSoundEffectVolume(float volume)    // 控制音效音量的函数
    {
        audioMixer.SetFloat("EffectVolume", volume);
        // EffectVolume为我们暴露出来的SoundEffect的参数
    }


}