﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Class providing a base implementation of the <see cref="IMixedRealityInputDeviceManager"/> interface.
    /// </summary>
    public abstract class BaseInputDeviceManager : BaseDataProvider<IMixedRealityInputSystem>, IMixedRealityInputDeviceManager
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the data provider.</param>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        [System.Obsolete("This constructor is obsolete (registrar parameter is no longer required) and will be removed in a future version of the Microsoft Mixed Reality Toolkit.")]
        protected BaseInputDeviceManager(
            IMixedRealityServiceRegistrar registrar,
            IMixedRealityInputSystem inputSystem,
            string name, 
            uint priority, 
            BaseMixedRealityProfile profile) : this(inputSystem, name, priority, profile)
        {
            Registrar = registrar;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        protected BaseInputDeviceManager(
            IMixedRealityInputSystem inputSystem,
            string name,
            uint priority,
            BaseMixedRealityProfile profile) : base(inputSystem, name, priority, profile)
        { }

        /// <summary>
        /// The input system configuration profile in use in the application.
        /// </summary>
        protected MixedRealityInputSystemProfile InputSystemProfile => Service != null ? Service.InputSystemProfile : null;

        /// <inheritdoc />
        public virtual IMixedRealityController[] GetActiveControllers() => System.Array.Empty<IMixedRealityController>();

        /// <summary>
        /// Request an array of pointers for the controller type.
        /// </summary>
        /// <param name="controllerType">The controller type making the request for pointers.</param>
        /// <param name="controllingHand">The handedness of the controller making the request.</param>
        /// <param name="useSpecificType">Only register pointers with a specific type.</param>
        protected virtual IMixedRealityPointer[] RequestPointers(SupportedControllerType controllerType, Handedness controllingHand)
        {
            var pointers = new List<IMixedRealityPointer>();

            if ((Service != null) &&
                (InputSystemProfile != null) &&
                InputSystemProfile.PointerProfile != null)
            {
                for (int i = 0; i < InputSystemProfile.PointerProfile.PointerOptions.Length; i++)
                {
                    var pointerProfile = InputSystemProfile.PointerProfile.PointerOptions[i];

                    if (((pointerProfile.ControllerType & controllerType) != 0) &&
                        (pointerProfile.Handedness == Handedness.Any || pointerProfile.Handedness == Handedness.Both || pointerProfile.Handedness == controllingHand))
                    {
                        var pointerObject = Object.Instantiate(pointerProfile.PointerPrefab);
                        MixedRealityPlayspace.AddChild(pointerObject.transform);
                        var pointer = pointerObject.GetComponent<IMixedRealityPointer>();

                        if (pointer != null)
                        {
                            pointers.Add(pointer);
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to attach {pointerProfile.PointerPrefab.name} to {controllerType}.");
                        }
                    }
                }
            }

            return pointers.Count == 0 ? null : pointers.ToArray();
        }
    }
}
