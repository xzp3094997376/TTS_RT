using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
    /// <summary>iOS voice provider.</summary>
    public class VoiceProviderIOS : BaseVoiceProvider
    {
        #region Variables

        private static System.Collections.Generic.List<Model.Voice> cachediOSVoices;

#if UNITY_IOS || UNITY_EDITOR

        private static string[] speechTextArray;

        private static int wordIndex = 0;

        private static bool isWorking = false;

        private static Model.Wrapper wrapperNative;
#endif

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for VoiceProviderIOS.
        /// </summary>
        /// <param name="obj">Instance of the speaker</param>
        public VoiceProviderIOS(MonoBehaviour obj) : base(obj)
        {
#if UNITY_IOS || UNITY_EDITOR
            GetVoices();
#endif
        }

        #endregion


        #region Bridge declaration and methods

#if UNITY_IOS || UNITY_EDITOR

        /// <summary>Silence the current TTS-provider (native mode).</summary>
        [System.Runtime.InteropServices.DllImport("__Internal")]
        extern static public void Stop();

        /// <summary>Silence the current TTS-provider (native mode).</summary>
        [System.Runtime.InteropServices.DllImport("__Internal")]
        extern static public void GetVoices();

        /*
        /// <summary>Bridge to the native tts system</summary>
        /// <param name="name">Name of the voice to speak.</param>
        /// <param name="text">Text to speak.</param>
        /// <param name="rate">Speech rate of the speaker in percent (default: 1, optional).</param>
        /// <param name="pitch">Pitch of the speech in percent (default: 1, optional).</param>
        /// <param name="volume">Volume of the speaker in percent (default: 1, optional).</param>
        [System.Runtime.InteropServices.DllImport("__Internal")]
        extern static public void Speak(string name, string text, float rate = 1f, float pitch = 1f, float volume = 1f);
        */

        /// <summary>Bridge to the native tts system</summary>
        /// <param name="id">Identifier of the voice to speak.</param>
        /// <param name="text">Text to speak.</param>
        /// <param name="rate">Speech rate of the speaker in percent (default: 1, optional).</param>
        /// <param name="pitch">Pitch of the speech in percent (default: 1, optional).</param>
        /// <param name="volume">Volume of the speaker in percent (default: 1, optional).</param>
        [System.Runtime.InteropServices.DllImport("__Internal")]
        extern static public void Speak(string id, string text, float rate = 1f, float pitch = 1f, float volume = 1f);

#endif

        /// <summary>Receives all voices</summary>
        /// <param name="voicesText">All voices as text string.</param>
        public static void SetVoices(string voicesText)
        {
#if UNITY_IOS || UNITY_EDITOR
            string[] voices = voicesText.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

            //if (voices.Length % 2 == 0)
            if (voices.Length % 3 == 0)
            {
                string name;
                string culture;
                Model.Voice newVoice;

                System.Collections.Generic.List<Model.Voice> voicesList = new System.Collections.Generic.List<Model.Voice>(60);

                //for (int ii = 0; ii < voices.Length; ii += 2)
                for (int ii = 0; ii < voices.Length; ii += 3)
                {
                    //name = voices[ii];
                    //culture = voices[ii + 1];
                    //newVoice = new Model.Voice(name, "iOS voice: " + name + " " + culture, Util.Helper.AppleVoiceNameToGender(name), "unknown", culture);

                    name = voices[ii + 1];
                    culture = voices[ii + 2];
                    newVoice = new Model.Voice(name, "iOS voice: " + name + " " + culture, Util.Helper.AppleVoiceNameToGender(name), "unknown", culture, voices[ii], "Apple");

                    voicesList.Add(newVoice);
                }

                cachediOSVoices = voicesList.OrderBy(s => s.Name).ToList();

                if (Util.Constants.DEV_DEBUG)
                    Debug.Log("Voices read: " + cachediOSVoices.CTDump());

                //onVoicesReady();
            }
            else
            {
                Debug.LogWarning("Voice-string contains wrong number of elements!");
            }
#endif

            onVoicesReady();
        }

        /// <summary>Receives the state of the speaker.</summary>
        /// <param name="state">The state of the speaker.</param>
        public static void SetState(string state)
        {
#if UNITY_IOS || UNITY_EDITOR
            if (state.Equals("Start"))
            {
                // do nothing
            }
            else if (state.Equals("Finsish"))
            {
                isWorking = false;
            }
            else
            { //cancel
                isWorking = false;
            }
#endif
        }

        /// <summary>Called everytime a new word is spoken.</summary>
        public static void WordSpoken()
        {
#if UNITY_IOS || UNITY_EDITOR
            if (wrapperNative != null)
            {
                onSpeakCurrentWord(wrapperNative, speechTextArray, wordIndex);
                wordIndex++;
            }
#endif
        }

        #endregion


        #region Implemented methods

        public override string AudioFileExtension
        {
            get
            {
                return "none";
            }
        }

        public override AudioType AudioFileType
        {
            get
            {
                return AudioType.UNKNOWN;
            }
        }

        public override string DefaultVoiceName
        {
            get
            {
                return "Daniel";
            }
        }

        public override System.Collections.Generic.List<Model.Voice> Voices
        {
            get
            {
                return cachediOSVoices;
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
                return 256000; //TODO find correct value
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
                return false;
            }
        }

        public override bool isPlatformSupported
        {
            get
            {
                return Util.Helper.isIOSPlatform;
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
            yield return speak(wrapper, true);
        }

        public override IEnumerator Speak(Model.Wrapper wrapper)
        {
            yield return speak(wrapper, false);
        }

        public override IEnumerator Generate(Model.Wrapper wrapper)
        {
            Debug.LogError("Generate is not supported for iOS!");
            yield return null;
        }

        public override void Silence()
        {
            silence = true;

#if UNITY_IOS || UNITY_EDITOR
            Stop();
#endif
        }

        #endregion


        #region Private methods

        private IEnumerator speak(Model.Wrapper wrapper, bool isNative)
        {

#if UNITY_IOS || UNITY_EDITOR
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

                    //string voiceName = getVoiceName(wrapper);
                    string voiceId = getVoiceId(wrapper);

                    silence = false;

                    if (!isNative)
                    {
                        onSpeakAudioGenerationStart(wrapper); //just a fake event if some code needs the feedback...
                        onSpeakAudioGenerationComplete(wrapper); //just a fake event if some code needs the feedback...
                    }

                    onSpeakStart(wrapper);
                    isWorking = true;

                    speechTextArray = Util.Helper.CleanText(wrapper.Text, false).Split(splitCharWords, System.StringSplitOptions.RemoveEmptyEntries);
                    wordIndex = 0;
                    wrapperNative = wrapper;

                    //Speak(voiceName, wrapper.Text, calculateRate(wrapper.Rate), wrapper.Pitch, wrapper.Volume);
                    Speak(voiceId, wrapper.Text, calculateRate(wrapper.Rate), wrapper.Pitch, wrapper.Volume);

                    do
                    {
                        yield return null;
                    } while (isWorking && !silence);

                    if (Util.Config.DEBUG)
                        Debug.Log("Text spoken: " + wrapper.Text);

                    wrapperNative = null;
                    onSpeakComplete(wrapper);
                }
            }
#else
            yield return null;
#endif
        }

        private static float calculateRate(float rate)
        {
            float result = rate;

            if (rate > 1f)
            {
                //result = (rate + 1f) * 0.5f;
                result = 1f + (rate - 1f) * 0.25f;
            }

            if (Util.Constants.DEV_DEBUG)
                Debug.Log("calculateRate: " + result + " - " + rate);

            return result;
        }

        private string getVoiceId(Model.Wrapper wrapper)
        {
            if (wrapper.Voice == null || string.IsNullOrEmpty(wrapper.Voice.Identifier))
            {
                if (Util.Config.DEBUG)
                    Debug.LogWarning("'wrapper.Voice' or 'wrapper.Voice.Identifier' is null! Using the OS 'default' voice.");

                if (Voices.Count > 0)
                {
                    //always use English as fallback
                    return Speaker.VoiceForCulture("en").Identifier;
                }

                return "Daniel"; //TODO change!
            }
            else
            {
                return wrapper.Voice.Identifier;
            }
        }

        #endregion


        #region Editor-only methods

#if UNITY_EDITOR

        public override void GenerateInEditor(Model.Wrapper wrapper)
        {
            Debug.LogError("GenerateInEditor is not supported for Unity iOS!");
        }

        public override void SpeakNativeInEditor(Model.Wrapper wrapper)
        {
            Debug.LogError("SpeakNativeInEditor is not supported for Unity iOS!");
        }
#endif

        #endregion
    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)