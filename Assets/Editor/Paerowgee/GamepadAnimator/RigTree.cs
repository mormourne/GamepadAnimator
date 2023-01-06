using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigTree
{
    public Vector3 position;
    public List<RigTree> children;
    public RigTree parent;

    public RigTree(Transform transform, RigTree _parent = null)
    {
        position = transform.position;
        parent = _parent;
        children = new List<RigTree>();
        for (int i = 0; i < transform.childCount; i++)
        {
            children.Add(new RigTree(transform.GetChild(i), this));
        }
    }
    
}
