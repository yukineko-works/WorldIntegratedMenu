using System.Collections;
using UnityEditor;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace yukineko.WorldIntegratedMenu.EditorShared
{
    public class ListDrawerProperties
    {
        public bool draggable;
        public bool displayHeader;
        public bool displayAddButton;
        public bool displayRemoveButton;
        public bool disableCustomBGColor;
        public bool disableDeleteConfirm;

        public ListDrawerProperties(
            bool draggable = true,
            bool displayHeader = true,
            bool displayAddButton = true,
            bool displayRemoveButton = true,
            bool disableCustomBGColor = false,
            bool disableDeleteConfirm = false
        ) {
            this.draggable = draggable;
            this.displayHeader = displayHeader;
            this.displayAddButton = displayAddButton;
            this.displayRemoveButton = displayRemoveButton;
            this.disableCustomBGColor = disableCustomBGColor;
            this.disableDeleteConfirm = disableDeleteConfirm;
        }
    }

    public class ListDrawerCallbacks
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public delegate string HeaderStringCallbackDelegate();
        public delegate int ElementCountCallbackDelegate(int index);

        public HeaderStringCallbackDelegate drawHeader;
        public ReorderableList.ElementCallbackDelegate drawElement;
        public ReorderableList.ElementHeightCallbackDelegate elementHeight;
        public ReorderableList.AddCallbackDelegate onAdd;
        public ReorderableList.AddDropdownCallbackDelegate onAddDropdown;
        public ReorderableList.RemoveCallbackDelegate onRemove;
        public ReorderableList.SelectCallbackDelegate onSelect;
        public ReorderableList.ReorderCallbackDelegate onReorder;
        public ReorderableList.ReorderCallbackDelegateWithDetails onReorderWithDetails;
        public ReorderableList.ChangedCallbackDelegate onChanged;

        public ElementCountCallbackDelegate elementCount;
#endif
    }

    public class ListDrawer
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private readonly ReorderableList _reorderableList;

        public ListDrawer(IList elements, ListDrawerCallbacks callbacks, ListDrawerProperties properties = null)
        {
            var p = properties ?? new ListDrawerProperties();
            _reorderableList = new ReorderableList(elements, typeof(object), p.draggable, p.displayHeader, p.displayAddButton, p.displayRemoveButton);
            Initialize(callbacks, p);
        }

        public ListDrawer(SerializedProperty serializedProperty, ListDrawerCallbacks callbacks, ListDrawerProperties properties = null)
        {
            var p = properties ?? new ListDrawerProperties();
            _reorderableList = new ReorderableList(serializedProperty.serializedObject, serializedProperty, p.draggable, p.displayHeader, p.displayAddButton, p.displayRemoveButton);
            Initialize(callbacks, p);
        }

        public ReorderableList List => _reorderableList;

        public static Color activeBGColorLight = new Color(0.3f, 0.6f, 0.95f, 0.95f);
        public static Color activeBGColorDark = new Color(0.2f, 0.4f, 0.7f, 0.95f);
        public static Color altBGColorLight = new Color(0.85f, 0.85f, 0.85f, 1f);
        public static Color altBGColorDark = new Color(0.3f, 0.3f, 0.3f, 1f);

        public void Draw()
        {
            _reorderableList.DoLayoutList();
        }

#region Internal
        private bool IsDarkMode => EditorGUIUtility.isProSkin;

        private void Initialize(ListDrawerCallbacks callbacks, ListDrawerProperties properties)
        {
            if (!properties.disableCustomBGColor)
            {
                _reorderableList.drawElementBackgroundCallback += DrawElementBackgroundInternal;
            }

            if (callbacks.onRemove != null)
            {
                if (!properties.disableDeleteConfirm)
                {
                    _reorderableList.onRemoveCallback += (list) =>
                    {
                        if (Event.current.shift || EditorUtility.DisplayDialog(EditorI18n.GetTranslation("warning"), EditorI18n.GetTranslation("beforeDelete"), EditorI18n.GetTranslation("delete"), EditorI18n.GetTranslation("cancel")))
                        {
                            callbacks.onRemove(list);
                        }
                    };
                }
                else
                {
                    _reorderableList.onRemoveCallback += callbacks.onRemove;
                }
            }

            if (callbacks.drawHeader != null)
            {
                _reorderableList.drawHeaderCallback += (rect) => EditorGUI.LabelField(rect, callbacks.drawHeader());
            }

            if (callbacks.drawElement != null)
            {
                _reorderableList.drawElementCallback += (rect, index, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += EditorGUIUtility.standardVerticalSpacing * 2;

                    callbacks.drawElement(rect, index, isActive, isFocused);
                };
            }

            if (callbacks.elementHeight != null)
            {
                _reorderableList.elementHeightCallback += callbacks.elementHeight;
            }
            else
            {
                _reorderableList.elementHeightCallback += (index) => ((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * callbacks.elementCount(index)) + (EditorGUIUtility.standardVerticalSpacing * 2);
            }

            if (callbacks.onAdd != null)
            {
                _reorderableList.onAddCallback += callbacks.onAdd;
            }

            if (callbacks.onAddDropdown != null)
            {
                _reorderableList.onAddDropdownCallback += callbacks.onAddDropdown;
            }

            if (callbacks.onSelect != null)
            {
                _reorderableList.onSelectCallback += callbacks.onSelect;
            }

            if (callbacks.onReorder != null)
            {
                _reorderableList.onReorderCallback += callbacks.onReorder;
            }

            if (callbacks.onReorderWithDetails != null)
            {
                _reorderableList.onReorderCallbackWithDetails += callbacks.onReorderWithDetails;
            }

            if (callbacks.onChanged != null)
            {
                _reorderableList.onChangedCallback += callbacks.onChanged;
            }
        }

        private void DrawElementBackgroundInternal(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (isFocused)
            {

                DrawBGInternal(rect, IsDarkMode ? activeBGColorDark : activeBGColorLight, 1, IsDarkMode ? 2 : 1);
                return;
            }

            if (index % 2 == 0) return;

            DrawBGInternal(rect, IsDarkMode ? altBGColorDark : altBGColorLight, IsDarkMode ? 1 : 2, 2);
        }

        private void DrawBGInternal(Rect rect, Color color, int leftMargin, int rightExtrusion)
        {
            rect.x += leftMargin;
            rect.width -= leftMargin + rightExtrusion;
            EditorGUI.DrawRect(rect, color);
        }
#endregion
#endif
    }

    public static class ListDrawerUtils
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public static Rect AdjustRect(ref Rect rect)
        {
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return rect;
        }
#endif
    }
}