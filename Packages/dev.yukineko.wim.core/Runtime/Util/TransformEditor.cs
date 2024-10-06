
using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.DevTools
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    internal class TransformEditor : UdonSharpBehaviour
    {
        [SerializeField] private UdonBehaviour _udonBehaviour;
        [SerializeField] private int _udonBehaviourIndex = -1;
        [SerializeField] private string _udonCustomEventMethod;

        [SerializeField] private Slider _positionX;
        [SerializeField] private Slider _positionY;
        [SerializeField] private Slider _positionZ;
        [SerializeField] private Slider _rotationX;
        [SerializeField] private Slider _rotationY;
        [SerializeField] private Slider _rotationZ;
        [SerializeField] private Text _positionXText;
        [SerializeField] private Text _positionYText;
        [SerializeField] private Text _positionZText;
        [SerializeField] private Text _rotationXText;
        [SerializeField] private Text _rotationYText;
        [SerializeField] private Text _rotationZText;

        private Vector3 _position;
        private Quaternion _rotation;

        public Vector3 Position => _position;
        public Quaternion Rotation => _rotation;

        private void Start()
        {
            if (_udonBehaviour != null && _udonBehaviourIndex != -1)
            {
                var udonBehaviours = _udonBehaviour.GetComponents<UdonBehaviour>();
                if (udonBehaviours.Length > _udonBehaviourIndex)
                {
                    _udonBehaviour = udonBehaviours[_udonBehaviourIndex];
                }
            }
        }

        public void UpdateTransform()
        {
            if (_udonBehaviour == null || string.IsNullOrEmpty(_udonCustomEventMethod)) return;

            _position = new Vector3(_positionX.value, _positionY.value, _positionZ.value);
            _rotation = Quaternion.Euler(_rotationX.value, _rotationY.value, _rotationZ.value);

            _positionXText.text = _positionX.value.ToString("F2");
            _positionYText.text = _positionY.value.ToString("F2");
            _positionZText.text = _positionZ.value.ToString("F2");
            _rotationXText.text = _rotationX.value.ToString("F2");
            _rotationYText.text = _rotationY.value.ToString("F2");
            _rotationZText.text = _rotationZ.value.ToString("F2");

            _udonBehaviour.SendCustomEvent(_udonCustomEventMethod);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [CustomEditor(typeof(TransformEditor))]
        internal class TransformEditorInspector : Editor
        {
            private TransformEditor _transformEditor;
            private bool _showObjectProperties = false;

            private void OnEnable()
            {
                _transformEditor = target as TransformEditor;
            }

            public override void OnInspectorGUI()
            {

                serializedObject.Update();
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Transform Editor", EditorStyles.largeLabel);
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("_udonBehaviour"));
                if (_transformEditor._udonBehaviour != null)
                {
                    var udonBehaviours = _transformEditor._udonBehaviour.GetComponents<UdonBehaviour>();
                    if (udonBehaviours.Length > 1)
                    {
                        var udonList = udonBehaviours.Select(x => x.programSource.name).ToArray();
                        var udonBehaviourIndex = EditorGUILayout.Popup("Program source", _transformEditor._udonBehaviourIndex, udonList);
                        if (udonBehaviourIndex >= 0)
                        {
                            serializedObject.FindProperty("_udonBehaviourIndex").intValue = udonBehaviourIndex;
                        }
                    }
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("_udonCustomEventMethod"));
                EditorGUILayout.Space();

                EditorGUILayout.Space();
                _showObjectProperties = EditorGUILayout.Foldout(_showObjectProperties, "Object Properties");
                if (_showObjectProperties)
                {
                    EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_positionX"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_positionY"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_positionZ"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_positionXText"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_positionYText"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_positionZText"));

                    EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationX"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationY"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationZ"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationXText"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationYText"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_rotationZText"));
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
#endif
    }
}
