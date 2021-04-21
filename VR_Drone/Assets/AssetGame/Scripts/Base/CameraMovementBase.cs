using UnityEngine;

public class CameraMovementBase : MonoBehaviour
{
    public Transform drone;

    public bool FPV = false;

    DroneMovement droneScript;
    Transform camera;

    void Start()
    {
        camera = GetComponent<Transform>();
        droneScript = drone.GetComponent<DroneMovement>();
    }

    void Update()
    {
        if (FPV)
            FollowDroneFirstPerson();
        else
            FollowDroneThirdPerson();
    }

    void FollowDroneFirstPerson()
    {
        var dronePosition = drone.transform.position;
        var droneRotation = drone.rotation.eulerAngles;

        camera.transform.position = dronePosition;

        var rotation = droneRotation + new Vector3(-20, 0, 0);
        camera.transform.rotation = Quaternion.Euler(rotation);
    }

    void FollowDroneThirdPerson()
    {
        var throttle = droneScript.InputValues[0];
        var maxThrottle = droneScript.MaxThrottle;

        var dronePosition = drone.transform.position;
        var droneYAngle = drone.eulerAngles.y;
        var droneYAngleRadians = droneYAngle * Mathf.Deg2Rad;

        // TODO - animation
        //var offset = ((throttle / maxThrottle) + 1) * PositionOffset;
        var rotatedOffset = new Vector3
        {
            x = PositionOffset.z * Mathf.Sin(droneYAngleRadians),
            y = PositionOffset.y,
            z = PositionOffset.z * Mathf.Cos(droneYAngleRadians)
        };
        camera.transform.position = dronePosition + rotatedOffset;

        var yAngle = drone.rotation.eulerAngles.y;
        var rotation = RotationOffest + new Vector3(0, yAngle, 0);
        camera.transform.rotation = Quaternion.Euler(rotation);
    }

    static readonly Vector3 PositionOffset = new Vector3(0, 0.6f, -1f);
    static readonly Vector3 RotationOffest = new Vector3(15, 0, 0);
}