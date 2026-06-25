using UnityEngine;

public static class DuplicateSystemRootUtility
{
    public static bool DestroyDuplicate(Component currentComponent, Component existingComponent)
    {
        if (currentComponent == null || existingComponent == null || currentComponent == existingComponent)
            return false;

        var currentRoot = currentComponent.transform.root != null
            ? currentComponent.transform.root.gameObject
            : currentComponent.gameObject;
        var existingRoot = existingComponent.transform.root != null
            ? existingComponent.transform.root.gameObject
            : existingComponent.gameObject;

        var duplicateTarget = currentRoot != null && existingRoot != null && currentRoot != existingRoot
            ? currentRoot
            : currentComponent.gameObject;

        Debug.LogWarning($"[SystemRoot] Destroying duplicate object: {duplicateTarget.name}", duplicateTarget);
        Object.Destroy(duplicateTarget);
        return true;
    }
}
