using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FullBodyIKSetup : MonoBehaviour
{
    public FullBodyFABRIK fullBodyIK;
    
    [Header("Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform headTarget;
    
    [Header("Auto Find Settings")]
    public bool useHumanoidAPI = true;
    
    [Header("Hinge Constraint Settings")]
    [Tooltip("Maximum elbow bend angle")]
    [Range(90f, 170f)]
    public float elbowMaxAngle = 160f;
    
    [Tooltip("Maximum knee bend angle")]
    [Range(90f, 170f)]
    public float kneeMaxAngle = 130f;
    
    [Header("Ball Constraint Settings")]
    [Tooltip("Shoulder (clavicle) max angle")]
    [Range(0f, 30f)]
    public float shoulderMaxAngle = 5f;
    
    [Tooltip("Upper arm max angle")]
    [Range(30f, 120f)]
    public float upperArmMaxAngle = 85f;
    
    [Tooltip("Upper leg (thigh) max angle")]
    [Range(30f, 90f)]
    public float upperLegMaxAngle = 65f;
    
    [Tooltip("Spine/Hips max angle")]
    [Range(10f, 60f)]
    public float spineMaxAngle = 30f;
    
    [Tooltip("Neck max angle")]
    [Range(10f, 60f)]
    public float neckMaxAngle = 30f;
    
    [Tooltip("Head max angle")]
    [Range(10f, 60f)]
    public float headMaxAngle = 30f;
    
    [Tooltip("Foot (ankle) max angle")]
    [Range(5f, 45f)]
    public float footMaxAngle = 15f;
    
    [ContextMenu("Auto Setup")]
    public void AutoSetup()
    {
        if (fullBodyIK == null)
        {
            Debug.LogError("Please assign FullBodyFABRIK component first!");
            return;
        }
        
        Animator animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("Requires Animator component with Humanoid Rig!");
            return;
        }
        
        fullBodyIK.rootBone = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (fullBodyIK.rootBone == null)
        {
            Debug.LogError("Cannot find Hips bone!");
            return;
        }
        
        Debug.Log($"Root Bone: {fullBodyIK.rootBone.name}");
        
        fullBodyIK.endEffectors.Clear();
        int effectorCount = 0;
        
        if (leftHandTarget != null)
        {
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftHand != null)
            {
                fullBodyIK.endEffectors.Add(new FullBodyFABRIK.EndEffector
                {
                    name = "Left Hand",
                    bone = leftHand,
                    target = leftHandTarget,
                    weight = 1f,
                    priority = 1,
                    enabled = true
                });
                effectorCount++;
                Debug.Log($"Added left hand end effector ({leftHand.name})");
            }
            else
            {
                Debug.LogWarning("Cannot find left hand bone");
            }
        }
        
        if (rightHandTarget != null)
        {
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand != null)
            {
                fullBodyIK.endEffectors.Add(new FullBodyFABRIK.EndEffector
                {
                    name = "Right Hand",
                    bone = rightHand,
                    target = rightHandTarget,
                    weight = 1f,
                    priority = 1,
                    enabled = true
                });
                effectorCount++;
                Debug.Log($"Added right hand end effector ({rightHand.name})");
            }
            else
            {
                Debug.LogWarning("Cannot find right hand bone");
            }
        }
        
        if (leftFootTarget != null)
        {
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            if (leftFoot != null)
            {
                fullBodyIK.endEffectors.Add(new FullBodyFABRIK.EndEffector
                {
                    name = "Left Foot",
                    bone = leftFoot,
                    target = leftFootTarget,
                    weight = 1f,
                    priority = 2,
                    enabled = true
                });
                effectorCount++;
                Debug.Log($"Added left foot end effector ({leftFoot.name})");
            }
            else
            {
                Debug.LogWarning("Cannot find left foot bone");
            }
        }
        
        if (rightFootTarget != null)
        {
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (rightFoot != null)
            {
                fullBodyIK.endEffectors.Add(new FullBodyFABRIK.EndEffector
                {
                    name = "Right Foot",
                    bone = rightFoot,
                    target = rightFootTarget,
                    weight = 1f,
                    priority = 2,
                    enabled = true
                });
                effectorCount++;
                Debug.Log($"Added right foot end effector ({rightFoot.name})");
            }
            else
            {
                Debug.LogWarning("Cannot find right foot bone");
            }
        }
        
        if (headTarget != null)
        {
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
            {
                fullBodyIK.endEffectors.Add(new FullBodyFABRIK.EndEffector
                {
                    name = "Head",
                    bone = head,
                    target = headTarget,
                    weight = 1f,
                    priority = 2,
                    enabled = true
                });
                effectorCount++;
                Debug.Log($"Added head end effector ({head.name})");
            }
            else
            {
                Debug.LogWarning("Cannot find head bone");
            }
        }
        
        if (Application.isPlaying)
        {
            fullBodyIK.Initialize();
            Debug.Log("Runtime initialization complete");
        }
        else
        {
            Debug.Log("Editor mode configuration complete, will auto-initialize at runtime");
        }
        
        Debug.Log($"Auto setup complete! {effectorCount} end effectors");
    }
    
    [ContextMenu("Setup Common Constraints")]
    public void SetupCommonConstraints()
    {
        if (fullBodyIK == null)
        {
            Debug.LogError("Please assign FullBodyFABRIK component first!");
            return;
        }
        
        Animator animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("Requires Animator component with Humanoid Rig!");
            return;
        }
        
        fullBodyIK.boneConstraints.Clear();
        int constraintCount = 0;
        
        Transform leftForeArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        if (leftForeArm != null)
        {
            fullBodyIK.boneConstraints.Add(new FullBodyFABRIK.BoneConstraintPair
            {
                boneName = leftForeArm.name,
                constraint = new FullBodyFABRIK.JointConstraint
                {
                    type = FullBodyFABRIK.ConstraintType.Hinge,
                    hingeAxis = -Vector3.forward,
                    minAngle = 0f,
                    maxAngle = elbowMaxAngle,
                    enabled = true
                }
            });
            constraintCount++;
            Debug.Log($"Added constraint: {leftForeArm.name} (left elbow) - hinge axis: (0,0,-1), angle range: 0° ~ {elbowMaxAngle}°");
        }
        else
        {
            Debug.LogWarning("Cannot find left elbow bone");
        }
        
        Transform rightForeArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        if (rightForeArm != null)
        {
            fullBodyIK.boneConstraints.Add(new FullBodyFABRIK.BoneConstraintPair
            {
                boneName = rightForeArm.name,
                constraint = new FullBodyFABRIK.JointConstraint
                {
                    type = FullBodyFABRIK.ConstraintType.Hinge,
                    hingeAxis = Vector3.forward,
                    minAngle = 0f,
                    maxAngle = elbowMaxAngle,
                    enabled = true
                }
            });
            constraintCount++;
            Debug.Log($"Added constraint: {rightForeArm.name} (right elbow) - hinge axis: (0,0,1), angle range: 0° ~ {elbowMaxAngle}°");
        }
        else
        {
            Debug.LogWarning("Cannot find right elbow bone");
        }
        
        Transform leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        if (leftLeg != null)
        {
            fullBodyIK.boneConstraints.Add(new FullBodyFABRIK.BoneConstraintPair
            {
                boneName = leftLeg.name,
                constraint = new FullBodyFABRIK.JointConstraint
                {
                    type = FullBodyFABRIK.ConstraintType.Hinge,
                    hingeAxis = Vector3.right,
                    minAngle = 0f,
                    maxAngle = kneeMaxAngle,
                    enabled = true
                }
            });
            constraintCount++;
            Debug.Log($"Added constraint: {leftLeg.name} (left knee) - hinge axis: (1,0,0), angle range: 0° ~ {kneeMaxAngle}°");
        }
        else
        {
            Debug.LogWarning("Cannot find left knee bone");
        }
        
        Transform rightLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        if (rightLeg != null)
        {
            fullBodyIK.boneConstraints.Add(new FullBodyFABRIK.BoneConstraintPair
            {
                boneName = rightLeg.name,
                constraint = new FullBodyFABRIK.JointConstraint
                {
                    type = FullBodyFABRIK.ConstraintType.Hinge,
                    hingeAxis = Vector3.right,
                    minAngle = 0f,
                    maxAngle = kneeMaxAngle,
                    enabled = true
                }
            });
            constraintCount++;
            Debug.Log($"Added constraint: {rightLeg.name} (right knee) - hinge axis: (1,0,0), angle range: 0° ~ {kneeMaxAngle}°");
        }
        else
        {
            Debug.LogWarning("Cannot find right knee bone");
        }
        
        AddBallConstraint(animator, HumanBodyBones.LeftShoulder, shoulderMaxAngle, "left shoulder", ref constraintCount);
        AddBallConstraint(animator, HumanBodyBones.RightShoulder, shoulderMaxAngle, "right shoulder", ref constraintCount);
        
        AddBallConstraint(animator, HumanBodyBones.LeftUpperArm, upperArmMaxAngle, "left upper arm", ref constraintCount);
        AddBallConstraint(animator, HumanBodyBones.RightUpperArm, upperArmMaxAngle, "right upper arm", ref constraintCount);
        
        AddBallConstraint(animator, HumanBodyBones.LeftUpperLeg, upperLegMaxAngle, "left upper leg", ref constraintCount);
        AddBallConstraint(animator, HumanBodyBones.RightUpperLeg, upperLegMaxAngle, "right upper leg", ref constraintCount);
        
        AddBallConstraint(animator, HumanBodyBones.Hips, spineMaxAngle, "hips", ref constraintCount);
        AddBallConstraint(animator, HumanBodyBones.Spine, spineMaxAngle, "spine", ref constraintCount);
        
        Transform spine1 = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (spine1 != null && spine1.name.Contains("Spine1"))
        {
            fullBodyIK.boneConstraints.Add(new FullBodyFABRIK.BoneConstraintPair
            {
                boneName = spine1.name,
                constraint = new FullBodyFABRIK.JointConstraint
                {
                    type = FullBodyFABRIK.ConstraintType.Ball,
                    minAngle = 0f,
                    maxAngle = spineMaxAngle,
                    enabled = true
                }
            });
            constraintCount++;
            Debug.Log($"Added constraint: {spine1.name} (spine1) - Ball, max angle: {spineMaxAngle}°");
        }
        
        Transform spine2 = FindTransformByName(animator.GetBoneTransform(HumanBodyBones.Hips), "Spine2");
        if (spine2 != null)
        {
            fullBodyIK.boneConstraints.Add(new FullBodyFABRIK.BoneConstraintPair
            {
                boneName = spine2.name,
                constraint = new FullBodyFABRIK.JointConstraint
                {
                    type = FullBodyFABRIK.ConstraintType.Ball,
                    minAngle = 0f,
                    maxAngle = spineMaxAngle,
                    enabled = true
                }
            });
            constraintCount++;
            Debug.Log($"Added constraint: {spine2.name} (spine2) - Ball, max angle: {spineMaxAngle}°");
        }
        
        AddBallConstraint(animator, HumanBodyBones.Neck, neckMaxAngle, "neck", ref constraintCount);
        AddBallConstraint(animator, HumanBodyBones.Head, headMaxAngle, "head", ref constraintCount);
        
        AddBallConstraint(animator, HumanBodyBones.LeftFoot, footMaxAngle, "left foot", ref constraintCount);
        AddBallConstraint(animator, HumanBodyBones.RightFoot, footMaxAngle, "right foot", ref constraintCount);
        
        Debug.Log($"Constraint setup complete! {constraintCount} constraints (4 Hinge + {constraintCount - 4} Ball)");
        
        Debug.Log("Hinge Constraints:");
        Debug.Log("  Left Elbow:  Axis=(0,0,-1), Max=" + elbowMaxAngle + "°");
        Debug.Log("  Right Elbow: Axis=(0,0,1),  Max=" + elbowMaxAngle + "°");
        Debug.Log("  Both Knees:  Axis=(1,0,0),  Max=" + kneeMaxAngle + "°");
        Debug.Log("Ball Constraints:");
        Debug.Log("  Shoulders: " + shoulderMaxAngle + "°, Upper Arms: " + upperArmMaxAngle + "°");
        Debug.Log("  Upper Legs: " + upperLegMaxAngle + "°, Spine/Hips: " + spineMaxAngle + "°");
        Debug.Log("  Neck: " + neckMaxAngle + "°, Head: " + headMaxAngle + "°, Feet: " + footMaxAngle + "°");
    }
    
    [ContextMenu("Create Target Objects")]
    public void CreateTargetObjects()
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("Requires Animator component with Humanoid Rig!");
            return;
        }
        
        GameObject targetsRoot = new GameObject("IK_Targets");
        targetsRoot.transform.SetParent(transform);
        targetsRoot.transform.localPosition = Vector3.zero;
        
        if (leftHandTarget == null)
        {
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftHand != null)
            {
                GameObject target = CreateTargetSphere("LeftHandTarget", Color.cyan);
                target.transform.SetParent(targetsRoot.transform);
                target.transform.position = leftHand.position;
                leftHandTarget = target.transform;
                Debug.Log("Created left hand target");
            }
        }
        
        if (rightHandTarget == null)
        {
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand != null)
            {
                GameObject target = CreateTargetSphere("RightHandTarget", Color.cyan);
                target.transform.SetParent(targetsRoot.transform);
                target.transform.position = rightHand.position;
                rightHandTarget = target.transform;
                Debug.Log("Created right hand target");
            }
        }
        
        if (leftFootTarget == null)
        {
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            if (leftFoot != null)
            {
                GameObject target = CreateTargetSphere("LeftFootTarget", Color.green);
                target.transform.SetParent(targetsRoot.transform);
                target.transform.position = leftFoot.position;
                leftFootTarget = target.transform;
                Debug.Log("Created left foot target");
            }
        }
        
        if (rightFootTarget == null)
        {
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (rightFoot != null)
            {
                GameObject target = CreateTargetSphere("RightFootTarget", Color.green);
                target.transform.SetParent(targetsRoot.transform);
                target.transform.position = rightFoot.position;
                rightFootTarget = target.transform;
                Debug.Log("Created right foot target");
            }
        }
        
        if (headTarget == null)
        {
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
            {
                GameObject target = CreateTargetSphere("HeadTarget", Color.yellow);
                target.transform.SetParent(targetsRoot.transform);
                target.transform.position = head.position;
                headTarget = target.transform;
                Debug.Log("Created head target");
            }
        }
        
        Debug.Log("Target objects created!");
    }
    
    GameObject CreateTargetSphere(string name, Color color)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.name = name;
        target.transform.localScale = Vector3.one * 0.1f;
        
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.material = mat;
        }
        
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        return target;
    }
    
    
    void AddBallConstraint(Animator animator, HumanBodyBones boneType, float maxAngle, string description, ref int count)
    {
        Transform bone = animator.GetBoneTransform(boneType);
        if (bone != null)
        {
            fullBodyIK.boneConstraints.Add(new FullBodyFABRIK.BoneConstraintPair
            {
                boneName = bone.name,
                constraint = new FullBodyFABRIK.JointConstraint
                {
                    type = FullBodyFABRIK.ConstraintType.Ball,
                    minAngle = 0f,
                    maxAngle = maxAngle,
                    enabled = true
                }
            });
            count++;
            Debug.Log($"Added constraint: {bone.name} ({description}) - Ball, max angle: {maxAngle}°");
        }
        else
        {
            Debug.LogWarning($"Cannot find {description} bone");
        }
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
}

#if UNITY_EDITOR
[CustomEditor(typeof(FullBodyIKSetup))]
public class FullBodyIKSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        FullBodyIKSetup setup = (FullBodyIKSetup)target;
        
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. Create Target Objects", GUILayout.Height(30)))
        {
            setup.CreateTargetObjects();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("2. Auto Setup End Effectors", GUILayout.Height(40)))
        {
            setup.AutoSetup();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("3. Setup Common Constraints", GUILayout.Height(40)))
        {
            setup.SetupCommonConstraints();
        }
        
        EditorGUILayout.Space(15);
        EditorGUILayout.HelpBox(
            "Usage Steps:\n" +
            "1. Click '1. Create Target Objects' (creates 5 targets)\n" +
            "2. Click '2. Auto Setup End Effectors' (configures 5 IK chains)\n" +
            "3. Click '3. Setup Common Constraints' (adds 18 constraints)\n" +
            "   • 4 Hinge: Left/Right Elbow, Left/Right Knee\n" +
            "   • 14 Ball: Shoulders, Arms, Legs, Spine, Neck, Head, Feet\n" +
            "4. Enter Play Mode to test\n" +
            "5. Drag target spheres in Scene view\n" +
            "6. Observe character IK response\n\n" +
            "Constraint System:\n" +
            "- Hinge: Limits rotation to single axis (elbows/knees)\n" +
            "- Ball: Limits rotation cone angle (shoulders/spine/etc)\n" +
            "- All angles adjustable in Inspector before setup",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        if (setup.fullBodyIK != null)
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Root Bone: {(setup.fullBodyIK.rootBone ? setup.fullBodyIK.rootBone.name : "Not set")}");
            EditorGUILayout.LabelField($"End Effectors: {setup.fullBodyIK.endEffectors.Count}");
            EditorGUILayout.LabelField($"Constraints: {setup.fullBodyIK.boneConstraints.Count}");
        }
    }
}
#endif
