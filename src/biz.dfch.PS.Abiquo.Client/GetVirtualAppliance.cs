﻿/**
 * Copyright 2016 d-fens GmbH
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Management.Automation;
using biz.dfch.CS.Abiquo.Client.v1.Model;
using biz.dfch.CS.PowerShell.Commons;

namespace biz.dfch.PS.Abiquo.Client
{
    /// <summary>
    /// This class defines the GetVirtualAppliance Cmdlet that retrieves a list of virtual appliances
    /// </summary>
    [Cmdlet(
         VerbsCommon.Get, "VirtualAppliance"
         ,
         ConfirmImpact = ConfirmImpact.Low
         ,
         DefaultParameterSetName = ParameterSets.LIST
         ,
         SupportsShouldProcess = true
         ,
         HelpUri = "http://dfch.biz/biz/dfch/PS/Abiquo/Client/Get-VirtualAppliance/"
     )]
    [OutputType(typeof(VirtualAppliance))]
    public class GetVirtualAppliance : PsCmdletBase
    {
        /// <summary>
        /// Defines all valid parameter sets for this cmdlet
        /// </summary>
        public static class ParameterSets
        {
            /// <summary>
            /// ParameterSetName used when requesting all entities
            /// </summary>
            public const string LIST = "list";

            /// <summary>
            /// ParameterSetName used when requesting an entity by id
            /// </summary>
            public const string ID = "id";

            /// <summary>
            /// ParameterSetName used when requesting an entity by name
            /// </summary>
            public const string NAME = "name";
        }

        /// <summary>
        /// Specifies the entity id
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSets.ID)]
        [ValidateRange(1, int.MaxValue)]
        public int Id { get; set; }

        /// <summary>
        /// Specifies the entity name
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ParameterSets.NAME)]
        public string Name { get; set; }

        /// <summary>
        /// VirtualDataCenterId
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSets.LIST)]
        [Parameter(Mandatory = true, ParameterSetName = ParameterSets.ID)]
        [Parameter(Mandatory = true, ParameterSetName = ParameterSets.NAME)]
        [ValidateRange(1, int.MaxValue)]
        [Alias("vdc")]
        public int VirtualDataCenterId { get; set; }

        /// <summary>
        /// Retrieve all entities for the current enterprise
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = ParameterSets.LIST)]
        public SwitchParameter ListAvailable { get; set; }

        /// <summary>
        /// ProcessRecord
        /// </summary>
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            Contract.Assert(null != ModuleConfiguration.Current.Client);
            Contract.Assert(ModuleConfiguration.Current.Client.IsLoggedIn);

            var shouldProcessMessage = string.Format(Messages.GetVirtualApplianceShouldProcess, ParameterSetName);
            if (!ShouldProcess(shouldProcessMessage))
            {
                return;
            }

            switch (ParameterSetName)
            {
                case ParameterSets.LIST:
                {
                    ProcessParameterSetList();
                    return;
                }

                case ParameterSets.ID:
                {
                    ProcessParameterSetId();
                    return;
                }

                case ParameterSets.NAME:
                {
                    ProcessParameterSetName();
                    return;
                }

                default:
                    const bool isValidParameterSetName = false;
                    Contract.Assert(isValidParameterSetName, ParameterSetName);
                    break;
            }

        }

        private void ProcessParameterSetId()
        {
            try
            {
                var result = ModuleConfiguration.Current.Client.GetVirtualAppliance(VirtualDataCenterId, Id);
                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(ErrorRecordFactory.GetGeneric(ex));
                WriteError(ErrorRecordFactory.GetNotFound(Messages.GetVirtualApplianceIdNotFound,
                    Constants.EventId.GetVirtualApplianceIdNotFound.ToString(), Id, VirtualDataCenterId));
            }
        }

        private void ProcessParameterSetName()
        {
            var collection = ModuleConfiguration.Current.Client
                                 .GetVirtualAppliances(VirtualDataCenterId)
                                 .Collection ?? new List<VirtualAppliance>();
            var results = collection
                .Where(e => Name.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (0 == results.Count)
            {
                WriteError(ErrorRecordFactory.GetNotFound(Messages.GetVirtualApplianceNameNotFound,
                    Constants.EventId.GetVirtualApplianceNameNotFound.ToString(), Name, VirtualDataCenterId));
                return;
            }

            results.ForEach(WriteObject);
        }

        private void ProcessParameterSetList()
        {
            var virtualDataCenters = new List<VirtualDataCenter>();

            if (0 >= VirtualDataCenterId)
            {
                virtualDataCenters = ModuleConfiguration.Current.Client
                                         .GetVirtualDataCenters()
                                         .Collection ?? new List<VirtualDataCenter>();
            }
            else
            {
                virtualDataCenters.Add(new VirtualDataCenter() {Id = VirtualDataCenterId});
            }

            try
            {
                var collection = new List<VirtualAppliance>();
                foreach (var virtualDataCenter in virtualDataCenters)
                {
                    collection = ModuleConfiguration.Current.Client
                                                .GetVirtualAppliances(virtualDataCenter.Id)
                                                .Collection ?? new List<VirtualAppliance>();
                }

                collection.ForEach(WriteObject);
            }
            catch (Exception ex)
            {
                WriteError(ErrorRecordFactory.GetGeneric(ex));
                WriteError(ErrorRecordFactory.GetNotFound(Messages.GetVirtualApplianceVdcNotFound,
                    Constants.EventId.GetVirtualApplianceVirtualDataCenterNotFound.ToString(), VirtualDataCenterId));
            }
        }
    }
}
