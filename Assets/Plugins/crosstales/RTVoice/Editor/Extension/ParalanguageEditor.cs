using UnityEngine;
using UnityEditor;
using Crosstales.RTVoice.EditorUtil;

namespace Crosstales.RTVoice.EditorExtension
{
    /// <summary>Custom editor for the 'Paralanguage'-class.</summary>
    [CustomEditor(typeof(Tool.Paralanguage))]
    [CanEditMultipleObjects]
    public class ParalanguageEditor : Editor
    {

        #region Variables

        private Tool.Paralanguage script;

        #endregion


        #region Editor methods

        public void OnEnable()
        {
            script = (Tool.Paralanguage)target;
        }

        public void OnDisable()
        {
            if (Util.Helper.isEditorMode)
            {
                Speaker.Silence();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            //EditorHelper.SeparatorUI();

            if (script.isActiveAndEnabled)
            {
                if (!string.IsNullOrEmpty(script.Text))
                {
                    if (Speaker.isTTSAvailable && EditorHelper.isRTVoiceInScene)
                    {

/*
                        GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

                        if (Util.Helper.isEditorMode)
                        {
                            if (Speaker.isWorkingInEditor)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button(new GUIContent(" Speak", EditorHelper.Icon_Speak, "Speaks the text with the selected voice and settings.")))
                                    {
                                        script.Speak();
                                    }

                                    GUI.enabled = Speaker.isSpeaking;
                                    if (GUILayout.Button(new GUIContent(" Silence", EditorHelper.Icon_Silence, "Silence the active speaker.")))
                                    {
                                        script.Silence();
                                    }
                                    GUI.enabled = true;
                                }
                                GUILayout.EndHorizontal();
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("Test-Drive is not supported for current TTS-system inside the Unity Editor.", MessageType.Info);
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
                        }
*/

                    }
                    else
                    {
                        EditorHelper.NoVoicesUI();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Please enter a 'Text'!", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
            }
        }

        #endregion


        #region Private methods

        private void refreshAssetDatabase()
        {
            if (Util.Helper.isEditorMode)
            {
                AssetDatabase.Refresh();
            }
        }

        #endregion
    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)