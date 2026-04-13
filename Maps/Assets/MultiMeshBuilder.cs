using Godot;
using System;

public partial class MultiMeshBuilder : MultiMeshInstance3D
{
    [Export] public Node3D sourceParent; // your Crates node
    private MultiMeshInstance3D multiMeshInstance;

    public override void _Ready()
    {
        multiMeshInstance = this as MultiMeshInstance3D;
        BuildMultiMesh();
    }

    public void BuildMultiMesh()
    {
        var children = sourceParent.GetChildren();
        int count = children.Count;

        var mm = multiMeshInstance.Multimesh;
        mm.InstanceCount = count;

        for (int i = 0; i < count; i++)
        {
            if (children[i] is Node3D node)
            {
                mm.SetInstanceTransform(i, node.GlobalTransform);
            }
        }
    }
}