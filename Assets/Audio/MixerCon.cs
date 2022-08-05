using UnityEngine;
using UnityEngine.Audio;

public class MixerCon : MonoBehaviour
{
    public AudioMixer masterMixer;
    public AudioMixer musicMixer;
    public AudioMixer talkMixer;

    //下方函数使用Dynamic float的方式，赋值给我们的slider

    // 控制主音量大小
    public void SetMasterMixerVolume(float volume)
    {
        // MasterVolume为我们暴露出来的Master的参数
        masterMixer.SetFloat("Master", volume);
    }

    // 控制说话音量
    public void SetTalkMixerlume(float volume)
    {
        talkMixer.SetFloat("Talk", volume);
    }

    // 控制音乐音量
    public void SetMusicMixerVolume(float volume)
    {
        musicMixer.SetFloat("Music", volume);
    }
}