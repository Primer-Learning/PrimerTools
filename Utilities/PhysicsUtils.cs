using Godot;
using PrimerTools.Simulation;

namespace PrimerTools;

public static class PhysicsUtils
{
    public static Rid Make6DoFJointWithSprings(Rid bodyA, Rid bodyB)
    {
        var joint = PhysicsServer3D.JointCreate();
        PhysicsServer3D.JointMakeGeneric6Dof(
            joint,
            bodyA,
            Transform3D.Identity,
            bodyB,
            Transform3D.Identity
        );
        PhysicsServer3D.JointDisableCollisionsBetweenBodies(joint, true);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisFlag.EnableLinearSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringStiffness, 1000);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringDamping, 0.1f);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisFlag.EnableLinearSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringStiffness, 1000);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringDamping, 0.1f);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisFlag.EnableLinearSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringStiffness, 1000);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.LinearSpringDamping, 0.1f);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisFlag.EnableAngularSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringStiffness, 1000);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.X,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringDamping, 0.1f);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisFlag.EnableAngularSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringStiffness, 1000);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Y,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringDamping, 0.1f);
        
        PhysicsServer3D.Generic6DofJointSetFlag(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisFlag.EnableAngularSpring, true);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringStiffness, 1000);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringEquilibriumPoint, 0);
        PhysicsServer3D.Generic6DofJointSetParam(joint, Vector3.Axis.Z,
            PhysicsServer3D.G6DofJointAxisParam.AngularSpringDamping, 0.1f);

        return joint;
    }
}