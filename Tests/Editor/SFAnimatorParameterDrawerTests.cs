using System;
using System.Collections.Generic;
using System.Reflection;
using _Client_.AnimatorExtensions.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SFramework.Animator.Editor.Tests
{
    public sealed class SFAnimatorParameterDrawerTests
    {
        private GameObject _gameObject;
        private UnityEngine.Animator _animator;
        private AnimatorController _controller;
        private AnimatorStateMachine _stateMachine;
        private AnimatorState _idleState;
        private AnimatorFields _fields;
        private SerializedObject _serialized;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Animator drawer test");
            _animator = _gameObject.AddComponent<UnityEngine.Animator>();
            _controller = new AnimatorController { name = "Drawer test controller" };
            _controller.AddLayer("Base Layer");
            _stateMachine = _controller.layers[0].stateMachine;
            _idleState = _stateMachine.AddState("Idle");
            _stateMachine.defaultState = _idleState;
            _controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            _controller.AddParameter("IsOpen", AnimatorControllerParameterType.Bool);
            _controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            _animator.runtimeAnimatorController = _controller;
            _fields = ScriptableObject.CreateInstance<AnimatorFields>();
            _fields.Animator = _animator;
            _fields._value = new NestedFields { NestedAnimator = _animator };
            _serialized = new SerializedObject(_fields);
        }

        [TearDown]
        public void TearDown()
        {
            _serialized?.Dispose();
            if (_fields != null) Object.DestroyImmediate(_fields);
            if (_gameObject != null) Object.DestroyImmediate(_gameObject);
            if (_controller != null) Object.DestroyImmediate(_controller);
            if (_idleState != null) Object.DestroyImmediate(_idleState);
            if (_stateMachine != null) Object.DestroyImmediate(_stateMachine);
        }

        [Test]
        public void ResolveController_ReadsActualAnimatorParametersFromRootSerializedField()
        {
            var parameters = Resolve(_serialized.FindProperty(nameof(AnimatorFields.Parameter)), nameof(AnimatorFields.Animator));

            AssertParameters(parameters);
        }

        [Test]
        public void ResolveController_FallsBackToAnimatorInsideSerializedValueWrapper()
        {
            var parameters = Resolve(_serialized.FindProperty("_value.Parameter"), nameof(NestedFields.NestedAnimator));

            AssertParameters(parameters);
        }

        [Test]
        public void ResolveController_WithEmptyAnimatorReference_ReturnsNull()
        {
            _fields.Animator = null;
            _serialized.Update();

            Assert.That(Resolve(_serialized.FindProperty(nameof(AnimatorFields.Parameter)), nameof(AnimatorFields.Animator)), Is.Null);
        }

        [Test]
        public void ResolveController_WithNonAnimatorObjectReference_ReturnsNull()
        {
            _fields.OtherObject = _gameObject;
            _serialized.Update();

            Assert.That(Resolve(_serialized.FindProperty(nameof(AnimatorFields.Parameter)), nameof(AnimatorFields.OtherObject)), Is.Null);
        }

        [Test]
        public void ResolveController_WithMissingRootAndNestedAnimatorField_ReturnsNull()
        {
            Assert.That(Resolve(_serialized.FindProperty(nameof(AnimatorFields.Parameter)), "MissingAnimator"), Is.Null);
        }

        [Test]
        public void ResolveController_WithMissingAnimatorAndNoValueWrapper_ReturnsNull()
        {
            var fields = ScriptableObject.CreateInstance<UnwrappedFields>();
            try
            {
                using var serialized = new SerializedObject(fields);
                Assert.That(Resolve(serialized.FindProperty(nameof(UnwrappedFields.Parameter)), "MissingAnimator"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(fields);
            }
        }

        [Test]
        public void ResolveController_WithMixedAnimatorReferences_ReturnsNull()
        {
            var other = ScriptableObject.CreateInstance<AnimatorFields>();
            try
            {
                using var serialized = new SerializedObject(new Object[] { _fields, other });
                Assert.That(Resolve(serialized.FindProperty(nameof(AnimatorFields.Parameter)), nameof(AnimatorFields.Animator)), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void DisplayOptions_PrependsNoneAndPreservesControllerParameterOrder()
        {
            var parameters = Resolve(_serialized.FindProperty(nameof(AnimatorFields.Parameter)), nameof(AnimatorFields.Animator));
            var options = (string[])typeof(SFAnimatorParameterAttributeDrawer)
                .GetMethod("GetDisplayOptions", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { new List<AnimatorControllerParameter>(parameters) });

            CollectionAssert.AreEqual(new[] { "None", "Speed", "IsOpen", "Fire" }, options);
        }

        private static AnimatorControllerParameter[] Resolve(SerializedProperty property, string animatorName) =>
            (AnimatorControllerParameter[])typeof(SFAnimatorParameterAttributeDrawer)
                .GetMethod("GetAnimatorController", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { property, animatorName });

        private static void AssertParameters(AnimatorControllerParameter[] parameters)
        {
            Assert.That(parameters, Has.Length.EqualTo(3));
            Assert.That(parameters[0].name, Is.EqualTo("Speed"));
            Assert.That(parameters[0].type, Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(parameters[0].nameHash, Is.EqualTo(UnityEngine.Animator.StringToHash("Speed")));
            Assert.That(parameters[1].name, Is.EqualTo("IsOpen"));
            Assert.That(parameters[1].type, Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(parameters[2].name, Is.EqualTo("Fire"));
            Assert.That(parameters[2].type, Is.EqualTo(AnimatorControllerParameterType.Trigger));
        }

        public sealed class AnimatorFields : ScriptableObject
        {
            public UnityEngine.Animator Animator;
            public Object OtherObject;
            public string Parameter;
            public NestedFields _value;
        }

        public sealed class UnwrappedFields : ScriptableObject
        {
            public string Parameter;
        }

        [Serializable]
        public sealed class NestedFields
        {
            public UnityEngine.Animator NestedAnimator;
            public int Parameter;
        }
    }
}
