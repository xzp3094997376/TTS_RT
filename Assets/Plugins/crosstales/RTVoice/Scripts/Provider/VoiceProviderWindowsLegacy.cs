using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
    /// <summary>Windows voice provider (Legacy).</summary>
    public class VoiceProviderWindowsLegacy : BaseVoiceProvider
    {

        #region Variables

        private string dataPath;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        private const string idVoice = "@VOICE:";
        private const string idSpeak = "@SPEAK";
        private const string idWord = "@WORD";
        private const string idPhoneme = "@PHONEME:";
        private const string idViseme = "@VISEME:";
        private const string idStart = "@STARTED";

        private static char[] splitChar = new char[] { ':' };
#endif

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for VoiceProviderWindowsLegacy.
        /// </summary>
        /// <param name="obj">Instance of the speaker</param>
        public VoiceProviderWindowsLegacy(MonoBehaviour obj) : base(obj)
        {
            dataPath = Application.dataPath;

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
                return 32000;
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
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
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

                    string application = applicationName();

                    if (System.IO.File.Exists(application))
                    {
                        string voiceName = getVoiceName(wrapper);
                        int calculatedRate = calculateRate(wrapper.Rate);
                        int calculatedVolume = calculateVolume(wrapper.Volume);

                        System.Diagnostics.Process speakProcess = new System.Diagnostics.Process();

                        string args = "--speak " + '"' + prepareText(wrapper) + "\" " +
                                      calculatedRate.ToString() + " " +
                                      calculatedVolume.ToString() + " \"" +
                                      voiceName.Replace('"', '\'') + '"';

                        if (Util.Config.DEBUG)
                            Debug.Log("Process arguments: " + args);

                        speakProcess.StartInfo.FileName = application;
                        speakProcess.StartInfo.Arguments = args;

                        string[] speechTextArray = Util.Helper.CleanText(wrapper.Text, false).Split(splitCharWords, System.StringSplitOptions.RemoveEmptyEntries);
                        int wordIndex = 0;
                        int wordIndexCompare = 0;
                        string phoneme = string.Empty;
                        string viseme = string.Empty;
                        bool start = false;

                        System.Threading.Thread worker = new System.Threading.Thread(() => readSpeakNativeStream(ref speakProcess, ref speechTextArray, out wordIndex, out phoneme, out viseme, out start)) { Name = wrapper.Uid.ToString() };
                        worker.Start();

                        silence = false;
                        processes.Add(wrapper.Uid, speakProcess);
                        //workers.Add(wrapper.Uid, worker);

                        do
                        {
                            yield return null;

                            if (wordIndex != wordIndexCompare)
                            {
                                onSpeakCurrentWord(wrapper, speechTextArray, wordIndex - 1);

                                wordIndexCompare = wordIndex;
                            }

                            if (!string.IsNullOrEmpty(phoneme))
                            {
                                onSpeakCurrentPhoneme(wrapper, phoneme);

                                phoneme = string.Empty;
                            }

                            if (!string.IsNullOrEmpty(viseme))
                            {
                                onSpeakCurrentViseme(wrapper, viseme);

                                viseme = string.Empty;
                            }

                            if (start)
                            {
                                onSpeakStart(wrapper);

                                start = false;
                            }
                        } while (worker.IsAlive || !speakProcess.HasExited);

                        // clear output
                        onSpeakCurrentPhoneme(wrapper, string.Empty);
                        onSpeakCurrentViseme(wrapper, string.Empty);

                        if (speakProcess.ExitCode == 0 || speakProcess.ExitCode == -1)
                        { //0 = normal ended, -1 = killed
                            if (Util.Config.DEBUG)
                                Debug.Log("Text spoken: " + wrapper.Text);

                            onSpeakComplete(wrapper);
                        }
                        else
                        {
                            using (System.IO.StreamReader sr = speakProcess.StandardError)
                            {
                                string errorMessage = "Could not speak the text: " + wrapper + System.Environment.NewLine + "Exit code: " + speakProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                                Debug.LogError(errorMessage);
                                onErrorInfo(wrapper, errorMessage);
                            }
                        }

                        processes.Remove(wrapper.Uid);
                        //workers.Remove(wrapper.Uid);
                        speakProcess.Dispose();
                    }
                    else
                    {
                        string errorMessage = "Could not find the TTS-wrapper: '" + application + "'";
                        Debug.LogError(errorMessage);
                        onErrorInfo(wrapper, errorMessage);
                    }
                }
            }
#else
            yield return null;
#endif
        }


        public override IEnumerator Speak(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
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

                        string application = applicationName();

                        if (System.IO.File.Exists(application))
                        {
                            string voiceName = getVoiceName(wrapper);
                            int calculatedRate = calculateRate(wrapper.Rate);
                            int calculatedVolume = calculateVolume(wrapper.Volume);

                            System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                            string outputFile = getOutputFile(wrapper.Uid);

                            //Debug.Log("Pitch: " + wrapper.Pitch + " - Rate: " + wrapper.Rate + " - Volume: " + wrapper.Volume);
                            //Debug.Log(prepareText(wrapper));

                            string args = "--speakToFile" + " \"" +
                                          prepareText(wrapper) + "\" \"" +
                                          outputFile.Replace('"', '\'') + "\" " +
                                          calculatedRate.ToString() + " " +
                                          calculatedVolume.ToString() + " \"" +
                                          voiceName.Replace('"', '\'') + '"';

                            if (Util.Config.DEBUG)
                                Debug.Log("Process arguments: " + args);

                            speakToFileProcess.StartInfo.FileName = application;
                            speakToFileProcess.StartInfo.Arguments = args;

                            System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(ref speakToFileProcess)) { Name = wrapper.Uid.ToString() };
                            worker.Start();

                            silence = false;
                            onSpeakAudioGenerationStart(wrapper);

                            do
                            {
                                yield return null;
                            } while (worker.IsAlive || !speakToFileProcess.HasExited);

                            if (speakToFileProcess.ExitCode == 0)
                            {
                                yield return playAudioFile(wrapper, Util.Constants.PREFIX_FILE + outputFile, outputFile);
                            }
                            else
                            {
                                using (System.IO.StreamReader sr = speakToFileProcess.StandardError)
                                {
                                    string errorMessage = "Could not speak the text: " + wrapper + System.Environment.NewLine + "Exit code: " + speakToFileProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                                    Debug.LogError(errorMessage);
                                    onErrorInfo(wrapper, errorMessage);
                                }
                            }

                            speakToFileProcess.Dispose();
                        }
                        else
                        {
                            string errorMessage = "Could not find the TTS-wrapper: '" + application + "'";
                            Debug.LogError(errorMessage);
                            onErrorInfo(wrapper, errorMessage);
                        }
                    }
                }
            }
#else
         yield return null;
#endif
        }

        public override IEnumerator Generate(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
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

                    string application = applicationName();

                    if (System.IO.File.Exists(application))
                    {
                        string voiceName = getVoiceName(wrapper);
                        int calculatedRate = calculateRate(wrapper.Rate);
                        int calculatedVolume = calculateVolume(wrapper.Volume);

                        System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                        string outputFile = getOutputFile(wrapper.Uid);

                        string args = "--speakToFile" + " \"" +
                                      prepareText(wrapper) + "\" \"" +
                                      outputFile.Replace('"', '\'') + "\" " +
                                      calculatedRate.ToString() + " " +
                                      calculatedVolume.ToString() + " \"" +
                                      voiceName.Replace('"', '\'') + '"';

                        if (Util.Config.DEBUG)
                            Debug.Log("Process arguments: " + args);

                        speakToFileProcess.StartInfo.FileName = application;
                        speakToFileProcess.StartInfo.Arguments = args;

                        System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(ref speakToFileProcess)) { Name = wrapper.Uid.ToString() };
                        worker.Start();

                        silence = false;
                        onSpeakAudioGenerationStart(wrapper);

                        do
                        {
                            yield return null;
                        } while (worker.IsAlive || !speakToFileProcess.HasExited);

                        if (speakToFileProcess.ExitCode == 0)
                        {
                            processAudioFile(wrapper, outputFile);
                        }
                        else
                        {
                            using (System.IO.StreamReader sr = speakToFileProcess.StandardError)
                            {
                                string errorMessage = "Could not generate the text: " + wrapper + System.Environment.NewLine + "Exit code: " + speakToFileProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                                Debug.LogError(errorMessage);
                                onErrorInfo(wrapper, errorMessage);
                            }
                        }

                        speakToFileProcess.Dispose();
                    }
                    else
                    {
                        string errorMessage = "Could not find the TTS-wrapper: '" + application + "'";
                        Debug.LogError(errorMessage);
                        onErrorInfo(wrapper, errorMessage);
                    }
                }
            }
#else
            yield return null;
#endif
        }

        #endregion


        #region Private methods

        private IEnumerator getVoices()
        {

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            string application = applicationName();

            if (System.IO.File.Exists(application))
            {
                System.Diagnostics.Process voicesProcess = new System.Diagnostics.Process();

                voicesProcess.StartInfo.FileName = application;
                voicesProcess.StartInfo.Arguments = "--voices";

                System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(ref voicesProcess, Util.Constants.DEFAULT_TTS_KILL_TIME));
                worker.Start();

                do
                {
                    yield return null;
                } while (worker.IsAlive || !voicesProcess.HasExited);

                if (voicesProcess.ExitCode == 0)
                {
                    System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(100);

                    using (System.IO.StreamReader streamReader = voicesProcess.StandardOutput)
                    {
                        string reply;
                        while (!streamReader.EndOfStream)
                        {
                            reply = streamReader.ReadLine();

                            if (!string.IsNullOrEmpty(reply))
                            {
                                if (reply.StartsWith(idVoice))
                                {
                                    string[] splittedString = reply.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries);

                                    if (splittedString.Length == 6)
                                    {
                                        voices.Add(new Model.Voice(splittedString[1], splittedString[2], Util.Helper.StringToGender(splittedString[3]), splittedString[4], splittedString[5]));
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Voice is invalid: " + reply);
                                    }
                                    //                     } else if(reply.Equals("@DONE") || reply.Equals("@COMPLETED")) {
                                    //                        complete = true;
                                }
                            }
                        }
                    }

                    cachedVoices = voices.OrderBy(s => s.Name).ToList();

                    if (Util.Constants.DEV_DEBUG)
                        Debug.Log("Voices read: " + cachedVoices.CTDump());

                    //onVoicesReady();
                }
                else
                {
                    using (System.IO.StreamReader sr = voicesProcess.StandardError)
                    {
                        string errorMessage = "Could not get any voices: " + voicesProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                        Debug.LogError(errorMessage);
                        onErrorInfo(null, errorMessage);
                    }
                }

                voicesProcess.Dispose();
            }
            else
            {
                string errorMessage = "Could not find the TTS-wrapper: '" + application + "'";
                Debug.LogError(errorMessage);
                onErrorInfo(null, errorMessage);
            }
#else
            yield return null;
#endif

            onVoicesReady();
        }


#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        private void readSpeakNativeStream(ref System.Diagnostics.Process process, ref string[] speechTextArray, out int wordIndex, out string phoneme, out string viseme, out bool start)
        {
            wordIndex = 0;
            phoneme = string.Empty;
            viseme = string.Empty;
            start = false;

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

            try
            {
                /*
                if (Util.Config.DEBUG)
                {
                    process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(outputDataReceived);
                    process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(errorDataReceived);

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
                else
                {
                    process.Start();
                }
                */

                process.Start();

                string reply;

                using (System.IO.StreamReader streamReader = process.StandardOutput)
                {
                    reply = streamReader.ReadLine();
                    if (reply.Equals(idSpeak))
                    {
                        while (!process.HasExited)
                        {
                            reply = streamReader.ReadLine();

                            if (!string.IsNullOrEmpty(reply))
                            {
                                if (reply.StartsWith(idWord))
                                {
                                    if (wordIndex < speechTextArray.Length)
                                    {
                                        if (speechTextArray[wordIndex].Equals("-"))
                                        {
                                            wordIndex++;
                                        }

                                        wordIndex++;
                                    }
                                    //else
                                    //{
                                    //    Debug.LogWarning("Word index is larger than the speech text word count: " + wordIndex + "/" + speechTextArray.Length);
                                    //}
                                }
                                else if (reply.StartsWith(idPhoneme))
                                {

                                    string[] splittedString = reply.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries);

                                    if (splittedString.Length > 1)
                                    {
                                        phoneme = splittedString[1];
                                    }
                                }
                                else if (reply.StartsWith(idViseme))
                                {

                                    string[] splittedString = reply.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries);

                                    if (splittedString.Length > 1)
                                    {
                                        viseme = splittedString[1];
                                    }
                                }
                                else if (reply.Equals(idStart))
                                {
                                    start = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Unexpected process output: " + reply + System.Environment.NewLine + streamReader.ReadToEnd());
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Could not speak: " + ex);
            }
        }

#endif

        private string applicationName()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                if (Util.Config.ENFORCE_32BIT_WINDOWS)
                {
                    return dataPath + Util.Config.TTS_WINDOWS_EDITOR_x86;
                }
                else
                {
                    return dataPath + Util.Config.TTS_WINDOWS_EDITOR;
                }
            }
            else
            {
                return dataPath + Util.Config.TTS_WINDOWS_BUILD;
            }
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

        /*
                private void outputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
                {
                    Debug.Log(e.Data);
                }

                private void errorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
                {
                    Debug.LogError(e.Data);
                }
        */

        #endregion


        #region Editor-only methods

#if UNITY_EDITOR

        public override void GenerateInEditor(Model.Wrapper wrapper)
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
                    string application = applicationName();

                    if (System.IO.File.Exists(application))
                    {
                        string voiceName = getVoiceName(wrapper);
                        int calculatedRate = calculateRate(wrapper.Rate);
                        int calculatedVolume = calculateVolume(wrapper.Volume);

                        System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                        string outputFile = getOutputFile(wrapper.Uid);

                        string args = "--speakToFile" + " \"" +
                                      prepareText(wrapper) + "\" \"" +
                                      outputFile.Replace('"', '\'') + "\" " +
                                      calculatedRate.ToString() + " " +
                                      calculatedVolume.ToString() + " \"" +
                                      voiceName.Replace('"', '\'') + '"';

                        if (Util.Config.DEBUG)
                            Debug.Log("Process arguments: " + args);

                        speakToFileProcess.StartInfo.FileName = application;
                        speakToFileProcess.StartInfo.Arguments = args;

                        System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(ref speakToFileProcess)) { Name = wrapper.Uid.ToString() };
                        worker.Start();

                        silence = false;
                        onSpeakAudioGenerationStart(wrapper);

                        do
                        {
                            System.Threading.Thread.Sleep(50);
                        } while (worker.IsAlive || !speakToFileProcess.HasExited);

                        if (speakToFileProcess.ExitCode == 0)
                        {
                            processAudioFile(wrapper, outputFile);
                        }
                        else
                        {
                            using (System.IO.StreamReader sr = speakToFileProcess.StandardError)
                            {
                                string errorMessage = "Could not generate the text: " + wrapper + System.Environment.NewLine + "Exit code: " + speakToFileProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                                Debug.LogError(errorMessage);
                                onErrorInfo(wrapper, errorMessage);
                            }
                        }

                        speakToFileProcess.Dispose();
                    }
                    else
                    {
                        string errorMessage = "Could not find the TTS-wrapper: '" + application + "'";
                        Debug.LogError(errorMessage);
                        onErrorInfo(wrapper, errorMessage);
                    }
                }
            }
        }

        public override void SpeakNativeInEditor(Model.Wrapper wrapper)
        {
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
                    string application = applicationName();

                    if (System.IO.File.Exists(application))
                    {
                        string voiceName = getVoiceName(wrapper);
                        int calculatedRate = calculateRate(wrapper.Rate);
                        int calculatedVolume = calculateVolume(wrapper.Volume);

                        System.Diagnostics.Process speakProcess = new System.Diagnostics.Process();

                        string args = "--speak " + '"' +
                                      prepareText(wrapper) + "\" " +
                                      calculatedRate.ToString() + " " +
                                      calculatedVolume.ToString() + " \"" +
                                      voiceName.Replace('"', '\'') + '"';

                        if (Util.Config.DEBUG)
                            Debug.Log("Process arguments: " + args);

                        speakProcess.StartInfo.FileName = application;
                        speakProcess.StartInfo.Arguments = args;

                        System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(ref speakProcess)) { Name = wrapper.Uid.ToString() };
                        worker.Start();

                        silence = false;
                        onSpeakStart(wrapper);

                        do
                        {
                            System.Threading.Thread.Sleep(50);

                            if (silence)
                            {
                                speakProcess.Kill();
                            }
                        } while (worker.IsAlive || !speakProcess.HasExited);

                        if (speakProcess.ExitCode == 0 || speakProcess.ExitCode == -1)
                        { //0 = normal ended, -1 = killed
                            if (Util.Config.DEBUG)
                                Debug.Log("Text spoken: " + wrapper.Text);

                            onSpeakComplete(wrapper);
                        }
                        else
                        {
                            using (System.IO.StreamReader sr = speakProcess.StandardError)
                            {
                                Debug.LogError("Could not speak the text: " + speakProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd());
                            }
                        }
                        
                        speakProcess.Dispose();
                    }
                    else
                    {
                        string errorMessage = "Could not find the TTS-wrapper: '" + application + "'";
                        Debug.LogError(errorMessage);
                        onErrorInfo(wrapper, errorMessage);
                    }
                }
            }
        }

#endif

        private void getVoicesInEditor()
        {

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            string application = applicationName();

            if (System.IO.File.Exists(application))
            {
                System.Diagnostics.Process voicesProcess = new System.Diagnostics.Process();

                voicesProcess.StartInfo.FileName = application;
                voicesProcess.StartInfo.Arguments = "--voices";

                try
                {
                    long time = System.DateTime.Now.Ticks;

                    System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(ref voicesProcess, Util.Constants.DEFAULT_TTS_KILL_TIME));
                    worker.Start();

                    do
                    {
                        System.Threading.Thread.Sleep(50);
                    } while (worker.IsAlive || !voicesProcess.HasExited);

                    if (Util.Constants.DEV_DEBUG)
                        Debug.Log("Finished after: " + ((System.DateTime.Now.Ticks - time)/10000000));

                    if (voicesProcess.ExitCode == 0)
                    {
                        System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>();

                        using (System.IO.StreamReader streamReader = voicesProcess.StandardOutput)
                        {
                            string reply;
                            while (!streamReader.EndOfStream)
                            {
                                reply = streamReader.ReadLine();

                                if (!string.IsNullOrEmpty(reply))
                                {
                                    if (reply.StartsWith(idVoice))
                                    {
                                        string[] splittedString = reply.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries);

                                        if (splittedString.Length == 6)
                                        {
                                            voices.Add(new Model.Voice(splittedString[1], splittedString[2], Util.Helper.StringToGender(splittedString[3]), splittedString[4], splittedString[5]));
                                        }
                                        else
                                        {
                                            Debug.LogWarning("Voice is invalid: " + reply);
                                        }
                                        //                     } else if(reply.Equals("@DONE") || reply.Equals("@COMPLETED")) {
                                        //                        complete = true;
                                    }
                                }
                            }
                        }

                        cachedVoices = voices.OrderBy(s => s.Name).ToList();

                        if (Util.Constants.DEV_DEBUG)
                            Debug.Log("Voices read: " + cachedVoices.CTDump());
                    }
                    else
                    {
                        using (System.IO.StreamReader sr = voicesProcess.StandardError)
                        {
                            string errorMessage = "Could not get any voices: " + voicesProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                            Debug.LogError(errorMessage);
                        }
                    }

                }
                catch (System.Exception ex)
                {
                    string errorMessage = "Could not get any voices!" + System.Environment.NewLine + ex;
                    Debug.LogError(errorMessage);
                }

                voicesProcess.Dispose();
            }
            else
            {
                string errorMessage = "Could not find the TTS-wrapper: '" + application + "'";
                Debug.LogError(errorMessage);
            }
#endif

            onVoicesReady();
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)