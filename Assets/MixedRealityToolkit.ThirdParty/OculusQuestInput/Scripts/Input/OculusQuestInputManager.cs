﻿using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace prvncher.MixedReality.Toolkit.OculusQuestInput
{
    /// <summary>
    /// Manages Oculus Quest Hand Inputs
    /// </summary>
    [MixedRealityDataProvider(typeof(IMixedRealityInputSystem), SupportedPlatforms.Android | SupportedPlatforms.WindowsEditor | SupportedPlatforms.MacEditor | SupportedPlatforms.LinuxEditor, "Oculus Quest Input Manager")]
    public class OculusQuestInputManager : BaseInputDeviceManager, IMixedRealityCapabilityCheck
    {
        private Dictionary<Handedness, OculusQuestHand> trackedHands = new Dictionary<Handedness, OculusQuestHand>();
        private Dictionary<Handedness, OculusQuestController> trackedControllers = new Dictionary<Handedness, OculusQuestController>();

        private OVRCameraRig cameraRig;

        private OVRHand rightHand;
        private OVRSkeleton rightSkeleton;

        private OVRHand leftHand;
        private OVRSkeleton leftSkeleton;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the data provider.</param>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public OculusQuestInputManager(
            IMixedRealityInputSystem inputSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(inputSystem, name, priority, profile)
        {
        }

        public override void Enable()
        {
            base.Enable();

            cameraRig = GameObject.FindObjectOfType<OVRCameraRig>();
            var ovrHands = cameraRig.GetComponentsInChildren<OVRHand>();

            foreach (var ovrHand in ovrHands)
            {
                var skeltonDataProvider = ovrHand as OVRSkeleton.IOVRSkeletonDataProvider;
                var skeltonType = skeltonDataProvider.GetSkeletonType();

                var ovrSkelton = ovrHand.GetComponent<OVRSkeleton>();
                if (ovrSkelton == null)
                {
                    continue;
                }

                switch (skeltonType)
                {
                    case OVRSkeleton.SkeletonType.HandLeft:
                        leftHand = ovrHand;
                        leftSkeleton = ovrSkelton;
                        break;
                    case OVRSkeleton.SkeletonType.HandRight:
                        rightHand = ovrHand;
                        rightSkeleton = ovrSkelton;
                        break;
                }
            }
        }

        public override void Disable()
        {
            base.Disable();

            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;

            foreach (var hand in trackedHands)
            {
                if (hand.Value != null)
                {
                    inputSystem?.RaiseSourceLost(hand.Value.InputSource, hand.Value);
                }
            }

            trackedHands.Clear();
        }

        public override IMixedRealityController[] GetActiveControllers()
        {
            return trackedHands.Values.ToArray<IMixedRealityController>();
        }

        /// <inheritdoc />
        public bool CheckCapability(MixedRealityCapability capability)
        {
            return (capability == MixedRealityCapability.ArticulatedHand);
        }

        public override void Update()
        {
            base.Update();
            if (OVRPlugin.GetHandTrackingEnabled())
            {
                RemoveAllControllerDevices();
                UpdateHands();
            }
            else
            {
                RemoveAllHandDevices();
                UpdateControllers();
            }
        }

        #region Controller Management
        protected void UpdateControllers()
        {
            UpdateController(OVRInput.Controller.LTouch, Handedness.Left);
            UpdateController(OVRInput.Controller.RTouch, Handedness.Right);
        }

        protected void UpdateController(OVRInput.Controller controller, Handedness handedness)
        {
            if (OVRInput.IsControllerConnected(controller))
            {
                var touchController = GetOrAddController(handedness);
                touchController.UpdateController(cameraRig, controller);
            }
            else
            {
                RemoveHandDevice(handedness);
            }
        }

        private OculusQuestController GetOrAddController(Handedness handedness)
        {
            if (trackedControllers.ContainsKey(handedness))
            {
                return trackedControllers[handedness];
            }

            var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
            var inputSourceType = InputSourceType.Hand;

            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;
            var inputSource = inputSystem?.RequestNewGenericInputSource($"Oculus Quest {handedness} Controller", pointers, inputSourceType);

            var controller = new OculusQuestController(TrackingState.Tracked, handedness, inputSource);
            controller.SetupConfiguration(typeof(OculusQuestController));

            for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
            {
                controller.InputSource.Pointers[i].Controller = controller;
            }

            inputSystem?.RaiseSourceDetected(controller.InputSource, controller);

            trackedControllers.Add(handedness, controller);

            return controller;
        }

        private void RemoveControllerDevice(Handedness handedness)
        {
            if (trackedControllers.ContainsKey(handedness))
            {
                var hand = trackedControllers[handedness];
                CoreServices.InputSystem?.RaiseSourceLost(hand.InputSource, hand);
                trackedControllers.Remove(handedness);
            }
        }

        private void RemoveAllControllerDevices()
        {
            foreach (var controller in trackedControllers.Values)
            {
                CoreServices.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
            }
            trackedControllers.Clear();
        }
        #endregion

        #region Hand Management
        protected void UpdateHands()
        {
            UpdateHand(rightHand, rightSkeleton, Handedness.Right);
            UpdateHand(leftHand, leftSkeleton, Handedness.Left);
        }

        protected void UpdateHand(OVRHand ovrHand, OVRSkeleton ovrSkeleton, Handedness handedness)
        {
            if (ovrHand.IsTracked)
            {
                var hand = GetOrAddHand(handedness);
                hand.UpdateController(ovrHand, ovrSkeleton);
            }
            else
            {
                RemoveHandDevice(handedness);
            }
        }

        private OculusQuestHand GetOrAddHand(Handedness handedness)
        {
            if (trackedHands.ContainsKey(handedness))
            {
                return trackedHands[handedness];
            }

            // Add new hand
            var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
            var inputSourceType = InputSourceType.Hand;

            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;
            var inputSource = inputSystem?.RequestNewGenericInputSource($"Oculus Quest {handedness} Hand", pointers, inputSourceType);

            var controller = new OculusQuestHand(TrackingState.Tracked, handedness, inputSource);
            controller.SetupConfiguration(typeof(OculusQuestHand));

            for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
            {
                controller.InputSource.Pointers[i].Controller = controller;
            }

            inputSystem?.RaiseSourceDetected(controller.InputSource, controller);

            trackedHands.Add(handedness, controller);

            return controller;
        }

        private void RemoveHandDevice(Handedness handedness)
        {
            if (trackedHands.ContainsKey(handedness))
            {
                var hand = trackedHands[handedness];
                CoreServices.InputSystem?.RaiseSourceLost(hand.InputSource, hand);
                trackedHands.Remove(handedness);
            }
        }

        private void RemoveAllHandDevices()
        {
            foreach (var controller in trackedHands.Values)
            {
                CoreServices.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
            }
            trackedHands.Clear();
        }
        #endregion
    }
}
