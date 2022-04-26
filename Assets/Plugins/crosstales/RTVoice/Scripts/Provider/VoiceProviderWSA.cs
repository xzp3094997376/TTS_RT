using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
    /// <summary>WSA (UWP) voice provider.</summary>
    public class VoiceProviderWSA : BaseVoiceProvider
    {

        #region Variables

#if UNITY_WSA || UNITY_EDITOR

        private static bool isInitialized = false;
        private static RTVoiceUWPBridge ttsHandler;
        private readonly WaitForSeconds wfs = new WaitForSeconds(0.1f);

#endif

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for VoiceProviderWSA.
        /// </summary>
        /// <param name="obj">Instance of the speaker</param>
        public VoiceProviderWSA(MonoBehaviour obj) : base(obj)
        {
#if UNITY_WSA || UNITY_EDITOR
            if (!isInitialized)
            {
                initializeTTS();
            }
#endif

            speakerObj.StartCoroutine(getVoices());
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
                return "Microsoft David";
            }
        }

        public override bool isWorkingInEditor
        {
            get
            {
                return false;
            }
        }

        public override int MaxTextLength
        {
            get
            {
                return 64000;
            }
        }

        public override bool isSpeakNativeSupported
        {
            get
            {
                return false;
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
                return Util.Helper.isWSAPlatform;
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
            yield return speak(wrapper, true);
        }

        public override IEnumerator Speak(Model.Wrapper wrapper)
        {
            yield return speak(wrapper, false);
        }

        public override IEnumerator Generate(Model.Wrapper wrapper)
        {
#if UNITY_WSA || UNITY_EDITOR
            if (wrapper == null)
            {
                Debug.LogWarning("'wrapper' is null!");
            }
            else
            {
                if (string.IsNullOrEmpty(wrapper.Text))
                {
                    Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
                    yield return null;
                }
                else
                {
                    do
                    {
                        yield return null;
                    } while (!isInitialized);

                    string voiceName = getVoiceName(wrapper);
                    string outputFile = getOutputFile(wrapper.Uid, true);

                    ttsHandler.SynthesizeToFile(prepareText(wrapper), Application.persistentDataPath.Replace('/', '\\'), Util.Constants.AUDIOFILE_PREFIX + wrapper.Uid + AudioFileExtension, voiceName);

                    silence = false;

                    onSpeakAudioGenerationStart(wrapper);

                    do
                    {
                        yield return wfs;
                    } while (!silence && ttsHandler.isBusy);

                    //Debug.Log("FILE: " + "file://" + outputFile + "/" + wrapper.Uid + extension);

                    processAudioFile(wrapper, outputFile);
                }
            }

#else
            yield return null;
#endif
        }

        public override void Silence()
        {
            silence = true;
        }

        #endregion


        #region Private methods

        private IEnumerator getVoices()
        {
#if UNITY_WSA || UNITY_EDITOR

            do
            {
                yield return null;
            } while (!isInitialized);

            try
            {
                System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(70);
                string[] myStringVoices = ttsHandler.Voices;
                string name;

                foreach (string voice in myStringVoices)
                {
                    string[] currentVoiceData = voice.Split(';');
                    name = currentVoiceData[0];
                    Model.Voice newVoice = new Model.Voice(name, "UWP voice: " + voice, Util.Helper.WSAVoiceNameToGender(name), "unknown", currentVoiceData[1]);
                    voices.Add(newVoice);
                }

                cachedVoices = voices.OrderBy(s => s.Name).ToList();

                if (Util.Constants.DEV_DEBUG)
                    Debug.Log("Voices read: " + cachedVoices.CTDump());
            }
            catch (System.Exception ex)
            {
                string errorMessage = "Could not get any voices!" + System.Environment.NewLine + ex;
                Debug.LogError(errorMessage);
                onErrorInfo(null, errorMessage);
            }
#else
            yield return null;
#endif

            onVoicesReady();
        }

        private IEnumerator speak(Model.Wrapper wrapper, bool isNative)
        {

#if UNITY_WSA || UNITY_EDITOR
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
                        do
                        {
                            yield return null;
                        } while (!isInitialized);

                        string voiceName = getVoiceName(wrapper);
                        string outputFile = getOutputFile(wrapper.Uid, true);

                        ttsHandler.SynthesizeToFile(prepareText(wrapper), Application.persistentDataPath.Replace('/', '\\'), Util.Constants.AUDIOFILE_PREFIX + wrapper.Uid + AudioFileExtension, voiceName);

                        silence = false;

                        if (!isNative)
                        {
                            onSpeakAudioGenerationStart(wrapper);
                        }

                        do
                        {
                            yield return wfs;
                        } while (!silence && ttsHandler.isBusy);

                        yield return playAudioFile(wrapper, Util.Constants.PREFIX_FILE + outputFile, outputFile, AudioType.WAV, isNative);
                    }
                }
            }

#else
            yield return null;
#endif
        }

#if UNITY_WSA || UNITY_EDITOR

        private void initializeTTS()
        {
            if (Util.Constants.DEV_DEBUG)
                Debug.Log("Initializing TTS...");

            ttsHandler = new RTVoiceUWPBridge();

            ttsHandler.DEBUG = Util.Config.DEBUG;

            //Debug.Log("TARGET FOLDER: " + ttsHandler.GetTargetFolder());

            isInitialized = true;
        }

#endif

        private static string prepareText(Model.Wrapper wrapper)
        {
            if (wrapper.ForceSSML && !Speaker.isAutoClearTags)
            {
                System.Text.StringBuilder sbXML = new System.Text.StringBuilder();

                sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                //sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">");
                sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
                sbXML.Append(wrapper.Voice == null ? "en-US" : wrapper.Voice.Culture);
                sbXML.Append("\">");

                sbXML.Append("<prosody pitch=\"");

                float _pitch = wrapper.Pitch - 1f;

                if (_pitch >= 0f)
                {
                    sbXML.Append(_pitch.ToString("+#0%", Util.Helper.BaseCulture));
                }
                else
                {
                    sbXML.Append(_pitch.ToString("#0%", Util.Helper.BaseCulture));
                }

                sbXML.Append("\">");

                sbXML.Append("<prosody rate=\"");
                sbXML.Append(wrapper.Rate.ToString());
                sbXML.Append("\">");

                sbXML.Append("<prosody volume=\"");

                float _volume = wrapper.Volume - 1f;

                if (_volume >= 0f)
                {
                    sbXML.Append(_volume.ToString("+#0%", Util.Helper.BaseCulture));
                }
                else
                {
                    sbXML.Append(_volume.ToString("#0%", Util.Helper.BaseCulture));
                }

                sbXML.Append("\">");

                sbXML.Append(wrapper.Text);

                sbXML.Append("</prosody>");
                sbXML.Append("</prosody>");
                sbXML.Append("</prosody>");

                sbXML.Append("</speak>");

                //Debug.Log(sbXML);

                //return sbXML.ToString().Replace('"', '\'');
                return sbXML.ToString();
            }

            return wrapper.Text;
        }

        #endregion


        #region Editor-only methods


#if UNITY_EDITOR

        public override void GenerateInEditor(Model.Wrapper wrapper)
        {
            Debug.LogError("GenerateInEditor is not supported for Unity WSA!");
        }

        public override void SpeakNativeInEditor(Model.Wrapper wrapper)
        {
            Debug.LogError("SpeakNativeInEditor is not supported for Unity WSA!");
        }

#endif

        #endregion

    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)