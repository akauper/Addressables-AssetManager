using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class Il2cppCustomFlag : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
#if UNITY_IOS || UNITY_ANDROID
        ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        if (backend == ScriptingImplementation.IL2CPP)
        {
            PlayerSettings.SetAdditionalIl2CppArgs("--maximum-recursive-generic-depth=50");
        }
#endif
    }
}
