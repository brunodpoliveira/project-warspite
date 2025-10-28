using UnityEngine;
using UnityEditor;
using Warspite.UI;

namespace Warspite.World.Editor
{
    /// <summary>
    /// Editor helper to quickly add TurningCrosshair to selected enemy.
    /// Menu: GameObject > Warspite > Add Crosshair to Enemy
    /// </summary>
    public static class EnemySetupHelper
    {
        [MenuItem("GameObject/Warspite/Add Crosshair to Enemy", false, 0)]
        public static void AddCrosshairToEnemy()
        {
            GameObject selected = Selection.activeGameObject;
            
            if (selected == null)
            {
                Debug.LogError("Please select an enemy GameObject first!");
                return;
            }

            EnemyLogic enemyLogic = selected.GetComponent<EnemyLogic>();
            if (enemyLogic == null)
            {
                Debug.LogError($"{selected.name} doesn't have an EnemyLogic component!");
                return;
            }

            // Check if crosshair already exists
            TurningCrosshair existing = selected.GetComponentInChildren<TurningCrosshair>();
            if (existing != null)
            {
                Debug.LogWarning($"{selected.name} already has a TurningCrosshair!");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create crosshair child object
            GameObject crosshairObj = new GameObject("TurningCrosshair");
            crosshairObj.transform.SetParent(selected.transform);
            crosshairObj.transform.localPosition = new Vector3(0, 2f, 0); // Above enemy
            crosshairObj.transform.localRotation = Quaternion.identity;
            crosshairObj.transform.localScale = Vector3.one;

            // Add TurningCrosshair component
            TurningCrosshair crosshair = crosshairObj.AddComponent<TurningCrosshair>();

            // Mark as modified for prefab
            if (PrefabUtility.IsPartOfPrefabInstance(selected))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(selected);
            }

            Debug.Log($"Added TurningCrosshair to {selected.name}");
            Selection.activeGameObject = crosshairObj;
        }

        [MenuItem("GameObject/Warspite/Add Crosshair to Enemy", true)]
        public static bool ValidateAddCrosshairToEnemy()
        {
            return Selection.activeGameObject != null;
        }
    }
}
