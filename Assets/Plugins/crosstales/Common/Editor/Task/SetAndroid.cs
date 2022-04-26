#if UNITY_ANDROID || CT_ENABLED
using UnityEditor;
using UnityEngine;

namespace Crosstales.Common.EditorTask
{
    /// <summary>Sets the required build parameters for Android.</summary>
    [InitializeOnLoad]
    public static class SetAndroid
    {

        #region Constructor

        static SetAndroid()
        {
            if (!PlayerSettings.Android.forceInternetPermission) {
                PlayerSettings.Android.forceInternetPermission = true;

                Debug.Log("Android: 'forceInternetPermission' set to true");
            }
            
            StrippingLevel level = StrippingLevel.Disabled;
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

            //Debug.Log("Stripping level: " + PlayerSettings.strippingLevel + " (" + PlayerSettings.GetScriptingBackend(group) + ")");

            if (PlayerSettings.GetScriptingBackend(group).ToString().CTEquals("Mono2x") && PlayerSettings.strippingLevel != level)
            {
                PlayerSettings.strippingLevel = level;

                Debug.Log("Android: stripping level changed to '" + level + "'");
            }
        }

        #endregion
    }
}
#endif
// © 2017-2019 crosstales LLC (https://www.crosstales.com)