using System;
using System.Collections.Generic;
using _Client_.AnimatorExtensions.Runtime;
using UnityEditor;
using UnityEngine;

namespace _Client_.AnimatorExtensions.Editor
{
    [CustomPropertyDrawer(typeof(SFAnimatorParameterAttribute))]
    public class SFAnimatorParameterAttributeDrawer : PropertyDrawer
    {
        private const string InvalidAnimatorControllerWarningMessage = "A shared Animator with a controller is required";
        private const string InvalidTypeWarningMessage = "{0} must be an int or a string";

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(rect, label, property);
            try
            {
                if (property.propertyType != SerializedPropertyType.Integer && property.propertyType != SerializedPropertyType.String)
                {
                    EditorGUI.HelpBox(rect, string.Format(InvalidTypeWarningMessage, property.name), MessageType.Warning);
                    return;
                }

                var parameterAttribute = (SFAnimatorParameterAttribute)attribute;
                var controllerParameters = GetAnimatorController(property, parameterAttribute.AnimatorName);
                if (controllerParameters == null)
                {
                    EditorGUI.HelpBox(rect, InvalidAnimatorControllerWarningMessage, MessageType.Warning);
                    return;
                }

                var parameters = new List<AnimatorControllerParameter>(controllerParameters.Length);
                foreach (var parameter in controllerParameters)
                    if (parameterAttribute.AnimatorParamType == null || parameter.type == parameterAttribute.AnimatorParamType)
                        parameters.Add(parameter);

                DrawParameterPopup(rect, property, label, parameters);
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }

        private static void DrawParameterPopup(Rect rect, SerializedProperty property, GUIContent label,
            List<AnimatorControllerParameter> animatorParameters)
        {
            var storesHash = property.propertyType == SerializedPropertyType.Integer;
            var hash = storesHash ? property.intValue : 0;
            var name = storesHash ? null : property.stringValue;
            var displayOptions = GetDisplayOptions(animatorParameters);
            var index = 0;

            if (property.hasMultipleDifferentValues)
            {
                index = -1;
            }
            else if (hash != 0 || !string.IsNullOrEmpty(name))
            {
                index = animatorParameters.FindIndex(parameter => storesHash
                    ? parameter.nameHash == hash
                    : string.Equals(parameter.name, name, StringComparison.Ordinal)) + 1;
                if (index == 0)
                {
                    index = displayOptions.Length;
                    Array.Resize(ref displayOptions, index + 1);
                    var missingValue = storesHash ? hash.ToString() : name;
                    displayOptions[index] = $"Missing ({missingValue})";
                }
            }

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUI.Popup(rect, label.text, index, displayOptions);
            if (!EditorGUI.EndChangeCheck() || newIndex < 0 || newIndex > animatorParameters.Count)
                return;

            if (storesHash)
                property.intValue = newIndex == 0 ? 0 : animatorParameters[newIndex - 1].nameHash;
            else
                property.stringValue = newIndex == 0 ? string.Empty : animatorParameters[newIndex - 1].name;
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
            if (string.IsNullOrEmpty(animatorName)) return null;

            var animatorProperty = property.serializedObject.FindProperty(animatorName) ??
                property.serializedObject.FindProperty("_value")?.FindPropertyRelative(animatorName);

            if (animatorProperty == null || animatorProperty.propertyType != SerializedPropertyType.ObjectReference ||
                animatorProperty.hasMultipleDifferentValues)
                return null;

            if (!(animatorProperty.objectReferenceValue is Animator animator) || animator == null ||
                animator.runtimeAnimatorController == null)
                return null;

            return animator.parameters;
        }
    }
}
