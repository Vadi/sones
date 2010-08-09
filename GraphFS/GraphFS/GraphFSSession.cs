﻿/*
* sones GraphDB - OpenSource Graph Database - http://www.sones.com
* Copyright (C) 2007-2010 sones GmbH
*
* This file is part of sones GraphDB OpenSource Edition.
*
* sones GraphDB OSE is free software: you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License as published by
* the Free Software Foundation, version 3 of the License.
*
* sones GraphDB OSE is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU Affero General Public License for more details.
*
* You should have received a copy of the GNU Affero General Public License
* along with sones GraphDB OSE. If not, see <http://www.gnu.org/licenses/>.
*/

/*
 * GraphFSSession
 * Achim Friedland, 2008 - 2010
 */

#region Usings

using System;
using System.IO;
using System.Collections.Generic;

using sones.Notifications;
using sones.Lib.DataStructures;
using sones.Lib.ErrorHandling;

using sones.StorageEngines;
using sones.GraphFS.Caches;
using sones.GraphFS.Events;
using sones.GraphFS.InternalObjects;
using sones.GraphFS.Objects;
using sones.GraphFS.DataStructures;
using sones.GraphFS.Errors;
using sones.Lib.Session;
using sones.GraphFS.Transactions;
using System.Text;
using sones.Lib.DataStructures.Indices;
using sones.Lib.DataStructures.WeakReference;

#endregion

namespace sones.GraphFS.Session
{

    /// <summary>
    /// The session interface for all graph file systems
    /// </summary>

    public class GraphFSSession : IGraphFSSession
    {


        #region Properties

        #region IGraphFS

        public IGraphFS IGraphFS { get; private set; }

        #endregion

        #region Implemenation

        public String Implemenation
        {

            get
            {

                if (IGraphFS == null)
                    return String.Empty;

                return IGraphFS.GetType().Name;

            }

        }

        #endregion

        #region SessionToken

        public SessionToken SessionToken { get; private set; }

        #endregion

        #endregion

        #region Events

        //#region OnExceptionOccurred(myEventArgs)

        //public event ExceptionOccuredEvent OnExceptionOccurred;

        //protected void OnExceptionOccured(Object mySender, ExceptionOccuredEventArgs myEventArgs)
        //{
        //    Debug.WriteLine("[!CRITICAL Exception][" + mySender.GetType().FullName + "] " + myEventArgs.Exception.Message + Environment.NewLine + myEventArgs.Exception.StackTrace);
        //    // if (OnExceptionOccurred != null)
        //    OnExceptionOccurred(mySender, myEventArgs);
        //}

        //#endregion


        #region OnLoad

        /// <summary>
        /// An event to be notified whenever a AFSObject is
        /// ready to be loaded.
        /// </summary>
        public event GraphFSEventHandlers.OnLoadEventHandler OnLoad
        {

            add
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnLoad += value;
                }
            }

            remove
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnLoad -= value;
                }
            }

        }

        #endregion

        #region OnLoaded

        /// <summary>
        /// An event to be notified whenever a AFSObject
        /// was successfully loaded.
        /// </summary>
        public event GraphFSEventHandlers.OnLoadedEventHandler OnLoaded
        {

            add
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnLoaded += value;
                }
            }

            remove
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnLoaded -= value;
                }
            }

        }

        #endregion

        #region OnSave

        /// <summary>
        /// An event to be notified whenever a AFSObject
        /// is ready to be saved.
        /// </summary>
        public event GraphFSEventHandlers.OnSaveEventHandler OnSave
        {

            add
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnSave += value;
                }
            }

            remove
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnSave -= value;
                }
            }

        }

        #endregion

        #region OnSaved

        /// <summary>
        /// An event to be notified whenever a AFSObject
        /// was successfully saved on disc.
        /// </summary>
        public event GraphFSEventHandlers.OnSavedEventHandler OnSaved
        {

            add
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnSaved += value;
                }
            }

            remove
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnSaved -= value;
                }
            }

        }

        #endregion

        #region OnRemove

        /// <summary>
        /// An event to be notified whenever a AFSObject
        /// is ready to be removed.
        /// </summary>
        public event GraphFSEventHandlers.OnRemoveEventHandler OnRemove
        {

            add
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnRemove += value;
                }
            }

            remove
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnRemove -= value;
                }
            }

        }

        #endregion

        #region OnSaved

        /// <summary>
        /// An event to be notified whenever a AFSObject
        /// was successfully removed.
        /// </summary>
        public event GraphFSEventHandlers.OnRemovedEventHandler OnRemoved
        {

            add
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnRemoved += value;
                }
            }

            remove
            {
                lock (IGraphFS)
                {
                    IGraphFS.OnRemoved -= value;
                }
            }

        }

        #endregion

        #endregion

        #region Constructor(s)

        #region GraphFSSession(myPandoraFS, myUsername)

        /// <summary>
        /// This will create a new PandoraFS session on an existing PandoraFS and will verify the given credentials
        /// </summary>
        /// <param name="myPandoraFS"></param>
        /// <param name="myUsername"></param>
        public GraphFSSession(IGraphFS myIPandoraFS, String myUsername)
        {

            IGraphFS = myIPandoraFS;
            var sessionInfo = new FSSessionInfo(myUsername);            
            SessionToken    = new SessionToken(sessionInfo);

        }

        #endregion

        #endregion


        #region Session specific Members

        public IGraphFSSession CreateNewSession(String myUsername)
        {
            return new GraphFSSession(IGraphFS, myUsername);
        }

        #endregion


        #region IGraphFSSession Members

        #region Information Methods

        #region IsMounted

        public Boolean IsMounted
        {

            get
            {

                if (IGraphFS == null)
                    return false;
                
                return IGraphFS.IsMounted; 
            
            }

        }

        #endregion

        #region IsPersistent

        public Boolean IsPersistent
        {
            
            get
            {

                if (IGraphFS == null)
                    return false;

                return IGraphFS.IsPersistent;

            }
        
        }

        #endregion


        #region TraverseChildFSs(myFunc, myDepth, mySessionToken)

        public IEnumerable<Object> TraverseChildFSs(Func<IGraphFS, UInt64, IEnumerable<Object>> myFunc, UInt64 myDepth)
        {
            return IGraphFS.TraverseChildFSs(myFunc, myDepth, SessionToken);
        }

        #endregion

        
        public FileSystemUUID GetFileSystemUUID()
        {
            return IGraphFS.GetFileSystemUUID(SessionToken);
        }

        public FileSystemUUID GetFileSystemUUID(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetFileSystemUUID(myObjectLocation, SessionToken);
        }

        public IEnumerable<FileSystemUUID> GetFileSystemUUIDs(UInt64 myDepth)
        {
            return IGraphFS.GetFileSystemUUIDs(myDepth, SessionToken);
        }

        public String GetFileSystemDescription()
        {
            return IGraphFS.GetFileSystemDescription(SessionToken);
        }

        public String GetFileSystemDescription(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetFileSystemDescription(myObjectLocation, SessionToken);
        }

        public IEnumerable<String> GetFileSystemDescriptions(UInt64 myDepth)
        {
            return IGraphFS.GetFileSystemDescriptions(myDepth, SessionToken);
        }

        public void SetFileSystemDescription(String myFileSystemDescription)
        {
            IGraphFS.SetFileSystemDescription(myFileSystemDescription, SessionToken);
        }

        public void SetFileSystemDescription(ObjectLocation myObjectLocation, String myFileSystemDescription)
        {
            IGraphFS.SetFileSystemDescription(myObjectLocation, myFileSystemDescription, SessionToken);
        }

        public UInt64 GetNumberOfBytes()
        {
            return IGraphFS.GetNumberOfBytes(SessionToken);
        }

        public UInt64 GetNumberOfBytes(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetNumberOfBytes(myObjectLocation, SessionToken);
        }

        public IEnumerable<UInt64> GetNumberOfBytes(Boolean myRecursiveOperation)
        {
            return IGraphFS.GetNumberOfBytes(myRecursiveOperation, SessionToken);
        }

        public UInt64 GetNumberOfFreeBytes()
        {
            return IGraphFS.GetNumberOfFreeBytes(SessionToken);
        }

        public UInt64 GetNumberOfFreeBytes(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetNumberOfFreeBytes(myObjectLocation, SessionToken);
        }

        public IEnumerable<UInt64> GetNumberOfFreeBytes(Boolean myRecursiveOperation)
        {
            return IGraphFS.GetNumberOfFreeBytes(myRecursiveOperation, SessionToken);
        }

        public AccessModeTypes GetAccessMode()
        {
            return IGraphFS.GetAccessMode(SessionToken);
        }

        public AccessModeTypes GetAccessMode(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetAccessMode(myObjectLocation, SessionToken);
        }

        public IEnumerable<AccessModeTypes> GetAccessModes(Boolean myRecursiveOperation)
        {
            return IGraphFS.GetAccessModes(myRecursiveOperation, SessionToken);
        }

#warning "Change to method"
        public IGraphFS ParentFileSystem
        {
            get
            {
                return IGraphFS.ParentFileSystem;
            }
            set
            {
                IGraphFS.ParentFileSystem = value;
            }
        }

        public IEnumerable<ObjectLocation> GetChildFileSystemMountpoints(Boolean myRecursiveOperation)
        {
            return IGraphFS.GetChildFileSystemMountpoints(myRecursiveOperation, SessionToken);
        }

        public IGraphFS GetChildFileSystem(ObjectLocation myObjectLocation, Boolean myRecursive)
        {
            return IGraphFS.GetChildFileSystem(myObjectLocation, myRecursive, SessionToken);
        }

        public NotificationDispatcher GetNotificationDispatcher()
        {
            return IGraphFS.GetNotificationDispatcher(SessionToken);
        }

        public NotificationDispatcher GetNotificationDispatcher(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetNotificationDispatcher(myObjectLocation, SessionToken);
        }

        public NotificationSettings GetNotificationSettings()
        {
            return IGraphFS.GetNotificationSettings(SessionToken);
        }

        public NotificationSettings GetNotificationSettings(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetNotificationSettings(myObjectLocation, SessionToken);
        }

        public void SetNotificationDispatcher(NotificationDispatcher myNotificationDispatcher)
        {
            IGraphFS.SetNotificationDispatcher(myNotificationDispatcher, SessionToken);
        }

        public void SetNotificationDispatcher(ObjectLocation myObjectLocation, NotificationDispatcher myNotificationDispatcher)
        {
            IGraphFS.SetNotificationDispatcher(myObjectLocation, myNotificationDispatcher, SessionToken);
        }

        public void SetNotificationSettings(NotificationSettings myNotificationSettings)
        {
            IGraphFS.SetNotificationSettings(myNotificationSettings, SessionToken);
        }

        public void SetNotificationSettings(ObjectLocation myObjectLocation, NotificationSettings myNotificationSettings)
        {
            IGraphFS.SetNotificationSettings(myObjectLocation, myNotificationSettings, SessionToken);
        }

        #endregion

        #region ObjectCache

        public ObjectCacheSettings GetObjectCacheSettings()
        {
            return IGraphFS.GetObjectCacheSettings(SessionToken);
        }

        public ObjectCacheSettings GetObjectCacheSettings(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetObjectCacheSettings(myObjectLocation, SessionToken);
        }

        public void SetObjectCacheSettings(ObjectCacheSettings myObjectCacheSettings)
        {
            IGraphFS.SetObjectCacheSettings(myObjectCacheSettings, SessionToken);
        }

        public void SetObjectCacheSettings(ObjectLocation myObjectLocation, ObjectCacheSettings myObjectCacheSettings)
        {
            IGraphFS.SetObjectCacheSettings(myObjectLocation, myObjectCacheSettings, SessionToken);
        }

        #endregion

        #region MakeFileSystem

        public Exceptional<FileSystemUUID> MakeFileSystem(String myStorageLocation, String myDescription, UInt64 myNumberOfBytes, Boolean myOverwriteExistingFileSystem, Action<Double> myAction)
        {

            var _MKFSBufferSize  = 1 * 1024 * 1024u;
            var _IStorageEngines = new List<IStorageEngine>();

            if (myOverwriteExistingFileSystem)
                _IStorageEngines.Add(StorageEngineFactory.Instance.CreateIStorageEngine(myStorageLocation, myNumberOfBytes, _MKFSBufferSize, myOverwriteExistingFileSystem, myAction));
            
            else
                _IStorageEngines.Add(StorageEngineFactory.Instance.ActivateIStorageEngine(myStorageLocation).Value);

            var _Exceptional = IGraphFS.MakeFileSystem(_IStorageEngines, myDescription, myOverwriteExistingFileSystem, myAction, SessionToken);

            foreach (var _IStorageEngine in _IStorageEngines)
                _IStorageEngine.DetachStorage();

            return _Exceptional;

        }

        public void GrowFileSystem(UInt64 myNumberOfBytesToAdd)
        {
            IGraphFS.GrowFileSystem(myNumberOfBytesToAdd, SessionToken);
        }

        public void ShrinkFileSystem(UInt64 myNumberOfBytesToRemove)
        {
            IGraphFS.ShrinkFileSystem(myNumberOfBytesToRemove, SessionToken);
        }

        #endregion

        #region Mount

        public void MountFileSystem(String myStorageLocation, AccessModeTypes myFSAccessMode)
        {
            IGraphFS.MountFileSystem(myStorageLocation, myFSAccessMode, SessionToken);
        }

        public void MountFileSystem(IGraphFSSession myIGraphFSSession, ObjectLocation myMountPoint, AccessModeTypes myFSAccessMode)
        {
            IGraphFS.MountFileSystem(myIGraphFSSession.IGraphFS, myMountPoint, myFSAccessMode, SessionToken);
        }

        public void MountFileSystem(String myStorageLocation, ObjectLocation myMountPoint, AccessModeTypes myFSAccessMode)
        {
            IGraphFS.MountFileSystem(myStorageLocation, myMountPoint, myFSAccessMode, SessionToken);
        }


        public void RemountFileSystem(AccessModeTypes myFSAccessMode)
        {
            IGraphFS.RemountFileSystem(myFSAccessMode, SessionToken);
        }

        public void RemountFileSystem(ObjectLocation myMountPoint, AccessModeTypes myFSAccessMode)
        {
            IGraphFS.RemountFileSystem(myMountPoint, myFSAccessMode, SessionToken);
        }


        public void UnmountFileSystem()
        {
            IGraphFS.UnmountFileSystem(SessionToken);
        }

        public void UnmountFileSystem(ObjectLocation myMountPoint)
        {
            IGraphFS.UnmountFileSystem(myMountPoint, SessionToken);
        }

        public void UnmountAllFileSystems()
        {
            IGraphFS.UnmountAllFileSystems(SessionToken);
        }

        #endregion

        public void ChangeRootDirectory(String myChangeRootPrefix)
        {
            IGraphFS.ChangeRootDirectory(myChangeRootPrefix, SessionToken);
        }


        public Trinary ResolveObjectLocation(ref ObjectLocation myObjectLocation, out IEnumerable<String> myObjectStreams, out ObjectLocation myObjectPath, out String myObjectName, out IDirectoryObject myIDirectoryObject, out IGraphFS myIPandoraFS)
        {
            throw new NotImplementedException();
            //return IGraphFS.ResolveObjectLocation(ref myObjectLocation, out myObjectStreams, out myObjectPath, out myObjectName, out myIDirectoryObject, out myIPandoraFS, SessionToken);
        }

        public String ResolveObjectLocation(ObjectLocation myObjectLocation, Boolean myThrowObjectNotFoundException)
        {
            throw new NotImplementedException();
            //return IGraphFS.ResolveObjectLocation(myObjectLocation, myThrowObjectNotFoundException, SessionToken);
        }



        #region INode and ObjectLocator

        #region GetINode(myObjectLocation)

        public Exceptional<INode> GetINode(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetINode(myObjectLocation, SessionToken);
        }

        #endregion

        #region GetObjectLocator(ObjectLocation)

        public Exceptional<ObjectLocator> GetObjectLocator(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetObjectLocator(myObjectLocation, SessionToken);
        }

        #endregion

        #endregion

        #region Object specific methods

        #region LockFSObject(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectLock, myObjectLockType, myLockingTime)

        public Exceptional LockFSObject(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, ObjectRevisionID myObjectRevisionID, ObjectLocks myObjectLock, ObjectLockTypes myObjectLockType, UInt64 myLockingTime)
        {
            return IGraphFS.LockFSObject(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectLock, myObjectLockType, myLockingTime, SessionToken);
        }

        #endregion


        #region GetOrCreateFSObject<PT>(myObjectLocation)

        public Exceptional<PT> GetOrCreateFSObject<PT>(ObjectLocation myObjectLocation) where PT : AFSObject, new()
        {
            var _AFSObject = IGraphFS.GetOrCreateFSObject<PT>(myObjectLocation, null, null, null, 0, false, SessionToken);
            if (_AFSObject != null && _AFSObject.Value != null)
                _AFSObject.Value.IGraphFSSessionReference = this;
            return _AFSObject;
        }

        #endregion

        #region GetOrCreateFSObject<PT>(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectCopy, myIgnoreIntegrityCheckFailures)

        public Exceptional<PT> GetOrCreateFSObject<PT>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null, UInt64 myObjectCopy = 0, Boolean myIgnoreIntegrityCheckFailures = false) where PT : AFSObject, new()
        {

            var _AFSObject = IGraphFS.GetOrCreateFSObject<PT>(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectCopy, myIgnoreIntegrityCheckFailures, SessionToken);

            if (_AFSObject != null && _AFSObject.Value != null)
            {
                _AFSObject.Value.IGraphFSSessionReference = new WeakReference<IGraphFSSession>(this);
                _AFSObject.Value.IGraphFSReference        = new WeakReference<IGraphFS>(IGraphFS);
            }

            return _AFSObject;

        }

        #endregion

        #region GetOrCreateFSObject<PT>(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectCopy, myIgnoreIntegrityCheckFailures, myFunc, SessionToken)

        public Exceptional<PT> GetOrCreateFSObject<PT>(ObjectLocation myObjectLocation, String myObjectStream, Func<PT> myFunc, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null, UInt64 myObjectCopy = 0, Boolean myIgnoreIntegrityCheckFailures = false) where PT : AFSObject
        {
            var _AFSObject = IGraphFS.GetOrCreateFSObject<PT>(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectCopy, myIgnoreIntegrityCheckFailures, myFunc, SessionToken);
            if (_AFSObject != null && _AFSObject.Value != null)
                _AFSObject.Value.IGraphFSSessionReference = new WeakReference<IGraphFSSession>(this);
            return _AFSObject;
        }

        #endregion


        #region GetFSObject<PT>(myObjectLocation)

        public Exceptional<PT> GetFSObject<PT>(ObjectLocation myObjectLocation) where PT : AFSObject, new()
        {
            var _AFSObject = IGraphFS.GetFSObject<PT>(myObjectLocation, null, null, null, 0, false, SessionToken);
            if (_AFSObject != null && _AFSObject.Value != null)
                _AFSObject.Value.IGraphFSSessionReference = new WeakReference<IGraphFSSession>(this);
            return _AFSObject;
        }

        #endregion

        #region GetFSObject<PT>(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectCopy, myIgnoreIntegrityCheckFailures)

        public Exceptional<PT> GetFSObject<PT>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null, UInt64 myObjectCopy = 0, Boolean myIgnoreIntegrityCheckFailures = false) where PT : AFSObject, new()
        {
            var _AFSObject = IGraphFS.GetFSObject<PT>(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectCopy, myIgnoreIntegrityCheckFailures, SessionToken);
            if (_AFSObject != null && _AFSObject.Value != null)
                _AFSObject.Value.IGraphFSSessionReference = new WeakReference<IGraphFSSession>(this);
            return _AFSObject;
        }

        #endregion

        #region GetFSObject<PT>(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectCopy, myIgnoreIntegrityCheckFailures, myFunc, SessionToken)

        public Exceptional<PT> GetFSObject<PT>(ObjectLocation myObjectLocation, String myObjectStream, Func<PT> myFunc, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null, UInt64 myObjectCopy = 0, Boolean myIgnoreIntegrityCheckFailures = false) where PT : AFSObject
        {
            var _AFSObject = IGraphFS.GetFSObject<PT>(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, myObjectCopy, myIgnoreIntegrityCheckFailures, myFunc, SessionToken);
            if (_AFSObject != null && _AFSObject.Value != null)
                _AFSObject.Value.IGraphFSSessionReference = new WeakReference<IGraphFSSession>(this);
            return _AFSObject;
        }

        #endregion


        #region StoreFSObject(myAFSObject, myAllowOverwritting)

        public Exceptional StoreFSObject(AFSObject myAFSObject, Boolean myAllowOverwritting = false)
        {

            myAFSObject.IGraphFSSessionReference = new WeakReference<IGraphFSSession>(this);

            return IGraphFS.StoreFSObject(myAFSObject.ObjectLocation, myAFSObject, myAllowOverwritting, SessionToken);

        }

        #endregion


        #region ObjectExists(params myObjectLocation)

        public Exceptional<Trinary> ObjectExists(ObjectLocation myObjectLocation)
        {
            return IGraphFS.ObjectExists(myObjectLocation, SessionToken);
        }

        #endregion

        #region ObjectStreamExists(ObjectLocation, myObjectStream)

        public Exceptional<Trinary> ObjectStreamExists(ObjectLocation myObjectLocation, String myObjectStream)
        {
            return IGraphFS.ObjectStreamExists(myObjectLocation, myObjectStream, SessionToken);
        }

        #endregion

        #region ObjectEditionExists(myObjectLocation, myObjectStream, myObjectEdition)

        public Exceptional<Trinary> ObjectEditionExists(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition)
        {
            return IGraphFS.ObjectEditionExists(myObjectLocation, myObjectStream, myObjectEdition, SessionToken);
        }

        #endregion

        #region ObjectRevisionExists(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID)

        public Exceptional<Trinary> ObjectRevisionExists(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null)
        {
            return IGraphFS.ObjectRevisionExists(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, SessionToken);
        }

        #endregion


        #region GetObjectStreams(myObjectLocation)

        public Exceptional<IEnumerable<String>> GetObjectStreams(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetObjectStreams(myObjectLocation, SessionToken);
        }

        #endregion

        #region GetObjectEditions(myObjectLocation, myObjectStream)

        public Exceptional<IEnumerable<String>> GetObjectEditions(ObjectLocation myObjectLocation, String myObjectStream)
        {
            return IGraphFS.GetObjectEditions(myObjectLocation, myObjectStream, SessionToken);
        }

        #endregion

        #region GetObjectRevisionIDs(myObjectLocation, myObjectStream, myObjectEdition)

        public Exceptional<IEnumerable<ObjectRevisionID>> GetObjectRevisionIDs(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition)
        {
            return IGraphFS.GetObjectRevisionIDs(myObjectLocation, myObjectStream, myObjectEdition, SessionToken);
        }

        #endregion


        #region RenameFSObject(myObjectLocation, myNewObjectName)

        public Exceptional RenameFSObject(ObjectLocation myObjectLocation, String myNewObjectName)
        {
            return IGraphFS.RenameObject(myObjectLocation, myNewObjectName, SessionToken);
        }

        #endregion

        #region RemoveFSObject(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID)

        public Exceptional RemoveFSObject(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null)
        {

            lock (this)
            {

                var _Exceptional = new Exceptional();

                if (myObjectEdition == null || myObjectRevisionID == null)
                {

                    return IGraphFS.GetObjectLocator(myObjectLocation, SessionToken).
                        WhenFailed(e => e.PushT(new GraphFSError_CouldNotGetObjectLocator(myObjectLocation))).
                        WhenSucceded<ObjectLocator>(e =>
                        {
                            if (myObjectEdition == null)
                                myObjectEdition = e.Value[myObjectStream].DefaultEditionName;

                            if (myObjectRevisionID == null)
                                myObjectRevisionID = e.Value[myObjectStream].DefaultEdition.LatestRevisionID;

                            return IGraphFS.RemoveFSObject(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, SessionToken).
                                       Convert<ObjectLocator>();

                        });

                }

                return IGraphFS.RemoveFSObject(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, SessionToken);

            }

        }

        #endregion

        #region EraseFSObject(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID)

        public Exceptional EraseFSObject(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, ObjectRevisionID myObjectRevisionID)
        {
            return IGraphFS.EraseFSObject(myObjectLocation, myObjectStream, myObjectEdition, myObjectRevisionID, SessionToken);
        }

        #endregion

        #endregion


        #region Symlink

        public Exceptional AddSymlink(ObjectLocation myObjectLocation, ObjectLocation myTargetLocation)
        {
            return IGraphFS.AddSymlink(myObjectLocation, myTargetLocation, SessionToken);
        }

        public Exceptional AddSymlink(ObjectLocation myObjectLocation, AFSObject myTargetAFSObject)
        {
            return IGraphFS.AddSymlink(myObjectLocation, myTargetAFSObject.ObjectLocation, SessionToken);
        }

        public Exceptional<Trinary> isSymlink(ObjectLocation myObjectLocation)
        {
            return IGraphFS.isSymlink(myObjectLocation, SessionToken);
        }

        public Exceptional<ObjectLocation> GetSymlink(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetSymlink(myObjectLocation, SessionToken);
        }

        public Exceptional RemoveSymlink(ObjectLocation myObjectLocation)
        {
            return IGraphFS.RemoveSymlink(myObjectLocation, SessionToken);
        }

        #endregion

        #region DirectoryObject

        #region CreateDirectoryObject(myObjectLocation)

        public Exceptional<IDirectoryObject> CreateDirectoryObject(ObjectLocation myObjectLocation)
        {

            var _DirectoryObject = IGraphFS.CreateDirectoryObject(myObjectLocation, 0, SessionToken);
            //_DirectoryObject.IGraphFSSessionReference = this;

            return _DirectoryObject;

        }

        #endregion

        #region CreateDirectoryObject(myObjectLocation, myBlocksize)

        public Exceptional<IDirectoryObject> CreateDirectoryObject(ObjectLocation myObjectLocation, UInt64 myBlocksize)
        {

            var _DirectoryObject = IGraphFS.CreateDirectoryObject(myObjectLocation, myBlocksize, SessionToken);
            //_DirectoryObject.IGraphFSSessionReference = this;

            return _DirectoryObject;

        }

        #endregion

        #region CreateDirectoryObjectRecursive(myObjectLocation, myBlocksize, myRecursive)

        public Exceptional<IDirectoryObject> CreateDirectoryObject(ObjectLocation myObjectLocation, UInt64 myBlocksize, Boolean myRecursive)
        {

            var _DirectoryObject = IGraphFS.CreateDirectoryObject(myObjectLocation, myBlocksize, myRecursive, SessionToken);            
            //_DirectoryObject.IGraphFSSessionReference = this;

            return _DirectoryObject;

        }

        #endregion

        #region isIDirectoryObject(params myObjectLocation)

        public Exceptional<Trinary> isIDirectoryObject(ObjectLocation myObjectLocation)
        {
            return IGraphFS.isIDirectoryObject(myObjectLocation, SessionToken);
        }

        #endregion

        #region GetDirectoryListing(myObjectLocation, ...)

        public Exceptional<IEnumerable<String>> GetDirectoryListing(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetDirectoryListing(myObjectLocation, SessionToken);
        }

        public Exceptional<IEnumerable<String>> GetDirectoryListing(ObjectLocation myObjectLocation, Func<KeyValuePair<String, DirectoryEntry>, Boolean> myFunc)
        {
            return IGraphFS.GetDirectoryListing(myObjectLocation, myFunc, SessionToken);
        }

        public Exceptional<IEnumerable<String>> GetFilteredDirectoryListing(ObjectLocation myObjectLocation, String[] myName, String[] myIgnoreName, String[] myRegExpr, List<String> myObjectStreams, List<String> myIgnoreObjectStreams, String[] mySize, String[] myCreationTime, String[] myLastModificationTime, String[] myLastAccessTime, String[] myDeletionTime)
        {
            return IGraphFS.GetFilteredDirectoryListing(myObjectLocation, myName, myIgnoreName, myRegExpr, myObjectStreams, myIgnoreObjectStreams, mySize, myCreationTime, myLastModificationTime, myLastAccessTime, myDeletionTime, SessionToken);
        }

        #endregion

        #region GetExtendedDirectoryListing(params myObjectLocation)

        public Exceptional<IEnumerable<DirectoryEntryInformation>> GetExtendedDirectoryListing(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetExtendedDirectoryListing(myObjectLocation, SessionToken);
        }

        #endregion

        #region GetFilteredExtendedDirectoryListing(myObjectLocation, myName, myIgnoreName, myRegExpr, myObjectStreams, myIgnoreObjectStreams, mySize, myCreationTime, myLastModificationTime, myLastAccessTime, myDeletionTime)

        public Exceptional<IEnumerable<DirectoryEntryInformation>> GetFilteredExtendedDirectoryListing(ObjectLocation myObjectLocation, String[] myName, String[] myIgnoreName, String[] myRegExpr, List<String> myObjectStreams, List<String> myIgnoreObjectStreams, String[] mySize, String[] myCreationTime, String[] myLastModificationTime, String[] myLastAccessTime, String[] myDeletionTime)
        {
            return IGraphFS.GetFilteredExtendedDirectoryListing(myObjectLocation, myName, myIgnoreName, myRegExpr, myObjectStreams, myIgnoreObjectStreams, mySize, myCreationTime, myLastModificationTime, myLastAccessTime, myDeletionTime, SessionToken);
        }

        #endregion

        #region RemoveDirectoryObject(myObjectLocation, MyRemoveRecursive)

        public Exceptional RemoveDirectoryObject(ObjectLocation myObjectLocation, Boolean MyRemoveRecursive)
        {
            return IGraphFS.RemoveDirectoryObject(myObjectLocation, MyRemoveRecursive, SessionToken);
        }

        #endregion

        #region EraseDirectoryObject(myObjectLocation, MyEraseRecursive)

        public Exceptional EraseDirectoryObject(ObjectLocation myObjectLocation, Boolean MyEraseRecursive)
        {
            return IGraphFS.EraseDirectoryObject(myObjectLocation, MyEraseRecursive, SessionToken);
        }

        #endregion

        #endregion

        #region Metadata Maintenance

        #region SetMetadatum(myObjectLocation, myObjectStream, myObjectEdition, myKey, myValue, myIndexSetStrategy)

        public Exceptional SetMetadatum<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey, TValue myValue, IndexSetStrategy myIndexSetStrategy)
        {
            return IGraphFS.SetMetadatum<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey, myValue, myIndexSetStrategy, SessionToken);
        }

        #endregion

        #region SetMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myMetadata, myIndexSetStrategy)

        public Exceptional SetMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, IEnumerable<KeyValuePair<String, TValue>> myMetadata, IndexSetStrategy myIndexSetStrategy)
        {
            return IGraphFS.SetMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myMetadata, myIndexSetStrategy, SessionToken);
        }

        #endregion


        #region MetadatumExists<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey, myValue)

        public Exceptional<Trinary> MetadatumExists<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey, TValue myValue)
        {
            return IGraphFS.MetadatumExists<TValue>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myKey, myValue, SessionToken);
        }

        #endregion

        #region MetadataExists<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myUserMetadataKey)

        public Exceptional<Trinary> MetadataExists<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey)
        {
            return IGraphFS.MetadataExists<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey, SessionToken);
        }

        #endregion


        #region GetMetadatum<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey)

        public Exceptional<IEnumerable<TValue>> GetMetadatum<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey)
        {
            return IGraphFS.GetMetadatum<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey, SessionToken);
        }

        #endregion

        #region GetMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition)

        public Exceptional<IEnumerable<KeyValuePair<String, TValue>>> GetMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition)
        {
            return IGraphFS.GetMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, SessionToken);
        }

        #endregion

        #region GetMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myMinKey, myMaxKey)

        public Exceptional<IEnumerable<KeyValuePair<String, TValue>>> GetMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myMinKey, String myMaxKey)
        {
            return IGraphFS.GetMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myMinKey, myMaxKey, SessionToken);
        }

        #endregion

        #region GetMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myFunc)

        public Exceptional<IEnumerable<KeyValuePair<String, TValue>>> GetMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, Func<KeyValuePair<String, TValue>, Boolean> myFunc)
        {
            return IGraphFS.GetMetadata<TValue>(myObjectLocation, myObjectStream, myObjectStream, myFunc, SessionToken); ;
        }

        #endregion


        #region RemoveMetadatum<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey, myValue)

        public Exceptional RemoveMetadatum<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey, TValue myValue)
        {
            return IGraphFS.RemoveMetadatum<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey, myValue, SessionToken);
        }

        #endregion

        #region RemoveMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey)

        public Exceptional RemoveMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey)
        {
            return IGraphFS.RemoveMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myKey, SessionToken);
        }

        #endregion

        #region RemoveMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myMetadata)

        public Exceptional RemoveMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, IEnumerable<KeyValuePair<String, TValue>> myMetadata)
        {
            return IGraphFS.RemoveMetadata(myObjectLocation, myObjectStream, myObjectEdition, myMetadata, SessionToken);
        }

        #endregion

        #region RemoveMetadata<TValue>(myObjectLocation, myObjectStream, myObjectEdition, myFunc)

        public Exceptional RemoveMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, Func<KeyValuePair<String, TValue>, Boolean> myFunc)
        {
            return IGraphFS.RemoveMetadata(myObjectLocation, myObjectStream, myObjectEdition, myFunc, SessionToken);
        }

        #endregion

        #endregion

        #region UserMetadatum Maintenance

        #region SetMetadatum(myObjectLocation, myKey, myObject, myIndexSetStrategy)

        public Exceptional SetUserMetadatum(ObjectLocation myObjectLocation, String myKey, Object myObject, IndexSetStrategy myIndexSetStrategy)
        {
            return IGraphFS.SetMetadatum<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myKey, myObject, IndexSetStrategy.MERGE, SessionToken);
        }

        #endregion

        #region SetUserMetadata(myObjectLocation, myUserMetadata, myIndexSetStrategy)

        public Exceptional SetUserMetadata(ObjectLocation myObjectLocation, IEnumerable<KeyValuePair<String, Object>> myUserMetadata, IndexSetStrategy myIndexSetStrategy)
        {
            return IGraphFS.SetMetadata<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myUserMetadata, IndexSetStrategy.MERGE, SessionToken);
        }

        #endregion


        #region UserMetadatumExists(myObjectLocation, myKey, myObject)

        public Exceptional<Trinary> UserMetadatumExists(ObjectLocation myObjectLocation, String myKey, Object myObject)
        {
            return IGraphFS.MetadatumExists<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myKey, myObject, SessionToken);
        }

        #endregion

        #region UserMetadataExists(myObjectLocation, myKey)

        public Exceptional<Trinary> UserMetadataExists(ObjectLocation myObjectLocation, String myKey)
        {
            return IGraphFS.MetadataExists<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myKey, SessionToken);
        }

        #endregion


        #region GetUserMetadatum(myObjectLocation, myKey)

        public Exceptional<IEnumerable<Object>> GetUserMetadatum(ObjectLocation myObjectLocation, String myKey)
        {

            var _GetMetadata = IGraphFS.GetMetadatum<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myKey, SessionToken);

            if (_GetMetadata != null && _GetMetadata.Success && _GetMetadata.Value != null)
                return new Exceptional<IEnumerable<Object>>() { Value = _GetMetadata.Value };

            else
                return new Exceptional<IEnumerable<Object>>(_GetMetadata);

        }

        #endregion

        #region GetUserMetadata(myObjectLocation)

        public Exceptional<IEnumerable<KeyValuePair<String, Object>>> GetUserMetadata(ObjectLocation myObjectLocation)
        {

            var _GetMetadata = IGraphFS.GetMetadata<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, SessionToken);

            if (_GetMetadata != null && _GetMetadata.Success && _GetMetadata.Value != null)
                return new Exceptional<IEnumerable<KeyValuePair<String, Object>>>() { Value = _GetMetadata.Value };

            else
                return new Exceptional<IEnumerable<KeyValuePair<String, Object>>>(_GetMetadata);

        }

        #endregion

        #region GetUserMetadata(myObjectLocation, myMinKey, myMaxKey)

        public Exceptional<IEnumerable<KeyValuePair<String, Object>>> GetUserMetadata(ObjectLocation myObjectLocation, String myMinKey, String myMaxKey)
        {

            var _GetMetadata = IGraphFS.GetMetadata<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myMinKey, myMaxKey, SessionToken);

            if (_GetMetadata != null && _GetMetadata.Success && _GetMetadata.Value != null)
                return new Exceptional<IEnumerable<KeyValuePair<String, Object>>>() { Value = _GetMetadata.Value };

            else
                return new Exceptional<IEnumerable<KeyValuePair<String, Object>>>(_GetMetadata);

        }

        #endregion

        #region GetUserMetadata(myObjectLocation, myFunc)

        public Exceptional<IEnumerable<KeyValuePair<String, Object>>> GetUserMetadata(ObjectLocation myObjectLocation, Func<KeyValuePair<String, Object>, Boolean> myFunc)
        {

            var _GetMetadata = IGraphFS.GetMetadata<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myFunc, SessionToken); ;

            if (_GetMetadata != null && _GetMetadata.Success && _GetMetadata.Value != null)
                return new Exceptional<IEnumerable<KeyValuePair<String, Object>>>() { Value = _GetMetadata.Value };

            else
                return new Exceptional<IEnumerable<KeyValuePair<String, Object>>>(_GetMetadata);

        }

        #endregion


        #region RemoveUserMetadatum(myObjectLocation, myKey, myObject)

        public Exceptional RemoveUserMetadatum(ObjectLocation myObjectLocation, String myKey, Object myObject)
        {
            return IGraphFS.RemoveMetadatum<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myKey, myObject, SessionToken);
        }

        #endregion

        #region RemoveUserMetadata(myObjectLocation, myKey)

        public Exceptional RemoveUserMetadata(ObjectLocation myObjectLocation, String myKey)
        {
            return IGraphFS.RemoveMetadata<Object>(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myKey, SessionToken);
        }

        #endregion

        #region RemoveUserMetadata(myObjectLocation, myMetadata)

        public Exceptional RemoveUserMetadata(ObjectLocation myObjectLocation, IEnumerable<KeyValuePair<String, Object>> myMetadata)
        {
            return IGraphFS.RemoveMetadata(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myMetadata, SessionToken);
        }

        #endregion

        #region RemoveUserMetadata(myObjectLocation, myFunc)

        public Exceptional RemoveUserMetadata(ObjectLocation myObjectLocation, Func<KeyValuePair<String, Object>, Boolean> myFunc)
        {
            return IGraphFS.RemoveMetadata(myObjectLocation, FSConstants.USERMETADATASTREAM, FSConstants.DefaultEdition, myFunc, SessionToken);
        }

        #endregion

        #endregion


        #region FileObject

        #region GetFileObject(myObjectLocation)

        public Exceptional<FileObject> GetFileObject(ObjectLocation myObjectLocation)
        {
            return IGraphFS.GetFSObject<FileObject>(myObjectLocation, FSConstants.FILESTREAM, FSConstants.DefaultEdition, null, 0, false, SessionToken);
        }

        #endregion

        #region GetFileObject(myObjectLocation, myRevisionID)

        public Exceptional<FileObject> GetFileObject(ObjectLocation myObjectLocation, ObjectRevisionID myRevisionID)
        {
            return IGraphFS.GetFSObject<FileObject>(myObjectLocation, FSConstants.FILESTREAM, FSConstants.DefaultEdition, myRevisionID, 0, false, SessionToken);
        }

        #endregion

        #region StoreFileObject(myObjectLocation, myData, myAllowOverwritte)

        public Exceptional StoreFileObject(ObjectLocation myObjectLocation, Byte[] myData, Boolean myAllowOverwritte)
        {
            return IGraphFS.StoreFSObject(myObjectLocation, new FileObject() { ObjectLocation = myObjectLocation, ObjectData = myData }, myAllowOverwritte, SessionToken);
        }

        #endregion

        #region StoreFileObject(myObjectLocation, myStringData, myAllowOverwritte)

        public Exceptional StoreFileObject(ObjectLocation myObjectLocation, String myStringData, Boolean myAllowOverwritte)
        {
            return IGraphFS.StoreFSObject(myObjectLocation, new FileObject() { ObjectLocation = myObjectLocation, ObjectData = UTF8Encoding.UTF8.GetBytes(myStringData) }, myAllowOverwritte, SessionToken);
        }

        #endregion

        #endregion

        //#region Rights

        //public Boolean AddRightsStreamToObject(ObjectLocation myObjectLocation, AccessControlObject myRightsObject)
        //{
        //    return IGraphFS.AddRightsStreamToObject(myObjectLocation, myRightsObject, SessionToken);
        //}

        //public Boolean AddEntityToRightsStreamAllowACL(ObjectLocation myObjectLocation, RightUUID myRightUUID, EntityUUID myEntitiyUUID)
        //{
        //    return IGraphFS.AddEntityToRightsStreamAllowACL(myObjectLocation, myRightUUID, myEntitiyUUID, SessionToken);
        //}

        //public Boolean AddEntityToRightsStreamDenyACL(ObjectLocation myObjectLocation, RightUUID myRightUUID, EntityUUID myEntitiyUUID)
        //{
        //    return IGraphFS.AddEntityToRightsStreamDenyACL(myObjectLocation, myRightUUID, myEntitiyUUID, SessionToken);
        //}

        //public Boolean RemoveEntityFromRightsStreamAllowACL(ObjectLocation myObjectLocation, RightUUID myRightUUID, EntityUUID myEntitiyUUID)
        //{
        //    return IGraphFS.RemoveEntityFromRightsStreamAllowACL(myObjectLocation, myRightUUID, myEntitiyUUID, SessionToken);
        //}

        //public Boolean RemoveEntityFromRightsStreamDenyACL(ObjectLocation myObjectLocation, RightUUID myRightUUID, EntityUUID myEntitiyUUID)
        //{
        //    return IGraphFS.RemoveEntityFromRightsStreamDenyACL(myObjectLocation, myRightUUID, myEntitiyUUID, SessionToken);
        //}

        //public Boolean ChangeAllowOverDenyOfRightsStream(ObjectLocation myObjectLocation, DefaultRuleTypes myDefaultRule)
        //{
        //    return IGraphFS.ChangeAllowOverDenyOfRightsStream(myObjectLocation, myDefaultRule, SessionToken);
        //}

        //public Boolean AddAlertToPandoraRightsAlertHandlingList(ObjectLocation myObjectLocation, NHAccessControlObject myAlert)
        //{
        //    return IGraphFS.AddAlertToPandoraRightsAlertHandlingList(myObjectLocation, myAlert, SessionToken);
        //}

        //public Boolean RemoveAlertFromPandoraRightsAlertHandlingList(ObjectLocation myObjectLocation, NHAccessControlObject myAlert)
        //{
        //    return IGraphFS.RemoveAlertFromPandoraRightsAlertHandlingList(myObjectLocation, myAlert, SessionToken);
        //}

        //public List<Right> EvaluateRightsForEntity(ObjectLocation myObjectLocation, EntityUUID myEntityGuid, AccessControlObject myRightsObject)
        //{
        //    return IGraphFS.EvaluateRightsForEntity(myObjectLocation, myEntityGuid, myRightsObject, SessionToken);
        //}

        //#endregion
       
        //#region Enitity

        //public EntityUUID AddEntity(ObjectLocation myObjectLocation, String myLogin, String myRealname, String myDescription, Dictionary<ContactTypes, List<String>> myContacts, String myPassword, List<PublicKey> myPublicKeyList, HashSet<EntityUUID> myMembership)
        //{
        //    return IGraphFS.AddEntity(myObjectLocation, myLogin, myRealname, myDescription, myContacts, myPassword, myPublicKeyList, myMembership, SessionToken);
        //}

        //public Trinary EntityExists(ObjectLocation myObjectLocation, EntityUUID myEntityUUID)
        //{
        //    return IGraphFS.EntityExists(myObjectLocation, myEntityUUID, SessionToken);
        //}

        //public EntityUUID GetEntityUUID(ObjectLocation myObjectLocation, String aName)
        //{
        //    return IGraphFS.GetEntityUUID(myObjectLocation, aName, SessionToken);
        //}

        //public Boolean RemoveEntity(ObjectLocation myObjectLocation, EntityUUID myEntityUUID)
        //{
        //    return IGraphFS.RemoveEntity(myObjectLocation, myEntityUUID, SessionToken);
        //}

        //public HashSet<EntityUUID> GetMemberships(ObjectLocation myObjectLocation, EntityUUID myEntityUUID, Boolean myRecursion)
        //{
        //    return IGraphFS.GetMemberships(myObjectLocation, myEntityUUID, myRecursion, SessionToken);
        //}

        //public void ChangeEntityPassword(ObjectLocation myObjectLocation, EntityUUID myEntityUUID, String myOldPassword, String myNewPassword)
        //{
        //    IGraphFS.ChangeEntityPassword(myObjectLocation, myEntityUUID, myOldPassword, myNewPassword, SessionToken);
        //}

        //public void ChangeEntityPublicKeyList(ObjectLocation myObjectLocation, EntityUUID myEntityUUID, String myPassword, List<PublicKey> myNewPublicKeyList)
        //{
        //    IGraphFS.ChangeEntityPublicKeyList(myObjectLocation, myEntityUUID, myPassword, myNewPublicKeyList, SessionToken);
        //}

        //public void ChangeEntityAddMembership(ObjectLocation myObjectLocation, EntityUUID myEntityUUID, EntityUUID myNewMembershipUUID)
        //{
        //     IGraphFS.ChangeEntityAddMembership(myObjectLocation, myEntityUUID, myNewMembershipUUID, SessionToken);
        //}

        //public void ChangeEntityRemoveMembership(ObjectLocation myObjectLocation, EntityUUID myEntityUUID, EntityUUID myNewMembershipUUID)
        //{
        //    IGraphFS.ChangeEntityRemoveMembership(myObjectLocation, myEntityUUID, myNewMembershipUUID, SessionToken);
        //}

        //public List<PublicKey> GetEntityPublicKeyList(ObjectLocation myObjectLocation, EntityUUID myEntityUUID)
        //{
        //    return IGraphFS.GetEntityPublicKeyList(myObjectLocation, myEntityUUID, SessionToken);
        //}

        //public Boolean AddPandoraRight(ObjectLocation myObjectLocation, String Name, String ValidationScript)
        //{
        //    return IGraphFS.AddPandoraRight(myObjectLocation, Name, ValidationScript, SessionToken);
        //}

        //public Boolean RemovePandoraRight(ObjectLocation myObjectLocation, RightUUID myRightUUID)
        //{
        //    return IGraphFS.RemovePandoraRight(myObjectLocation, myRightUUID, SessionToken);
        //}

        //public Right GetRightByName(String RightName)
        //{
        //    return IGraphFS.GetRightByName(RightName, SessionToken);
        //}

        //public Boolean ContainsRightUUID(RightUUID myRightUUID)
        //{
        //    return IGraphFS.ContainsRightUUID(myRightUUID, SessionToken);
        //}

        //#endregion

        #region FlushObjectLocation(myObjectLocation)

        public void FlushObjectLocation(ObjectLocation myObjectLocation)
        {
            IGraphFS.FlushObjectLocationNew(myObjectLocation, SessionToken);
        }

        #endregion


        #region Stream

        public Exceptional<IGraphFSStream> OpenStream(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, ObjectRevisionID myObjectRevision, UInt64 myObjectCopy)
        {
            return IGraphFS.OpenStream(SessionToken, myObjectLocation, myObjectStream, myObjectEdition, myObjectRevision, myObjectCopy);
        }

        public Exceptional<IGraphFSStream> OpenStream(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, ObjectRevisionID myObjectRevision, UInt64 myObjectCopy,
                                           FileMode myFileMode, FileAccess myFileAccess, FileShare myFileShare, FileOptions myFileOptions, UInt64 myBufferSize)
        {
            return IGraphFS.OpenStream(SessionToken, myObjectLocation, myObjectStream, myObjectEdition, myObjectRevision, myObjectCopy,
                                          myFileMode, myFileAccess, myFileShare, myFileOptions, myBufferSize);
        }

        #endregion

        #region Transactions

        public FSTransaction BeginTransaction(Boolean myDistributed = false, Boolean myLongRunning = false, IsolationLevel myIsolationLevel = IsolationLevel.Serializable, String myName = "", DateTime? myTimeStamp = null)
        {

            //FSTransaction _FSTransaction = null;

            //if (CurrentFSTransaction.Transaction == null)
            //{
            //    _FSTransaction = CurrentFSTransaction.Transaction = new FSTransaction(myDistributed, myLongRunning, myIsolationLevel, myName, myTimeStamp);
            //    //_FSTransaction.OnDispose += new TransactionDisposedHandler(TransactionOnDisposeHandler);
            //}

            //else
            //{
                //_FSTransaction = new FSTransaction(new GraphFSError_TransactionAlreadyRunning());
            //}

                return new FSTransaction(myDistributed, myLongRunning, myIsolationLevel, myName, myTimeStamp); ;

            //return IGraphFS.BeginTransaction(SessionToken, myDistributed, myLongRunning, myIsolationLevel, myName, myTimeStamp);

        }

        #endregion


        #region StorageEngine Maintenance

        public IEnumerable<StorageUUID> StorageUUIDs()
        {
            return IGraphFS.StorageUUIDs(SessionToken);
        }

        public IEnumerable<StorageUUID> StorageUUIDs(ObjectLocation myObjectLocation)
        {
            return IGraphFS.StorageUUIDs(myObjectLocation, SessionToken);
        }


        public IEnumerable<String> StorageDescriptions()
        {
            return IGraphFS.StorageDescriptions(SessionToken);
        }

        public IEnumerable<String> StorageDescriptions(ObjectLocation myObjectLocation)
        {
            return IGraphFS.StorageDescriptions(myObjectLocation, SessionToken);
        }

        #endregion


        #endregion

    }

}
