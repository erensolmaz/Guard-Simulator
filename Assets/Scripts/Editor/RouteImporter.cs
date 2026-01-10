using UnityEngine;
using UnityEditor;

public class RouteImporter : EditorWindow
{
    private CinematicCamera targetCamera;
    private GameObject routeParent;
    private string[] routeNames = new string[] { "ROUTE A/route 1", "ROUTE B", "ROUTE C" };
    
    [MenuItem("Tools/Route Importer")]
    public static void ShowWindow()
    {
        GetWindow<RouteImporter>("Route Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Import Routes to Cinematic Camera", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        targetCamera = (CinematicCamera)EditorGUILayout.ObjectField(
            "Target Camera", 
            targetCamera, 
            typeof(CinematicCamera), 
            true
        );
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Import", EditorStyles.miniBoldLabel);
        
        if (GUILayout.Button("Find and Import ROUTE A"))
        {
            ImportRouteByName("ROUTE A/route 1");
        }
        
        if (GUILayout.Button("Find and Import ROUTE B"))
        {
            ImportRouteByName("ROUTE B");
        }
        
        if (GUILayout.Button("Find and Import ROUTE C"))
        {
            ImportRouteByName("ROUTE C");
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Manual Import", EditorStyles.miniBoldLabel);
        
        routeParent = (GameObject)EditorGUILayout.ObjectField(
            "Route Parent", 
            routeParent, 
            typeof(GameObject), 
            true
        );
        
        if (GUILayout.Button("Import from Selected Route Parent"))
        {
            if (routeParent != null)
            {
                ImportFromParent(routeParent.transform);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Route Parent first!", "OK");
            }
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Select 'ROUTE A/route 1', 'ROUTE B', or 'ROUTE C' from the hierarchy, " +
            "then use the import buttons to automatically add their waypoints to the camera.",
            MessageType.Info
        );
    }

    private void ImportRouteByName(string routeName)
    {
        if (targetCamera == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Target Camera first!", "OK");
            return;
        }
        
        GameObject route = GameObject.Find(routeName);
        
        if (route == null)
        {
            EditorUtility.DisplayDialog("Error", $"Could not find '{routeName}' in the scene!", "OK");
            return;
        }
        
        ImportFromParent(route.transform);
    }

    private void ImportFromParent(Transform parent)
    {
        if (targetCamera == null || parent == null) return;
        
        targetCamera.waypoints.Clear();
        
        int waypointCount = 0;
        foreach (Transform child in parent)
        {
            if (child.name.Contains("rote point") || child.name.Contains("waypoint"))
            {
                targetCamera.waypoints.Add(child);
                waypointCount++;
            }
        }
        
        EditorUtility.SetDirty(targetCamera);
        
        if (waypointCount > 0)
        {
            Debug.Log($"Successfully imported {waypointCount} waypoints from {parent.name} to {targetCamera.name}");
            EditorUtility.DisplayDialog(
                "Success", 
                $"Imported {waypointCount} waypoints from {parent.name}", 
                "OK"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Warning", 
                $"No waypoints found in {parent.name}. Make sure children are named 'rote point' or 'waypoint'.", 
                "OK"
            );
        }
    }
}
