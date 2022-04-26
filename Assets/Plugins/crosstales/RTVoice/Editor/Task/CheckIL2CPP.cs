using UnityEditor;
using UnityEngine;

namespace Crosstales.RTVoice.EditorTask
{
    /// <summary>Checks if IL2CPP is enabled under standalone.</summary>
    [InitializeOnLoad]
    public static class CheckIL2CPP
    {

        #region Constructor

        static CheckIL2CPP()
        {
#if UNITY_STANDALONE
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

            if (PlayerSettings.GetScriptingBackend(group) == ScriptingImplementation.IL2CPP)
            {
                Debug.LogWarning(Util.Constants.ASSET_NAME + ": IL2CPP is currently not supported for standalone builds! Please use MaryTTS or a custom provider (e.g. AWS Polly).");
            }
#endif
        }

        #endregion
    }
}
// © 2019 crosstales LLC (https://www.crosstales.com)