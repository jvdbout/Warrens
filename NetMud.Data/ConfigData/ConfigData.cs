﻿using NetMud.Data.System;
using NetMud.DataAccess;
using NetMud.DataAccess.Cache;
using NetMud.DataStructure.Base.System;
using NetMud.DataStructure.Behaviors.System;
using NetMud.DataStructure.SupportingClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace NetMud.Data.ConfigData
{
    /// <summary>
    /// Configuration data. Only one of these spawns forever
    /// </summary>
    [Serializable]
    public abstract class ConfigData : SerializableDataPartial, IConfigData
    {
        /// <summary>
        /// The unique key used to identify, store and retrieve data
        /// </summary>
        [ScriptIgnore]
        [JsonIgnore]
        public virtual string UniqueKey => Name;

        /// <summary>
        /// The type of data this is (for storage)
        /// </summary>
        [ScriptIgnore]
        [JsonIgnore]
        public abstract ConfigDataType Type { get; }

        /// <summary>
        /// The unique name of this configuration data
        /// </summary>
        public string Name { get; set; }

        #region Approval System
        /// <summary>
        /// What type of approval is necessary for this content
        /// </summary>
        [ScriptIgnore]
        [JsonIgnore]
        public virtual ContentApprovalType ApprovalType => ContentApprovalType.Admin; //Config data defaults to admin

        /// <summary>
        /// Is this able to be seen and used for live purposes
        /// </summary>
        public bool SuitableForUse => State == ApprovalState.Approved || ApprovalType == ContentApprovalType.None || ApprovalType == ContentApprovalType.ReviewOnly;

        /// <summary>
        /// Has this been approved?
        /// </summary>
        public ApprovalState State { get; set; }

        /// <summary>
        /// When was this approved
        /// </summary>
        public DateTime ApprovedOn { get; set; }

        /// <summary>
        /// Who created this thing, their GlobalAccountHandle
        /// </summary>
        public string CreatorHandle { get; set; }

        /// <summary>
        /// The creator's account permissions level
        /// </summary>
        public StaffRank CreatorRank { get; set; }

        [ScriptIgnore]
        [JsonIgnore]
        private IAccount _creator { get; set; }

        /// <summary>
        /// Who created this thing
        /// </summary>
        [JsonIgnore]
        [ScriptIgnore]
        public IAccount Creator
        {
            get
            {
                if (_creator == null && !string.IsNullOrWhiteSpace(CreatorHandle))
                    _creator = Account.GetByHandle(CreatorHandle);

                return _creator;
            }
            set
            {
                if (value != null)
                    CreatorHandle = value.GlobalIdentityHandle;
                else
                    CreatorHandle = string.Empty;

                _creator = value;
            }
        }

        /// <summary>
        /// Who approved this thing, their GlobalAccountHandle
        /// </summary>
        public string ApproverHandle { get; set; }

        /// <summary>
        /// The approver's account permissions level
        /// </summary>
        public StaffRank ApproverRank { get; set; }

        [ScriptIgnore]
        [JsonIgnore]
        private IAccount _approvedBy { get; set; }

        /// <summary>
        /// Who approved this thing
        /// </summary>
        [JsonIgnore]
        [ScriptIgnore]
        public IAccount ApprovedBy
        {
            get
            {
                if (_approvedBy == null && !string.IsNullOrWhiteSpace(ApproverHandle))
                    _approvedBy = Account.GetByHandle(ApproverHandle);

                return _approvedBy;
            }
            set
            {
                if (value != null)
                    ApproverHandle = value.GlobalIdentityHandle;
                else
                    ApproverHandle = string.Empty;

                _approvedBy = value;
            }
        }

        /// <summary>
        /// Can the given rank approve this or not
        /// </summary>
        /// <param name="rank">Approver's rank</param>
        /// <returns>If it can</returns>
        public bool CanIBeApprovedBy(StaffRank rank, IAccount approver)
        {
            return rank == StaffRank.Admin || rank >= CreatorRank || Creator.Equals(approver);
        }

        /// <summary>
        /// Change the approval status of this thing
        /// </summary>
        /// <returns>success</returns>
        public bool ChangeApprovalStatus(IAccount approver, StaffRank rank, ApprovalState newState)
        {
            //Can't approve/deny your own stuff
            if (rank < StaffRank.Admin && Creator.Equals(approver))
                return false;

            var accessor = new DataAccess.FileSystem.ConfigData();
            ApproveMe(approver, rank, newState);

            PersistToCache();
            accessor.WriteEntity(this);

            return true;
        }

        /// <summary>
        /// Get the significant details of what needs approval
        /// </summary>
        /// <returns>A list of strings</returns>
        public virtual IDictionary<string, string> SignificantDetails()
        {
            var returnList = new Dictionary<string, string>
            {
                { "Name", Name },
                { "Creator", CreatorHandle },
                { "Creator Rank", CreatorRank.ToString() }
           };

            return returnList;
        }

        internal void ApproveMe(IAccount approver, StaffRank rank, ApprovalState state = ApprovalState.Approved)
        {
            State = state;
            ApprovedBy = approver;
            ApprovedOn = DateTime.Now;
            ApproverRank = rank;
        }
        #endregion

        #region Caching
        /// <summary>
        /// What type of cache is this using
        /// </summary>
        public virtual CacheType CachingType => CacheType.ConfigData;

        /// <summary>
        /// Put it in the cache
        /// </summary>
        /// <returns>success status</returns>
        public virtual bool PersistToCache()
        {
            try
            {
                ConfigDataCache.Add(this);
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex, LogChannels.SystemWarnings);
                return false;
            }

            return true;
        }
        #endregion

        #region Data persistence functions
        /// <summary>
        /// Remove this object from the db permenantly
        /// </summary>
        /// <returns>success status</returns>
        public virtual bool Remove(IAccount remover, StaffRank rank)
        {
            var accessor = new DataAccess.FileSystem.ConfigData();

            try
            {                
                //Not allowed to remove stuff you didn't make unless you're an admin, TODO: Make this more nuanced for guilds
                if (rank < StaffRank.Admin && !remover.Equals(Creator))
                {
                    return false;
                }

                //Remove from cache first
                ConfigDataCache.Remove(new ConfigDataCacheKey(this));

                //Remove it from the file system.
                accessor.ArchiveEntity(this);
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Update the field data for this object to the db
        /// </summary>
        /// <returns>success status</returns>
        public virtual bool Save(IAccount editor, StaffRank rank)
        {
            var accessor = new DataAccess.FileSystem.ConfigData();

            try
            {
                //Not allowed to edit stuff you didn't make unless you're an admin, TODO: Make this more nuanced for guilds
                if (ApprovalType != ContentApprovalType.None && rank < StaffRank.Admin && !editor.Equals(Creator))
                {
                    return false;
                }

                //Disapprove of things first
                State = ApprovalState.Pending;
                ApprovedBy = null;
                ApprovedOn = DateTime.MinValue;

                //Figure out automated approvals, always throw reviewonly in there
                if (rank < StaffRank.Admin && ApprovalType != ContentApprovalType.ReviewOnly)
                {
                    switch (ApprovalType)
                    {
                        case ContentApprovalType.None:
                            ApproveMe(editor, rank);
                            break;
                        case ContentApprovalType.Leader:
                            if (rank == StaffRank.Builder)
                                ApproveMe(editor, rank);
                            break;
                    }
                }
                else
                {
                    //Staff Admin always get approved
                    ApproveMe(editor, rank);
                }

                if (Creator == null)
                {
                    Creator = editor;
                    CreatorRank = rank;
                }

                PersistToCache();
                accessor.WriteEntity(this);
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Update the field data for this object to the db
        /// </summary>
        /// <returns>success status</returns>
        public virtual bool SystemSave()
        {
            var accessor = new DataAccess.FileSystem.ConfigData();

            try
            {
                if (String.IsNullOrWhiteSpace(CreatorHandle))
                    CreatorHandle = DataHelpers.SystemUserHandle;

                //only able to edit its own crap
                if (CreatorHandle != DataHelpers.SystemUserHandle)
                    return false;

                State = ApprovalState.Approved;
                ApproverHandle = DataHelpers.SystemUserHandle;
                ApprovedOn = DateTime.Now;
                ApproverRank = StaffRank.Builder;

                PersistToCache();
                accessor.WriteEntity(this);
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex);
                return false;
            }

            return true;
        }

        #endregion
    }
}