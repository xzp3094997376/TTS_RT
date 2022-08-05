using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
//using NAudio.CoreAudioApi;
using UnityEngine;
using UnityEngine.Networking;

public class TextToSpeech_ZH : MonoBehaviour
{
    [Header("音源")]
    public AudioSource _Audio;

    //全局变量
    public static TextToSpeech_ZH _World;

    [Multiline]
    public string Str;

    //网页文字转语音
    private string _Url;

    [Range(0,15)]
    public int speed;


    private void Awake()
    {
        //CullOff();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Start();   
        }
    }

    private void Start()
    {
        _World = this;
        _World.StartCoroutine(GetAudioClip(UnityWebRequest.EscapeURL(Str)));
    }


    //获取 Web网页音源信息并播放
    private IEnumerator GetAudioClip(string AudioText)
    {
        _Url = $"https://tsn.baidu.com/text2audio?tex={AudioText}&lan=zh&cuid=7919875968150074&ctp=1&aue=6&tok=25.3141e5ae3aa109abb6fc9a8179131181.315360000.1886566986.282335-17539441&spd={speed}&per=1&aue=3";

        //过期_Url = "https://tsn.baidu.com/text2audio?tex=" + AudioText + "&lan=zh&cuid=18-C0-4D-A7-1D-8E&ctp=1&tok=24.8707b30700694628a94c4e0c4496f670.2592000.1653460765.282335-26070831&vol=9&per=0&spd=5&pit=5&aue=3";
        //过期_Url =
        //   $"http://tsn.baidu.com/text2audio?tex={AudioText}&lan=zh&ctp=1&cuid=18-C0-4D-A7-1D-8E&tok=24.c71bb71c42a3c53ce6d801c2fbb27f68.2592000.1653463945.282335-26070831&&vol=9&per=0&spd=5&pit=5&aue=3";

        using (UnityWebRequest _AudioWeb = UnityWebRequestMultimedia.GetAudioClip(/*_Url*/UrlWrap_SouGou(AudioText), AudioType.MPEG))
        {

            yield return _AudioWeb.SendWebRequest();
            if (_AudioWeb.isNetworkError)
            {   
                yield break;
            }

            //yield return _AudioWeb.downloadedBytes.ToString();

            AudioClip _Cli = DownloadHandlerAudioClip.GetContent(_AudioWeb);
            if (_Cli.LoadAudioData())
                Debug.Log("音频已成功加载");
            else
            {
                Debug.LogError("音效加载失败");
                yield break;
            }
            //将clip赋给A
            _Audio.clip = _Cli;
            _Audio.Play();
        }
    }


    /// <summary>
    /// 有道翻译
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    string UrlWrap(string AudioText)
    {
        //AudioType.MPEG
        string url = $"http://tts.youdao.com/fanyivoice?word={AudioText}&le=zh&keyfrom=speaker-target";
        return url;
    }
    //3,5,6,
    string UrlWrap_SouGou(string AudioText)
    {
        //AudioType.MPEG
        string url = $"https://fanyi.sogou.com/reventondc/synthesis?text={AudioText}&speed=1&lang=zh-CHS&from=translateweb&speaker=4&speaking_rate=1.3";
        return url;
    }


    string CullOff()
    {
        var v = Regex.Replace(Str, "[ \\[ \\] \\^ \\-_*×――(^)（^）$%~!@#$…&%￥—+=<>《》!！??？:：•`·、。，；,.;\"‘’“”-]", "");
    //Debug.Log(v);


        return v;
    }
}
