using UnityEngine;
using Valve.VR;

public class DroneMovementBase : MonoBehaviour
{
    #region Inspector properties

    public bool ShowMotorsForces = true;
    public bool ShowGlobalForces = true;
    public Transform YAxisLine;
    public Transform XAxisLine;
    public Transform ZAxisLine;

    [ReadOnly] public float Velocity;
    [ReadOnly] public float AngularVelocity;

    public float MaxVelocity = 10.0f;
    public float MaxThrottle = 10;
    public float MaxPitchRollSpeed = 5;
    public float MaxYawSpeed = 5;
    public float MaxDrag = 2.0f;
    public float MinDrag = -0.3f;

    public Transform MotorFL;
    public Transform MotorFR;
    public Transform MotorRR;
    public Transform MotorRL;

    public bool HoldThrottle = true;

    // TODO
    public bool AutoLevel = true;
    public float MaxAngle = 35;

    #endregion

    #region Public properties

    [HideInInspector]
    public float[] InputValues = new float[4];

    public SteamVR_Input_Sources TargetSource;

    public SteamVR_Action_Boolean Lt_Trackpad;
    public SteamVR_Action_Vector2 Lt_TrackpadPos;

    public SteamVR_Action_Boolean Rt_Trackpad;
    public SteamVR_Action_Vector2 Rt_TrackpadPos;

    #endregion

    #region Private properties

    Rigidbody drone;
    Vector3[] motorsForces;
    Transform[] motors;
    Vector3 yawTorque;

    public float throttleForce;
    float yawForce;
    float pitchForce;
    float rollForce;

    float? targetHeight;

    #endregion

    void Awake()
    {
        drone = GetComponent<Rigidbody>();
        motorsForces = new Vector3[4];
        motors = new Transform[4]
        {
            MotorFL,
            MotorFR,
            MotorRR,
            MotorRL
        };
    }

    protected virtual void Update()
    {
        CheckFlipDrone();
        ShowForceVectors();
        ShowGravityForce();
    }

    protected virtual void FixedUpdate()
    {

    }

    private void ShowForceVectors()
    {
        var lineTransformRL = MotorRL.GetChild(0);
        var lineTransformFL = MotorFL.GetChild(0);
        var lineTransformFR = MotorFR.GetChild(0);
        var lineTransformRR = MotorRR.GetChild(0);
        if (!ShowMotorsForces)
        {
            lineTransformRL.localScale = Vector3.zero;
            lineTransformFL.localScale = Vector3.zero;
            lineTransformFR.localScale = Vector3.zero;
            lineTransformRR.localScale = Vector3.zero;
            return;
        }

        var normalizedVector0 = motorsForces[0].magnitude * 2.0f / MaxPitchRollSpeed;
        var normalizedVector1 = motorsForces[1].magnitude * 2.0f / MaxPitchRollSpeed;
        var normalizedVector2 = motorsForces[2].magnitude * 2.0f / MaxPitchRollSpeed;
        var normalizedVector3 = motorsForces[3].magnitude * 2.0f / MaxPitchRollSpeed;

        lineTransformFL.localScale = new Vector3(1, 1, normalizedVector0);
        lineTransformFR.localScale = new Vector3(1, 1, normalizedVector1);
        lineTransformRR.localScale = new Vector3(1, 1, normalizedVector2);
        lineTransformRL.localScale = new Vector3(1, 1, normalizedVector3);
    }

    void ShowGravityForce()
    {
        if (!ShowGlobalForces)
        {
            YAxisLine.localScale = Vector3.zero;
            XAxisLine.localScale = Vector3.zero;
            ZAxisLine.localScale = Vector3.zero;
            return;
        }

        YAxisLine.position = drone.position;
        XAxisLine.position = drone.position;
        ZAxisLine.position = drone.position;

        var gravity = new Vector3(0, -drone.mass * 9.81f, 0);
        var resultant = gravity + motorsForces[0] + motorsForces[1] + motorsForces[2] + motorsForces[3];

        var normalized = resultant * 2.0f / MaxThrottle;
        YAxisLine.localScale = new Vector3(1, 1, -normalized.y);

        XAxisLine.localScale = new Vector3(1, 1, normalized.x);
        ZAxisLine.localScale = new Vector3(1, 1, normalized.z);
    }

    void CheckFlipDrone()
    {
        if (Rt_Trackpad.GetStateUp(TargetSource))
        {
            var newAngles = new Vector3(drone.transform.eulerAngles.x, drone.transform.eulerAngles.y + 180, 0);
            drone.transform.eulerAngles = newAngles;
        }
    }

    protected void CalculateVelocity()
    {
        Velocity = drone.velocity.y;
        AngularVelocity = drone.angularVelocity.magnitude;
    }

    protected void CalculateDrag()
    {
        Velocity = drone.velocity.magnitude;
        var newDrag = (Velocity / MaxVelocity) * MaxDrag;
        if (newDrag < MinDrag) newDrag = MinDrag;
        if (newDrag > MaxDrag) newDrag = MaxDrag;

        drone.drag = newDrag;
    }

    protected void CalculateMotorsForces()
    {
        var throttle = Lt_TrackpadPos.axis.y;
        var yaw = Lt_TrackpadPos.axis.x;
        var pitch = Rt_TrackpadPos.axis.y;
        var roll = Rt_TrackpadPos.axis.x;

        CalculateThrottleForce(throttle);
        yawForce = yaw * MaxYawSpeed;
        pitchForce = pitch * MaxPitchRollSpeed * 2.0f;
        rollForce = roll * MaxPitchRollSpeed * 2.0f;

        if (AutoLevel)
            AutoLevelAdjust();

        ValidateInput();
        SaveInputValues();

        var droneUpTransform = drone.transform.up;

        var force0 = throttleForce;
        var force1 = throttleForce;
        var force2 = throttleForce;
        var force3 = throttleForce;

        if (pitchForce > 0)
        {
            force2 += pitchForce;
            force3 += pitchForce;
        }
        else
        {
            force0 -= pitchForce;
            force1 -= pitchForce;
        }

        if (rollForce > 0)
        {
            force0 += rollForce;
            force3 += rollForce;
        }
        else
        {
            force1 -= rollForce;
            force2 -= rollForce;
        }

        motorsForces[0] = droneUpTransform * force0;
        motorsForces[1] = droneUpTransform * force1;
        motorsForces[2] = droneUpTransform * force2;
        motorsForces[3] = droneUpTransform * force3;

        // TODO: Имитация рыскания с помощью двигателей, а не крутящего момента
        yawTorque = new Vector3(0, yawForce / 2, 0);

    }

    private void AutoLevelAdjust()
    {
        var coeff = MaxPitchRollSpeed * (90 - MaxAngle) / 1000f;

        var pitchAngle = drone.transform.eulerAngles.x;
        if (pitchAngle > 180)
            pitchAngle -= 360;
        var pitchAdjust = pitchAngle * coeff;
        pitchForce -= pitchAdjust;

        var rollAngle = -drone.transform.eulerAngles.z;
        if (rollAngle < -180)
            rollAngle += 360;
        var rollAdjust = rollAngle * coeff;
        rollForce -= rollAdjust;
    }

    protected void CalculateThrottleForce(float throttleInput)
    {
        if (HoldThrottle)
        {
            throttleForce += throttleInput;
            if (throttleInput == 0)
            {
                if (targetHeight == null)
                    targetHeight = drone.transform.position.y;

                // TODO: Держите высоту, используя PIDController

            }
            else
            {
                targetHeight = null;
            }
        }
        else
            throttleForce = throttleInput * MaxThrottle;
    }

    protected void ValidateInput()
    {
        if (throttleForce > MaxThrottle)
            throttleForce = MaxThrottle;

        if (throttleForce < 0) throttleForce = 0;
        if (pitchForce < -MaxPitchRollSpeed) pitchForce = -MaxPitchRollSpeed;
        if (rollForce < -MaxPitchRollSpeed) rollForce = -MaxPitchRollSpeed;
        if (yawForce < -MaxYawSpeed) yawForce = -MaxYawSpeed;

        if (pitchForce > MaxPitchRollSpeed) pitchForce = MaxPitchRollSpeed;
        if (rollForce > MaxPitchRollSpeed) rollForce = MaxPitchRollSpeed;
        if (yawForce > MaxYawSpeed) yawForce = MaxYawSpeed;
    }

    protected void SaveInputValues()
    {
        InputValues[0] = throttleForce;
        InputValues[1] = yawForce;
        InputValues[2] = pitchForce;
        InputValues[3] = rollForce;
    }

    protected void ApplyMotorsForces()
    {
        for (int i = 0; i < 4; i++)
        {
            drone.AddForceAtPosition(motorsForces[i], motors[i].transform.position, ForceMode.Force);
        }

        drone.AddTorque(yawTorque, ForceMode.Force);
    }
}
