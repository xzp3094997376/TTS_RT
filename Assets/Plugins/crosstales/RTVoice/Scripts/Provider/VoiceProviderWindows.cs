using UnityEngine;
using System.Collections;
using System.Linq;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using SpeechLib;
#endif

namespace Crosstales.RTVoice.Provider
{
    /// <summary>Windows voice provider.</summary>
    public class VoiceProviderWindows : BaseVoiceProvider
    {

        #region Variables

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private ISpeechObjectTokens availableVoices;
#endif

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for VoiceProviderWindows.
        /// </summary>
        /// <param name="obj">Instance of the speaker</param>
        public VoiceProviderWindows(MonoBehaviour obj) : base(obj)
        {
            if (Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR_WIN
                getVoicesInEditor();
#endif
            }
            else
            {
                speakerObj.StartCoroutine(getVoices());
            }
        }

        #endregion


        #region Implemented methods

        public override string AudioFileExtension
        {
            get
            {
                return ".wav";
            }
        }

        public override AudioType AudioFileType
        {
            get
            {
                return AudioType.WAV;
            }
        }

        public override string DefaultVoiceName
        {
            get
            {
                return "Microsoft David Desktop";
            }
        }

        public override bool isWorkingInEditor
        {
            get
            {
                return true;
            }
        }

        public override int MaxTextLength
        {
            get
            {
                return 64000; //TODO verify!
            }
        }

        public override bool isSpeakNativeSupported
        {
            get
            {
                return true;
            }
        }

        public override bool isSpeakSupported
        {
            get
            {
                return true;
            }
        }

        public override bool isPlatformSupported
        {
            get
            {
                return Util.Helper.isWindowsPlatform || Util.Helper.isEditorMode;
            }
        }

        public override bool isSSMLSupported
        {
            get
            {
                return true;
            }
        }

        public override IEnumerator SpeakNative(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (wrapper == null)
            {
                Debug.LogWarning("'wrapper' is null!");
            }
            else
            {
                if (string.IsNullOrEmpty(wrapper.Text))
                {
                    Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
                }
                else
                {
                    silence = false;

                    onSpeakStart(wrapper);

                    System.Threading.Thread worker = new System.Threading.Thread(() => speakNative(wrapper));
                    worker.Start();

                    do
                    {
                        yield return null;
                    } while (worker.IsAlive);

                    onSpeakComplete(wrapper);
                }
            }
#else
            yield return null;
#endif
        }

        public override IEnumerator Speak(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (wrapper == null)
            {
                Debug.LogWarning("'wrapper' is null!");
            }
            else
            {
                if (string.IsNullOrEmpty(wrapper.Text))
                {
                    Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
                }
                else
                {
                    if (wrapper.Source == null)
                    {
                        Debug.LogWarning("'wrapper.Source' is null: " + wrapper);
                    }
                    else
                    {
                        string outputFile = getOutputFile(wrapper.Uid);

                        System.Threading.Thread worker = new System.Threading.Thread(() => speakToFile(wrapper, outputFile));
                        worker.Start();

                        silence = false;

                        onSpeakAudioGenerationStart(wrapper);

                        do
                        {
                            yield return null;
                        } while (worker.IsAlive);

                        yield return playAudioFile(wrapper, Util.Constants.PREFIX_FILE + outputFile, outputFile, AudioType.WAV);
                    }
                }
            }

#else
            yield return null;
#endif
        }

        public override IEnumerator Generate(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (wrapper == null)
            {
                Debug.LogWarning("'wrapper' is null!");
            }
            else
            {
                if (string.IsNullOrEmpty(wrapper.Text))
                {
                    Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
                }
                else
                {

                    string outputFile = getOutputFile(wrapper.Uid);

                    System.Threading.Thread worker = new System.Threading.Thread(() => speakToFile(wrapper, outputFile));
                    worker.Start();

                    silence = false;

                    onSpeakAudioGenerationStart(wrapper);

                    do
                    {
                        yield return null;
                    } while (worker.IsAlive);

                    processAudioFile(wrapper, outputFile);

                }
            }

#else
            yield return null;
#endif
        }

        #endregion


        #region Private methods

        private void speakNative(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            SpVoice speechSynthesizer = new SpVoice();

            SpObjectToken voice = getSapiVoice(wrapper.Voice);

            if (voice != null)
                speechSynthesizer.Voice = voice;

            speechSynthesizer.Volume = calculateVolume(wrapper.Volume);
            speechSynthesizer.Rate = calculateRate(wrapper.Rate);

            System.Threading.Thread worker = new System.Threading.Thread(() => speechSynthesizer.Speak(prepareText(wrapper), wrapper.ForceSSML && !Speaker.isAutoClearTags ? SpeechVoiceSpeakFlags.SVSFIsXML : SpeechVoiceSpeakFlags.SVSFIsNotXML));
            worker.Start();

            do
            {
                System.Threading.Thread.Sleep(50);
            } while (worker.IsAlive && !silence);

            if (silence)
                speechSynthesizer.Skip("Sentence", int.MaxValue);

            //speechSynthesizer.WaitUntilDone(int.MaxValue);
#endif
        }

        private void speakToFile(Model.Wrapper wrapper, string outputFile)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            SpVoice speechSynthesizer = new SpVoice();

            SpFileStream spFile = new SpFileStream();

            spFile.Format.Type = SpeechAudioFormatType.SAFT48kHz16BitStereo;
            spFile.Open(outputFile, SpeechStreamFileMode.SSFMCreateForWrite);

            SpObjectToken voice = getSapiVoice(wrapper.Voice);

            if (voice != null)
                speechSynthesizer.Voice = voice;

            speechSynthesizer.AudioOutputStream = spFile;
            speechSynthesizer.Volume = calculateVolume(wrapper.Volume);
            speechSynthesizer.Rate = calculateRate(wrapper.Rate);
            speechSynthesizer.Speak(prepareText(wrapper), wrapper.ForceSSML && !Speaker.isAutoClearTags ? SpeechVoiceSpeakFlags.SVSFIsXML : SpeechVoiceSpeakFlags.SVSFIsNotXML);

            speechSynthesizer.WaitUntilDone(int.MaxValue);

            spFile.Close();
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private SpObjectToken getSapiVoice(Model.Voice voice)
        {
            if (voice != null)
            {
                foreach (SpObjectToken sapiVoice in availableVoices)
                {
                    if (sapiVoice.Id.Equals(voice.Identifier))
                        return sapiVoice;
                }
            }

            return null;
        }

        private void voices()
        {
            SpVoice speechSynthesizer = new SpVoice();
            availableVoices = speechSynthesizer.GetVoices();

            string name;
            string desc;
            string gender;
            string age;
            string lang;
            string vendor;
            System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>();

            foreach (SpObjectToken voice in availableVoices)
            {
                if (voice.Id.StartsWith(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices\Tokens\eSpeakNG_"))
                {
                    if (Util.Config.DEBUG)
                        Debug.LogWarning("eSpeak-NG voices are ignored!");
                }
                else
                {
                    name = voice.GetAttribute("name");
                    desc = voice.GetDescription();
                    gender = voice.GetAttribute("gender");
                    age = voice.GetAttribute("age");
                    lang = voice.GetAttribute("language");
                    vendor = voice.GetAttribute("vendor");

                    if (string.IsNullOrEmpty(lang))
                    {
                        lang = "409"; //en-US
                    }

                    int langCode = int.Parse(lang, System.Globalization.NumberStyles.HexNumber);

                    string culture;
                    if (!Util.Helper.LocaleCodes.TryGetValue(langCode, out culture))
                    {
                        Debug.LogWarning("Voice with name '" + name + "' has an unknown language code: " + langCode + "(" + lang + ")!");

                        culture = "en-us";
                    }

                    voices.Add(new Model.Voice(name, desc, Util.Helper.StringToGender(gender), age, culture, voice.Id, vendor));
                }
            }

            cachedVoices = voices.OrderBy(s => s.Name).ToList();

            if (Util.Constants.DEV_DEBUG)
                Debug.Log("Voices read: " + cachedVoices.CTDump());
        }
#endif

        private IEnumerator getVoices()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

            System.Threading.Thread worker = new System.Threading.Thread(() => voices());
            worker.Start();

            do
            {
                yield return null;
            } while (worker.IsAlive);
#else
            yield return null;
#endif

            onVoicesReady();
        }

        private static string prepareText(Model.Wrapper wrapper)
        {
            //TEST
            //wrapper.ForceSSML = false;

            if (wrapper.ForceSSML && !Speaker.isAutoClearTags)
            {
                System.Text.StringBuilder sbXML = new System.Text.StringBuilder();

                sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                //sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">");
                sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
                sbXML.Append(wrapper.Voice == null ? "en-US" : wrapper.Voice.Culture);
                sbXML.Append("\">");

                float _pitch = wrapper.Pitch - 1f;

                if (_pitch != 0)
                {
                    sbXML.Append("<prosody pitch='");

                    if (_pitch > 0f)
                    {
                        sbXML.Append(_pitch.ToString("+#0%", Util.Helper.BaseCulture));
                    }
                    else
                    {
                        sbXML.Append(_pitch.ToString("#0%", Util.Helper.BaseCulture));
                    }

                    sbXML.Append("'>");
                }

                sbXML.Append(wrapper.Text);

                if (_pitch != 0f)
                    sbXML.Append("</prosody>");

                sbXML.Append("</speak>");

                //Debug.Log(sbXML.ToString().Replace('"', '\''));

                return sbXML.ToString().Replace('"', '\'');
            }

            return wrapper.Text.Replace('"', '\'');
        }

        private static int calculateVolume(float volume)
        {
            return Mathf.Clamp((int)(100 * volume), 0, 100);
        }

        private static int calculateRate(float rate)
        { //allowed range: 0 - 3f - all other values were cropped
            int result = 0;

            if (rate != 1f)
            { //relevant?
                if (rate > 1f)
                { //larger than 1
                    if (rate >= 2.75f)
                    {
                        result = 10; //2.78
                    }
                    else if (rate >= 2.6f && rate < 2.75f)
                    {
                        result = 9; //2.6
                    }
                    else if (rate >= 2.35f && rate < 2.6f)
                    {
                        result = 8; //2.39
                    }
                    else if (rate >= 2.2f && rate < 2.35f)
                    {
                        result = 7; //2.2
                    }
                    else if (rate >= 2f && rate < 2.2f)
                    {
                        result = 6; //2
                    }
                    else if (rate >= 1.8f && rate < 2f)
                    {
                        result = 5; //1.8
                    }
                    else if (rate >= 1.6f && rate < 1.8f)
                    {
                        result = 4; //1.6
                    }
                    else if (rate >= 1.4f && rate < 1.6f)
                    {
                        result = 3; //1.45
                    }
                    else if (rate >= 1.2f && rate < 1.4f)
                    {
                        result = 2; //1.28
                    }
                    else if (rate > 1f && rate < 1.2f)
                    {
                        result = 1; //1.14
                    }
                }
                else
                { //smaller than 1
                    if (rate <= 0.3f)
                    {
                        result = -10; //0.33
                    }
                    else if (rate > 0.3 && rate <= 0.4f)
                    {
                        result = -9; //0.375
                    }
                    else if (rate > 0.4 && rate <= 0.45f)
                    {
                        result = -8; //0.42
                    }
                    else if (rate > 0.45 && rate <= 0.5f)
                    {
                        result = -7; //0.47
                    }
                    else if (rate > 0.5 && rate <= 0.55f)
                    {
                        result = -6; //0.525
                    }
                    else if (rate > 0.55 && rate <= 0.6f)
                    {
                        result = -5; //0.585
                    }
                    else if (rate > 0.6 && rate <= 0.7f)
                    {
                        result = -4; //0.655
                    }
                    else if (rate > 0.7 && rate <= 0.8f)
                    {
                        result = -3; //0.732
                    }
                    else if (rate > 0.8 && rate <= 0.9f)
                    {
                        result = -2; //0.82
                    }
                    else if (rate > 0.9 && rate < 1f)
                    {
                        result = -1; //0.92
                    }
                }
            }

            if (Util.Constants.DEV_DEBUG)
                Debug.Log("calculateRate: " + result + " - " + rate);

            return result;
        }

        #endregion


        #region Editor-only methods

#if UNITY_EDITOR

        public override void GenerateInEditor(Model.Wrapper wrapper)
        {
#if UNITY_EDITOR_WIN
            if (wrapper == null)
            {
                Debug.LogWarning("'wrapper' is null!");
            }
            else
            {
                if (string.IsNullOrEmpty(wrapper.Text))
                {
                    Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
                }
                else
                {
                    string outputFile = getOutputFile(wrapper.Uid);

                    System.Threading.Thread worker = new System.Threading.Thread(() => speakToFile(wrapper, outputFile));
                    worker.Start();

                    silence = false;

                    onSpeakAudioGenerationStart(wrapper);

                    do
                    {
                        System.Threading.Thread.Sleep(50);
                    } while (worker.IsAlive);

                    processAudioFile(wrapper, outputFile);

                }
            }
#endif
        }

        public override void SpeakNativeInEditor(Model.Wrapper wrapper)
        {
#if UNITY_EDITOR_WIN
            if (wrapper == null)
            {
                Debug.LogWarning("'wrapper' is null!");
            }
            else
            {
                if (string.IsNullOrEmpty(wrapper.Text))
                {
                    Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
                }
                else
                {
                    silence = false;

                    onSpeakStart(wrapper);

                    System.Threading.Thread worker = new System.Threading.Thread(() => speakNative(wrapper));
                    worker.Start();

                    do
                    {
                        System.Threading.Thread.Sleep(50);
                    } while (worker.IsAlive);

                    onSpeakComplete(wrapper);
                }
            }
#endif
        }

        private void getVoicesInEditor()
        {
#if UNITY_EDITOR_WIN
            System.Threading.Thread worker = new System.Threading.Thread(() => voices());
            worker.Start();

            do
            {
                System.Threading.Thread.Sleep(50);
            } while (worker.IsAlive);

            onVoicesReady();
#endif
        }
#endif

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)