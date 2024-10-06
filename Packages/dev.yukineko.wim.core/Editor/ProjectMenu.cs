using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace yukineko.WorldIntegratedMenu.Dev
{
    internal static class ProjectMenu
    {
        [MenuItem("Assets/World Integrated Menu/Copy GUID", true)]
        private static bool CopyGuidValidate()
        {
            return Selection.activeObject != null && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        [MenuItem("Assets/World Integrated Menu/Copy GUID")]
        private static void CopyGuid()
        {
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(Selection.activeObject));
            EditorGUIUtility.systemCopyBuffer = guid;
        }
    }
}