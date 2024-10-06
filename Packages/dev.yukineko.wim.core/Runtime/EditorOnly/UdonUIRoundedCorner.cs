
using UdonSharp;
using UnityEditor;
using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UdonUIRoundedCorner : UdonSharpBehaviour
    {
        public Vector4 radiuses = new Vector4(40f, 40f, 40f, 40f);
        public Material material;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [CustomEditor(typeof(UdonUIRoundedCorner))]
        public class UVector4Editor : Editor
        {
            private static readonly int _idHalfSize = Shader.PropertyToID("_halfSize");
            private static readonly int _idRadiuses = Shader.PropertyToID("_r");
            private static readonly int _idRect2props = Shader.PropertyToID("_rect2props");
            private static readonly int _idOuterUV = Shader.PropertyToID("_OuterUV");
            private static readonly Vector2 _wNorm = new Vector2(.7071068f, -.7071068f);
            private static readonly Vector2 _hNorm = new Vector2(.7071068f, .7071068f);
            private Vector4 _outerUV = new Vector4(0, 0, 1, 1);

            private UdonUIRoundedCorner _component;
            private RectTransform _rectTransform;
            private Image _image;
            private bool _isIndependent = false;
            private Vector2 _previousDeltaSize;
            private Vector4 _rect2props;

            private void OnEnable()
            {
                _component = (UdonUIRoundedCorner)target;
                _rectTransform = (RectTransform)_component.transform;
                _image = _component.GetComponent<Image>();
                _isIndependent = _component.radiuses.x != _component.radiuses.y || _component.radiuses.y != _component.radiuses.z || _component.radiuses.z != _component.radiuses.w;

                serializedObject.Update();

                if (_component.material == null)
                {
                    _component.material = new Material(Shader.Find("YNWorks/UI/RoundedCorners"));
                }

                if (_image != null)
                {
                    _image.material = _component.material;
                }

                if (_image.sprite != null)
                {
                    _outerUV = DataUtility.GetOuterUV(_image.sprite);
                }

                serializedObject.ApplyModifiedProperties();
            }

            private void Refresh()
            {
                var rect = _rectTransform.rect;
                RecalculateProps(rect.size);
                _component.material.SetVector(_idRect2props, _rect2props);
                _component.material.SetVector(_idHalfSize, rect.size * .5f);
                _component.material.SetVector(_idRadiuses, _component.radiuses);
                _component.material.SetVector(_idOuterUV, _outerUV);
            }

            private void RecalculateProps(Vector2 size)
            {
                var aVec = new Vector2(size.x, -size.y + _component.radiuses.x + _component.radiuses.z);
                var halfWidth = Vector2.Dot(aVec, _wNorm) * .5f;
                _rect2props.z = halfWidth;
                var bVec = new Vector2(size.x, size.y - _component.radiuses.w - _component.radiuses.y);
                var halfHeight = Vector2.Dot(bVec, _hNorm) * .5f;
                _rect2props.w = halfHeight;
                var efVec = new Vector2(size.x - _component.radiuses.x - _component.radiuses.y, 0);
                var egVec = _hNorm * Vector2.Dot(efVec, _hNorm);
                var ePoint = new Vector2(_component.radiuses.x - (size.x / 2), size.y / 2);
                var origin = ePoint + egVec + _wNorm * halfWidth + _hNorm * -halfHeight;
                _rect2props.x = origin.x;
                _rect2props.y = origin.y;
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();

                _isIndependent = EditorGUILayout.Toggle("Independent Corners", _isIndependent);
                EditorGUILayout.Space();

                var vector4Prop = serializedObject.FindProperty("radiuses");

                if (_isIndependent)
                {
                    EditorGUILayout.PropertyField(vector4Prop.FindPropertyRelative("x"), new GUIContent("Top Left Corner"));
                    EditorGUILayout.PropertyField(vector4Prop.FindPropertyRelative("y"), new GUIContent("Top Right Corner"));
                    EditorGUILayout.PropertyField(vector4Prop.FindPropertyRelative("w"), new GUIContent("Bottom Left Corner"));
                    EditorGUILayout.PropertyField(vector4Prop.FindPropertyRelative("z"), new GUIContent("Bottom Right Corner"));
                }
                else
                {
                    var radius = EditorGUILayout.FloatField("Radius", vector4Prop.vector4Value.x);
                    vector4Prop.vector4Value = new Vector4(radius, radius, radius, radius);
                }

                var needRefresh = EditorGUI.EndChangeCheck() || _previousDeltaSize != _rectTransform.sizeDelta;
                serializedObject.ApplyModifiedProperties();

                if (needRefresh)
                {
                    _previousDeltaSize = _rectTransform.sizeDelta;
                    Refresh();
                }
            }
        }
#endif
    }
}
