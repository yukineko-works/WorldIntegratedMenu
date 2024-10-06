using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu.Dev
{
    internal class UdonBehaviourCounter : EditorWindow
    {
        [MenuItem("Window/World Integrated Menu/Udon Behaviour Counter")]
        private static void ShowWindow()
        {
            GetWindow<UdonBehaviourCounter>("Udon Behaviour Counter");
        }

        private GameObject _rootObject;
        private bool _includeInactive = true;
        private Vector2 _scrollPosition;

        private List<UdonBehaviour> _udonBehavioursCache = new List<UdonBehaviour>();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Udon Behaviour Counter", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            _rootObject = EditorGUILayout.ObjectField("Root Object", _rootObject, typeof(GameObject), true) as GameObject;
            _includeInactive = EditorGUILayout.Toggle("Include Inactive", _includeInactive);

            if (_rootObject == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Please assign a root object to count UdonBehaviours from.", MessageType.Info);
            }

            EditorGUILayout.Space();

            GUI.enabled = _rootObject != null;
            if (GUILayout.Button("Count"))
            {
                _udonBehavioursCache.Clear();
                _rootObject.GetComponentsInChildren(_includeInactive, _udonBehavioursCache);
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UdonBehaviours found: ", _udonBehavioursCache.Count.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_udonBehavioursCache.Count > 0)
            {
                EditorGUILayout.LabelField("UdonBehaviours breakdown", EditorStyles.boldLabel);
                var udonBehaviourCount = new Dictionary<string, int>();
                foreach (var udonBehaviour in _udonBehavioursCache)
                {
                    var udonBehaviourName = udonBehaviour.programSource.name;
                    if (udonBehaviourCount.ContainsKey(udonBehaviourName))
                    {
                        udonBehaviourCount[udonBehaviourName]++;
                    }
                    else
                    {
                        udonBehaviourCount[udonBehaviourName] = 1;
                    }
                }

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                EditorGUI.indentLevel++;
                foreach (var pair in udonBehaviourCount.OrderByDescending(pair => pair.Value))
                {
                    EditorGUILayout.LabelField($"{pair.Key}: {pair.Value}");
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
