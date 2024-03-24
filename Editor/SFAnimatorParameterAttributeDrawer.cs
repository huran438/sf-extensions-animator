using System.Collections.Generic;
using _Client_.AnimatorExtensions.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace _Client_.AnimatorExtensions.Editor
{
    [CustomPropertyDrawer(typeof(SFAnimatorParameterAttribute))]
    public class SFAnimatorParameterAttributeDrawer : PropertyDrawer
    {
        private const string InvalidAnimatorControllerWarningMessage = "Target animator controller is null";
        private const string InvalidTypeWarningMessage = "{0} must be an int or a string";

        private T GetAttribute<T>() where T : class
        {
            return attribute as T;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);


            var animatorParamAttribute = GetAttribute<SFAnimatorParameterAttribute>();
            var animatorControllerParameters = GetAnimatorController(property, animatorParamAttribute.AnimatorName);

            if (animatorControllerParameters == null)
            {
                EditorGUI.HelpBox(rect, InvalidAnimatorControllerWarningMessage, MessageType.Warning);
                return;
            }

            var parametersCount = animatorControllerParameters.Length;
            var animatorParameters = new List<AnimatorControllerParameter>(parametersCount);
            for (var i = 0; i < parametersCount; i++)
            {
                var parameter = animatorControllerParameters[i];
                if (animatorParamAttribute.AnimatorParamType == null || parameter.type == animatorParamAttribute.AnimatorParamType)
                {
                    animatorParameters.Add(parameter);
                }
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    DrawPropertyForInt(rect, property, label, animatorParameters);
                    break;
                case SerializedPropertyType.String:
                    DrawPropertyForString(rect, property, label, animatorParameters);
                    break;
                default:
                    EditorGUI.HelpBox(rect, string.Format(InvalidTypeWarningMessage, property.name), MessageType.Warning);
                    break;
            }

            EditorGUI.EndProperty();
        }

        private static void DrawPropertyForInt(Rect rect, SerializedProperty property, GUIContent label,
            List<AnimatorControllerParameter> animatorParameters)
        {
            var paramNameHash = property.intValue;
            var index = 0;

            for (var i = 0; i < animatorParameters.Count; i++)
            {
                if (paramNameHash == animatorParameters[i].nameHash)
                {
                    index = i + 1;
                    break;
                }
            }

            var displayOptions = GetDisplayOptions(animatorParameters);


            EditorGUI.BeginChangeCheck();

            var newIndex = EditorGUI.Popup(rect, label.text, index, displayOptions);
            var newValue = newIndex == 0 ? 0 : animatorParameters[newIndex - 1].nameHash;

            if (EditorGUI.EndChangeCheck())
            {
                if (property.intValue != newValue)
                {
                    property.intValue = newValue;
                }
            }
        }

        private static void DrawPropertyForString(Rect rect, SerializedProperty property, GUIContent label,
            List<AnimatorControllerParameter> animatorParameters)
        {
            var paramName = property.stringValue;
            var index = 0;

            for (var i = 0; i < animatorParameters.Count; i++)
            {
                if (paramName.Equals(animatorParameters[i].name, System.StringComparison.Ordinal))
                {
                    index = i + 1; // +1 because the first option is reserved for (None)
                    break;
                }
            }

            var displayOptions = GetDisplayOptions(animatorParameters);

            var newIndex = EditorGUI.Popup(rect, label.text, index, displayOptions);
            var newValue = newIndex == 0 ? null : animatorParameters[newIndex - 1].name;

            if (!property.stringValue.Equals(newValue, System.StringComparison.Ordinal))
            {
                property.stringValue = newValue;
            }
        }

        private static string[] GetDisplayOptions(List<AnimatorControllerParameter> animatorParams)
        {
            var displayOptions = new string[animatorParams.Count + 1];
            displayOptions[0] = "None";

            for (var i = 0; i < animatorParams.Count; i++)
            {
                displayOptions[i + 1] = animatorParams[i].name;
            }

            return displayOptions;
        }

        private static AnimatorControllerParameter[] GetAnimatorController(SerializedProperty property, string animatorName)
        {
            var animatorProperty = property.serializedObject.FindProperty(animatorName);
            if (animatorProperty == null)
            {
                animatorProperty = property.serializedObject.FindProperty("_value").FindPropertyRelative(animatorName);
                if (animatorProperty == null) return null;
            }

            var animatorObject = animatorProperty.objectReferenceValue;
            if (animatorObject == null) return null;
            var animator = animatorObject as Animator;
            if (animator == null) return null;
            return animator.parameters;
        }
    }
}