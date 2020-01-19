﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    [AddComponentMenu("Scripts/MRTK/SDK/SpherePointer")]
    public class SpherePointer : BaseControllerPointer, IMixedRealityNearPointer
    {
        private SceneQueryType raycastMode = SceneQueryType.SphereOverlap;

        /// <inheritdoc />
        public override SceneQueryType SceneQueryType { get { return raycastMode; } set { raycastMode = value; } }

        [SerializeField]
        [Min(0.0f)]
        [Tooltip("Additional distance on top of sphere cast radius when pointer is considered 'near' an object and far interaction will turn off")]
        private float nearObjectMargin = 0.2f;
        /// <summary>
        /// Additional distance on top of<see cref="BaseControllerPointer.SphereCastRadius"/> when pointer is considered 'near' an object and far interaction will turn off.
        /// </summary>
        /// <remarks>
        /// This creates a dead zone in which far interaction is disabled before objects become grabbable.
        /// </remarks>
        public float NearObjectMargin => nearObjectMargin;

        /// <summary>
        /// Distance at which the pointer is considered "near" an object.
        /// </summary>
        /// <remarks>
        /// Sum of <see cref="BaseControllerPointer.SphereCastRadius"/> and <see cref="NearObjectMargin"/>. Entering the <see cref="NearObjectRadius"/> disables far interaction.
        /// </remarks>
        public float NearObjectRadius => SphereCastRadius + NearObjectMargin;

        [SerializeField]
        [Tooltip("The LayerMasks, in prioritized order, that are used to determine the grabbable objects. Remember to also add NearInteractionGrabbable! Only collidables with NearInteractionGrabbable will raise events.")]
        private LayerMask[] grabLayerMasks = { UnityEngine.Physics.DefaultRaycastLayers };
        /// <summary>
        /// The LayerMasks, in prioritized order, that are used to determine the touchable objects.
        /// </summary>
        /// <remarks>
        /// Only [NearInteractionGrabbables](xref:Microsoft.MixedReality.Toolkit.Input.NearInteractionGrabbable) in one of the LayerMasks will raise events.
        /// </remarks>
        public LayerMask[] GrabLayerMasks => grabLayerMasks;

        [SerializeField]
        [Tooltip("Specify whether queries for grabbable objects hit triggers.")]
        protected QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal;
        /// <summary>
        /// Specify whether queries for grabbable objects hit triggers.
        /// </summary>
        public QueryTriggerInteraction TriggerInteraction => triggerInteraction;


        [SerializeField]
        [Tooltip("Maximum number of colliders that can be detected in a scene query.")]
        [Min(1)]
        private int sceneQueryBufferSize = 64;
        /// <summary>
        /// Maximum number of colliders that can be detected in a scene query.
        /// </summary>
        public int SceneQueryBufferSize => sceneQueryBufferSize;

        [SerializeField]
        [Tooltip("Whether to ignore colliders that may be near the pointer, but not actually in the visual FOV. This can prevent accidental grabs, and will allow hand rays to turn on when you may be near a grabbable but cannot see it. Visual FOV is defined by cone centered about display center, radius equal to half display height.")]
        private bool ignoreCollidersNotInFOV = true;
        /// <summary>
        /// Whether to ignore colliders that may be near the pointer, but not actually in the visual FOV.
        /// This can prevent accidental grabs, and will allow hand rays to turn on when you may be near 
        /// a grabbable but cannot see it. Visual FOV is defined by cone centered about display center, 
        /// radius equal to half display height.
        /// </summary>
        public bool IgnoreCollidersNotInFOV
        {
            get => ignoreCollidersNotInFOV;
            set => ignoreCollidersNotInFOV = value;
        }

        private SpherePointerQueryInfo queryBufferNearObjectRadius;
        private SpherePointerQueryInfo queryBufferInteractionRadius;

        /// <summary>
        /// Test if the pointer is near any collider that's both on a grabbable layer mask, and has a NearInteractionGrabbable.
        /// Uses SphereCastRadius + NearObjectMargin to determine if near an object.
        /// </summary>
        /// <returns>True if the pointer is near any collider that's both on a grabbable layer mask, and has a NearInteractionGrabbable.</returns>
        public bool IsNearObject
        {
            get
            {
                return queryBufferNearObjectRadius.ContainsGrabbable();
            }
        }

        /// <summary>
        /// Test if the pointer is within the grabbable radius of collider that's both on a grabbable layer mask, and has a NearInteractionGrabbable.
        /// Uses SphereCastRadius to determine if near an object.
        /// Note: if focus on pointer is locked, will always return true.
        /// </summary>
        /// <returns>True if the pointer is within the grabbable radius of collider that's both on a grabbable layer mask, and has a NearInteractionGrabbable.</returns>
        public override bool IsInteractionEnabled
        {
            get
            {
                if (IsFocusLocked)
                {
                    return true;
                }
                return base.IsInteractionEnabled && queryBufferInteractionRadius.ContainsGrabbable();
            }
        }

        private void Awake()
        {
            queryBufferNearObjectRadius = new SpherePointerQueryInfo(sceneQueryBufferSize, NearObjectRadius);
            queryBufferInteractionRadius = new SpherePointerQueryInfo(sceneQueryBufferSize, SphereCastRadius);
        }

        /// <inheritdoc />
        public override void OnPreSceneQuery()
        {
            if (Rays == null)
            {
                Rays = new RayStep[1];
            }

            Vector3 pointerPosition;
            if (TryGetNearGraspPoint(out pointerPosition))
            {
                Vector3 endPoint = Vector3.forward * SphereCastRadius;
                Rays[0].UpdateRayStep(ref pointerPosition, ref endPoint);

                var layerMasks = PrioritizedLayerMasksOverride ?? GrabLayerMasks;

                for (int i = 0; i < layerMasks.Length; i++)
                {
                    if (queryBufferNearObjectRadius.TryUpdateQueryBufferForLayerMask(layerMasks[i], pointerPosition, triggerInteraction, ignoreCollidersNotInFOV))
                    {
                        break;
                    }
                }

                for (int i = 0; i < layerMasks.Length; i++)
                {
                    if (queryBufferInteractionRadius.TryUpdateQueryBufferForLayerMask(layerMasks[i], pointerPosition, triggerInteraction, ignoreCollidersNotInFOV))
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the position of where grasp happens
        /// For IMixedRealityHand it's the average of index and thumb.
        /// For any other IMixedRealityController, return just the pointer origin
        /// </summary>
        public bool TryGetNearGraspPoint(out Vector3 result)
        {
            // If controller is of kind IMixedRealityHand, return average of index and thumb
            if (Controller != null && Controller is IMixedRealityHand)
            {
                var hand = Controller as IMixedRealityHand;
                hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose index);
                if (index != null)
                {
                    hand.TryGetJoint(TrackedHandJoint.ThumbTip, out MixedRealityPose thumb);
                    if (thumb != null)
                    {
                        result = 0.5f * (index.Position + thumb.Position);
                        return true;
                    }
                }
            }
            else
            {
                result = Position;
                return true;
            }

            result = Vector3.zero;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetDistanceToNearestSurface(out float distance)
        {
            var focusProvider = CoreServices.InputSystem?.FocusProvider;
            if (focusProvider != null)
            {
                FocusDetails focusDetails;
                if (focusProvider.TryGetFocusDetails(this, out focusDetails))
                {
                    distance = focusDetails.RayDistance;
                    return true;
                }
            }

            distance = 0.0f;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetNormalToNearestSurface(out Vector3 normal)
        {
            var focusProvider = CoreServices.InputSystem?.FocusProvider;
            if (focusProvider != null)
            {
                FocusDetails focusDetails;
                if (focusProvider.TryGetFocusDetails(this, out focusDetails))
                {
                    normal = focusDetails.Normal;
                    return true;
                }
            }

            normal = Vector3.forward;
            return false;
        }

        /// <summary>
        /// Helper class for storing and managing near grabbables close to a point
        /// </summary>
        private class SpherePointerQueryInfo
        {
            // List of corners shared across all sphere pointer query instances --
            // used to store list of corners for a bounds. Shared and static
            // to avoid allocating memory each frame
            private static List<Vector3> corners = new List<Vector3>();
            // Help to clear caches when new frame runs
            static private int lastCalculatedFrame = -1;
            // Map from grabbable => is the grabbable in FOV for this frame. Cleared every frame
            private static Dictionary<Collider, bool> colliderCache = new Dictionary<Collider, bool>();

            /// <summary>
            /// How many colliders are near the point from the latest call to TryUpdateQueryBufferForLayerMask 
            /// </summary>
            private int numColliders;

            /// <summary>
            /// Fixed-length array used to story physics queries
            /// </summary>
            private Collider[] queryBuffer;

            /// <summary>
            /// Distance for performing queries.
            /// </summary>
            private float queryRadius;

            /// <summary>
            /// The grabbable near the QueryRadius. 
            /// </summary>
            private NearInteractionGrabbable grabbable;

            public SpherePointerQueryInfo(int bufferSize, float radius)
            {
                numColliders = 0;
                queryBuffer = new Collider[bufferSize];
                queryRadius = radius;
            }

            /// <summary>
            /// Intended to be called once per frame, this method performs a sphere intersection test against
            /// all colliders in the layers defined by layerMask at the given pointer position.
            /// All colliders intersecting the sphere at queryRadius and pointerPosition are stored in queryBuffer,
            /// and the first grabbable in the list of returned colliders is stored.
            /// Also provides an option to ignore colliders that are not visible.
            /// </summary>
            /// <param name="layerMask">Filter to only perform sphere cast on these layers.</param>
            /// <param name="pointerPosition">The position of the pointer to query against.</param>
            /// <param name="triggerInteraction">Passed along to the OverlapSphereNonAlloc call.</param>
            /// <param name="ignoreCollidersNotInFOV">Whether to ignore colliders that are not visible.</param>
            public bool TryUpdateQueryBufferForLayerMask(LayerMask layerMask, Vector3 pointerPosition, QueryTriggerInteraction triggerInteraction, bool ignoreCollidersNotInFOV)
            {
                grabbable = null;
                numColliders = UnityEngine.Physics.OverlapSphereNonAlloc(
                    pointerPosition,
                    queryRadius,
                    queryBuffer,
                    layerMask,
                    triggerInteraction);

                if (numColliders == queryBuffer.Length)
                {
                    Debug.LogWarning($"Maximum number of {numColliders} colliders found in SpherePointer overlap query. Consider increasing the query buffer size in the pointer profile.");
                }

                for (int i = 0; i < numColliders; i++)
                {
                    Collider collider = queryBuffer[i];
                    grabbable = collider.GetComponent<NearInteractionGrabbable>();
                    if (grabbable != null)
                    {
                        if (ignoreCollidersNotInFOV)
                        {
                            if (!isInFOVCone(collider))
                            {
                                // Additional check: is grabbable in the camera frustrum
                                // We do this so that if grabbable is not visible it is not accidentally grabbed
                                // Also to not turn off the hand ray if hand is near a grabbable that's not actually visible
                                grabbable = null;
                            }
                        }   
                    }

                    if (grabbable != null)
                    {
                        return true;
                    }
                }
                return false;
            }


            /// <summary>
            /// Returns true if a collider's bounds is within the camera FOV
            /// </summary>
            /// <param name="myCollider">The collider to test</param>
            private bool isInFOVCone(Collider myCollider)
            {
                if (lastCalculatedFrame != Time.frameCount)
                {
                    colliderCache.Clear();
                    lastCalculatedFrame = Time.frameCount;
                }
                if (colliderCache.TryGetValue(myCollider, out bool result))
                {
                    return result;
                }

                corners.Clear();
                BoundsExtensions.GetColliderBoundsPoints(myCollider, corners, 0);

                float xMin = float.MaxValue, yMin = float.MaxValue, zMin = float.MaxValue;
                float xMax = float.MinValue, yMax = float.MinValue, zMax = float.MinValue;
                for (int i = 0; i < corners.Count; i++)
                {
                    var corner = corners[i];
                    if (isPointInFOVCone(corner, 0))
                    {
                        colliderCache.Add(myCollider, true);
                        return true;
                    }

                    xMin = Mathf.Min(xMin, corner.x);
                    yMin = Mathf.Min(yMin, corner.y);
                    zMin = Mathf.Min(zMin, corner.z);
                    xMax = Mathf.Max(xMax, corner.x);
                    yMax = Mathf.Max(yMax, corner.y);
                    zMax = Mathf.Max(zMax, corner.z);
                }

                // edge case: check if camera is inside the entire bounds of the collider;
                // Consider simplifying to myCollider.bounds.Contains(CameraCache.main.transform.position)
                var cameraPos = CameraCache.Main.transform.position;
                result = xMin <= cameraPos.x && cameraPos.x <= xMax 
                    && yMin <= cameraPos.y && cameraPos.y <= yMax
                    && zMin <= cameraPos.z && cameraPos.z <= zMax;
                colliderCache.Add(myCollider, result);
                return result;
            }

            /// <summary>
            /// Returns true if a point is in the a cone inscribed into the 
            /// Camera's frustrum. The cone is inscribed to match the vertical height of the camera's
            /// FOV. By default, the cone's tip is "chopped off" by an amount defined by minDist. 
            /// The cone's height is given by maxDist.
            /// </summary>
            /// <param name="point">Point to test</param>
            /// <param name="coneAngleBufferDegrees">Degrees to expand the cone by.</param>
            /// <param name="minDist">Point must be at least this far away (along the camera forward) from camera. </param>
            /// <param name="maxDist">Point must be at most this far away (along camera forward) from camera. </param>
            private static bool isPointInFOVCone(
                Vector3 point, 
                float coneAngleBufferDegrees = 0,
                float minDist = 0.05f,
                float maxDist = 100f)
            {
                Camera mainCam = CameraCache.Main;
                var cameraToPoint = point - mainCam.transform.position;
                var pointCameraDist = Vector3.Dot(mainCam.transform.forward, cameraToPoint);
                if (pointCameraDist < minDist || pointCameraDist > maxDist)
                {
                    return false;
                }
                var verticalFOV = mainCam.fieldOfView + coneAngleBufferDegrees;
                var degrees = Mathf.Acos(pointCameraDist / cameraToPoint.magnitude) * Mathf.Rad2Deg;
                return degrees < verticalFOV * 0.5f;
            }

            /// <summary>
            /// Returns true if any of the objects inside QueryBuffer contain a grabbable
            /// </summary>
            public bool ContainsGrabbable()
            {
                return grabbable != null;
            }
        }
    }
}
