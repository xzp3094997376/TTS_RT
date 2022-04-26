using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
    /// <summary>Android voice provider.</summary>
    public class VoiceProviderAndroid : BaseVoiceProvider
    {

        #region Variables

#if UNITY_ANDROID || UNITY_EDITOR
        private static bool isInitialized = false;
        private static AndroidJavaObject TtsHandler;

        private readonly WaitForSeconds wfs = new WaitForSeconds(0.1f);
#endif

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for VoiceProviderAndroid.
        /// </summary>
        /// <param name="obj">Instance of the speaker</param>
        public VoiceProviderAndroid(MonoBehaviour obj) : base(obj)
        {
#if UNITY_ANDROID || UNITY_EDITOR
            if (!isInitialized)
            {
                initializeTTS();
            }

            speakerObj.StartCoroutine(getVoices());
#endif
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
                return "English (United States)";
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
                return 3999;
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
                return Util.Helper.isAndroidPlatform;
            }
        }

        public override bool isSSMLSupported
        {
            get
            {
                return false;
            }
        }

        public override IEnumerator SpeakNative(Model.Wrapper wrapper)
        {
#if UNITY_ANDROID || UNITY_EDITOR
            if (wrapper == null)
            {
                Debug.LogWarning("'wrapper' is null!");
            }
            else
            {
                if (string.IsNullOrEmpty(wrapper.Text))
                {
                    Debug.LogWarning("'wrapper.Text' is null or empty!");
                }
                else
                {
                    yield return null; //return to the main process (uid)

                    if (!isInitialized)
                    {
                        do
                        {
                            // waiting...
                            yield return wfs;

                        } while (!(isInitialized = TtsHandler.CallStatic<bool>("isInitalized")));
                    }

                    string voiceName = getVoiceName(wrapper);
                    silence = false;
                    onSpeakStart(wrapper);

                    TtsHandler.CallStatic("SpeakNative", new object[] {
                        wrapper.Text,
                        wrapper.Rate,
                        wrapper.Pitch,
                        wrapper.Volume,
                        voiceName
                    });

                    do
                    {
                        yield return wfs;
                    } while (!silence && TtsHandler.CallStatic<bool>("isWorking"));

                    if (Util.Config.DEBUG)
                        Debug.Log("Text spoken: " + wrapper.Text);

                    onSpeakComplete(wrapper);
                }
            }

#else
            yield return null;
#endif

        }

        public override IEnumerator Speak(Model.Wrapper wrapper)
        {
#if UNITY_ANDROID || UNITY_EDITOR
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
                        yield return null; //return to the main process (uid)

                        if (!isInitialized)
                        {
                            do
                            {
                                // waiting...
                                yield return wfs;

                            } while (!(isInitialized = TtsHandler.CallStatic<bool>("isInitalized")));
                        }

                        string voiceName = getVoiceName(wrapper);
                        string outputFile = getOutputFile(wrapper.Uid, true);

                        TtsHandler.CallStatic<string>("Speak", new object[] {
                            wrapper.Text,
                            wrapper.Rate,
                            wrapper.Pitch,
                            voiceName,
                            outputFile
                        });

                        silence = false;
                        onSpeakAudioGenerationStart(wrapper);

                        do
                        {
                            yield return wfs;
                        } while (!silence && TtsHandler.CallStatic<bool>("isWorking"));

                        yield return playAudioFile(wrapper, Util.Constants.PREFIX_FILE + outputFile, outputFile);
                    }
                }
            }
#else
            yield return null;
#endif

        }

        public override IEnumerator Generate(Model.Wrapper wrapper)
        {
#if UNITY_ANDROID || UNITY_EDITOR
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
                    yield return null; //return to the main process (uid)

                    if (!isInitialized)
                    {
                        do
                        {
                            // waiting...
                            yield return wfs;

                        } while (!(isInitialized = TtsHandler.CallStatic<bool>("isInitalized")));
                    }

                    string voiceName = getVoiceName(wrapper);
                    string outputFile = getOutputFile(wrapper.Uid, true);

                    TtsHandler.CallStatic<string>("Speak", new object[] {
                        wrapper.Text,
                        wrapper.Rate,
                        wrapper.Pitch,
                        voiceName,
                        outputFile
                    });

                    silence = false;
                    onSpeakAudioGenerationStart(wrapper);

                    do
                    {
                        yield return wfs;
                    } while (!silence && TtsHandler.CallStatic<bool>("isWorking"));

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

#if UNITY_ANDROID || UNITY_EDITOR
            TtsHandler.CallStatic("StopNative");
#endif
        }

        #endregion


        #region Public methods


        public void ShutdownTTS()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            TtsHandler.CallStatic("Shutdown");
#endif
        }

        #endregion


        #region Private methods

        private IEnumerator getVoices()
        {
#if UNITY_ANDROID || UNITY_EDITOR
            yield return null;

            if (!isInitialized)
            {
                do
                {
                    yield return wfs;
                } while (!(isInitialized = TtsHandler.CallStatic<bool>("isInitalized")));
            }

            try
            {
                string[] myStringVoices = TtsHandler.CallStatic<string[]>("GetVoices");

                System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(300);

                foreach (string voice in myStringVoices)
                {
                    string[] currentVoiceData = voice.Split(';');

                    Model.Enum.Gender gender = Model.Enum.Gender.UNKNOWN;

                    if (currentVoiceData[0].CTContains("#male"))
                    {
                        gender = Model.Enum.Gender.MALE;
                    }
                    else if (currentVoiceData[0].CTContains("#female"))
                    {
                        gender = Model.Enum.Gender.FEMALE;
                    }

                    Model.Voice newVoice = new Model.Voice(currentVoiceData[0], "Android voice: " + voice, gender, "unknown", currentVoiceData[1]);
                    voices.Add(newVoice);
                }

                cachedVoices = voices.OrderBy(s => s.Name).ToList();

                if (Util.Constants.DEV_DEBUG)
                    Debug.Log("Voices read: " + cachedVoices.CTDump());

                //onVoicesReady();
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

#if UNITY_ANDROID || UNITY_EDITOR

        private void initializeTTS()
        {
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
            TtsHandler = new AndroidJavaObject("com.crosstales.RTVoice.RTVoiceAndroidBridge", new object[] { jo });
        }

#endif
        #endregion


        #region Editor-only methods

#if UNITY_EDITOR

        public override void GenerateInEditor(Model.Wrapper wrapper)
        {
            Debug.LogError("GenerateInEditor is not supported for Unity Android!");
        }

        public override void SpeakNativeInEditor(Model.Wrapper wrapper)
        {
            Debug.LogError("SpeakNativeInEditor is not supported for Unity Android!");
        }

#endif

        #endregion

    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)