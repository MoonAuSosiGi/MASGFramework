using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static GameObject FindChild(GameObject parent, string uiName, bool isRecursive)
    {
        if (parent == null || string.IsNullOrEmpty(uiName))
            return null;

        Transform parentTM = parent.transform;

        foreach (Transform childTM in parentTM)
        {
            if (childTM.name == uiName)
            {
                return childTM.gameObject;
            }
            else
            {
                if (isRecursive)
                {
                    GameObject obj = FindChild(childTM.gameObject, uiName, isRecursive);
                    if (obj != null)
                        return obj;
                }
            }
        }

        return null;
    }
    
    public static T FindChild<T>(GameObject parent, string uiName, bool isRecursive) where T : UnityEngine.Object
    {
        if (parent == null || string.IsNullOrEmpty(uiName))
            return null;

        Transform parentTM = parent.transform;

        foreach (Transform childTM in parentTM)
        {
            if (childTM.name == uiName)
            {                
                var comp = childTM.GetComponent<T>();

                if (comp != null)
                    return comp;
            }
            else
            {
                if (isRecursive)
                {
                    var comp = FindChild<T>(childTM.gameObject, uiName, isRecursive);
                    if (comp != null)
                        return comp;
                }
            }
        }
        
        return null;
    }
}
