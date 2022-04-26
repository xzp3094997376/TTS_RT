﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
    /// <summary>MaryTTS voice provider.</summary>
    public class VoiceProviderMary : BaseVoiceProvider
    {

        #region Variables

        private string uri;

        private System.Collections.Generic.Dictionary<string, string> headers = new System.Collections.Generic.Dictionary<string, string>();

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for VoiceProviderMary. Needed to pass IP and Port of the MaryTTS server to the Provider.
        /// </summary>
        /// <param name="obj">Instance of the speaker</param>
        /// <param name="url">IP-Address of the MaryTTS-server</param>
        /// <param name="port">Port to connect to on the MaryTTS-server</param>
        public VoiceProviderMary(MonoBehaviour obj, string url, int port, string user, string password) : base(obj)
        {
            uri = Util.Helper.CleanUrl(url, false, false) + ":" + port;

            if (!string.IsNullOrEmpty(user))
            {
                headers["Authorization"] = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(user + ":" + password));
            }

            if (Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR
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
                return "cmu-rms-hsmm";
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
                return 256000;
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
                return true;
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
                    if (!Util.Helper.isInternetAvailable)
                    {
                        string errorMessage = "Internet is not available - can't use MaryTTS right now!";
                        Debug.LogError(errorMessage);
                        onErrorInfo(wrapper, errorMessage);
                    }
                    else
                    {
                        yield return null; //return to the main process (uid)

                        string voiceCulture = getVoiceCulture(wrapper);
                        string voiceName = getVoiceName(wrapper);

                        silence = false;

                        onSpeakAudioGenerationStart(wrapper);

                        System.Text.StringBuilder sbXML = new System.Text.StringBuilder();
                        string request = null;

                        if (Speaker.MaryType == Model.Enum.MaryTTSType.RAWMARYXML)
                        {
                            //RAWMARYXML
                            sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                            sbXML.Append("<maryxml version=\"0.5\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://mary.dfki.de/2002/MaryXML\" xml:lang=\"");
                            sbXML.Append(voiceCulture);
                            sbXML.Append("\">");

                            sbXML.Append(prepareProsody(wrapper));

                            sbXML.Append("</maryxml>");

                            request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(sbXML.ToString()) + "&INPUT_TYPE=RAWMARYXML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" + voiceCulture + "&VOICE=" + voiceName;
                        }
                        else if (Speaker.MaryType == Model.Enum.MaryTTSType.EMOTIONML)
                        {

                            //EMOTIONML
                            sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                            sbXML.Append("<emotionml version=\"1.0\" ");
                            sbXML.Append("xmlns=\"http://www.w3.org/2009/10/emotionml\"");
                            //sbXML.Append (" category-set=\"http://www.w3.org/TR/emotion-voc/xml#everyday-categories\"");
                            sbXML.Append(" category-set=\"http://www.w3.org/TR/emotion-voc/xml#big6\"");
                            sbXML.Append(">");

                            sbXML.Append(wrapper.Text);
                            sbXML.Append("</emotionml>");

                            request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(sbXML.ToString()) + "&INPUT_TYPE=EMOTIONML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" + voiceCulture + "&VOICE=" + voiceName;
                        }
                        else if (Speaker.MaryType == Model.Enum.MaryTTSType.SSML)
                        {
                            //SSML
                            sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                            sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
                            sbXML.Append(voiceCulture);
                            sbXML.Append("\"");
                            //sbXML.Append (" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
                            //sbXML.Append (" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\"");
                            sbXML.Append(">");

                            sbXML.Append(prepareProsody(wrapper));

                            sbXML.Append("</speak>");

                            request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(sbXML.ToString()) + "&INPUT_TYPE=SSML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" + voiceCulture + "&VOICE=" + voiceName;
                        }
                        else
                        {
                            //TEXT
                            request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(wrapper.Text) + "&INPUT_TYPE=TEXT&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" + voiceCulture + "&VOICE=" + voiceName;
                        }

                        if (Util.Constants.DEV_DEBUG)
                            Debug.Log(sbXML);

                        if (wrapper.Volume != 1f)
                        {
                            request += "&effect_Volume_selected=on&effect_Volume_parameters=amount:" + wrapper.Volume;
                        }

                        //Debug.Log (request);

                        using (UnityWebRequest www = UnityWebRequest.Get(request.Trim()))
                        {
                            if (headers != null)
                            {
                                foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in headers)
                                {
                                    www.SetRequestHeader(kvp.Key, kvp.Value);
                                }
                            }

#if UNITY_2017_2_OR_NEWER
                            www.downloadHandler = new DownloadHandlerBuffer();
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
                                processAudioFile(wrapper, wrapper.OutputFile, false, www.downloadHandler.data);
                            }
                            else
                            {
                                string errorMessage = "Could not generate the speech: " + wrapper + System.Environment.NewLine + "WWW error: " + www.error;
                                Debug.LogError(errorMessage);
                                onErrorInfo(wrapper, errorMessage);
                            }
                        }
                    }
                }
            }
        }

        public override void Silence()
        {
            silence = true;
        }

        #endregion


        #region Private methods

        private IEnumerator speak(Model.Wrapper wrapper, bool isNative)
        {
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
                        if (!Util.Helper.isInternetAvailable)
                        {
                            string errorMessage = "Internet is not available - can't use MaryTTS right now!";
                            Debug.LogError(errorMessage);
                            onErrorInfo(wrapper, errorMessage);
                        }
                        else
                        {
                            yield return null; //return to the main process (uid)

                            string voiceCulture = getVoiceCulture(wrapper);
                            string voiceName = getVoiceName(wrapper);

                            silence = false;

                            if (!isNative)
                            {
                                onSpeakAudioGenerationStart(wrapper);
                            }

                            System.Text.StringBuilder sbXML = new System.Text.StringBuilder();
                            string request = null;

                            if (Speaker.MaryType == Model.Enum.MaryTTSType.RAWMARYXML)
                            {
                                //RAWMARYXML
                                sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                                sbXML.Append("<maryxml version=\"0.5\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://mary.dfki.de/2002/MaryXML\" xml:lang=\"");
                                sbXML.Append(voiceCulture);
                                sbXML.Append("\">");

                                sbXML.Append(prepareProsody(wrapper));

                                sbXML.Append("</maryxml>");

                                request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(sbXML.ToString()) + "&INPUT_TYPE=RAWMARYXML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" + voiceCulture + "&VOICE=" + voiceName;
                            }
                            else if (Speaker.MaryType == Model.Enum.MaryTTSType.EMOTIONML)
                            {

                                //EMOTIONML
                                sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                                sbXML.Append("<emotionml version=\"1.0\" ");
                                sbXML.Append("xmlns=\"http://www.w3.org/2009/10/emotionml\"");
                                //sbXML.Append (" category-set=\"http://www.w3.org/TR/emotion-voc/xml#everyday-categories\"");
                                sbXML.Append(" category-set=\"http://www.w3.org/TR/emotion-voc/xml#big6\""); //TODO needed?
                                sbXML.Append(">");

                                sbXML.Append(wrapper.Text);
                                sbXML.Append("</emotionml>");

                                request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(sbXML.ToString()) + "&INPUT_TYPE=EMOTIONML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" + voiceCulture + "&VOICE=" + voiceName;
                            }
                            else if (Speaker.MaryType == Model.Enum.MaryTTSType.SSML)
                            {
                                //SSML
                                sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                                //sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\"");
                                sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
                                sbXML.Append(voiceCulture);
                                sbXML.Append("\"");
                                //sbXML.Append (" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
                                //sbXML.Append (" xsi:schemaLocation=\"http://www.w3.org/2001/10/synthesis http://www.w3.org/TR/speech-synthesis/synthesis.xsd\"");
                                sbXML.Append(">");

                                sbXML.Append(prepareProsody(wrapper));

                                sbXML.Append("</speak>");

                                request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(sbXML.ToString()) + "&INPUT_TYPE=SSML&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" + voiceCulture + "&VOICE=" + voiceName;
                            }
                            else
                            {
                                //TEXT
                                request = uri + "/process?INPUT_TEXT=" + System.Uri.EscapeDataString(wrapper.Text) + "&INPUT_TYPE=TEXT&OUTPUT_TYPE=AUDIO&AUDIO=WAVE_FILE&LOCALE=" + voiceCulture + "&VOICE=" + voiceName;
                            }

                            if (Util.Constants.DEV_DEBUG)
                                Debug.Log(sbXML);

                            if (wrapper.Volume != 1f)
                            {
                                request += "&effect_Volume_selected=on&effect_Volume_parameters=amount:" + wrapper.Volume;
                            }

                            //Debug.Log(request);

                            yield return playAudioFile(wrapper, request, wrapper.OutputFile, AudioFileType, isNative, false, headers);
                        }
                    }
                }
            }
        }

        private IEnumerator getVoices()
        {
            System.Collections.Generic.List<string[]> serverVoicesResponse = new System.Collections.Generic.List<string[]>();

            if (!Util.Helper.isInternetAvailable)
            {
                string errorMessage = "Internet is not available - can't use MaryTTS right now!";
                Debug.LogError(errorMessage);
                onErrorInfo(null, errorMessage);
            }
            else
            {
                using (UnityWebRequest www = UnityWebRequest.Get(uri + "/voices"))
                {
                    if (headers != null)
                    {
                        foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in headers)
                        {
                            www.SetRequestHeader(kvp.Key, kvp.Value);
                        }
                    }

#if UNITY_2017_2_OR_NEWER
                    www.downloadHandler = new DownloadHandlerBuffer();
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
                        string[] rawVoices = www.downloadHandler.text.Split('\n');
                        foreach (string rawVoice in rawVoices)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(rawVoice))
                                {
                                    string[] newVoice = {
                                        rawVoice.Split (' ') [0],
                                        rawVoice.Split (' ') [1],
                                        rawVoice.Split (' ') [2]
                                    };
                                    serverVoicesResponse.Add(newVoice);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning("Problem preparing voice: " + rawVoice + " - " + ex);
                            }
                        }

                        System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(40);

                        foreach (string[] voice in serverVoicesResponse)
                        {
                            voices.Add(new Model.Voice(voice[0], "MaryTTS voice: " + voice[0], Util.Helper.StringToGender(voice[2]), "unknown", voice[1]));
                        }

                        cachedVoices = voices.OrderBy(s => s.Name).ToList();

                        if (Util.Constants.DEV_DEBUG)
                            Debug.Log("Voices read: " + cachedVoices.CTDump());
                    }
                    else
                    {
                        string errorMessage = "Could not get the voices: " + www.error;

                        Debug.LogError(errorMessage);
                        onErrorInfo(null, errorMessage);
                    }
                }

                onVoicesReady();
            }
        }

        private static string prepareProsody(Model.Wrapper wrapper)
        {
            if (wrapper.Rate != 1f || wrapper.Pitch != 1f)
            {
                System.Text.StringBuilder sbXML = new System.Text.StringBuilder();

                sbXML.Append("<prosody");

                if (wrapper.Rate != 1f)
                {
                    float _rate = wrapper.Rate > 1 ? (wrapper.Rate - 1f) * 0.5f : wrapper.Rate - 1f;

                    sbXML.Append(" rate=\"");
                    if (_rate >= 0f)
                    {
                        sbXML.Append(_rate.ToString("+#0%", Util.Helper.BaseCulture));
                    }
                    else
                    {
                        sbXML.Append(_rate.ToString("#0%", Util.Helper.BaseCulture));
                    }

                    sbXML.Append("\"");
                }

                if (wrapper.Pitch != 1f)
                {
                    float _pitch = wrapper.Pitch - 1f;

                    sbXML.Append(" pitch=\"");
                    if (_pitch >= 0f)
                    {
                        sbXML.Append(_pitch.ToString("+#0%", Util.Helper.BaseCulture));
                    }
                    else
                    {
                        sbXML.Append(_pitch.ToString("#0%", Util.Helper.BaseCulture));
                    }

                    sbXML.Append("\"");
                }

                sbXML.Append(">");

                sbXML.Append(wrapper.Text);

                sbXML.Append("</prosody>");

                return sbXML.ToString();
            }

            return wrapper.Text;
        }

        private string getVoiceCulture(Model.Wrapper wrapper)
        {
            if (wrapper.Voice == null || string.IsNullOrEmpty(wrapper.Voice.Culture))
            {
                if (Util.Config.DEBUG)
                    Debug.LogWarning("'wrapper.Voice' or 'wrapper.Voice.Culture' is null! Using the 'default' English voice.");

                //always use English as fallback
                return "en-US";
            }
            else
            {
                return wrapper.Voice.Culture;
            }
        }

        #endregion


        #region Editor-only methods

#if UNITY_EDITOR

        public override void GenerateInEditor(Model.Wrapper wrapper)
        {
            Debug.LogError("GenerateInEditor is not supported for MaryTTS!");
        }

        public override void SpeakNativeInEditor(Model.Wrapper wrapper)
        {
            Debug.LogError("SpeakNativeInEditor is not supported for MaryTTS!");
        }

        private void getVoicesInEditor()
        {
            System.Collections.Generic.List<string[]> serverVoicesResponse = new System.Collections.Generic.List<string[]>();

            if (!Util.Helper.isInternetAvailable)
            {
                string errorMessage = "Internet is not available - can't use MaryTTS right now!";
                Debug.LogError(errorMessage);
            }
            else
            {
                try
                {
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = Util.Helper.RemoteCertificateValidationCallback;

                    using (System.Net.WebClient client = new Common.Util.CTWebClient())
                    {
                        if (headers != null)
                        {
                            foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in headers)
                            {
                                client.Headers.Add(kvp.Key, kvp.Value);
                            }
                        }

                        using (System.IO.Stream stream = client.OpenRead(uri + "/voices"))
                        {
                            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                            {
                                string content = reader.ReadToEnd();

                                if (Util.Config.DEBUG)
                                    Debug.Log(content);

                                string[] rawVoices = content.Split('\n');
                                foreach (string rawVoice in rawVoices)
                                {
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(rawVoice))
                                        {
                                            string[] newVoice = {
                                        rawVoice.Split (' ') [0],
                                        rawVoice.Split (' ') [1],
                                        rawVoice.Split (' ') [2]
                                    };
                                            serverVoicesResponse.Add(newVoice);
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        Debug.LogWarning("Problem preparing voice: " + rawVoice + " - " + ex);
                                    }
                                }

                                System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(40);

                                foreach (string[] voice in serverVoicesResponse)
                                {
                                    voices.Add(new Model.Voice(voice[0], "MaryTTS voice: " + voice[0], Util.Helper.StringToGender(voice[2]), "unknown", voice[1]));
                                }

                                cachedVoices = voices.OrderBy(s => s.Name).ToList();

                                if (Util.Constants.DEV_DEBUG)
                                    Debug.Log("Voices read: " + cachedVoices.CTDump());
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(ex);
                }

                onVoicesReady();
            }
        }
#endif

        #endregion

    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)