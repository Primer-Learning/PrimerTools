using Godot;

namespace PrimerTools;

public static class Transform3DUtils
{
    public static Quaternion QuaternionFromEulerDeg(Vector3 euler)
    {
        return Quaternion.FromEuler(euler * Mathf.Pi / 180);
    }

    public static Basis BasisFromForwardAndUp(Vector3 forward, Vector3 up)
    {
        var left = up.Cross(forward).Normalized();
        up = forward.Cross(left).Normalized(); // Recompute up to ensure orthogonality
        return new Basis(left, up, forward).Orthonormalized();
    }

    public static (Vector3 axis, float angle) GetAxisAngleRotationTowardBasis(this Basis basis, Basis desiredBasis)
    {
        var currentRotation = basis.GetRotationQuaternion();
        var targetRotation = desiredBasis.GetRotationQuaternion();
        var rotationDifference = targetRotation * currentRotation.Inverse();
        
        // Convert to axis-angle representation
        var axis = rotationDifference.GetAxis();//.Normalized();
        var angle = rotationDifference.GetAngle();
        if (angle > Mathf.Pi)
        {
            angle = 2 * Mathf.Pi - angle;
            axis = -axis;
        }

        return (axis, angle);
    }

    public static Vector3 CalculateAngularVelocityTowardBasis(this Transform3D transform, Basis desiredBasis, float rotationSpeedFactor = 10, float maxRotationSpeed = 10)
    {
        var (axis, angle) = transform.Basis.GetAxisAngleRotationTowardBasis(desiredBasis);
        return axis * Mathf.Min(angle * rotationSpeedFactor, maxRotationSpeed);
    }

    public static Vector3 CalculateVelocityAcceleratedTowardTarget(Vector3 targetDestination, Vector3 currentPosition, Vector3 currentVelocity, float maxSpeed, float accelerationFactor = 0.1f)
    {
        var accelerationVector =
            CalculateAccelerationTowardTarget(targetDestination, currentPosition, currentVelocity, maxSpeed, accelerationFactor);
        var newVelocity = currentVelocity + accelerationVector;
        
        return newVelocity.LimitLength(maxSpeed);
        
    }

    public static Vector3 CalculateAccelerationTowardTarget(Vector3 targetDestination, Vector3 currentPosition, Vector3 currentVelocity, float maxSpeed, float accelerationFactor = 0.1f)
    {
        if (targetDestination == Vector3.Zero) GD.Print("Moving to the origin");
        var desiredDisplacement = targetDestination - currentPosition;
        var desiredDisplacementLengthSquared = desiredDisplacement.LengthSquared();
        
        var desiredVelocity = Vector3.Zero;
        if (desiredDisplacementLengthSquared != 0)
        {
            desiredVelocity = desiredDisplacement * maxSpeed;
        }
        
        var velocityChange = desiredVelocity - currentVelocity;
        var velocityChangeLengthSquared = velocityChange.LengthSquared();

        var maxAccelerationMagnitudeSquared = maxSpeed * maxSpeed * accelerationFactor * accelerationFactor;
        Vector3 accelerationVector;
        if (velocityChangeLengthSquared > maxAccelerationMagnitudeSquared)
        {
            accelerationVector = Mathf.Sqrt(maxAccelerationMagnitudeSquared / velocityChangeLengthSquared) * velocityChange;
        }
        else
        {
            accelerationVector = velocityChange;
        }

        return accelerationVector;
    }
}