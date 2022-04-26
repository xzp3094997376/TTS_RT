using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
    /// <summary>MacOS voice provider.</summary>
    public class VoiceProviderMacOS : BaseVoiceProvider
    {

        #region Variables

#if UNITY_STANDALONE_OSX || UNITY_EDITOR
        private static readonly System.Text.RegularExpressions.Regex sayRegex = new System.Text.RegularExpressions.Regex(@"^([^#]+?)\s*([^ ]+)\s*# (.*?)$");
#endif

        private const int defaultRate = 175;

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for VoiceProviderMacOS.
        /// </summary>
        /// <param name="obj">Instance of the speaker</param>
        public VoiceProviderMacOS(MonoBehaviour obj) : base(obj)
        {
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
                return ".aiff";
            }
        }

        public override AudioType AudioFileType
        {
            get
            {
                return AudioType.AIFF;
            }
        }

        public override string DefaultVoiceName
        {
            get
            {
                return "Alex";
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
                return 256000;
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
                return Util.Helper.isMacOSPlatform || Util.Helper.isEditorMode;
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
#if UNITY_STANDALONE_OSX || UNITY_EDITOR
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

                    string voiceName = getVoiceName(wrapper);
                    int calculatedRate = calculateRate(wrapper.Rate);

                    System.Diagnostics.Process speakProcess = new System.Diagnostics.Process();

                    string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : (" -v \"" + voiceName.Replace('"', '\'') + '"')) +
                                  (calculatedRate != defaultRate ? (" -r " + calculatedRate) : string.Empty) + " \"" +
                                  wrapper.Text.Replace('"', '\'') + '"';

                    if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);

                    speakProcess.StartInfo.FileName = Util.Config.TTS_MACOS;
                    speakProcess.StartInfo.Arguments = args;

                    System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(ref speakProcess)) { Name = wrapper.Uid.ToString() };
                    worker.Start();

                    silence = false;

                    processes.Add(wrapper.Uid, speakProcess);
                    onSpeakStart(wrapper);

                    do
                    {
                        yield return null;

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
                            string errorMessage = "Could not speak the text: " + wrapper + System.Environment.NewLine + "Exit code: " + speakProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                            Debug.LogError(errorMessage);
                            onErrorInfo(wrapper, errorMessage);
                        }
                    }

                    processes.Remove(wrapper.Uid);
                    speakProcess.Dispose();
                }
            }
#else
            yield return null;
#endif
        }

        public override IEnumerator Speak(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR
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

                        string voiceName = getVoiceName(wrapper);
                        int calculatedRate = calculateRate(wrapper.Rate);
                        string outputFile = getOutputFile(wrapper.Uid);

                        System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                        string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : (" -v \"" + voiceName.Replace('"', '\'') + '"')) +
                                      (calculatedRate != defaultRate ? (" -r " + calculatedRate) : string.Empty) + " -o \"" +
                                      outputFile.Replace('"', '\'') + '"' +
                                      " --file-format=AIFFLE" + " \"" +
                                      wrapper.Text.Replace('"', '\'') + '"';

                        if (Util.Config.DEBUG)
                            Debug.Log("Process arguments: " + args);

                        speakToFileProcess.StartInfo.FileName = Util.Config.TTS_MACOS;
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
                            yield return playAudioFile(wrapper, Util.Constants.PREFIX_FILE + outputFile, outputFile, AudioFileType);
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
                }
            }
#else
            yield return null;
#endif
        }

        public override IEnumerator Generate(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR
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

                    string voiceName = getVoiceName(wrapper);
                    int calculatedRate = calculateRate(wrapper.Rate);
                    string outputFile = getOutputFile(wrapper.Uid);

                    System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                    string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : (" -v \"" + voiceName.Replace('"', '\'') + '"')) +
                                  (calculatedRate != defaultRate ? (" -r " + calculatedRate) : string.Empty) + " -o \"" +
                                  outputFile.Replace('"', '\'') + '"' +
                                  " --file-format=AIFFLE" + " \"" +
                                  wrapper.Text.Replace('"', '\'') + '"';

                    if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);

                    speakToFileProcess.StartInfo.FileName = Util.Config.TTS_MACOS;
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
            }
#else
            yield return null;
#endif
        }

        #endregion


        #region Private methods

        private IEnumerator getVoices()
        {

#if UNITY_STANDALONE_OSX || UNITY_EDITOR
            System.Diagnostics.Process voicesProcess = new System.Diagnostics.Process();

            voicesProcess.StartInfo.FileName = Util.Config.TTS_MACOS;
            voicesProcess.StartInfo.Arguments = "-v '?'";

            voicesProcess.Start();

            System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(ref voicesProcess, Util.Constants.DEFAULT_TTS_KILL_TIME));
            worker.Start();

            do
            {
                yield return null;
            } while (worker.IsAlive || !voicesProcess.HasExited);

            if (voicesProcess.ExitCode == 0)
            {
                System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(60);

                using (System.IO.StreamReader streamReader = voicesProcess.StandardOutput)
                {
                    string reply;
                    string name;

                    while (!streamReader.EndOfStream)
                    {
                        reply = streamReader.ReadLine();

                        if (!string.IsNullOrEmpty(reply))
                        {
                            System.Text.RegularExpressions.Match match = sayRegex.Match(reply);

                            if (match.Success)
                            {
                                name = match.Groups[1].ToString();
                                voices.Add(new Model.Voice(name, match.Groups[3].ToString(), Util.Helper.AppleVoiceNameToGender(name), "unknown", match.Groups[2].ToString().Replace('_', '-'), string.Empty, "Apple"));
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
                    onErrorInfo(null, errorMessage);
                }
            }

            voicesProcess.Dispose();
#else
            yield return null;
#endif

            onVoicesReady();
        }

        private static int calculateRate(float rate)
        {
            int result = Mathf.Clamp(rate != 1f ? (int)(defaultRate * rate) : defaultRate, 1, 3 * defaultRate);

            if (Util.Constants.DEV_DEBUG)
                Debug.Log("calculateRate: " + result + " - " + rate);

            return result;
        }

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
                    string voiceName = getVoiceName(wrapper);
                    int calculatedRate = calculateRate(wrapper.Rate);
                    string outputFile = getOutputFile(wrapper.Uid);

                    System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                    string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : (" -v \"" + voiceName.Replace('"', '\'') + '"')) +
                                  (calculatedRate != defaultRate ? (" -r " + calculatedRate) : string.Empty) + " -o \"" +
                                  outputFile.Replace('"', '\'') + '"' +
                                  " --file-format=AIFFLE" + " \"" +
                                  wrapper.Text.Replace('"', '\'') + '"';

                    if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);

                    speakToFileProcess.StartInfo.FileName = Util.Config.TTS_MACOS;
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
                    Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
                }
                else
                {
                    string voiceName = getVoiceName(wrapper);
                    int calculatedRate = calculateRate(wrapper.Rate);

                    System.Diagnostics.Process speakProcess = new System.Diagnostics.Process();

                    string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : (" -v \"" + voiceName.Replace('"', '\'') + '"')) +
                                  (calculatedRate != defaultRate ? (" -r " + calculatedRate) : string.Empty) + " \"" +
                                  wrapper.Text.Replace('"', '\'') + '"';

                    if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);

                    speakProcess.StartInfo.FileName = Util.Config.TTS_MACOS;
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
                            string errorMessage = "Could not speak the text: " + wrapper + System.Environment.NewLine + "Exit code: " + speakProcess.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                            Debug.LogError(errorMessage);
                            onErrorInfo(wrapper, errorMessage);
                        }
                    }

                    speakProcess.Dispose();
                }
            }
        }

#endif
        private void getVoicesInEditor()
        {

#if UNITY_STANDALONE_OSX || UNITY_EDITOR
            System.Diagnostics.Process voicesProcess = new System.Diagnostics.Process();

            voicesProcess.StartInfo.FileName = Util.Config.TTS_MACOS;
            voicesProcess.StartInfo.Arguments = "-v '?'";

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
                    System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(100);

                    using (System.IO.StreamReader streamReader = voicesProcess.StandardOutput)
                    {
                        string reply;
                        string name;

                        while (!streamReader.EndOfStream)
                        {
                            reply = streamReader.ReadLine();

                            if (!string.IsNullOrEmpty(reply))
                            {
                                System.Text.RegularExpressions.Match match = sayRegex.Match(reply);

                                if (match.Success)
                                {
                                    name = match.Groups[1].ToString();
                                    voices.Add(new Model.Voice(match.Groups[1].ToString(), match.Groups[3].ToString(), Util.Helper.AppleVoiceNameToGender(name), "unknown", match.Groups[2].ToString().Replace('_', '-')));
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
#endif
            onVoicesReady();
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)