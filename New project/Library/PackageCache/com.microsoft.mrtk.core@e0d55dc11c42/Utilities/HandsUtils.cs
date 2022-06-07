// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Subsystems;
using System;
using UnityEngine;
using UnityEngine.XR;

namespace Microsoft.MixedReality.Toolkit
{
    /// <summary>
    /// Collection of utility methods to simplify working with the Hands subsystem(s).
    /// </summary>
    public static class HandsUtils
    {
        /// <summary>
        /// Get the first running <see cref="HandsAggregatorSubsystem"/> instance.
        /// </summary>
        /// <returns>
        /// The first running <see cref="HandsAggregatorSubsystem"/> instance, or null.
        /// </returns>
        public static HandsAggregatorSubsystem GetSubsystem()
        {
            return XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
        }

        internal static readonly HandFinger[] handFingers = Enum.GetValues(typeof(HandFinger)) as HandFinger[];

        internal static readonly InputDeviceCharacteristics LeftHandCharacteristics =
            InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Left;
        internal static readonly InputDeviceCharacteristics RightHandCharacteristics =
            InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right;

        /// <summary>
        /// Converts a Unity finger bone into an MRTK hand joint.
        /// </summary>
        /// <remarks>
        /// <para>For HoloLens 2, Unity provides four joints per finger, in index order of metacarpal (0) to tip (4).
        /// The first joint for the thumb is the wrist joint. Palm joint is not provided.</para>
        /// </remarks>
        /// <param name="finger">The Unity classification of the current finger.</param>
        /// <param name="index">The Unity index of the current finger bone.</param>
        /// <returns>The current Unity finger bone converted into an MRTK joint.</returns>
        internal static TrackedHandJoint ConvertToTrackedHandJoint(HandFinger finger, int index)
        {
            switch (finger)
            {
                case HandFinger.Thumb: return (index == 0) ? TrackedHandJoint.Wrist : TrackedHandJoint.ThumbMetacarpalJoint + index - 1;
                case HandFinger.Index: return TrackedHandJoint.IndexMetacarpal + index;
                case HandFinger.Middle: return TrackedHandJoint.MiddleMetacarpal + index;
                case HandFinger.Ring: return TrackedHandJoint.RingMetacarpal + index;
                case HandFinger.Pinky: return TrackedHandJoint.PinkyMetacarpal + index;
                default: throw new ArgumentOutOfRangeException(nameof(finger));
            }
        }

        /// <summary>
        /// Converts an MRTK joint id into an index, for use when indexing into the joint pose array.
        /// </summary>
        internal static int ConvertToIndex(TrackedHandJoint joint)
        {
            return (int)joint - 1;
        }

        /// <summary>
        /// Converts a joint index into a TrackedHandJoint enum, for use when indexing into the joint pose array.
        /// </summary>
        internal static TrackedHandJoint ConvertFromIndex(int index)
        {
            return (TrackedHandJoint)(index + 1);
        }

        /// <summary>
        /// Gets the Unity finger identification for a given MRTK TrackedHandJoint.
        /// </summary>
        /// <remarks>Due to provider mappings, the wrist is considered the base of the thumb.</remarks>
        /// <param name="joint">The MRTK joint, for which we will return the Unity finger.</param>
        /// <returns>The HandFinger on which the joint exists.</returns>
        internal static HandFinger GetFingerFromJoint(TrackedHandJoint joint)
        {
            Debug.Assert(joint != TrackedHandJoint.None &&
                         joint != TrackedHandJoint.Palm,
                         "GetFingerFromJoint passed a non-finger joint");

            if (joint == TrackedHandJoint.Wrist || (joint >= TrackedHandJoint.ThumbMetacarpalJoint && joint <= TrackedHandJoint.ThumbTip))
            {
                return HandFinger.Thumb;
            }
            else if (joint >= TrackedHandJoint.IndexMetacarpal && joint <= TrackedHandJoint.IndexTip)
            {
                return HandFinger.Index;
            }
            else if (joint >= TrackedHandJoint.MiddleMetacarpal && joint <= TrackedHandJoint.MiddleTip)
            {
                return HandFinger.Middle;
            }
            else if (joint >= TrackedHandJoint.RingMetacarpal && joint <= TrackedHandJoint.RingTip)
            {
                return HandFinger.Ring;
            }
            else
            {
                return HandFinger.Pinky;
            }
        }

        /// <summary>
        /// Gets the index of the joint relative to the base of the finger.
        /// </summary>
        /// <remarks>Due to provider mappings, the wrist is considered the base of the thumb.</remarks>
        /// <param name="joint">The MRTK joint, for which we will return its offset from the base.</param>
        /// <returns>Index offset from the metacarpal/base of the finger.</returns>
        internal static int GetOffsetFromBase(TrackedHandJoint joint)
        {
            Debug.Assert(joint != TrackedHandJoint.None &&
                         joint != TrackedHandJoint.Palm,
                         "GetOffsetFromBase passed a non-finger joint");

            if (joint == TrackedHandJoint.Wrist)
            {
                return 0;
            }
            else if (joint >= TrackedHandJoint.ThumbMetacarpalJoint && joint <= TrackedHandJoint.ThumbTip)
            {
                return joint - TrackedHandJoint.ThumbMetacarpalJoint + 1; // Add one to account for wrist at the base
            }
            else if (joint >= TrackedHandJoint.IndexMetacarpal && joint <= TrackedHandJoint.IndexTip)
            {
                return joint - TrackedHandJoint.IndexMetacarpal;
            }
            else if (joint >= TrackedHandJoint.MiddleMetacarpal && joint <= TrackedHandJoint.MiddleTip)
            {
                return joint - TrackedHandJoint.MiddleMetacarpal;
            }
            else if (joint >= TrackedHandJoint.RingMetacarpal && joint <= TrackedHandJoint.RingTip)
            {
                return joint - TrackedHandJoint.RingMetacarpal;
            }
            else
            {
                return joint - TrackedHandJoint.PinkyMetacarpal;
            }
        }

        /// Utility class to serialize hand pose as a dictionary with full joint names
        [Serializable]
        internal struct ArticulatedHandPoseItem
        {
            // Helper list of joint names.
            private static readonly string[] JointNames = Enum.GetNames(typeof(TrackedHandJoint));

            [SerializeField, Tooltip("The human-readable name of the serialized joint.")]
            private string joint;

            /// <summary>
            /// The human-readable name of the serialized joint.
            /// </summary>
            public string Joint => joint;

            [SerializeField, Tooltip("The pose associated with the joint.")]
            private MixedRealityPose pose;

            /// <summary>
            /// The pose associated with the joint.
            /// </summary>
            public MixedRealityPose Pose
            {
                get => pose;
                set => pose = value;
            }

            /// <summary>
            /// Helper property to translate from human-readable joint name to
            /// TrackedHandJoint enum entry.
            /// </summary>
            public TrackedHandJoint JointIndex
            {
                get
                {
                    int nameIndex = Array.FindIndex(JointNames, IsJointName);
                    if (nameIndex < 0)
                    {
                        Debug.LogError($"Joint name {Joint} not in TrackedHandJoint enum");
                        return TrackedHandJoint.None;
                    }
                    return (TrackedHandJoint)nameIndex;
                }
                set { joint = JointNames[(int)value]; }
            }

            private bool IsJointName(string s)
            {
                return s == Joint;
            }

            /// <summary>
            /// Constructs a new serialized pose entry with the given joint enum id and pose.
            /// </summary>
            public ArticulatedHandPoseItem(TrackedHandJoint joint, MixedRealityPose pose)
            {
                this.joint = JointNames[(int)joint];
                this.pose = pose;
            }
        }

        /// Utility class to serialize hand pose as a dictionary with full joint names
        [Serializable]
        internal class ArticulatedHandPoseDictionary
        {
            /// <summary>
            /// The list of ArticulatedHandPoseItems, (de)serialized from JSON.
            /// </summary>
            public ArticulatedHandPoseItem[] items = null;

            /// <summary>
            /// Converts an array of JointPoses to an internal
            /// serialized array of ArticulatedHandPoseItems. This is
            /// kept for legacy compatibility with 2.x pose serialization formats.
            /// </summary>
            public void FromJointPoses(HandJointPose[] jointPoses)
            {
                items = new ArticulatedHandPoseItem[(int)TrackedHandJoint.TotalJoints];
                for (int i = 0; i < items.Length; ++i)
                {
                    items[i].JointIndex = ConvertFromIndex(i);
                    items[i].Pose = new MixedRealityPose(jointPoses[i].Position, jointPoses[i].Rotation);
                }
            }

            /// <summary>
            /// Converts the internal array of ArticulatedHandPoseItems to
            /// the provided array of JointPoses. This is kept for
            /// legacy compatibility with 2.x pose serialization formats.
            /// </summary>
            public void ToJointPoses(HandJointPose[] jointPoses)
            {
                for (int i = 0; i < jointPoses.Length; ++i)
                {
                    jointPoses[i].Position = Vector3.zero;
                    jointPoses[i].Rotation = Quaternion.identity;
                    jointPoses[i].Radius = 0.0f;
                }
                foreach (var item in items)
                {
                    if (item.JointIndex == TrackedHandJoint.None)
                    {
                        // Serialized joints have data on the "None" joint. What??
                        continue;
                    }

                    int index = ConvertToIndex(item.JointIndex);
                    jointPoses[index].Position = item.Pose.Position;
                    jointPoses[index].Rotation = item.Pose.Rotation;

                    // No joint radii from the JSON poses.. yet!
                    jointPoses[index].Radius = 0.005f;
                }
            }
        }

        /// <summary>
        /// Serialize pose data to JSON format.
        /// </summary>
        internal static string PoseToJson(HandJointPose[] poseData)
        {
            var dict = new ArticulatedHandPoseDictionary();
            dict.FromJointPoses(poseData);
            return JsonUtility.ToJson(dict, true);
        }

        /// <summary>
        /// Deserialize pose data from JSON format.
        /// </summary>
        internal static HandJointPose[] PoseFromJson(string json)
        {
            var dict = JsonUtility.FromJson<ArticulatedHandPoseDictionary>(json);

            HandJointPose[] poseData = new HandJointPose[(int)TrackedHandJoint.TotalJoints];
            dict.ToJointPoses(poseData);

            return poseData;
        }
    }
}