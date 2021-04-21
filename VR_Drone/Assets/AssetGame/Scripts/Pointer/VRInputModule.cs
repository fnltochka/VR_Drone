using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Valve.VR;

public class VRInputModule : BaseInputModule
{
    public Camera m_Camera;
    public SteamVR_Input_Sources m_TargetSource;
    public SteamVR_Action_Boolean m_ClickAction;

    private GameObject m_CurrentObject = null;
    private PointerEventData m_Data = null;

    protected override void Awake()
    {
        base.Awake();

        m_Data = new PointerEventData(eventSystem);
    }

    public override void Process()
    {
        // Reset data, set camera
        m_Data.Reset();
        m_Data.position = new Vector2(m_Camera.pixelWidth / 2, m_Camera.pixelHeight / 2);

        // Raycast, and set hit object
        eventSystem.RaycastAll(m_Data, m_RaycastResultCache);
        m_Data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_CurrentObject = m_Data.pointerCurrentRaycast.gameObject;

        // Clear
        m_RaycastResultCache.Clear();

        // Hover
        if (!eventSystem.currentSelectedGameObject)
            HandlePointerExitAndEnter(m_Data, m_CurrentObject);

        // Press
        if (m_ClickAction.GetStateDown(m_TargetSource))
            ProcessPress(m_Data);

        // Release
        if (m_ClickAction.GetStateUp(m_TargetSource))
            ProcessRelease(m_Data);
    }

    public PointerEventData GetData()
    {
        return m_Data;
    }

    private void ProcessPress(PointerEventData data)
    {
        // Set raycast
        data.pointerPressRaycast = data.pointerCurrentRaycast;

        // If we hit an object, get the down handler, and call down
        GameObject newPointerPress = ExecuteEvents.ExecuteHierarchy(m_CurrentObject, data, ExecuteEvents.pointerDownHandler);

        // If no valid down handler, try for a click handler, does not call
        if (newPointerPress == null)
            newPointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(m_CurrentObject);

        // Set data
        data.pressPosition = data.position;
        data.pointerPress = newPointerPress;
        data.rawPointerPress = m_CurrentObject;
    }

    private void ProcessRelease(PointerEventData data)
    {
        // Execute a pointer up
        ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);

        // Check if the raw object has a click handler
        GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(m_CurrentObject);

        // If the click handler matches the pressed one
        if (data.pointerPress == pointerUpHandler)
        {
            // Execute click
            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
        }

        // Clear selected button
        eventSystem.SetSelectedGameObject(null);

        // Reset data
        data.pressPosition = Vector2.zero;
        data.pointerPress = null;
        data.rawPointerPress = null;
    }
}
