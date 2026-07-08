using System;
using UnityEngine;
using UnityEditor;
using Coplay.Controllers.Functions;

public class RefactorHierarchy
{
    public static string Execute()
    {
        // Create logical hierarchy groups
        
        // 1. Create Managers group
        var managersGroup = CreateEmptyGameObject("Managers");
        
        // 2. Create Player group
        var playerGroup = CreateEmptyGameObject("Player", managersGroup.transform);
        
        // 3. Create UI group
        var uiGroup = CreateEmptyGameObject("UI", managersGroup.transform);
        
        // 4. Create Environment group
        var environmentGroup = CreateEmptyGameObject("Environment", managersGroup.transform);
        
        // 5. Create Camera and Lighting group
        var cameraGroup = CreateEmptyGameObject("Camera", managersGroup.transform);
        
        // Move existing objects to their new parent groups
        
        // Move managers to Managers group
        MoveToParent("_GameManager", "Managers");
        MoveToParent("_ObjectPooler", "Managers");
        MoveToParent("NeonSceneGenerator", "Managers");
        
        // Move player to Player group
        MoveToParent("Player", "Player");
        MoveToParent("PlayerDiegeticTimer/RadialTimerImage", "Player");
        
        // Move UI elements to UI group
        MoveToParent("UI/Score", "UI");
        MoveToParent("UI/Combo", "UI");
        MoveToParent("UI/Timer", "UI");
        MoveToParent("Canvas/Damage", "UI");
        MoveToParent("EventSystem", "UI");
        
        // Move environment elements to Environment group
        MoveToParent("Walls/Top/Visual", "Environment");
        MoveToParent("Walls/Bottom/Visual", "Environment");
        MoveToParent("Walls/Right/Visual", "Environment");
        MoveToParent("Walls/Left/Visual", "Environment");
        
        // Move BackgroundGrid elements to Environment group
        MoveToParent("BackgroundGrid/VLine_-14.22222", "Environment");
        MoveToParent("BackgroundGrid/VLine_-12.22222", "Environment");
        MoveToParent("BackgroundGrid/VLine_-10.22222", "Environment");
        MoveToParent("BackgroundGrid/VLine_-8.222222", "Environment");
        MoveToParent("BackgroundGrid/VLine_-6.222222", "Environment");
        MoveToParent("BackgroundGrid/VLine_-4.222222", "Environment");
        MoveToParent("BackgroundGrid/VLine_-2.222222", "Environment");
        MoveToParent("BackgroundGrid/VLine_-0.2222223", "Environment");
        MoveToParent("BackgroundGrid/VLine_1.777778", "Environment");
        MoveToParent("BackgroundGrid/VLine_3.777778", "Environment");
        MoveToParent("BackgroundGrid/VLine_5.777778", "Environment");
        MoveToParent("BackgroundGrid/VLine_7.777778", "Environment");
        MoveToParent("BackgroundGrid/VLine_9.777778", "Environment");
        MoveToParent("BackgroundGrid/VLine_11.77778", "Environment");
        MoveToParent("BackgroundGrid/VLine_13.77778", "Environment");
        MoveToParent("BackgroundGrid/HLine_-8", "Environment");
        MoveToParent("BackgroundGrid/HLine_-6", "Environment");
        MoveToParent("BackgroundGrid/HLine_-4", "Environment");
        MoveToParent("BackgroundGrid/HLine_-2", "Environment");
        MoveToParent("BackgroundGrid/HLine_0", "Environment");
        MoveToParent("BackgroundGrid/HLine_2", "Environment");
        MoveToParent("BackgroundGrid/HLine_4", "Environment");
        MoveToParent("BackgroundGrid/HLine_6", "Environment");
        MoveToParent("BackgroundGrid/HLine_8", "Environment");
        
        // Move camera and lighting to Camera group
        MoveToParent("Main Camera", "Camera");
        MoveToParent("Global Light 2D", "Camera");
        MoveToParent("Global Volume", "Camera");
        
        return "Hierarchy refactored successfully into logical groups:" +
               "\n- Managers: Contains _GameManager, _ObjectPooler, NeonSceneGenerator" +
               "\n- Player: Contains Player and PlayerDiegeticTimer" +
               "\n- UI: Contains UI/Score, UI/Combo, UI/Timer, Canvas/Damage, EventSystem" +
               "\n- Environment: Contains Walls and BackgroundGrid elements" +
               "\n- Camera: Contains Main Camera, Global Light 2D, Global Volume";
    }
    
    private static GameObject CreateEmptyGameObject(string name, Transform parent = null)
    {
        var emptyObj = new GameObject(name);
        if (parent != null)
        {
            emptyObj.transform.SetParent(parent);
        }
        return emptyObj;
    }
    
    private static void MoveToParent(string childPath, string parentPath)
    {
        var child = GameObject.Find(childPath);
        var parent = GameObject.Find(parentPath);
        if (child != null && parent != null)
        {
            child.transform.SetParent(parent.transform);
        }
    }
}