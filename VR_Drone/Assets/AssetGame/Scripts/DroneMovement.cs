using UnityEngine;

public class DroneMovement : DroneMovementBase
{
    protected override void Update()
    {
        CalculateVelocity();
        CalculateDrag();
        base.Update();
    }

    protected override void FixedUpdate()
    {
        CalculateMotorsForces();
        ApplyMotorsForces();
        base.FixedUpdate();
    }
}
