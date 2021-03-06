﻿// ***********************************************************************
// Assembly         : TSharp.Core
// Author           : tangjingbo
// Created          : 05-23-2013
//
// Last Modified By : tangjingbo
// Last Modified On : 05-23-2013
// ***********************************************************************
// <copyright file="ObjectsChangeTracker.cs" company="Extendsoft">
//     Copyright (c) Extendsoft. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace LightDataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EmitMapper;
    using EmitMapper.MappingConfiguration;
    using EmitMapper.MappingConfiguration.MappingOperations;
    using EmitMapper.Utils;

    /// <summary>
    /// Class ObjectsChangeTracker
    /// </summary>
    public class ObjectsChangeTracker
    {
        /// <summary>
        /// Class MappingConfiguration
        /// </summary>
        class MappingConfiguration : IMappingConfigurator
        {
            /// <summary>
            /// Gets the mapping operations.
            /// </summary>
            /// <param name="from">From.</param>
            /// <param name="to">To.</param>
            /// <returns>IMappingOperation[][].</returns>
            public IMappingOperation[] GetMappingOperations(Type from, Type to)
            {
                return ReflectionUtils
                    .GetPublicFieldsAndProperties(from)
                    .Select(m =>
                        new SrcReadOperation
                        {
                            Source = new MemberDescriptor(m),
                            Setter =
                                (obj, value, state) =>
                                    (state as TrackingMembersList).TrackingMembers.Add(
                                        new TrackingMember { name = m.Name, CurrentValue = value }
                                    )
                        }
                    )
                    .ToArray();
            }

            /// <summary>
            /// Gets the root mapping operation.
            /// </summary>
            /// <param name="from">From.</param>
            /// <param name="to">To.</param>
            /// <returns>IRootMappingOperation.</returns>
            public IRootMappingOperation GetRootMappingOperation(Type from, Type to)
            {
                return null;
            }

            /// <summary>
            /// Gets the name of the configuration.
            /// </summary>
            /// <returns>System.String.</returns>
            public string GetConfigurationName()
            {
                return "ObjectsTracker";
            }

            /// <summary>
            /// Gets the static converters manager.
            /// </summary>
            /// <returns>StaticConvertersManager.</returns>
            public StaticConvertersManager GetStaticConvertersManager()
            {
                return null;
            }
        }

        /// <summary>
        /// Class TrackingMembersList
        /// </summary>
        internal class TrackingMembersList
        {
            /// <summary>
            /// The tracking members
            /// </summary>
            public List<TrackingMember> TrackingMembers = new List<TrackingMember>();
        }

        /// <summary>
        /// Struct TrackingMember
        /// </summary>
        public struct TrackingMember
        {
            /// <summary>
            /// The name
            /// </summary>
            public string name;
            /// <summary>
            /// The current value
            /// </summary>
            public object CurrentValue;
            /// <summary>
            /// The original value
            /// </summary>
            public object OriginalValue;
        }

        /// <summary>
        /// The _tracking objects
        /// </summary>
        Dictionary<object, List<TrackingMember>> _trackingObjects = new Dictionary<object, List<TrackingMember>>();
        /// <summary>
        /// The _map manager
        /// </summary>
        ObjectMapperManager _mapManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectsChangeTracker"/> class.
        /// </summary>
        public ObjectsChangeTracker()
        {
            this._mapManager = ObjectMapperManager.DefaultInstance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectsChangeTracker"/> class.
        /// </summary>
        /// <param name="mapManager">The map manager.</param>
        public ObjectsChangeTracker(ObjectMapperManager mapManager)
        {
            this._mapManager = mapManager;
        }

        /// <summary>
        /// Registers the object.
        /// </summary>
        /// <param name="obj">The obj.</param>
        public void RegisterObject(object obj)
        {
            // var type = Obj.GetType();
            if (obj != null)
                this._trackingObjects[obj] = this.GetObjectMembers(obj);
        }

        /// <summary>
        /// Gets the changes.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>TrackingMember[][].</returns>
        public TrackingMember[] GetChanges(object obj)
        {
            List<TrackingMember> originalValues;
            if (!this._trackingObjects.TryGetValue(obj, out originalValues))
            {
                return null;
            }
            var currentValues = this.GetObjectMembers(obj);
            return currentValues.Select((x, idx) =>
            {
                var original = originalValues[idx];
                x.OriginalValue = original.CurrentValue;
                return x;

            })
                .Where(
                    (current, idx) =>
                    {
                        return
                            ((current.OriginalValue == null) != (current.CurrentValue == null))
                            ||
                            (current.OriginalValue != null && !current.OriginalValue.Equals(current.CurrentValue));
                    }
                )
                .ToArray();
        }

        public TrackingMember[] GetChanges(object originalObj, object currentObj)
        {
            if (originalObj == null || currentObj == null || originalObj.GetType() != currentObj.GetType())
                return null;
            var originalValues = GetObjectMembers(originalObj);
            var currentValues = GetObjectMembers(currentObj);
            return currentValues.Select((x, idx) =>
                {
                    var original = originalValues[idx];
                    x.OriginalValue = original.CurrentValue;
                    return x;

                })
                .Where(
                    (current, idx) =>
                    {
                        return
                            ((current.OriginalValue == null) != (current.CurrentValue == null))
                            ||
                            (current.OriginalValue != null && !current.OriginalValue.Equals(current.CurrentValue));
                    }
                )
                .ToArray();
        }
        /// <summary>
        /// Gets the object members.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>List{TrackingMember}.</returns>
        private List<TrackingMember> GetObjectMembers(object obj)
        {
            var type = obj?.GetType();
            while (type != null && type.Assembly.IsDynamic)
            {
                type = type.BaseType;
            }
            var fields = new TrackingMembersList();
            this._mapManager.GetMapperImpl(
                type,
                null,
                new MappingConfiguration()
            ).Map(obj, null, fields);

            return fields.TrackingMembers;
        }
    }
}
