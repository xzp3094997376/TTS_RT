﻿using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Tool
{
    /// <summary>Para-language simulator with audio files.</summary>
    //[ExecuteInEditMode]
    [RequireComponent(typeof(AudioSource))]
    [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_paralanguage.html")]
    public class Paralanguage : MonoBehaviour
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

        /// <summary>Audio clips to play.</summary>
        [Tooltip("Audio clips to play.")]
        public AudioClip[] Clips;

        [Header("Optional Settings")]

        /*
        /// <summary>AudioSource for the output (optional).</summary>
        [Tooltip("AudioSource for the output (optional).")]
        public AudioSource Source;
        */

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

        /// <summary>Delay until the speech for this text starts (default: 0).</summary>
        [Tooltip("Delay until the speech for this text starts (default: 0).")]
        public float Delay = 0f;

        private static readonly System.Text.RegularExpressions.Regex splitRegex = new System.Text.RegularExpressions.Regex(@"#.*?#");

        private string uid;

        private bool played = false;

        private System.Collections.Generic.IDictionary<int, string> stack = new System.Collections.Generic.SortedDictionary<int, string>();

        private System.Collections.Generic.IDictionary<string, AudioClip> clipDict = new System.Collections.Generic.Dictionary<string, AudioClip>();

        private AudioSource audioSource;

        private bool next = false;

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

        public void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.Stop(); //always stop the AudioSource at startup
        }

        public void Start()
        {
            // Subscribe event listeners
            Speaker.OnVoicesReady += onVoicesReady;
            Speaker.OnSpeakComplete += onSpeakComplete;

            play();
        }

        public void OnDestroy()
        {
            // Unsubscribe event listeners
            Speaker.OnVoicesReady -= onVoicesReady;
            Speaker.OnSpeakComplete -= onSpeakComplete;
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
        }

        #endregion


        #region Public methods

        /// <summary>Speak the text.</summary>
        public void Speak()
        {
            Silence();
            stack.Clear();
            clipDict.Clear();

            foreach (AudioClip clip in Clips)
            {
                clipDict.Add("#" + clip.name + "#", clip);
            }

            string[] speechParts = splitRegex.Split(Text).Where(s => s != string.Empty).ToArray<string>();

            System.Text.RegularExpressions.MatchCollection mc = splitRegex.Matches(Text);

            int index = 0;

            foreach (System.Text.RegularExpressions.Match match in mc)
            {
                //Debug.Log("MATCH: '" + match + "' - " + Text.IndexOf(match.ToString(), index));
                stack.Add(index = Text.IndexOf(match.ToString(), index), match.ToString());
                index++;
            }

            index = 0;
            foreach (string speech in speechParts)
            {
                //Debug.Log("PART: '" + speech + "' - " + Text.IndexOf(speech, index));
                stack.Add(index = Text.IndexOf(speech, index), speech);
                index++;
            }

            StartCoroutine(processStack());
        }

        /// <summary>Silence the speech.</summary>
        public void Silence()
        {
            StopAllCoroutines();

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

        private IEnumerator processStack()
        {
            AudioClip clip;

            foreach (System.Collections.Generic.KeyValuePair<int, string> kvp in stack)
            {
                if (kvp.Value.StartsWith("#"))
                {
                    clipDict.TryGetValue(kvp.Value, out clip);

                    if (clipDict.TryGetValue(kvp.Value, out clip))
                    {
                        audioSource.clip = clip;
                        audioSource.Play();

                        do
                        {
                            yield return null;
                        } while (audioSource.isPlaying);
                    }
                    else
                    {
                        Debug.LogWarning("Clip not found: " + kvp.Value);
                    }
                }
                else
                {
                    next = false;

                    /*
                    if (Util.Helper.isEditorMode)
                    {
#if UNITY_EDITOR
                        Speaker.SpeakNativeInEditor(kvp.Value, Voices.Voice, Rate, Pitch, Volume);
#endif
                    }
                    else
                    {
                    */
                        if (Mode == Model.Enum.SpeakMode.Speak)
                        {
                            uid = Speaker.Speak(kvp.Value, audioSource, Voices.Voice, true, Rate, Pitch, Volume);
                        }
                        else
                        {
                            uid = Speaker.SpeakNative(kvp.Value, Voices.Voice, Rate, Pitch, Volume);
                        }
                    //}

                    do
                    {
                        yield return null;
                    } while (!next);


                    yield return null;
                }
            }
        }

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

        private void onSpeakComplete(Model.Wrapper wrapper)
        {
            if (wrapper.Uid.Equals(uid))
            {
                next = true;
            }
        }

        #endregion
    }
}
// © 2018-2019 crosstales LLC (https://www.crosstales.com)