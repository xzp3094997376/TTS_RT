﻿namespace Crosstales.RTVoice
{

    #region BaseVoiceProvider and Speaker

    public delegate void VoicesReady();

    public delegate void SpeakStart(Model.Wrapper wrapper);
    public delegate void SpeakComplete(Model.Wrapper wrapper);

    public delegate void SpeakCurrentWord(Model.Wrapper wrapper, string[] speechTextArray, int wordIndex);
    public delegate void SpeakCurrentPhoneme(Model.Wrapper wrapper, string phoneme);
    public delegate void SpeakCurrentViseme(Model.Wrapper wrapper, string viseme);

    public delegate void SpeakAudioGenerationStart(Model.Wrapper wrapper);
    public delegate void SpeakAudioGenerationComplete(Model.Wrapper wrapper);

    public delegate void ErrorInfo(Model.Wrapper wrapper, string info);

    #endregion


    #region Speaker

    public delegate void ProviderChange(string provider);

    #endregion
}
// © 2018-2019 crosstales LLC (https://www.crosstales.com)
