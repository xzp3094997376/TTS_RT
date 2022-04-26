﻿using UnityEngine;

namespace Crosstales.Common.Util
{
    /// <summary>Simple spectrum visualizer.</summary>
    //[HelpURL("https://www.crosstales.com/media/data/assets/radio/api/class_crosstales_1_1_radio_1_1_demo_1_1_util_1_1_spectrum_visualizer.html")] //TODO set URL
    public class SpectrumVisualizer : MonoBehaviour
    {
        #region Variables

        ///<summary>FFT-analyzer with the spectrum data.</summary>
        [Tooltip("FFT-analyzer with the spectrum data.")]
        public FFTAnalyzer Analyzer;

        ///<summary>Prefab for the frequency representation.</summary>
        [Tooltip("Prefab for the frequency representation.")]
        public GameObject VisualPrefab;

        ///<summary>Width per prefab.</summary>
        [Tooltip("Width per prefab.")]
        public float Width = 0.075f;

        ///<summary>Gain-power for the frequency.</summary>
        [Tooltip("Gain-power for the frequency.")]
        public float Gain = 70f;

        ///<summary>Frequency band from left-to-right (default: true).</summary>
        [Tooltip("Frequency band from left-to-right (default: true).")]
        public bool LeftToRight = true;

        ///<summary>Opacity of the material of the prefab (default: 1).</summary>
        [Tooltip("Opacity of the material of the prefab (default: 1).")]
        [Range(0f, 1f)]
        public float Opacity = 1f;

        private Transform tf;
        private Transform[] visualTransforms;

        private Vector3 visualPos = Vector3.zero;

        private int samplesPerChannel;

        #endregion


        #region MonoBehaviour methods

        public void Start()
        {
            tf = transform;
            samplesPerChannel = Analyzer.Samples.Length / 2;
            visualTransforms = new Transform[samplesPerChannel];

            GameObject tempCube;

            for (int ii = 0; ii < samplesPerChannel; ii++)
            { //cut the upper frequencies >11000Hz
                if (LeftToRight)
                {
                    tempCube = (GameObject)Instantiate(VisualPrefab, new Vector3(tf.position.x + (ii * Width), tf.position.y, tf.position.z), Quaternion.identity);
                }
                else
                {
                    tempCube = (GameObject)Instantiate(VisualPrefab, new Vector3(tf.position.x - (ii * Width), tf.position.y, tf.position.z), Quaternion.identity);
                }

                tempCube.GetComponent<Renderer>().material.color = Common.Util.BaseHelper.HSVToRGB((360f / samplesPerChannel) * ii, 1f, 1f, Opacity);

                visualTransforms[ii] = tempCube.GetComponent<Transform>();
                visualTransforms[ii].parent = tf;
            }
        }

        public void Update()
        {
            for (int ii = 0; ii < visualTransforms.Length; ii++)
            {
                visualPos.Set(Width, Analyzer.Samples[ii] * Gain, Width);
                visualTransforms[ii].localScale = visualPos;
            }
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)