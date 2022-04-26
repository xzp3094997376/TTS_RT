using UnityEngine;

namespace Crosstales.RTVoice.Tool
{
    /// <summary>Allows to speak and store generated audio.</summary>
    [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_speech_text.html")]
    public class SpeechText : MonoBehaviour
    {

        #region Variables

        /// <summary>Text to speak.</summary>
        [Tooltip("Text to speak.")]
        [Multiline]
        public string Text = string.Empty;

        /// <summary>Voices for the speech.</summary>
        [Tooltip("Voices for the speech.")]
        public Model.VoiceAlias Voices;

        /// <summary>Speak mode (default: 'Speak').</summary>
        [Tooltip("Speak mode (default: 'Speak').")]
        public Model.Enum.SpeakMode Mode = Model.Enum.SpeakMode.Speak;


        [Header("Optional Settings")]

        /// <summary>AudioSource for the output (optional).</summary>
        [Tooltip("AudioSource for the output (optional).")]
        public AudioSource Source;

        /// <summary>Speech rate of the speaker in percent (1 = 100%, default: 1, optional).</summary>
        [Tooltip("Speech rate of the speaker in percent (1 = 100%, default: 1, optional).")]
        [Range(0f, 3f)]
        public float Rate = 1f;

        /// <summary>Speech pitch of the speaker in percent (1 = 100%, default: 1, optional, mobile only).</summary>
        [Tooltip("Speech pitch of the speaker in percent (1 = 100%, default: 1, optional, mobile only).")]
        [Range(0f, 2f)]
        public float Pitch = 1f;

        /// <summary>Volume of the speaker in percent (1 = 100%, default: 1, optional, Windows only).</summary>
        [Tooltip("Volume of the speaker in percent (1 = 100%, default: 1, optional, Windows only).")]
        [Range(0f, 1f)]
        public float Volume = 1f;


        [Header("Behaviour Settings")]

        /// <summary>Enable speaking of the text on start (default: false).</summary>
        [Tooltip("Enable speaking of the text on start (default: false).")]
        public bool PlayOnStart = false;

        /// <summary>Delay in seconds until the speech for this text starts (default: 0).</summary>
        [Tooltip("Delay in seconds until the speech for this text starts (default: 0).")]
        public float Delay = 0f;


        [Header("Output File Settings")]

        /// <summary>Generate audio file on/off (default: false).</summary>
        [Tooltip("Generate audio file on/off (default: false).")]
        public bool GenerateAudioFile = false;

        /// <summary>File path for the generated audio.</summary>
        [Tooltip("File path for the generated audio.")]
        public string FilePath = @"_generatedAudio/";

        /// <summary>File name of the generated audio.</summary>
        [Tooltip("File name of the generated audio.")]
        public string FileName = "Speech01";

        /// <summary>Is the generated file path inside the Assets-folder (current project)? If this option is enabled, it prefixes the path with 'Application.dataPath'.</summary>
        [Tooltip("Is the generated file path inside the Assets-folder (current project)? If this option is enabled, it prefixes the path with 'Application.dataPath'.")]
        public bool FileInsideAssets = true;

        private string uid;

        private bool played = false;

        //private long lastPlaytime = long.MinValue;
		private float lastSpeaktime = float.MinValue;

        #endregion


        #region Properties

        /// <summary>Text to speak (main use is for UI).</summary>
        public string CurrentText
        {
            get
            {
                return Text;
            }

            set
            {
                Text = value;
            }
        }

        /// <summary>Speech rate of the speaker in percent (main use is for UI).</summary>
        public float CurrentRate
        {
            get
            {
                return Rate;
            }

            set
            {
                Rate = value;
            }
        }

        /// <summary>Speech pitch of the speaker in percent (main use is for UI).</summary>
        public float CurrentPitch
        {
            get
            {
                return Pitch;
            }

            set
            {
                Pitch = value;
            }
        }

        /// <summary>Volume of the speaker in percent (main use is for UI).</summary>
        public float CurrentVolume
        {
            get
            {
                return Volume;
            }

            set
            {
                Volume = value;
            }
        }

        #endregion


        #region MonoBehaviour methods

        public void Start()
        {
            // Subscribe event listeners
            Speaker.OnVoicesReady += onVoicesReady;

            play();
        }

        public void OnDestroy()
        {
            // Unsubscribe event listeners
            Speaker.OnVoicesReady -= onVoicesReady;
        }

        public void OnValidate()
        {
            if (Delay < 0f)
                Delay = 0f;

            if (Rate < 0f)
                Rate = 0f;

            if (Rate > 3f)
                Rate = 3f;

            if (Pitch < 0f)
                Pitch = 0f;

            if (Pitch > 2f)
                Pitch = 2f;

            if (Volume < 0f)
                Volume = 0f;

            if (Volume > 1f)
                Volume = 1f;

            if (!string.IsNullOrEmpty(FilePath))
            {
                FilePath = Util.Helper.ValidatePath(FilePath);
            }
        }

        #endregion


        #region Public methods

        /// <summary>Speak the text.</summary>
        public void Speak()
        {
			//long currentTime = System.DateTime.Now.Ticks;
			float currentTime = Time.realtimeSinceStartup;

			if (lastSpeaktime + Util.Constants.SPEAK_CALL_SPEED < currentTime)
			{
				lastSpeaktime = currentTime;

                Silence();

                string path = null;

                if (GenerateAudioFile && !string.IsNullOrEmpty(FilePath))
                {
                    if (FileInsideAssets)
                    {
                        path = Util.Helper.ValidatePath(Application.dataPath + @"/" + FilePath);
                    }
                    else
                    {
                        path = Util.Helper.ValidatePath(FilePath);
                    }

                    //                if (!System.IO.Directory.Exists(path))
                    //                {
                    //                    System.IO.Directory.CreateDirectory(path);
                    //                }

                    path += FileName;
                }

                if (Util.Helper.isEditorMode)
                {
#if UNITY_EDITOR
                    Speaker.SpeakNative(Text, Voices.Voice, Rate, Pitch, Volume);
                    if (GenerateAudioFile)
                    {
                        Speaker.Generate(Text, path, Voices.Voice, Rate, Pitch, Volume);
                    }
#endif
                }
                else
                {
                    if (Mode == Model.Enum.SpeakMode.Speak)
                    {
                        uid = Speaker.Speak(Text, Source, Voices.Voice, true, Rate, Pitch, Volume, path);
                    }
                    else
                    {
                        uid = Speaker.SpeakNative(Text, Voices.Voice, Rate, Pitch, Volume);
                    }
                }

            }
            else
            {
                Debug.LogWarning("'Speak' called too fast - please slow down!");
            }
        }

        /// <summary>Silence the speech.</summary>
        public void Silence()
        {
            if (Util.Helper.isEditorMode)
            {
                Speaker.Silence();
            }
            else
            {
                Speaker.Silence(uid);
            }
        }

        #endregion


        #region Private methods

        private void play()
        {
            if (PlayOnStart && !played && Speaker.Voices.Count > 0)
            {
                played = true;

                Invoke("Speak", Delay);
            }
        }

        #endregion


        #region Callbacks

        private void onVoicesReady()
        {
            play();
        }

        #endregion
    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)