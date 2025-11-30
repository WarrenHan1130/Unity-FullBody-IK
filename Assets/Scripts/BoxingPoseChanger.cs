using UnityEngine;

public class BoxingPoseChanger : MonoBehaviour
{
    public enum PoseState
    {
        TPose,
        Guard,
        RightPunch,
        LeftPunch,
        HighKick
    }
    
    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform headTarget;
    
    [Header("Pole Targets (Optional)")]
    public Transform leftElbowPole;
    public Transform rightElbowPole;
    public Transform leftKneePole;
    public Transform rightKneePole;
    
    [Header("Reference Bones")]
    public Transform headBone;
    public Transform hipsBone;
    
    [Header("Guard Pose - Boxing Guard Stance")]
    public Vector3 guardLeftHandOffset = new Vector3(0.2f, 0f, 0.6f);
    public Vector3 guardRightHandOffset = new Vector3(0.1f, -0.1f, 0.2f);
    public Vector3 guardLeftFoot = new Vector3(-0.3f, 0.087f, 0.25f);
    public Vector3 guardRightFoot = new Vector3(0.2f, 0.087f, -0.3f);
    
    [Header("Head Movement")]
    public Vector3 headStartPos = new Vector3(-4.579597e-07f, 1.599251f, -0.01519585f);
    public Vector3 headGuardPos = new Vector3(0.165f, 1.649f, 0.394f);
    public Vector3 headRightPunchPos = new Vector3(-0.072f, 1.635f, 0.279f);
    public Vector3 headLeftPunchPos = new Vector3(-0.072f, 1.617f, 0.132f);
    public Vector3 headHighKickPos = new Vector3(-0.268999994f, 1.55200005f, -0.40200001f);
    
    [Header("Right Punch - Target Position")]
    public Vector3 rightPunchRightHandPos = new Vector3(-0.0354f, 1.476582f, 1.14f);
    public Vector3 rightPunchLeftHandPos = new Vector3(-0.16f, 1.476582f, 0.352f);
    public Vector3 rightPunchRightFootPos = new Vector3(0.34f, -1.478f, 0.137f);
    public Vector3 rightPunchLeftFootPos = new Vector3(-0.261f, 0.087f, -0.723f);
    
    [Header("Right Punch - Pole Position")]
    public Vector3 rightPunchRightElbowPole = new Vector3(0.6775743f, 1.276582f, 0.5623819f);
    public Vector3 rightPunchLeftElbowPole = new Vector3(-0.781f, 0.842f, 0.1623819f);
    public Vector3 rightPunchRightKneePole = new Vector3(0.08207811f, 0.054f, 2.3f);
    public Vector3 rightPunchLeftKneePole = new Vector3(-0.261f, 0.087f, -0.723f);
    
    [Header("Left Punch - Target Position")]
    public Vector3 leftPunchLeftHandPos = new Vector3(-0.054f, 1.476582f, 1.453f);
    public Vector3 leftPunchRightHandPos = new Vector3(0.058f, 1.476582f, 0.253f);
    public Vector3 leftPunchLeftFootPos = new Vector3(-0.018f, -4.31f, 0.682f);
    public Vector3 leftPunchRightFootPos = new Vector3(0.145f, 0.155f, -0.724f);
    
    [Header("Left Punch - Pole Position")]
    public Vector3 leftPunchLeftElbowPole = new Vector3(-0.537f, 1.264f, 0.1623819f);
    public Vector3 leftPunchRightElbowPole = new Vector3(0.494f, 0.978f, -0.079f);
    public Vector3 leftPunchLeftKneePole = new Vector3(-0.261f, 0.15f, 0.93f);
    public Vector3 leftPunchRightKneePole = new Vector3(0.08207811f, 0.054f, 0.46f);
    
    [Header("High Kick - Target Position")]
    public Vector3 highKickLeftHandPos = new Vector3(-0.842999995f, 0.833000004f, -0.875f);
    public Vector3 highKickRightHandPos = new Vector3(0.0689999983f, 1.53199995f, 0.0520000011f);
    public Vector3 highKickLeftFootPos = new Vector3(-0.0280000009f, 0.25999999f, -0.405000001f);
    public Vector3 highKickRightFootPos = new Vector3(0.144999996f, 1.89600003f, 0.777999997f);
    
    [Header("High Kick - Pole Position")]
    public Vector3 highKickLeftElbowPole = new Vector3(-0.537f, 1.36300004f, -0.714999974f);
    public Vector3 highKickRightElbowPole = new Vector3(1.19500005f, 0.0359999985f, -0.36500001f);
    public Vector3 highKickRightKneePole = new Vector3(0.0820781067f, 1.89999998f, 0.448000014f);
    
    [Header("Transition Settings")]
    [Range(0.5f, 5f)]
    public float transitionSpeed = 2f;
    
    [Header("Hand Movement - Resting Position")]
    public Vector3 relaxLeftOffset = new Vector3(-0.25f, -0.3f, -0.05548593f);
    public Vector3 relaxRightOffset = new Vector3(0.25f, -0.3f, -0.05548593f);
    
    private Vector3 tPoseLeftHand = new Vector3(-0.713f, 1.440f, -0.055f);
    private Vector3 tPoseRightHand = new Vector3(0.713f, 1.440f, -0.055f);
    private Vector3 tPoseLeftFoot = new Vector3(-0.082f, 0.087f, -0.027f);
    private Vector3 tPoseRightFoot = new Vector3(0.082f, 0.087f, -0.027f);
    
    private Vector3 leftKneePoleBase = new Vector3(-0.08207811f, 0.4314919f, 1f);
    private Vector3 rightKneePoleBase = new Vector3(0.08207811f, 0.4314919f, 1f);
    
    private PoseState currentState = PoseState.TPose;
    private PoseState targetState = PoseState.TPose;
    private float progress = 0f;
    private bool isTransitioning = false;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerNextTransition();
        }
        
        if (isTransitioning)
        {
            progress += Time.deltaTime * transitionSpeed;
            
            if (progress >= 1f)
            {
                progress = 1f;
                currentState = targetState;
                isTransitioning = false;
                Debug.Log($"Transition complete, current state: {currentState}");
            }
        }
        
        UpdateTargets();
    }
    
    void TriggerNextTransition()
    {
        if (isTransitioning) return;
        
        switch (currentState)
        {
            case PoseState.TPose:
                targetState = PoseState.Guard;
                isTransitioning = true;
                progress = 0f;
                Debug.Log("Starting transition: T-pose -> Guard stance");
                break;
                
            case PoseState.Guard:
                targetState = PoseState.RightPunch;
                isTransitioning = true;
                progress = 0f;
                Debug.Log("Starting transition: Guard -> Right punch");
                break;
                
            case PoseState.RightPunch:
                targetState = PoseState.LeftPunch;
                isTransitioning = true;
                progress = 0f;
                Debug.Log("Starting transition: Right punch -> Left punch");
                break;
                
            case PoseState.LeftPunch:
                targetState = PoseState.HighKick;
                isTransitioning = true;
                progress = 0f;
                Debug.Log("Starting transition: Left punch -> High kick");
                break;
                
            case PoseState.HighKick:
                Debug.Log("Already in high kick state, demo ended");
                break;
        }
    }
    
    void UpdateTargets()
    {
        if (!isTransitioning)
        {
            ApplyPoseState(currentState, 1f);
        }
        else
        {
            TransitionBetweenStates(currentState, targetState, progress);
        }
        
        UpdatePoleTargets();
    }
    
    void TransitionBetweenStates(PoseState from, PoseState to, float t)
    {
        Vector3 fromLeftHand, fromRightHand, fromLeftFoot, fromRightFoot, fromHead;
        Vector3 toLeftHand, toRightHand, toLeftFoot, toRightFoot, toHead;
        
        GetPosePositions(from, out fromLeftHand, out fromRightHand, out fromLeftFoot, out fromRightFoot, out fromHead);
        GetPosePositions(to, out toLeftHand, out toRightHand, out toLeftFoot, out toRightFoot, out toHead);
        
        if (from == PoseState.TPose && to == PoseState.Guard)
        {
            Vector3 relaxLeft = GetRelaxedHandPosition(true);
            Vector3 relaxRight = GetRelaxedHandPosition(false);
            
            if (leftHandTarget != null)
                leftHandTarget.position = CalculateBezierPoint(fromLeftHand, relaxLeft, toLeftHand, t);
            
            if (rightHandTarget != null)
                rightHandTarget.position = CalculateBezierPoint(fromRightHand, relaxRight, toRightHand, t);
            
            float footT = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.5f) * 2f));
            if (leftFootTarget != null)
                leftFootTarget.position = Vector3.Lerp(fromLeftFoot, toLeftFoot, footT);
            if (rightFootTarget != null)
                rightFootTarget.position = Vector3.Lerp(fromRightFoot, toRightFoot, footT);
            
            if (headTarget != null)
                headTarget.position = Vector3.Lerp(fromHead, toHead, t);
        }
        else
        {
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            if (leftHandTarget != null)
                leftHandTarget.position = Vector3.Lerp(fromLeftHand, toLeftHand, smoothT);
            if (rightHandTarget != null)
                rightHandTarget.position = Vector3.Lerp(fromRightHand, toRightHand, smoothT);
            if (leftFootTarget != null)
                leftFootTarget.position = Vector3.Lerp(fromLeftFoot, toLeftFoot, smoothT);
            if (rightFootTarget != null)
                rightFootTarget.position = Vector3.Lerp(fromRightFoot, toRightFoot, smoothT);
            if (headTarget != null)
                headTarget.position = Vector3.Lerp(fromHead, toHead, smoothT);
        }
    }
    
    void ApplyPoseState(PoseState state, float blend)
    {
        Vector3 leftHand, rightHand, leftFoot, rightFoot, head;
        GetPosePositions(state, out leftHand, out rightHand, out leftFoot, out rightFoot, out head);
        
        if (leftHandTarget != null)
            leftHandTarget.position = leftHand;
        if (rightHandTarget != null)
            rightHandTarget.position = rightHand;
        if (leftFootTarget != null)
            leftFootTarget.position = leftFoot;
        if (rightFootTarget != null)
            rightFootTarget.position = rightFoot;
        if (headTarget != null)
            headTarget.position = head;
    }
    
    void GetPosePositions(PoseState state, out Vector3 leftHand, out Vector3 rightHand, 
                          out Vector3 leftFoot, out Vector3 rightFoot, out Vector3 head)
    {
        switch (state)
        {
            case PoseState.TPose:
                leftHand = tPoseLeftHand;
                rightHand = tPoseRightHand;
                leftFoot = tPoseLeftFoot;
                rightFoot = tPoseRightFoot;
                head = headStartPos;
                break;
                
            case PoseState.Guard:
                leftHand = GetGuardHandPosition(true);
                rightHand = GetGuardHandPosition(false);
                leftFoot = guardLeftFoot;
                rightFoot = guardRightFoot;
                head = headGuardPos;
                break;
                
            case PoseState.RightPunch:
                leftHand = rightPunchLeftHandPos;
                rightHand = rightPunchRightHandPos;
                leftFoot = rightPunchLeftFootPos;
                rightFoot = rightPunchRightFootPos;
                head = headRightPunchPos;
                break;
                
            case PoseState.LeftPunch:
                leftHand = leftPunchLeftHandPos;
                rightHand = leftPunchRightHandPos;
                leftFoot = leftPunchLeftFootPos;
                rightFoot = leftPunchRightFootPos;
                head = headLeftPunchPos;
                break;
                
            case PoseState.HighKick:
                leftHand = highKickLeftHandPos;
                rightHand = highKickRightHandPos;
                leftFoot = highKickLeftFootPos;
                rightFoot = highKickRightFootPos;
                head = headHighKickPos;
                break;
                
            default:
                leftHand = tPoseLeftHand;
                rightHand = tPoseRightHand;
                leftFoot = tPoseLeftFoot;
                rightFoot = tPoseRightFoot;
                head = headStartPos;
                break;
        }
    }
    
    void UpdatePoleTargets()
    {
        if (leftElbowPole != null)
        {
            Vector3 polePos = GetArmPolePosition(currentState, true);
            
            if (isTransitioning)
            {
                Vector3 fromPole = GetArmPolePosition(currentState, true);
                Vector3 toPole = GetArmPolePosition(targetState, true);
                polePos = Vector3.Lerp(fromPole, toPole, progress);
            }
            
            leftElbowPole.position = polePos;
        }
        
        if (rightElbowPole != null)
        {
            Vector3 polePos = GetArmPolePosition(currentState, false);
            
            if (isTransitioning)
            {
                Vector3 fromPole = GetArmPolePosition(currentState, false);
                Vector3 toPole = GetArmPolePosition(targetState, false);
                polePos = Vector3.Lerp(fromPole, toPole, progress);
            }
            
            rightElbowPole.position = polePos;
        }
        
        if (leftKneePole != null)
        {
            Vector3 polePos = GetLegPolePosition(currentState, true);
            
            if (isTransitioning)
            {
                Vector3 fromPole = GetLegPolePosition(currentState, true);
                Vector3 toPole = GetLegPolePosition(targetState, true);
                polePos = Vector3.Lerp(fromPole, toPole, progress);
            }
            
            leftKneePole.position = polePos;
        }
        
        if (rightKneePole != null)
        {
            Vector3 polePos = GetLegPolePosition(currentState, false);
            
            if (isTransitioning)
            {
                Vector3 fromPole = GetLegPolePosition(currentState, false);
                Vector3 toPole = GetLegPolePosition(targetState, false);
                polePos = Vector3.Lerp(fromPole, toPole, progress);
            }
            
            rightKneePole.position = polePos;
        }
    }
    
    Vector3 GetArmPolePosition(PoseState state, bool isLeft)
    {
        if (state == PoseState.TPose)
        {
            Vector3 handPos = isLeft ? tPoseLeftHand : tPoseRightHand;
            return handPos + (isLeft ? 
                new Vector3(-0.4f, 0f, -0.2f) : 
                new Vector3(0.4f, 0f, -0.2f));
        }
        else if (state == PoseState.Guard)
        {
            Vector3 handPos = isLeft ? GetGuardHandPosition(true) : GetGuardHandPosition(false);
            return handPos + (isLeft ? 
                new Vector3(-0.2f, -0.6f, -0.6f) : 
                new Vector3(0.4f, -0.3f, 0.1f));
        }
        else if (state == PoseState.RightPunch)
        {
            return isLeft ? rightPunchLeftElbowPole : rightPunchRightElbowPole;
        }
        else if (state == PoseState.LeftPunch)
        {
            return isLeft ? leftPunchLeftElbowPole : leftPunchRightElbowPole;
        }
        else
        {
            return isLeft ? highKickLeftElbowPole : highKickRightElbowPole;
        }
    }
    
    Vector3 GetLegPolePosition(PoseState state, bool isLeft)
    {
        if (state == PoseState.RightPunch)
        {
            return isLeft ? rightPunchLeftKneePole : rightPunchRightKneePole;
        }
        else if (state == PoseState.LeftPunch)
        {
            return isLeft ? leftPunchLeftKneePole : leftPunchRightKneePole;
        }
        else if (state == PoseState.HighKick)
        {
            return isLeft ? leftKneePoleBase : highKickRightKneePole;
        }
        
        return isLeft ? leftKneePoleBase : rightKneePoleBase;
    }
    
    Vector3 CalculateBezierPoint(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float u = 1 - t;
        return u * u * start + 2 * u * t * control + t * t * end;
    }
    
    Vector3 GetRelaxedHandPosition(bool isLeft)
    {
        if (hipsBone != null)
        {
            Vector3 offset = isLeft ? relaxLeftOffset : relaxRightOffset;
            return hipsBone.position + offset;
        }
        return isLeft ? new Vector3(-0.25f, 0.9f, 0f) : new Vector3(0.25f, 0.9f, 0f);
    }
    
    Vector3 GetGuardHandPosition(bool isLeft)
    {
        if (headBone != null)
        {
            Vector3 offset = isLeft ? guardLeftHandOffset : guardRightHandOffset;
            return headBone.position + offset;
        }
        return isLeft ? new Vector3(-0.1f, 1.45f, 0.2f) : new Vector3(0.1f, 1.45f, 0f);
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 30), "Press Space to switch pose");
        GUI.Label(new Rect(10, 40, 300, 30), $"Current state: {currentState}");
        if (isTransitioning)
            GUI.Label(new Rect(10, 70, 300, 30), $"Transitioning to: {targetState} ({(progress * 100):F0}%)");
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (leftElbowPole != null && leftHandTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(leftElbowPole.position, 0.06f);
            Gizmos.DrawLine(leftHandTarget.position, leftElbowPole.position);
        }
        
        if (rightElbowPole != null && rightHandTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(rightElbowPole.position, 0.06f);
            Gizmos.DrawLine(rightHandTarget.position, rightElbowPole.position);
        }
        
        if (leftKneePole != null && leftFootTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftKneePole.position, 0.06f);
            Gizmos.DrawLine(leftFootTarget.position, leftKneePole.position);
        }
        
        if (rightKneePole != null && rightFootTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rightKneePole.position, 0.06f);
            Gizmos.DrawLine(rightFootTarget.position, rightKneePole.position);
        }
    }
}
