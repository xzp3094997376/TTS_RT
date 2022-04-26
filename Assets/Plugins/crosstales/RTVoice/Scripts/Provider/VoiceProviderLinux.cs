using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
    /// <summary>
    /// Linux voice provider.
    /// Note: needs eSpeak to work: => http://espeak.sourceforge.net/
    /// </summary>
    public class VoiceProviderLinux : BaseVoiceProvider
    {

        #region Variables

        private const int defaultRate = 160;
        private const int defaultVolume = 100;
        private const int defaultPitch = 50;

        private System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(100);

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor for VoiceProviderLinux.
        /// </summary>
        /// <param name="obj">Instance of the speaker</param>
        public VoiceProviderLinux(MonoBehaviour obj) : base(obj)
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
                return "en";
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
                return true; //TODO is this setting ok?
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
#if UNITY_STANDALONE || UNITY_EDITOR
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
                    int calculatedVolume = calculateVolume(wrapper.Volume);
                    int calculatedPitch = calculatePitch(wrapper.Pitch);

                    System.Diagnostics.Process speakProcess = new System.Diagnostics.Process();

                    string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : ("-v \"" + voiceName.Replace('"', '\'') + '"')) +
                                  (calculatedRate != defaultRate ? (" -s " + calculatedRate + " ") : string.Empty) +
                                  (calculatedVolume != defaultVolume ? (" -a " + calculatedVolume + " ") : string.Empty) +
                                  (calculatedPitch != defaultPitch ? (" -p " + calculatedPitch + " ") : string.Empty) +
                                  " -m \"" +
                                  wrapper.Text.Replace('"', '\'') + '"' +
                                  (string.IsNullOrEmpty(Util.Config.TTS_LINUX_DATA) ? string.Empty : " --path=\"" + Util.Config.TTS_LINUX_DATA + '"');

                    if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);

                    speakProcess.StartInfo.FileName = Util.Config.TTS_LINUX;
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
#if UNITY_STANDALONE || UNITY_EDITOR
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
                        int calculatedVolume = calculateVolume(wrapper.Volume);
                        int calculatedPitch = calculatePitch(wrapper.Pitch);
                        string outputFile = getOutputFile(wrapper.Uid);

                        System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                        string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : ("-v \"" + voiceName.Replace('"', '\'') + '"')) +
                                      (calculatedRate != defaultRate ? (" -s " + calculatedRate + " ") : string.Empty) +
                                      (calculatedVolume != defaultVolume ? (" -a " + calculatedVolume + " ") : string.Empty) +
                                      (calculatedPitch != defaultPitch ? (" -p " + calculatedPitch + " ") : string.Empty) +
                                      " -w \"" + outputFile.Replace('"', '\'') + '"' +
                                      " -m \"" +
                                      wrapper.Text.Replace('"', '\'') + '"' +
                                      (string.IsNullOrEmpty(Util.Config.TTS_LINUX_DATA) ? string.Empty : " --path=\"" + Util.Config.TTS_LINUX_DATA + '"');

                        if (Util.Config.DEBUG)
                            Debug.Log("Process arguments: " + args);

                        speakToFileProcess.StartInfo.FileName = Util.Config.TTS_LINUX;
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
                }
            }
#else
            yield return null;
#endif
        }

        public override IEnumerator Generate(Model.Wrapper wrapper)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
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
                    int calculatedVolume = calculateVolume(wrapper.Volume);
                    int calculatedPitch = calculatePitch(wrapper.Pitch);
                    string outputFile = getOutputFile(wrapper.Uid);

                    System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                    string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : ("-v \"" + voiceName.Replace('"', '\'') + '"')) +
                                  (calculatedRate != defaultRate ? (" -s " + calculatedRate + " ") : string.Empty) +
                                  (calculatedVolume != defaultVolume ? (" -a " + calculatedVolume + " ") : string.Empty) +
                                  (calculatedPitch != defaultPitch ? (" -p " + calculatedPitch + " ") : string.Empty) +
                                  " -w \"" + outputFile.Replace('"', '\'') + '"' +
                                  " -m \"" +
                                  wrapper.Text.Replace('"', '\'') + '"' +
                                  (string.IsNullOrEmpty(Util.Config.TTS_LINUX_DATA) ? string.Empty : " --path=\"" + Util.Config.TTS_LINUX_DATA + '"');

                    if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);

                    speakToFileProcess.StartInfo.FileName = Util.Config.TTS_LINUX;
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

        protected override string getVoiceName(Model.Wrapper wrapper)
        {
            if (wrapper == null || wrapper.Voice == null || string.IsNullOrEmpty(wrapper.Voice.Name))
            {
                if (Util.Config.DEBUG)
                    Debug.LogWarning("'wrapper.Voice' or 'wrapper.Voice.Name' is null! Using the OS 'default' voice.");

                return DefaultVoiceName;
            }
            else
            {
                if (Speaker.ESpeakMod == Model.Enum.ESpeakModifiers.none)
                {
                    if (wrapper.Voice.Gender == Model.Enum.Gender.FEMALE)
                    {
                        return wrapper.Voice.Name + Util.Constants.ESPEAK_FEMALE_MODIFIER;
                    }

                    return wrapper.Voice.Name;
                }

                return wrapper.Voice.Name + "+" + Speaker.ESpeakMod.ToString();
            }


        }

        private IEnumerator getVoices()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            voices.Clear();

            System.Diagnostics.Process voicesProcess = new System.Diagnostics.Process();

            voicesProcess.StartInfo.FileName = Util.Config.TTS_LINUX;
            voicesProcess.StartInfo.Arguments = "--voices" + (string.IsNullOrEmpty(Util.Config.TTS_LINUX_DATA) ? string.Empty : " --path=\"" + Util.Config.TTS_LINUX_DATA + '"');

            voicesProcess.StartInfo.CreateNoWindow = true;
            voicesProcess.StartInfo.RedirectStandardOutput = true;
            voicesProcess.StartInfo.RedirectStandardError = true;
            voicesProcess.StartInfo.UseShellExecute = false;
            voicesProcess.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            voicesProcess.OutputDataReceived += process_OutputDataReceived;

            voicesProcess.Start();
            voicesProcess.BeginOutputReadLine();

            do
            {
                yield return null;
            } while (!voicesProcess.HasExited);

            if (voicesProcess.ExitCode == 0)
            {
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

        private void process_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            //Debug.Log(e.Data);

            string reply = e.Data;

            if (!string.IsNullOrEmpty(reply))
            {
                if (!reply.StartsWith("Pty")) //ignore header
                {
                    if (Util.Config.TTS_LINUX.CTContains("espeak-ng"))
                    {
                        voices.Add(new Model.Voice(reply.Substring(30, 19).Trim().Replace("_", " "), reply.Substring(50).Trim(), Util.Helper.StringToGender(reply.Substring(23, 1)), "unknown", reply.Substring(4, 15).Trim(), "", "espeak-ng"));
                    }
                    else
                    {
                        voices.Add(new Model.Voice(reply.Substring(22, 20).Trim(), reply.Substring(43).Trim(), Util.Helper.StringToGender(reply.Substring(19, 1)), "unknown", reply.Substring(4, 15).Trim(), "", "espeak"));
                    }
                }
            }
        }

        private static int calculateRate(float rate)
        {
            int result = Mathf.Clamp(rate != 1f ? (int)(defaultRate * rate) : defaultRate, 1, 3 * defaultRate);

            if (Util.Constants.DEV_DEBUG)
                Debug.Log("calculateRate: " + result + " - " + rate);

            return result;
        }

        private static int calculateVolume(float volume)
        {
            return Mathf.Clamp((int)(defaultVolume * volume), 0, 200);
        }

        private static int calculatePitch(float pitch)
        {
            return Mathf.Clamp((int)(defaultPitch * pitch), 0, 99);
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
                    int calculatedVolume = calculateVolume(wrapper.Volume);
                    int calculatedPitch = calculatePitch(wrapper.Pitch);
                    string outputFile = getOutputFile(wrapper.Uid);

                    System.Diagnostics.Process speakToFileProcess = new System.Diagnostics.Process();

                    string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : ("-v \"" + voiceName.Replace('"', '\'') + '"')) +
                                  (calculatedRate != defaultRate ? (" -s " + calculatedRate + " ") : string.Empty) +
                                  (calculatedVolume != defaultVolume ? (" -a " + calculatedVolume + " ") : string.Empty) +
                                  (calculatedPitch != defaultPitch ? (" -p " + calculatedPitch + " ") : string.Empty) +
                                  " -w \"" + outputFile.Replace('"', '\'') + '"' +
                                  " -m \"" +
                                  wrapper.Text.Replace('"', '\'') + '"' +
                                  (string.IsNullOrEmpty(Util.Config.TTS_LINUX_DATA) ? string.Empty : " --path=\"" + Util.Config.TTS_LINUX_DATA + '"');

                    if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);

                    speakToFileProcess.StartInfo.FileName = Util.Config.TTS_LINUX;
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
                        if (Util.Config.DEBUG)
                            Debug.Log("Text generated: " + wrapper.Text);

                        copyAudioFile(wrapper, outputFile);
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

                    onSpeakAudioGenerationComplete(wrapper);

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
                    int calculatedVolume = calculateVolume(wrapper.Volume);
                    int calculatedPitch = calculatePitch(wrapper.Pitch);

                    System.Diagnostics.Process speakProcess = new System.Diagnostics.Process();

                    string args = (string.IsNullOrEmpty(voiceName) ? string.Empty : ("-v \"" + voiceName.Replace('"', '\'') + '"')) +
                                  (calculatedRate != defaultRate ? (" -s " + calculatedRate + " ") : string.Empty) +
                                  (calculatedVolume != defaultVolume ? (" -a " + calculatedVolume + " ") : string.Empty) +
                                  (calculatedPitch != defaultPitch ? (" -p " + calculatedPitch + " ") : string.Empty) +
                                  " -m \"" +
                                  wrapper.Text.Replace('"', '\'') + '"' +
                                  (string.IsNullOrEmpty(Util.Config.TTS_LINUX_DATA) ? string.Empty : " --path=\"" + Util.Config.TTS_LINUX_DATA + '"');

                    if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);

                    speakProcess.StartInfo.FileName = Util.Config.TTS_LINUX;
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
#if UNITY_STANDALONE || UNITY_EDITOR
            cachedVoices.Clear();

            System.Diagnostics.Process voicesProcess = new System.Diagnostics.Process();

            voicesProcess.StartInfo.FileName = Util.Config.TTS_LINUX;
            voicesProcess.StartInfo.Arguments = "--voices" + (string.IsNullOrEmpty(Util.Config.TTS_LINUX_DATA) ? string.Empty : " --path=\"" + Util.Config.TTS_LINUX_DATA + '"');

            voicesProcess.StartInfo.CreateNoWindow = true;
            voicesProcess.StartInfo.RedirectStandardOutput = true;
            voicesProcess.StartInfo.RedirectStandardError = true;
            voicesProcess.StartInfo.UseShellExecute = false;
            voicesProcess.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            voicesProcess.OutputDataReceived += process_OutputDataReceived;

            try
            {
                long time = System.DateTime.Now.Ticks;

                voicesProcess.Start();
                voicesProcess.BeginOutputReadLine();

                do
                {
                    System.Threading.Thread.Sleep(50);
                } while (!voicesProcess.HasExited);

                if (Util.Constants.DEV_DEBUG)
                    Debug.Log("Finished after: " + ((System.DateTime.Now.Ticks - time) / 10000000));

                if (voicesProcess.ExitCode == 0)
                {
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
// © 2018-2019 crosstales LLC (https://www.crosstales.com)