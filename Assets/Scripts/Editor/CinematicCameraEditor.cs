using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(CinematicCamera))]
public class CinematicCameraEditor : Editor
{
    private CinematicCamera cinematicCamera;
    
    private Vector3 newWaypointPosition = Vector3.zero;
    private int waypointHeight = 10;
    private int numberOfWaypoints = 10;
    private float pathRadius = 50f;
    private bool useRandomHeight = true;
    private float minHeight = 5f;
    private float maxHeight = 20f;

    private void OnEnable()
    {
        cinematicCamera = (CinematicCamera)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Waypoint Tools", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        DrawManualWaypointCreator();
        EditorGUILayout.Space(10);
        DrawAutoWaypointCreator();
        EditorGUILayout.Space(10);
        DrawWaypointImporter();
        EditorGUILayout.Space(10);
        DrawWaypointActions();
    }

    private void DrawManualWaypointCreator()
    {
        EditorGUILayout.LabelField("Manual Waypoint Creation", EditorStyles.miniBoldLabel);
        
        newWaypointPosition = EditorGUILayout.Vector3Field("New Waypoint Position", newWaypointPosition);
        
        if (GUILayout.Button("Add Waypoint at Position"))
        {
            CreateWaypointAtPosition(newWaypointPosition);
        }
        
        if (GUILayout.Button("Add Waypoint at Camera Position"))
        {
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            {
                Vector3 cameraPos = SceneView.lastActiveSceneView.camera.transform.position;
                CreateWaypointAtPosition(cameraPos);
            }
        }
    }

    private void DrawAutoWaypointCreator()
    {
        EditorGUILayout.LabelField("Auto Generate Waypoints", EditorStyles.miniBoldLabel);
        
        numberOfWaypoints = EditorGUILayout.IntSlider("Number of Waypoints", numberOfWaypoints, 5, 50);
        pathRadius = EditorGUILayout.Slider("Path Radius", pathRadius, 10f, 200f);
        
        useRandomHeight = EditorGUILayout.Toggle("Use Random Height", useRandomHeight);
        
        if (useRandomHeight)
        {
            minHeight = EditorGUILayout.Slider("Min Height", minHeight, 0f, 50f);
            maxHeight = EditorGUILayout.Slider("Max Height", maxHeight, minHeight, 100f);
        }
        else
        {
            waypointHeight = EditorGUILayout.IntSlider("Waypoint Height", waypointHeight, 0, 50);
        }
        
        if (GUILayout.Button("Generate Circular Path"))
        {
            GenerateCircularPath();
        }
        
        if (GUILayout.Button("Generate Random Path"))
        {
            GenerateRandomPath();
        }
        
        if (GUILayout.Button("Generate Grid Path"))
        {
            GenerateGridPath();
        }
    }

    private void DrawWaypointImporter()
    {
        EditorGUILayout.LabelField("Import Waypoints", EditorStyles.miniBoldLabel);
        
        if (GUILayout.Button("Import from Selected GameObjects"))
        {
            ImportFromSelection();
        }
        
        if (GUILayout.Button("Import from Parent's Children"))
        {
            ImportFromParentChildren();
        }
    }

    private void DrawWaypointActions()
    {
        EditorGUILayout.LabelField("Waypoint Actions", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All Waypoints"))
        {
            if (EditorUtility.DisplayDialog("Clear Waypoints", 
                "Are you sure you want to clear all waypoints?", "Yes", "No"))
            {
                ClearWaypoints();
            }
        }
        
        if (GUILayout.Button("Reverse Order"))
        {
            ReverseWaypoints();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"Total Waypoints: {cinematicCamera.waypoints.Count}", EditorStyles.helpBox);
    }

    private void CreateWaypointAtPosition(Vector3 position)
    {
        GameObject waypointParent = GetOrCreateWaypointParent();
        
        GameObject waypoint = new GameObject($"Waypoint {cinematicCamera.waypoints.Count + 1}");
        waypoint.transform.position = position;
        waypoint.transform.SetParent(waypointParent.transform);
        
        Undo.RegisterCreatedObjectUndo(waypoint, "Create Waypoint");
        
        cinematicCamera.waypoints.Add(waypoint.transform);
        EditorUtility.SetDirty(cinematicCamera);
        
        Debug.Log($"Created waypoint at {position}");
    }

    private void GenerateCircularPath()
    {
        GameObject waypointParent = GetOrCreateWaypointParent();
        ClearWaypoints();
        
        Vector3 center = cinematicCamera.transform.position;
        
        for (int i = 0; i < numberOfWaypoints; i++)
        {
            float angle = i * (360f / numberOfWaypoints) * Mathf.Deg2Rad;
            float height = useRandomHeight ? Random.Range(minHeight, maxHeight) : waypointHeight;
            
            Vector3 position = new Vector3(
                center.x + Mathf.Cos(angle) * pathRadius,
                height,
                center.z + Mathf.Sin(angle) * pathRadius
            );
            
            GameObject waypoint = new GameObject($"Waypoint {i + 1}");
            waypoint.transform.position = position;
            waypoint.transform.SetParent(waypointParent.transform);
            
            Undo.RegisterCreatedObjectUndo(waypoint, "Generate Circular Path");
            
            cinematicCamera.waypoints.Add(waypoint.transform);
        }
        
        EditorUtility.SetDirty(cinematicCamera);
        Debug.Log($"Generated {numberOfWaypoints} waypoints in circular path");
    }

    private void GenerateRandomPath()
    {
        GameObject waypointParent = GetOrCreateWaypointParent();
        ClearWaypoints();
        
        Vector3 center = cinematicCamera.transform.position;
        
        for (int i = 0; i < numberOfWaypoints; i++)
        {
            float height = useRandomHeight ? Random.Range(minHeight, maxHeight) : waypointHeight;
            
            Vector3 randomOffset = new Vector3(
                Random.Range(-pathRadius, pathRadius),
                0f,
                Random.Range(-pathRadius, pathRadius)
            );
            
            Vector3 position = center + randomOffset;
            position.y = height;
            
            GameObject waypoint = new GameObject($"Waypoint {i + 1}");
            waypoint.transform.position = position;
            waypoint.transform.SetParent(waypointParent.transform);
            
            Undo.RegisterCreatedObjectUndo(waypoint, "Generate Random Path");
            
            cinematicCamera.waypoints.Add(waypoint.transform);
        }
        
        EditorUtility.SetDirty(cinematicCamera);
        Debug.Log($"Generated {numberOfWaypoints} random waypoints");
    }

    private void GenerateGridPath()
    {
        GameObject waypointParent = GetOrCreateWaypointParent();
        ClearWaypoints();
        
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(numberOfWaypoints));
        float spacing = (pathRadius * 2f) / (gridSize - 1);
        Vector3 startPos = cinematicCamera.transform.position - new Vector3(pathRadius, 0, pathRadius);
        
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (cinematicCamera.waypoints.Count >= numberOfWaypoints) break;
                
                float height = useRandomHeight ? Random.Range(minHeight, maxHeight) : waypointHeight;
                
                Vector3 position = startPos + new Vector3(x * spacing, height, z * spacing);
                
                GameObject waypoint = new GameObject($"Waypoint {cinematicCamera.waypoints.Count + 1}");
                waypoint.transform.position = position;
                waypoint.transform.SetParent(waypointParent.transform);
                
                Undo.RegisterCreatedObjectUndo(waypoint, "Generate Grid Path");
                
                cinematicCamera.waypoints.Add(waypoint.transform);
            }
        }
        
        EditorUtility.SetDirty(cinematicCamera);
        Debug.Log($"Generated {cinematicCamera.waypoints.Count} waypoints in grid path");
    }

    private void ImportFromSelection()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select GameObjects to import as waypoints", "OK");
            return;
        }
        
        ClearWaypoints();
        
        foreach (GameObject obj in selectedObjects)
        {
            cinematicCamera.waypoints.Add(obj.transform);
        }
        
        EditorUtility.SetDirty(cinematicCamera);
        Debug.Log($"Imported {selectedObjects.Length} waypoints from selection");
    }

    private void ImportFromParentChildren()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a parent GameObject", "OK");
            return;
        }
        
        Transform parent = Selection.activeGameObject.transform;
        
        if (parent.childCount == 0)
        {
            EditorUtility.DisplayDialog("No Children", "Selected GameObject has no children", "OK");
            return;
        }
        
        ClearWaypoints();
        
        foreach (Transform child in parent)
        {
            cinematicCamera.waypoints.Add(child);
        }
        
        EditorUtility.SetDirty(cinematicCamera);
        Debug.Log($"Imported {parent.childCount} waypoints from {parent.name}'s children");
    }

    private void ClearWaypoints()
    {
        cinematicCamera.waypoints.Clear();
        EditorUtility.SetDirty(cinematicCamera);
    }

    private void ReverseWaypoints()
    {
        cinematicCamera.waypoints.Reverse();
        EditorUtility.SetDirty(cinematicCamera);
        Debug.Log("Waypoints reversed");
    }

    private GameObject GetOrCreateWaypointParent()
    {
        GameObject parent = GameObject.Find("Camera Waypoints");
        
        if (parent == null)
        {
            parent = new GameObject("Camera Waypoints");
            Undo.RegisterCreatedObjectUndo(parent, "Create Waypoint Parent");
        }
        
        return parent;
    }

    private void OnSceneGUI()
    {
        if (cinematicCamera == null || cinematicCamera.waypoints == null) return;

        for (int i = 0; i < cinematicCamera.waypoints.Count; i++)
        {
            Transform waypoint = cinematicCamera.waypoints[i];
            if (waypoint == null) continue;

            Handles.color = Color.cyan;
            Handles.Label(waypoint.position + Vector3.up, $"WP {i + 1}");
            
            Vector3 newPos = Handles.PositionHandle(waypoint.position, Quaternion.identity);
            if (newPos != waypoint.position)
            {
                Undo.RecordObject(waypoint, "Move Waypoint");
                waypoint.position = newPos;
            }
        }
    }
}
