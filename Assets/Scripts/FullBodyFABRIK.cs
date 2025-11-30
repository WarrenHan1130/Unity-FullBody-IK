using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using System.Linq;

public class FullBodyFABRIK : MonoBehaviour
{
    #region Data Structures
    
    public class Bone
    {
        public Transform transform;
        public Bone parent;
        public List<Bone> children = new List<Bone>();
        
        [HideInInspector] public Vector3 position;
        [HideInInspector] public Quaternion rotation;
        [HideInInspector] public float length;
        [HideInInspector] public Quaternion lastRotation;

        [HideInInspector] public Vector3 posAccum;
        [HideInInspector] public int posCount;
        [HideInInspector] public Vector3 preIterationPos;
        
        [HideInInspector] public Vector3 initialLocalDirection;
        [HideInInspector] public Vector3 initialWorldDirection;

        public bool isRoot => parent == null;
        public bool isEndEffector => children.Count == 0;
    }

    [System.Serializable]
    public class EndEffector
    {
        public string name;
        public Transform bone;
        public Transform target;
        
        [Header("Pole Target (Optional)")]
        public Transform poleTarget;
        [Range(0f, 1f)]
        public float poleWeight = 1f;

        public int poleBoneIndex = -1;

        [Header("Chain Settings")]
        public int chainLength = 0;
        
        [Header("IK Settings")]
        [Range(0f, 1f)]
        public float weight = 1f;
        public int priority = 0;
        public bool enabled = true;

        [HideInInspector] public Bone boneNode;
        [HideInInspector] public List<Bone> chain;
        [HideInInspector] public float lastError = float.MaxValue;
    }

    public enum ConstraintType
    {
        None,
        Hinge,
        Ball
    }

    [System.Serializable]
    public class JointConstraint
    {
        public ConstraintType type = ConstraintType.Hinge;
        public Vector3 hingeAxis = Vector3.right;
        public float minAngle = 0f;
        public float maxAngle = 180f;
        
        [Range(0f, 1f)]
        public float strength = 1f;
        
        public bool enabled = true;
        
        [HideInInspector] public Vector3 hingeAxisWorld;
    }

    [System.Serializable]
    public class BoneConstraintPair
    {
        public string boneName;
        public JointConstraint constraint;
    }

    #endregion

    #region Configuration Parameters

    [Header("Skeleton")]
    public Transform rootBone;
    public RootPinMode rootPinMode = RootPinMode.InitialPose;
    [HideInInspector, FormerlySerializedAs("pinRoot")]
    [SerializeField] private bool pinRootLegacy = true;

    [Header("End Effectors")]
    public List<EndEffector> endEffectors = new List<EndEffector>();

    [Header("Solver Settings")]
    [Range(1, 30)] public int mainIterations = 10;
    [Range(1, 10)] public int subIterations = 3;
    public float tolerance = 0.01f;
    [Range(0f, 1f)] public float globalWeight = 1f;
    
    [Header("Constraint Settings")]
    public ConstraintMode constraintMode = ConstraintMode.AfterConvergence;
    [Range(1, 10)]
    public int constraintPasses = 2;
    
    public enum ConstraintMode
    {
        Disabled,
        AfterConvergence,
        PostIteration
    }

    public enum RootPinMode
    {
        InitialPose,
        CurrentPose,
        Free
    }
    
    [Header("Stabilization")]
    [Range(0f, 0.8f)]
    public float rotationDamping = 0.15f;
    
    [Range(0f, 180f)]
    public float maxRotationSpeed = 120f;

    [Header("Constraints")]
    public List<BoneConstraintPair> boneConstraints = new List<BoneConstraintPair>();

    [Header("Debug")]
    public bool showGizmos = true;
    public bool showBoneHierarchy = false;
    public bool showPoleTargets = true;
    public bool showConstraints = true;
    public bool logIterations = false;
    public bool logConstraints = true;

    #endregion

    #region Internal Data

    private Bone rootNode;
    private Dictionary<Transform, Bone> boneMap = new Dictionary<Transform, Bone>();
    private Dictionary<Transform, JointConstraint> constraintMap = new Dictionary<Transform, JointConstraint>();
    private Vector3 rootPinnedPosition;
    private Vector3 initialRootPosition;
    private bool legacyPinModeSynchronized = false;
    private bool isInitialized = false;

    private const float EPSILON = 1e-6f;

    #endregion

    #region Initialization

    void Start() => Initialize();

    public void Initialize()
    {
        SyncLegacyPinMode();
        
        if (rootBone == null)
        {
            Debug.LogError("[FullBodyFABRIK] Root Bone not set!");
            return;
        }

        boneMap.Clear();
        constraintMap.Clear();
        isInitialized = false;

        rootNode = BuildSkeletonTree(rootBone, null);
        initialRootPosition = rootBone.position;
        rootPinnedPosition = initialRootPosition;

        CalculateInitialDirections(rootNode);

        foreach (var effector in endEffectors)
        {
            if (effector.bone == null) continue;

            if (boneMap.TryGetValue(effector.bone, out Bone boneNode))
            {
                effector.boneNode = boneNode;
                
                if (effector.chainLength > 0)
                    effector.chain = GetChainWithLength(boneNode, effector.chainLength);
                else
                    effector.chain = GetChainToRoot(boneNode);
                
                Debug.Log($"[Initialize] Effector '{effector.name}' chain length: {effector.chain.Count}");
            }
            else
            {
                Debug.LogError($"[Initialize] Cannot find effector bone: {effector.bone.name}");
            }
        }

        foreach (var pair in boneConstraints)
        {
            if (string.IsNullOrEmpty(pair.boneName)) continue;

            Transform t = FindTransformByName(rootBone, pair.boneName);
            if (t == null) 
            {
                Debug.LogWarning($"[Constraint] Cannot find bone: {pair.boneName}");
                continue;
            }

            var constraint = new JointConstraint
            {
                type = pair.constraint.type,
                hingeAxis = pair.constraint.hingeAxis.normalized,
                minAngle = pair.constraint.minAngle,
                maxAngle = pair.constraint.maxAngle,
                strength = pair.constraint.strength,
                enabled = pair.constraint.enabled
            };
            
            if (constraint.type == ConstraintType.Hinge)
            {
                constraint.hingeAxisWorld = t.TransformDirection(constraint.hingeAxis).normalized;
                
                if (logConstraints)
                {
                    Debug.Log($"[Initialize Hinge Axis] {t.name}:");
                    Debug.Log($"  Local axis: {constraint.hingeAxis}");
                    Debug.Log($"  World axis: {constraint.hingeAxisWorld}");
                }
            }
            
            constraintMap[t] = constraint;
            
            Debug.Log($"[Constraint] Registered: {t.name}, Type: {constraint.type}, Strength: {constraint.strength}");
        }

        CalculateBoneLengths(rootNode);
        InitializeLastRotations(rootNode);
        
        isInitialized = true;
        Debug.Log($"[Initialize] Complete! Bone count: {boneMap.Count}, Constraint count: {constraintMap.Count}");
    }

    #endregion

    #region Main Solver Loop

    void LateUpdate()
    {
        if (!isInitialized || endEffectors.Count == 0) return;

        UpdateRootPinPosition();
        CopyPosesRecursive(rootNode);

        var sortedEffectors = endEffectors
            .Where(e => e.enabled && e.target != null && e.boneNode != null)
            .OrderByDescending(e => e.priority)
            .ToList();

        if (sortedEffectors.Count == 0) return;

        if (logIterations) Debug.Log("========== Step 1: FABRIK Solving ==========");
        
        bool converged = false;
        int convergenceIter = -1;
        
        for (int mainIter = 0; mainIter < mainIterations; mainIter++)
        {
            ResetAccumulationFlags();
            
            bool iterConverged = true;
            float maxErrorChange = 0f;

            foreach (var eff in sortedEffectors)
            {
                float err = SolveEffector(eff);
                
                float errorChange = Mathf.Abs(err - eff.lastError);
                maxErrorChange = Mathf.Max(maxErrorChange, errorChange);
                eff.lastError = err;
                
                if (err > tolerance) iterConverged = false;
            }

            AverageAccumulatedPositions();

            if (iterConverged || maxErrorChange < 0.001f)
            {
                converged = true;
                convergenceIter = mainIter + 1;
                break;
            }
        }

        foreach (var eff in sortedEffectors)
        {
            if (eff.poleTarget != null && eff.poleWeight > 0f)
            {
                ApplyPoleTarget(eff);
            }
        }

        if (logIterations)
            Debug.Log($"[FABRIK] Converged: {converged}, Iterations: {convergenceIter}/{mainIterations}");

        if (constraintMode == ConstraintMode.AfterConvergence)
        {
            if (logConstraints) Debug.Log("========== Step 2: Applying Constraints ==========");
            ApplyAllConstraintsFinal();
            if (logConstraints) Debug.Log("========== Constraints Applied ==========");
        }
        

        if (logIterations) Debug.Log("========== Step 3: Applying Rotations to Unity ==========");
        ApplyPosesToUnity();
        
        SyncPositionsFromTransforms();
    }

    void UpdateRootPinPosition()
    {
        switch (rootPinMode)
        {
            case RootPinMode.InitialPose:
                rootPinnedPosition = initialRootPosition;
                break;
            case RootPinMode.CurrentPose:
                rootPinnedPosition = rootBone.position;
                break;
            case RootPinMode.Free:
                break;
        }
    }

    void ResetAccumulationFlags()
    {
        foreach (var b in boneMap.Values)
        {
            b.preIterationPos = b.position;
        }
    }

    void CopyPosesRecursive(Bone b)
    {
        b.position = b.transform.position;
        b.rotation = b.transform.rotation;
        b.posAccum = Vector3.zero;
        b.posCount = 0;
        
        foreach (var c in b.children)
            CopyPosesRecursive(c);
    }

    float SolveEffector(EndEffector e)
    {
        if (e.chain == null || e.chain.Count < 2) return float.MaxValue;

        for (int subIter = 0; subIter < subIterations; subIter++)
        {
            SolveChainBackward(e);
            SolveChainForward(e);
        }

        return Vector3.Distance(e.boneNode.position, e.target.position);
    }

    void SolveChainBackward(EndEffector e)
    {
        var chain = e.chain;
        chain[chain.Count - 1].position = e.target.position;

        for (int i = chain.Count - 2; i >= 0; i--)
        {
            Bone current = chain[i];
            Bone child = chain[i + 1];

            Vector3 dir = (current.position - child.position).normalized;
            current.position = child.position + dir * child.length;
        }
    }

    void SolveChainForward(EndEffector e)
    {
        var chain = e.chain;
        Bone root = chain[0];

        if (root == rootNode && rootPinMode != RootPinMode.Free)
        {
            root.position = rootPinnedPosition;
        }

        for (int i = 1; i < chain.Count; i++)
        {
            Bone parent = chain[i - 1];
            Bone child = chain[i];

            Vector3 dir = (child.position - parent.position).normalized;
            Vector3 newPos = parent.position + dir * child.length;

            child.posAccum += newPos;
            child.posCount++;
        }
    }

    void ApplyPoleTarget(EndEffector e)
    {
        Debug.Log($"[Pole] Starting execution for {e.name}");
        var chain = e.chain;
        if (chain.Count < 3) return;

        int midIdx;
        if (e.poleBoneIndex >= 0)
        {
            midIdx = Mathf.Clamp(e.poleBoneIndex, 1, chain.Count - 2);
        }
        else
        {
            midIdx = chain.Count / 2;
        }
        
        Bone startBone = chain[0];
        Bone midBone = chain[midIdx];
        Bone endBone = chain[chain.Count - 1];

        Vector3 startPos = startBone.position;
        Vector3 endPos = endBone.position;

        float currentDistance = Vector3.Distance(startPos, endPos);
        float maxDistance = 0f;
        for (int i = 1; i < chain.Count; i++)
        {
            maxDistance += chain[i].length;
        }
        
        float stretchRatio = currentDistance / maxDistance;

        Debug.Log($"[Pole] {e.name} stretch ratio {stretchRatio:F2}");
        
        if (stretchRatio > 0.90f)
        {
            return;
        }

        Vector3 toEnd = endPos - startPos;
        Vector3 toMid = midBone.position - startPos;
        Vector3 currentNormal = Vector3.Cross(toEnd, toMid).normalized;

        Vector3 toPole = e.poleTarget.position - startPos;
        Vector3 targetNormal = Vector3.Cross(toEnd, toPole).normalized;

        if (currentNormal.sqrMagnitude < 0.01f || targetNormal.sqrMagnitude < 0.01f)
            return;

        Quaternion poleRotation = Quaternion.FromToRotation(currentNormal, targetNormal);
        float angle = Vector3.Angle(currentNormal, targetNormal);
        
        Debug.Log($"[Pole] {e.name} rotation angle: {angle:F1}°");

        for (int i = 1; i < chain.Count; i++)
        {
            Bone bone = chain[i];
            Vector3 offset = bone.position - startPos;
            Vector3 rotatedOffset = Vector3.Slerp(offset, poleRotation * offset, e.poleWeight);
            bone.position = startPos + rotatedOffset;
        }

        for (int i = 1; i < chain.Count; i++)
        {
            Bone parent = chain[i - 1];
            Bone child = chain[i];
            Vector3 dir = (child.position - parent.position).normalized;
            child.position = parent.position + dir * child.length;
        }
    }

    #endregion

    #region Constraint System

    void ApplyAllConstraintsFinal()
    {
        if (constraintMode == ConstraintMode.Disabled) 
        {
            if (logConstraints) Debug.Log("[Constraint] Constraint mode is disabled");
            return;
        }

        int totalApplied = 0;
        
        for (int pass = 0; pass < constraintPasses; pass++)
        {
            int passApplied = 0;

            foreach (var kv in constraintMap)
            {
                Transform t = kv.Key;
                JointConstraint con = kv.Value;

                if (!con.enabled) continue;

                if (boneMap.TryGetValue(t, out Bone bone))
                {
                    if (ApplyConstraintToJoint(bone, con))
                    {
                        passApplied++;
                    }
                }
            }

            totalApplied += passApplied;
            
            if (logConstraints) 
                Debug.Log($"[Constraint] Pass {pass + 1}/{constraintPasses}: Applied {passApplied} constraints");
            
            if (passApplied == 0) break;
        }

        if (logConstraints)
            Debug.Log($"[Constraint] Complete: Total {totalApplied} rotation adjustments");
    }

    bool ApplyConstraintToJoint(Bone j, JointConstraint con)
    {
        if (j.children.Count == 0) return false;

        bool anyChanged = false;

        foreach (var c in j.children)
        {
            Vector3 initialWorldDir = c.initialWorldDirection;
            Vector3 currentDir = (c.position - j.position).normalized;

            if (currentDir.sqrMagnitude < EPSILON) continue;

            bool wasConstrained = false;
            Quaternion correctionRotation = Quaternion.identity;

            switch (con.type)
            {
                case ConstraintType.Hinge:
                    (correctionRotation, wasConstrained) = ApplyHingeConstraint(j, c, initialWorldDir, currentDir, con);
                    break;
                case ConstraintType.Ball:
                    (correctionRotation, wasConstrained) = ApplyBallConstraint(j, c, initialWorldDir, currentDir, con);
                    break;
            }

            if (wasConstrained)
            {
                Quaternion finalRotation = Quaternion.Slerp(
                    Quaternion.identity, 
                    correctionRotation, 
                    con.strength
                );
                
                j.rotation = finalRotation * j.rotation;
                
                Vector3 constrainedDir = finalRotation * currentDir;
                c.position = j.position + constrainedDir * c.length;
                
                RebuildChildPositions(c);
                
                anyChanged = true;
                
                if (logConstraints)
                {
                    float angle = Quaternion.Angle(Quaternion.identity, correctionRotation);
                    Debug.Log($"[Constraint] {j.transform.name}->{c.transform.name}: Rotation correction {angle:F1}°, position updated");
                }
            }
        }

        return anyChanged;
    }

    void RebuildChildPositions(Bone bone)
    {
        foreach (var child in bone.children)
        {
            Vector3 dir = (child.position - bone.position).normalized;
            if (dir.sqrMagnitude < EPSILON)
                dir = bone.transform.TransformDirection(child.initialLocalDirection);
            
            child.position = bone.position + dir * child.length;
            
            RebuildChildPositions(child);
        }
    }

    (Quaternion, bool) ApplyHingeConstraint(Bone j, Bone c, Vector3 initialDir, Vector3 currentDir, JointConstraint con)
    {
        Vector3 axis = con.hingeAxisWorld;
        
        if (axis.sqrMagnitude < 0.9f)
        {
            if (logConstraints) Debug.LogWarning($"[Constraint] Invalid hinge axis");
            return (Quaternion.identity, false);
        }

        Vector3 initialProj = Vector3.ProjectOnPlane(initialDir, axis).normalized;
        Vector3 currentProj = Vector3.ProjectOnPlane(currentDir, axis).normalized;

        if (initialProj.sqrMagnitude < 0.01f || currentProj.sqrMagnitude < 0.01f)
        {
            if (logConstraints) Debug.LogWarning($"[Constraint] Direction too small after projection");
            return (Quaternion.identity, false);
        }

        float angle = Vector3.SignedAngle(initialProj, currentProj, axis);
        
        if (logConstraints) 
            Debug.Log($"[Constraint] Hinge {j.transform.name}->{c.transform.name}: angle={angle:F1}°, limit=[{con.minAngle:F1}, {con.maxAngle:F1}]");

        if (angle >= con.minAngle && angle <= con.maxAngle)
            return (Quaternion.identity, false);

        float clampedAngle = Mathf.Clamp(angle, con.minAngle, con.maxAngle);
        float correctionAngle = clampedAngle - angle;
        
        Quaternion correction = Quaternion.AngleAxis(correctionAngle, axis);

        if (logConstraints) 
            Debug.Log($"[Constraint] Hinge correction: {angle:F1}° -> {clampedAngle:F1}° (corrected {Mathf.Abs(correctionAngle):F1}°)");

        return (correction, true);
    }

    (Quaternion, bool) ApplyBallConstraint(Bone j, Bone c, Vector3 initialDir, Vector3 currentDir, JointConstraint con)
    {
        float angle = Vector3.Angle(initialDir, currentDir);

        if (logConstraints)
            Debug.Log($"[Constraint] Ball {j.transform.name}->{c.transform.name}: angle={angle:F1}°, limit=[{con.minAngle:F1}, {con.maxAngle:F1}]");

        if (angle >= con.minAngle && angle <= con.maxAngle)
            return (Quaternion.identity, false);

        float clampedAngle = Mathf.Clamp(angle, con.minAngle, con.maxAngle);
        float correctionAngle = clampedAngle - angle;

        Vector3 axis = Vector3.Cross(initialDir, currentDir).normalized;
        if (axis.sqrMagnitude < EPSILON)
            axis = Vector3.up;

        Quaternion correction = Quaternion.AngleAxis(correctionAngle, axis);

        if (logConstraints) 
            Debug.Log($"[Constraint] Ball correction: {angle:F1}° -> {clampedAngle:F1}° (corrected {Mathf.Abs(correctionAngle):F1}°)");

        return (correction, true);
    }

    #endregion

    #region Initialization Helpers

    void SyncLegacyPinMode()
    {
        if (legacyPinModeSynchronized) return;

        legacyPinModeSynchronized = true;
        if (!pinRootLegacy && rootPinMode == RootPinMode.InitialPose)
            rootPinMode = RootPinMode.Free;
    }

    Bone BuildSkeletonTree(Transform t, Bone parent)
    {
        Bone b = new Bone
        {
            transform = t,
            parent = parent,
            position = t.position,
            rotation = t.rotation,
            lastRotation = t.rotation,
            posAccum = Vector3.zero,
            posCount = 0,
            preIterationPos = t.position
        };
        boneMap[t] = b;
        foreach (Transform c in t)
            b.children.Add(BuildSkeletonTree(c, b));
        return b;
    }

    void CalculateInitialDirections(Bone bone)
    {
        foreach (var child in bone.children)
        {
            Vector3 worldDir = child.transform.position - bone.transform.position;

            if (worldDir.sqrMagnitude < EPSILON)
            {
                Vector3 refAxis = Mathf.Abs(Vector3.Dot(bone.transform.up, Vector3.up)) > 0.95f
                    ? bone.transform.right
                    : bone.transform.up;

                worldDir = Vector3.Cross(bone.transform.forward, refAxis);
            }

            worldDir = worldDir.normalized;

            child.initialLocalDirection = bone.transform.InverseTransformDirection(worldDir);
            child.initialWorldDirection = worldDir;

            if (logConstraints)
            {
                Debug.Log($"[Initialize Direction] {bone.transform.name}->{child.transform.name}:");
                Debug.Log($"  Local direction: {child.initialLocalDirection}");
                Debug.Log($"  World direction: {child.initialWorldDirection}");
            }

            CalculateInitialDirections(child);
        }
    }

    void CalculateBoneLengths(Bone bone)
    {
        if (bone.parent != null)
            bone.length = Vector3.Distance(bone.transform.position, bone.parent.transform.position);
        foreach (var c in bone.children)
            CalculateBoneLengths(c);
    }

    List<Bone> GetChainToRoot(Bone endBone)
    {
        List<Bone> chain = new List<Bone>();
        Bone current = endBone;
        while (current != null)
        {
            chain.Add(current);
            current = current.parent;
        }
        chain.Reverse();
        return chain;
    }
    
    List<Bone> GetChainWithLength(Bone endBone, int length)
    {
        List<Bone> chain = new List<Bone>();
        Bone current = endBone;
        int count = 0;
        
        while (current != null && count < length)
        {
            chain.Add(current);
            current = current.parent;
            count++;
        }
        
        chain.Reverse();
        return chain;
    }
    
    void InitializeLastRotations(Bone bone)
    {
        bone.lastRotation = bone.transform.rotation;
        foreach (var c in bone.children)
            InitializeLastRotations(c);
    }

    Transform FindTransformByName(Transform root, string name)
    {
        if (root.name == name) return root;
        
        foreach (Transform child in root)
        {
            Transform result = FindTransformByName(child, name);
            if (result != null) return result;
        }
        
        return null;
    }

    #endregion

    #region Apply to Unity

    void AverageAccumulatedPositions()
    {
        foreach (var b in boneMap.Values)
        {
            if (b.posCount > 0)
            {
                b.position = b.posAccum / b.posCount;
                b.posAccum = Vector3.zero;
                b.posCount = 0;
            }
        }
    }

    void ApplyPosesToUnity()
    {
        var boneRotations = new Dictionary<Bone, List<(Quaternion rot, float w)>>();

        foreach (var e in endEffectors)
        {
            if (!e.enabled || e.chain == null || e.chain.Count < 2) 
                continue;

            float w = Mathf.Clamp01(e.weight);
            var chain = e.chain;

            for (int i = 1; i < chain.Count; i++)
            {
                Bone parent = chain[i - 1];
                Bone child = chain[i];

                Vector3 newDir = (child.position - parent.position).normalized;
                Vector3 oldDir = (child.transform.position - parent.transform.position).normalized;

                if (newDir.sqrMagnitude < EPSILON || oldDir.sqrMagnitude < EPSILON)
                    continue;

                Quaternion delta = Quaternion.FromToRotation(oldDir, newDir);
                Quaternion targetRot = delta * parent.transform.rotation;

                if (!boneRotations.ContainsKey(parent))
                    boneRotations[parent] = new List<(Quaternion, float)>();

                boneRotations[parent].Add((targetRot, w));
            }
        }

        foreach (var kv in boneRotations)
        {
            Bone bone = kv.Key;
            var list = kv.Value;

            float totalW = list.Sum(x => x.w);
            if (totalW < EPSILON) 
                continue;

            Quaternion blended = bone.transform.rotation;

            foreach (var (rot, w) in list)
            {
                blended = Quaternion.Slerp(blended, rot, w / totalW);
            }

            Quaternion targetRotation = Quaternion.Slerp(bone.transform.rotation, blended, globalWeight);

            if (maxRotationSpeed > 0f)
            {
                float maxDelta = maxRotationSpeed * Time.deltaTime;
                float angle = Quaternion.Angle(bone.transform.rotation, targetRotation);
                if (angle > maxDelta)
                {
                    float t = maxDelta / angle;
                    targetRotation = Quaternion.Slerp(bone.transform.rotation, targetRotation, t);
                }
            }

            if (rotationDamping > 0f)
            {
                targetRotation = Quaternion.Slerp(bone.lastRotation, targetRotation, 1f - rotationDamping);
            }

            bone.transform.rotation = targetRotation;
            bone.lastRotation = targetRotation;
        }
    }

    void SyncPositionsFromTransforms()
    {
        foreach (var b in boneMap.Values)
        {
            b.position = b.transform.position;
            b.rotation = b.transform.rotation;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        legacyPinModeSynchronized = false;
        SyncLegacyPinMode();
    }
#endif

    #endregion

    #region Gizmos Debug

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || !isInitialized) return;
        
        if (showBoneHierarchy && rootNode != null) 
            DrawBoneHierarchy(rootNode);

        foreach (var e in endEffectors)
        {
            if (!e.enabled || e.target == null) continue;
            
            Gizmos.color = e.priority > 0 ? Color.cyan : Color.green;
            if (e.boneNode != null)
                Gizmos.DrawSphere(e.boneNode.position, 0.04f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(e.target.position, 0.06f);
            
            if (e.boneNode != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(e.boneNode.position, e.target.position);
            }
            
            if (showPoleTargets && e.poleTarget != null && e.chain != null && e.chain.Count >= 3)
            {
                int midIdx;
                if (e.poleBoneIndex >= 0)
                {
                    midIdx = Mathf.Clamp(e.poleBoneIndex, 1, e.chain.Count - 2);
                }
                else
                {
                    midIdx = e.chain.Count / 2;
                }
                
                Bone mid = e.chain[midIdx];
                
                UnityEditor.Handles.Label(
                    mid.position, 
                    $"Pole affects: {mid.transform.name}\nIndex: {midIdx}/{e.chain.Count-1}\nConfig: {e.poleBoneIndex}"
                );
                
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(mid.position, e.poleTarget.position);
                Gizmos.DrawWireSphere(e.poleTarget.position, 0.05f);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(mid.position, 0.15f);
            }
        }
        
        if (rootPinMode != RootPinMode.Free)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rootPinnedPosition, 0.08f);
        }

        if (showConstraints)
            DrawConstraintGizmos();
    }

    void DrawBoneHierarchy(Bone b)
    {
        foreach (var c in b.children)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(b.position, c.position);
            DrawBoneHierarchy(c);
        }
    }

    void DrawConstraintGizmos()
    {
        foreach (var kv in constraintMap)
        {
            Transform t = kv.Key;
            JointConstraint con = kv.Value;
            
            if (!con.enabled || !boneMap.TryGetValue(t, out Bone bone)) continue;
            
            switch (con.type)
            {
                case ConstraintType.Hinge:
                    foreach (var child in bone.children)
                    {
                        DrawHingeConstraintGizmo(bone, child, con);
                    }
                    break;
                    
                case ConstraintType.Ball:
                {
                    foreach (var child in bone.children)
                        DrawBallConstraintGizmo(bone, child, con);
                    break;
                }
            }
        }
    }

    void DrawHingeConstraintGizmo(Bone j, Bone c, JointConstraint con)
    {
        Vector3 origin = j.position;
        Vector3 axis = con.hingeAxisWorld;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin - axis * 0.6f, origin + axis * 0.6f);
        Gizmos.DrawSphere(origin, 0.05f);
        
        Vector3 initialWorldDir = c.initialWorldDirection;
        Vector3 currentDir = (c.position - j.position).normalized;
        
        Vector3 initialProj = Vector3.ProjectOnPlane(initialWorldDir, axis).normalized;
        Vector3 currentProj = Vector3.ProjectOnPlane(currentDir, axis).normalized;
        
        if (initialProj.sqrMagnitude < 0.01f || currentProj.sqrMagnitude < 0.01f)
            return;
        
        float radius = 0.4f;
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + initialProj * radius);
        Gizmos.DrawSphere(origin + initialProj * radius, 0.05f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + currentProj * radius);
        Gizmos.DrawSphere(origin + currentProj * radius, 0.06f);
        
        Gizmos.color = new Color(0, 0.5f, 1f, 0.4f);
        DrawHingeAngleRange(origin, axis, initialProj, con.minAngle, con.maxAngle, radius);
        
        float currentAngle = Vector3.SignedAngle(initialProj, currentProj, axis);
        
        if (currentAngle < con.minAngle || currentAngle > con.maxAngle)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + currentProj * radius * 1.3f);
            Gizmos.DrawSphere(origin + currentProj * radius * 1.3f, 0.08f);
        }
    }

    void DrawHingeAngleRange(Vector3 origin, Vector3 axis, Vector3 refDir, float minAngle, float maxAngle, float radius)
    {
        int segments = 16;
        float angleRange = maxAngle - minAngle;
        float angleStep = angleRange / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = minAngle + i * angleStep;
            Vector3 dir = Quaternion.AngleAxis(angle, axis) * refDir;
            Gizmos.DrawLine(origin, origin + dir * radius);
        }
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = minAngle + i * angleStep;
            float angle2 = minAngle + (i + 1) * angleStep;
            
            Vector3 dir1 = Quaternion.AngleAxis(angle1, axis) * refDir;
            Vector3 dir2 = Quaternion.AngleAxis(angle2, axis) * refDir;
            
            Gizmos.DrawLine(origin + dir1 * radius, origin + dir2 * radius);
        }
    }

    void DrawBallConstraintGizmo(Bone j, Bone c, JointConstraint con)
    {
        if (con.type != ConstraintType.Ball) return;

        Vector3 initialWorldDir = c.initialWorldDirection;
        Vector3 currentDir = (c.position - j.position).normalized;
        Vector3 origin = j.position;

        Gizmos.color = Color.green;
        Vector3 greenEnd = origin + initialWorldDir * 1.5f;
        Gizmos.DrawLine(origin, greenEnd);
        Gizmos.DrawSphere(greenEnd, 0.05f);

        Gizmos.color = Color.yellow;
        Vector3 yellowEnd = origin + currentDir * 1.0f;
        Gizmos.DrawLine(origin, yellowEnd);
        Gizmos.DrawSphere(yellowEnd, 0.06f);

        Gizmos.color = new Color(0, 0.5f, 1f, 0.25f);
        DrawBallCone(origin, initialWorldDir, con.maxAngle, 0.8f);

        if (con.minAngle > 0)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            DrawBallCone(origin, initialWorldDir, con.minAngle, 0.6f);
        }

        float angle = Vector3.Angle(initialWorldDir, currentDir);
        if (angle > con.maxAngle || angle < con.minAngle)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + currentDir * 1.2f);
            Gizmos.DrawSphere(origin + currentDir * 1.2f, 0.08f);
        }
    }

    void DrawBallCone(Vector3 origin, Vector3 dir, float angle, float radius)
    {
        int segments = 32;

        dir = dir.normalized;

        Vector3 refAxis = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f
            ? Vector3.right
            : Vector3.up;

        Vector3 ortho1 = Vector3.Cross(dir, refAxis).normalized;
        Vector3 ortho2 = Vector3.Cross(dir, ortho1).normalized;

        float rad = angle * Mathf.Deg2Rad;
        float sinA = Mathf.Sin(rad);
        float cosA = Mathf.Cos(rad);

        Vector3[] circle = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float theta = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 rim = (ortho1 * Mathf.Cos(theta) + ortho2 * Mathf.Sin(theta));

            Vector3 point = dir * cosA + rim * sinA;
            point = origin + point.normalized * radius;

            circle[i] = point;

            Gizmos.DrawLine(origin, point);
        }

        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(circle[i], circle[(i + 1) % segments]);
        }
    }
#endif

    #endregion
}
