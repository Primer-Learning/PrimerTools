using Godot;
using PrimerTools.Simulation;

namespace PrimerTools;

public static class PhysicsUtils
{
    public static Rid Make6DoFJointWithSprings(Rid bodyA, Rid bodyB, Transform3D transformA = default, Transform3D transformB = default)
    {
        if (transformA == default) transformA = Transform3D.Identity;
        if (transformB == default) transformB = Transform3D.Identity;
        
        GD.Print();
        
        var joint = PhysicsServer3D.JointCreate();
        PhysicsServer3D.JointMakeGeneric6Dof(
            joint,
            bodyA,
            transformA,
            bodyB,
            transformB
        );
        PhysicsServer3D.JointDisableCollisionsBetweenBodies(joint, true);
        
        // PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.X,
        //     PhysicsServer3D.G6DofJointAxisFlag.EnableLinearLimit, true);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
        //     PhysicsServer3D.G6DofJointAxisParam.LinearLowerLimit, 0);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
        //     PhysicsServer3D.G6DofJointAxisParam.LinearUpperLimit, 0);
        //
        // PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Y,
        //     PhysicsServer3D.G6DofJointAxisFlag.EnableLinearLimit, true);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
        //     PhysicsServer3D.G6DofJointAxisParam.LinearLowerLimit, 0);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
        //     PhysicsServer3D.G6DofJointAxisParam.LinearUpperLimit, 0);
        //
        // PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Z,
        //     PhysicsServer3D.G6DofJointAxisFlag.EnableLinearLimit, true);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
        //     PhysicsServer3D.G6DofJointAxisParam.LinearLowerLimit, 0);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
        //     PhysicsServer3D.G6DofJointAxisParam.LinearUpperLimit, 0);
        //
        //
        // PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.X,
        //     PhysicsServer3D.G6DofJointAxisFlag.EnableAngularLimit, true);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
        //     PhysicsServer3D.G6DofJointAxisParam.AngularLowerLimit, 0);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
        //     PhysicsServer3D.G6DofJointAxisParam.AngularUpperLimit, 0);
        //
        // PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Y,
        //     PhysicsServer3D.G6DofJointAxisFlag.EnableAngularLimit, true);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
        //     PhysicsServer3D.G6DofJointAxisParam.AngularLowerLimit, 0);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
        //     PhysicsServer3D.G6DofJointAxisParam.AngularUpperLimit, 0);
        //
        // PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Z,
        //     PhysicsServer3D.G6DofJointAxisFlag.EnableAngularLimit, true);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
        //     PhysicsServer3D.G6DofJointAxisParam.AngularLowerLimit, 0);
        // PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
        //     PhysicsServer3D.G6DofJointAxisParam.AngularUpperLimit, 0);
        
        
        var stiffness = 10000000;
        var damping = 1f;
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisFlag.EnableLinearSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringStiffness, stiffness);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringDamping, damping);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisFlag.EnableLinearSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringStiffness, stiffness);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringDamping, damping);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisFlag.EnableLinearSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringStiffness, stiffness);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringDamping, damping);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisFlag.EnableAngularSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringStiffness, stiffness);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringDamping, damping);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisFlag.EnableAngularSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringStiffness, stiffness);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringDamping, damping);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisFlag.EnableAngularSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringStiffness, stiffness);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringDamping, damping);

        return joint;
    }
}