﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
    //TODO add protected methods from BaseVoiceProvider

    /// <summary>Base class for custom voice providers (TTS-systems).</summary>
    public abstract class BaseCustomVoiceProvider : MonoBehaviour, IVoiceProvider
    {

        #region Variables

        protected System.Collections.Generic.List<Model.Voice> cachedVoices = new System.Collections.Generic.List<Model.Voice>();
        private System.Collections.Generic.List<string> cachedCultures;

#if !UNITY_WSA || UNITY_EDITOR
        protected System.Collections.Generic.Dictionary<string, System.Diagnostics.Process> processes = new System.Collections.Generic.Dictionary<string, System.Diagnostics.Process>();
#endif

        protected bool silence = false;

        protected static char[] splitCharWords = new char[] { ' ' };

        #endregion


        #region Events

        private VoicesReady _onVoicesReady;

        private SpeakStart _onSpeakStart;
        private SpeakComplete _onSpeakComplete;

        private SpeakCurrentWord _onSpeakCurrentWord;
        private SpeakCurrentPhoneme _onSpeakCurrentPhoneme;
        private SpeakCurrentViseme _onSpeakCurrentViseme;

        private SpeakAudioGenerationStart _onSpeakAudioGenerationStart;
        private SpeakAudioGenerationComplete _onSpeakAudioGenerationComplete;

        private ErrorInfo _onErrorInfo;

        /// <summary>An event triggered whenever the voices of a provider are ready.</summary>
        public event VoicesReady OnVoicesReady
        {
            add { _onVoicesReady += value; }
            remove { _onVoicesReady -= value; }
        }

        /// <summary>An event triggered whenever a speak is started.</summary>
        public event SpeakStart OnSpeakStart
        {
            add { _onSpeakStart += value; }
            remove { _onSpeakStart -= value; }
        }

        /// <summary>An event triggered whenever a speak is completed.</summary>
        public event SpeakComplete OnSpeakComplete
        {
            add { _onSpeakComplete += value; }
            remove { _onSpeakComplete -= value; }
        }

        /// <summary>An event triggered whenever a new word is spoken (native, Windows and iOS only).</summary>
        public event SpeakCurrentWord OnSpeakCurrentWord
        {
            add { _onSpeakCurrentWord += value; }
            remove { _onSpeakCurrentWord -= value; }
        }

        /// <summary>An event triggered whenever a new phoneme is spoken (native mode, Windows only).</summary>
        public event SpeakCurrentPhoneme OnSpeakCurrentPhoneme
        {
            add { _onSpeakCurrentPhoneme += value; }
            remove { _onSpeakCurrentPhoneme -= value; }
        }

        /// <summary>An event triggered whenever a new viseme is spoken (native mode, Windows only).</summary>
        public event SpeakCurrentViseme OnSpeakCurrentViseme
        {
            add { _onSpeakCurrentViseme += value; }
            remove { _onSpeakCurrentViseme -= value; }
        }

        /// <summary>An event triggered whenever a speak audio generation is started.</summary>
        public event SpeakAudioGenerationStart OnSpeakAudioGenerationStart
        {
            add { _onSpeakAudioGenerationStart += value; }
            remove { _onSpeakAudioGenerationStart -= value; }
        }

        /// <summary>An event triggered whenever a speak audio generation is completed.</summary>
        public event SpeakAudioGenerationComplete OnSpeakAudioGenerationComplete
        {
            add { _onSpeakAudioGenerationComplete += value; }
            remove { _onSpeakAudioGenerationComplete -= value; }
        }

        /// <summary>An event triggered whenever an error occurs.</summary>
        public event ErrorInfo OnErrorInfo
        {
            add { _onErrorInfo += value; }
            remove { _onErrorInfo -= value; }
        }

        #endregion


        #region Properties

        private bool active = false;

        public bool isActive
        {
            get
            {
                return active;
            }

            set
            {
                //cachedVoices = null;
                active = value;
            }
        }

        #endregion


        #region Implemented methods

        public abstract string AudioFileExtension
        {
            get;
        }

        public abstract AudioType AudioFileType
        {
            get;
        }

        public abstract string DefaultVoiceName
        {
            get;
        }

        public virtual System.Collections.Generic.List<Model.Voice> Voices
        {
            get
            {
                return cachedVoices;
            }
        }

        public abstract bool isWorkingInEditor
        {
            get;
        }

        public abstract int MaxTextLength
        {
            get;
        }

        public abstract bool isSpeakNativeSupported
        {
            get;
        }

        public abstract bool isSpeakSupported
        {
            get;
        }

        public abstract bool isPlatformSupported
        {
            get;
        }

        public abstract bool isSSMLSupported
        {
            get;
        }

        public System.Collections.Generic.List<string> Cultures
        {
            get
            {
                if (cachedCultures == null || cachedCultures.Count == 0)
                {
                    cachedCultures = new System.Collections.Generic.List<string>();

                    System.Collections.Generic.IEnumerable<Model.Voice> cultures = Voices.GroupBy(cul => cul.Culture).Select(grp => grp.First()).OrderBy(s => s.Culture).ToList();

                    foreach (Model.Voice voice in cultures)
                    {
                        cachedCultures.Add(voice.Culture);
                    }
                }

                return cachedCultures;
            }
        }

        public virtual void Silence()
        {
            silence = true;

#if UNITY_STANDALONE || UNITY_EDITOR

            foreach (System.Collections.Generic.KeyValuePair<string, System.Diagnostics.Process> kvp in processes)
            {
                if (!kvp.Value.HasExited)
                {
                    kvp.Value.Kill();
                }
            }

            processes.Clear();
#endif

        }

        public virtual void Silence(string uid)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            if (uid != null)
            {
                if (processes.ContainsKey(uid) && !processes[uid].HasExited)
                {
                    processes[uid].Kill();
                }

                processes.Remove(uid);
            }
#endif

        }

        public abstract IEnumerator SpeakNative(Model.Wrapper wrapper);

        public abstract IEnumerator Speak(Model.Wrapper wrapper);

        public abstract IEnumerator Generate(Model.Wrapper wrapper);

        /// <summary>Load the provider (e.g. all voices).</summary>
        public abstract void Load();

        #endregion


        #region Protected methods

        protected string getOutputFile(string uid, bool isPersistentData = false/*, bool createFile = false*/)
        {
            string filename = Util.Constants.AUDIOFILE_PREFIX + uid + AudioFileExtension;
            string outputFile;

            if (isPersistentData)
            {
                outputFile = Util.Helper.ValidatePath(Application.persistentDataPath) + filename;
            }
            else
            {
                outputFile = Util.Config.AUDIOFILE_PATH + filename;
            }

            /*
            if (createFile)
            {
                try
                {
                    System.IO.File.Create(outputFile).Dispose(); //to reduce AV-problems
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Could not create file: " + ex);
                }
            }
            */

            return outputFile;
        }

        protected virtual IEnumerator playAudioFile(Model.Wrapper wrapper, string url, string outputFile, AudioType type = AudioType.WAV, bool isNative = false, bool isLocalFile = true, System.Collections.Generic.Dictionary<string, string> headers = null)
        {
            //Debug.LogWarning("playAudioFile: " + wrapper);

            if (wrapper != null && wrapper.Source != null)
            {
                if (!isLocalFile || (isLocalFile && new System.IO.FileInfo(outputFile).Length > 1024))
                {
#if UNITY_2017_1_OR_NEWER
                    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url.Trim(), type))
#else
                    using (UnityWebRequest www = UnityWebRequest.GetAudioClip(url.Trim(), type))
#endif
                    {
                        if (headers != null)
                        {
                            foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in headers)
                            {
                                www.SetRequestHeader(kvp.Key, kvp.Value);
                            }
                        }

#if UNITY_2017_2_OR_NEWER
                        yield return www.SendWebRequest();
#else
                        yield return www.Send();
#endif
#if UNITY_2017_1_OR_NEWER
                        if (!www.isHttpError && !www.isNetworkError)
#else
                        if (string.IsNullOrEmpty(www.error))
#endif
                        {
                            //just for testing!
                            //string outputFile = Util.Config.AUDIOFILE_PATH + wrapper.Uid + extension;
                            //System.IO.File.WriteAllBytes(outputFile, www.bytes);

#if UNITY_WEBGL
                            AudioClip ac = Util.WavMaster.ToAudioClip(www.downloadHandler.data);
#else
                            AudioClip ac = DownloadHandlerAudioClip.GetContent(www);

                            do
                            {
                                yield return ac;
                            } while (ac.loadState == AudioDataLoadState.Loading);
#endif

                            //Debug.Log("ac.loadState: " + ac.loadState + " - " + www.downloadedBytes);

                            if (ac.loadState == AudioDataLoadState.Loaded)
                            {
                                wrapper.Source.clip = ac;

                                if (Util.Config.DEBUG)
                                    Debug.Log("Text generated: " + wrapper.Text);

                                copyAudioFile(wrapper, outputFile, isLocalFile, www.downloadHandler.data);

                                if (!isNative)
                                {
                                    onSpeakAudioGenerationComplete(wrapper);
                                }

                                if ((isNative || wrapper.SpeakImmediately) && wrapper.Source != null)
                                {
                                    wrapper.Source.Play();
                                    onSpeakStart(wrapper);

                                    do
                                    {
                                        yield return null;
                                    } while (!silence && Util.Helper.hasActiveClip(wrapper.Source));

                                    if (Util.Config.DEBUG)
                                        Debug.Log("Text spoken: " + wrapper.Text);

                                    onSpeakComplete(wrapper);

                                    if (ac != null)
                                        AudioClip.Destroy(ac);
                                }
                            }
                            else
                            {
                                string errorMessage = "Could not load the audio file the speech: " + wrapper;
                                Debug.LogError(errorMessage);
                                onErrorInfo(wrapper, errorMessage);
                            }
                        }
                        else
                        {
                            string errorMessage = "Could not generate the speech: " + wrapper + System.Environment.NewLine + "WWW error: " + www.error;
                            Debug.LogError(errorMessage);
                            onErrorInfo(wrapper, errorMessage);
                        }
                    }
                }
                else
                {
                    string errorMessage = "The generated audio file is invalid: " + wrapper;
                    Debug.LogError(errorMessage);
                    onErrorInfo(wrapper, errorMessage);
                }
            }
            else
            {
                string errorMessage = "'Source' is null: " + wrapper;
                Debug.LogError(errorMessage);
                onErrorInfo(wrapper, errorMessage);
            }

        }

        protected virtual void copyAudioFile(Model.Wrapper wrapper, string outputFile, bool isLocalFile = true, byte[] data = null)
        {
            if (wrapper != null)
            {
                if (!string.IsNullOrEmpty(wrapper.OutputFile))
                {
                    wrapper.OutputFile += AudioFileExtension;

                    if (isLocalFile)
                    {
                        Util.Helper.FileCopy(outputFile, wrapper.OutputFile, Util.Config.AUDIOFILE_AUTOMATIC_DELETE);
                    }
                    else
                    {
                        System.IO.File.WriteAllBytes(wrapper.OutputFile, data); //TODO write AudioClip
                    }
                }

                if (Util.Config.AUDIOFILE_AUTOMATIC_DELETE)
                {
                    //if (!Util.Helper.isEditorMode)
                    //{
                    //    Speaker.FilesToDelete.Add(outputFile);
                    //}
                    //else
                    //{
                    if (System.IO.File.Exists(outputFile))
                    {
                        try
                        {
                            System.IO.File.Delete(outputFile);
                        }
                        catch (System.Exception ex)
                        {
                            string errorMessage = "Could not delete file '" + outputFile + "'!" + System.Environment.NewLine + ex;
                            Debug.LogError(errorMessage);
                            onErrorInfo(wrapper, errorMessage);
                        }
                    }
                    //}
                }
                else
                {
                    if (string.IsNullOrEmpty(wrapper.OutputFile))
                    {
                        wrapper.OutputFile = outputFile;
                    }
                }
            }
            else
            {
                string errorMessage = "'wrapper' is null!";
                Debug.LogError(errorMessage);
                onErrorInfo(wrapper, errorMessage);
            }
        }

        protected virtual void processAudioFile(Model.Wrapper wrapper, string outputFile, bool isLocalFile = true, byte[] data = null)
        {
            if (wrapper != null)
            {
                if (!isLocalFile || (isLocalFile && new System.IO.FileInfo(outputFile).Length > 1024))
                {
                    if (Util.Config.DEBUG)
                        Debug.Log("Text generated: " + wrapper.Text);

                    copyAudioFile(wrapper, outputFile, isLocalFile, data);

                    onSpeakAudioGenerationComplete(wrapper);
                }
                else
                {
                    string errorMessage = "The generated audio file is invalid!";
                    Debug.LogError(errorMessage);
                    onErrorInfo(wrapper, errorMessage);
                }
            }
            else
            {
                string errorMessage = "'wrapper' is null!";
                Debug.LogError(errorMessage);
                onErrorInfo(wrapper, errorMessage);
            }
        }

        protected virtual string getVoiceName(Model.Wrapper wrapper)
        {
            if (wrapper == null || wrapper.Voice == null || string.IsNullOrEmpty(wrapper.Voice.Name))
            {
                if (Util.Config.DEBUG)
                    Debug.LogWarning("'wrapper.Voice' or 'wrapper.Voice.Name' is null! Using the OS 'default' voice.");

                if (Voices.Count > 0)
                {
                    //always use English as fallback
                    return Speaker.VoiceForCulture("en-US").Name;
                }

                return DefaultVoiceName;
            }
            else
            {
                return wrapper.Voice.Name;
            }
        }

        #endregion


        #region Event-trigger methods

        protected void onVoicesReady()
        {
            if (Util.Config.DEBUG)
                Debug.Log("onVoicesReady");

            if (_onVoicesReady != null)
            {
                _onVoicesReady();
            }
        }

        protected void onSpeakStart(Model.Wrapper wrapper)
        {
            if (Util.Config.DEBUG)
                Debug.Log("onSpeakStart: " + wrapper);

            if (_onSpeakStart != null)
            {
                _onSpeakStart(wrapper);
            }
        }

        protected void onSpeakComplete(Model.Wrapper wrapper)
        {
            if (Util.Config.DEBUG)
                Debug.Log("onSpeakComplete: " + wrapper);

            if (_onSpeakComplete != null)
            {
                _onSpeakComplete(wrapper);
            }
        }

        protected void onSpeakCurrentWord(Model.Wrapper wrapper, string[] speechTextArray, int wordIndex)
        {
            if (wordIndex < speechTextArray.Length)
            {
                if (Util.Config.DEBUG)
                    Debug.Log("onSpeakCurrentWord: " + speechTextArray[wordIndex] + System.Environment.NewLine + wrapper);

                if (_onSpeakCurrentWord != null)
                {
                    _onSpeakCurrentWord(wrapper, speechTextArray, wordIndex);
                }
            }
            else
            {
                Debug.LogWarning("Word index is larger than the speech text word count: " + wordIndex + "/" + speechTextArray.Length);
            }
        }

        protected void onSpeakCurrentPhoneme(Model.Wrapper wrapper, string phoneme)
        {
            if (Util.Config.DEBUG)
                Debug.Log("onSpeakCurrentPhoneme: " + phoneme + System.Environment.NewLine + wrapper);

            if (_onSpeakCurrentPhoneme != null)
            {
                _onSpeakCurrentPhoneme(wrapper, phoneme);
            }
        }

        protected void onSpeakCurrentViseme(Model.Wrapper wrapper, string viseme)
        {
            if (Util.Config.DEBUG)
                Debug.Log("onSpeakCurrentViseme: " + viseme + System.Environment.NewLine + wrapper);

            if (_onSpeakCurrentViseme != null)
            {
                _onSpeakCurrentViseme(wrapper, viseme);
            }
        }

        protected void onSpeakAudioGenerationStart(Model.Wrapper wrapper)
        {
            if (Util.Config.DEBUG)
                Debug.Log("onSpeakAudioGenerationStart: " + wrapper);

            if (_onSpeakAudioGenerationStart != null)
            {
                _onSpeakAudioGenerationStart(wrapper);
            }
        }

        protected void onSpeakAudioGenerationComplete(Model.Wrapper wrapper)
        {
            if (Util.Config.DEBUG)
                Debug.Log("onSpeakAudioGenerationComplete: " + wrapper);

            if (_onSpeakAudioGenerationComplete != null)
            {
                _onSpeakAudioGenerationComplete(wrapper);
            }
        }

        protected void onErrorInfo(Model.Wrapper wrapper, string info)
        {
            if (Util.Config.DEBUG)
                Debug.Log("onErrorInfo: " + info);

            if (_onErrorInfo != null)
            {
                _onErrorInfo(wrapper, info);
            }
        }

        #endregion


        #region Editor-only methods

#if UNITY_EDITOR

        public abstract void SpeakNativeInEditor(Model.Wrapper wrapper);

        public abstract void GenerateInEditor(Model.Wrapper wrapper);

#endif

        #endregion

    }
}
// © 2018-2019 crosstales LLC (https://www.crosstales.com)