using UnityEngine;
using UnityEditor;
using System.Linq;

#if UNITY_EDITOR
public class AnimationPoseExtractor : EditorWindow
{
    private AnimationClip sourceAnimation;
    private string newClipName = "ANIM_";
    private float timeToExtract = 1.0f;
    private bool stripRootMotion = true; // Added option to strip root motion

    [MenuItem("Window/Animation/Create Idle From Fire Animation")]
    public static void ShowWindow()
    {
        GetWindow<AnimationPoseExtractor>("Create Idle Animation");
    }

    private void OnGUI()
    {
        sourceAnimation = (AnimationClip)EditorGUILayout.ObjectField("Source Animation", sourceAnimation, typeof(AnimationClip), false);

        if (sourceAnimation != null)
        {
            timeToExtract = EditorGUILayout.Slider("Time to Extract", timeToExtract, 0, sourceAnimation.length);
        }

        newClipName = EditorGUILayout.TextField("New Clip Name", newClipName);
        stripRootMotion = EditorGUILayout.Toggle("Strip Root Motion", stripRootMotion);

        if (GUILayout.Button("Create Idle Animation") && sourceAnimation != null)
        {
            CreateIdleFromPose();
        }
    }

    private void CreateIdleFromPose()
    {
        AnimationClip newClip = new AnimationClip();
        newClip.legacy = sourceAnimation.legacy;

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceAnimation);

        foreach (var binding in bindings)
        {
            // Skip root motion curves if stripRootMotion is enabled
            if (stripRootMotion)
            {
                // Skip position curves on the root
                if (binding.path == "" && (
                    binding.propertyName.StartsWith("m_LocalPosition") ||
                    binding.propertyName.StartsWith("RootT")))
                {
                    continue;
                }

                // Skip root rotation if it's a quaternion or euler rotation
                if (binding.path == "" && (
                    binding.propertyName.StartsWith("m_LocalRotation") ||
                    binding.propertyName.StartsWith("m_LocalEulerAngles") ||
                    binding.propertyName.StartsWith("RootQ")))
                {
                    continue;
                }
            }

            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceAnimation, binding);

            // Evaluate the curve at our desired time
            float value = curve.Evaluate(timeToExtract);

            // Create a new constant curve
            var newCurve = new AnimationCurve(
                new Keyframe(0, value),
                new Keyframe(1 / 60f, value)  // Add a second keyframe to ensure proper looping
            );

            // Set the curve in the new clip
            AnimationUtility.SetEditorCurve(newClip, binding, newCurve);
        }

        // Set up proper looping
        var settings = AnimationUtility.GetAnimationClipSettings(newClip);
        settings.loopTime = true;
        settings.loopBlend = false;
        settings.cycleOffset = 0;
        AnimationUtility.SetAnimationClipSettings(newClip, settings);

        // Save the new clip
        string path = AssetDatabase.GetAssetPath(sourceAnimation);
        path = System.IO.Path.GetDirectoryName(path);
        path = $"{path}/{newClipName}.anim";

        AssetDatabase.CreateAsset(newClip, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created new idle animation: {path}");
    }
}
#endif