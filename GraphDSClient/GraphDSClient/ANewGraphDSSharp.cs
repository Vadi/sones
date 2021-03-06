﻿/* 
 * AGraphDSSharp
 * (c) Achim 'ahzf' Friedland, 2009 - 2010
 */

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using sones.GraphFS;
using sones.GraphFS.Caches;
using sones.GraphFS.DataStructures;
using sones.GraphFS.Events;
using sones.GraphFS.InternalObjects;
using sones.GraphFS.Objects;
using sones.GraphFS.Session;
using sones.GraphFS.Transactions;

using sones.GraphDB;
using sones.GraphDB.NewAPI;
using sones.GraphDB.Result;
using sones.GraphDB.Transactions;

using sones.Lib;
using sones.Lib.DataStructures;
using sones.Lib.ErrorHandling;
using sones.Lib.Settings;

using sones.Notifications;

using sones.GraphIO;
using sones.GraphIO.XML;

#endregion

namespace sones.GraphDSClient
{

    public abstract class ANewGraphDSSharp : IGraphFSSession
    {

        #region Data

        private const string GraphAppSettingsLocation = "GraphAppSettings.xml";
        protected ISessionInfo  _SessionInfo;
        protected SessionToken  _SessionToken;
        protected readonly  Dictionary<Type, List<Action<AFSObject>>> _PostSerializationActions;

        #endregion

        #region Properties

        public String  GraphFSImplementation    { get; set; }
        public String  DatabaseName             { get; set; }
        public String  Username                 { get; set; }
        public String  Password                 { get; set; }
        public UInt64  FileSystemSize           { get; set; }
        
        public ObjectCacheSettings    ObjectCacheSettings    { get; set; }        
        public NotificationSettings   NotificationSettings   { get; set; }
        public NotificationDispatcher NotificationDispatcher { get; set; }

        public GraphAppSettings GraphAppSettings  { get; private set; }

        #region StorageLocation

        public String StorageLocation
        {
            set
            {

                if (value == null)
                    throw new ArgumentNullException();

                // Use the property in order to set the GraphFSParametersDictionary
                StorageLocations = new HashSet<String> { value };

            }
        }

        #endregion

        #region StorageLocations

        public HashSet<String> _StorageLocations; 

        public HashSet<String> StorageLocations
        {

            get
            {
                return _StorageLocations;
            }

            set
            {
                
                if (value == null)
                    throw new ArgumentNullException();

                _StorageLocations = value;

                if (!_IGraphFSParametersDictionary.ContainsKey("StorageLocations"))
                    _IGraphFSParametersDictionary.Add("StorageLocations", _StorageLocations);
                else
                    _IGraphFSParametersDictionary["StorageLocations"] = _StorageLocations;

            }

        }

        #endregion

        #region IGraphFSParameters

        protected Object                                _IGraphFSParameters;
        protected readonly IDictionary<String, Object>  _IGraphFSParametersDictionary;

        public Object IGraphFSParameters
        {

            get
            {
                return _IGraphFSParameters;
            }

            set
            {
                if (value != null)
                {
                    
                    _IGraphFSParameters           = value;
                    
                    foreach (var _item in value.AnonymousTypeToDictionary())
                        _IGraphFSParametersDictionary.Add(_item);

                }
            }
        
        }

        #endregion

 //       public IGraphDBSession IGraphDBSession { get; protected set; }

        #endregion

        #region Events

        public delegate void ShutdownEventHandler(Object Sender, EventArgs e);

        public event ShutdownEventHandler ShutdownEvent;

        #endregion

        #region Constructor(s)

        public ANewGraphDSSharp()
        {
            FileSystemSize                  = 50000000;
            _SessionInfo                    = new FSSessionInfo("root");
            _SessionToken                   = new SessionToken(_SessionInfo);
            _IGraphFSParametersDictionary   = new Dictionary<String, Object>();
            GraphAppSettings                 = new GraphAppSettings();
            GraphAppSettings.LoadXML(GraphAppSettingsLocation);
            _PostSerializationActions       = new Dictionary<Type,List<Action<AFSObject>>>();
        }

        #endregion


        #region QueryResultAction(myQueryResult, myAction, mySuccessAction, myPartialSuccessAction, myFailureAction)

        public void QueryResultAction(QueryResult myQueryResult, Action<QueryResult> myAction = null, Action<QueryResult> mySuccessAction = null, Action<QueryResult> myPartialSuccessAction = null, Action<QueryResult> myFailureAction = null)
        {

            if (mySuccessAction         != null && myQueryResult.Success)
            {
                mySuccessAction(myQueryResult);
                return;
            }

            if (myPartialSuccessAction  != null && myQueryResult.PartialSuccess)
            {
                myPartialSuccessAction(myQueryResult);
                return;
            }

            if (myFailureAction         != null && myQueryResult.Failed)
            {
                myFailureAction(myQueryResult);
                return;
            }

            if (myAction                != null)
            {
                myAction(myQueryResult);
                return;
            }

        }

        #endregion


        #region Query(...)

        #region QueryAsString(myQuery)

        /// <summary>
        /// This will execute a usual query on the current GraphDBSharp implementation.
        /// Be aware that under some circumstances (e.g. REST) you will not get a valid result!
        /// </summary>
        /// <param name="myQueryString">A valid GQL-query as string</param>
        /// <returns>The QueryResult as string</returns>
        public abstract Exceptional<String> QueryAsString(String myQueryString);

        #endregion
        
        #region Query(myQuery, myAction = null, mySuccessAction = null, myPartialSuccessAction = null, myFailureAction = null)

        /// <summary>
        /// This will execute a usual query on the current GraphDBSharp implementation.
        /// Be aware that under some circumstances (e.g. REST) you will not get a valid result!
        /// </summary>
        /// <param name="myQueryString">A valid GQL-query as string</param>
        /// <returns>The QueryResult</returns>
        public abstract QueryResult Query(String myQueryString, Action<QueryResult> myAction = null, Action<QueryResult> mySuccessAction = null, Action<QueryResult> myPartialSuccessAction = null, Action<QueryResult> myFailureAction = null);

        #endregion

        // ToDo: Maybe better QueryResult<T>!!!
        #region Query<T>(myQuery, myAction = null, mySuccessAction = null, myPartialSuccessAction = null, myFailureAction = null)

        public Exceptional<IEnumerable<T>> Query<T>(String myQueryString, Action<QueryResult> myAction = null, Action<QueryResult> mySuccessAction = null, Action<QueryResult> myPartialSuccessAction = null, Action<QueryResult> myFailureAction = null)
            where T : Vertex, new()
        {
            return new Exceptional<IEnumerable<T>>(new SelectToObjectGraph(Query(myQueryString)).ToVertexType<T>());
        }

        #endregion


        #region QuerySelect(myQuery)

        [Obsolete]
        public abstract SelectToObjectGraph QuerySelect(String myQuery);

        #endregion

        #endregion






        #region IGraphDBSession

        public abstract DBTransaction BeginTransaction(Boolean myDistributed = false, Boolean myLongRunning = false, IsolationLevel myIsolationLevel = IsolationLevel.Serializable, String myName = "", DateTime? myCreated = null);

        #endregion

        #region IGraphFSSession Members

        #region FS Events

        public abstract event GraphFSEventHandlers.OnLoadEventHandler OnLoad;

        public abstract event GraphFSEventHandlers.OnLoadedEventHandler OnLoaded;

        public abstract event GraphFSEventHandlers.OnLoadedAsyncEventHandler OnLoadedAsync;

        public abstract event GraphFSEventHandlers.OnSaveEventHandler OnSave;

        public abstract event GraphFSEventHandlers.OnSavedEventHandler OnSaved;

        public abstract event GraphFSEventHandlers.OnSavedAsyncEventHandler OnSavedAsync;

        public abstract event GraphFSEventHandlers.OnRemoveEventHandler OnRemove;

        public abstract event GraphFSEventHandlers.OnRemovedEventHandler OnRemoved;

        public abstract event GraphFSEventHandlers.OnRemovedAsyncEventHandler OnRemovedAsync;

        public abstract event GraphFSEventHandlers.OnTransactionStartEventHandler OnTransactionStart;

        public abstract event GraphFSEventHandlers.OnTransactionStartedEventHandler OnTransactionStarted;

        public abstract event GraphFSEventHandlers.OnTransactionStartedAsyncEventHandler OnTransactionStartedAsync;

        public abstract event GraphFSEventHandlers.OnTransactionCommitEventHandler OnTransactionCommit;

        public abstract event GraphFSEventHandlers.OnTransactionCommittedEventHandler OnTransactionCommitted;

        public abstract event GraphFSEventHandlers.OnTransactionCommittedAsyncEventHandler OnTransactionCommittedAsync;

        public abstract event GraphFSEventHandlers.OnTransactionRollbackEventHandler OnTransactionRollback;

        public abstract event GraphFSEventHandlers.OnTransactionRollbackedEventHandler OnTransactionRollbacked;

        public abstract event GraphFSEventHandlers.OnTransactionRollbackedAsyncEventHandler OnTransactionRollbackedAsync;

        #endregion

        public abstract FSTransaction BeginFSTransaction(Boolean myDistributed = false, Boolean myLongRunning = false, IsolationLevel myIsolationLevel = IsolationLevel.Serializable, String myName = "", DateTime? myCreated = null);

        public abstract IGraphFS IGraphFS { get; protected set; }

        public SessionToken SessionToken
        {
            get { return _SessionToken; }
        }

        public UInt64 NumberOfSpecialDirectories
        {
            get
            {
                return 6UL;
            }
        }
        
        public abstract String Implementation { get; }

        public abstract IGraphFSSession CreateNewSession(String myUsername);

        public abstract bool IsMounted { get; }

        public abstract bool IsPersistent { get; }

        public abstract IEnumerable<object> TraverseChildFSs(Func<IGraphFS, ulong, IEnumerable<object>> myFunc, ulong myDepth);

        public abstract FileSystemUUID GetFileSystemUUID();

        public abstract FileSystemUUID GetFileSystemUUID(ObjectLocation myObjectLocation);

        public abstract IEnumerable<FileSystemUUID> GetFileSystemUUIDs(ulong myDepth);

        public abstract Exceptional WipeFileSystem();

        public abstract String GetFileSystemDescription();

        public abstract String GetFileSystemDescription(ObjectLocation myObjectLocation);

        public abstract IEnumerable<String> GetFileSystemDescriptions(ulong myDepth);

        public abstract void SetFileSystemDescription(String myFileSystemDescription);

        public abstract void SetFileSystemDescription(ObjectLocation myObjectLocation, String myFileSystemDescription);

        public abstract ulong GetNumberOfBytes();

        public abstract ulong GetNumberOfBytes(ObjectLocation myObjectLocation);

        public abstract IEnumerable<ulong> GetNumberOfBytes(bool myRecursiveOperation);

        public abstract ulong GetNumberOfFreeBytes();

        public abstract ulong GetNumberOfFreeBytes(ObjectLocation myObjectLocation);

        public abstract IEnumerable<ulong> GetNumberOfFreeBytes(bool myRecursiveOperation);

        public abstract AccessModeTypes GetAccessMode();

        public abstract AccessModeTypes GetAccessMode(ObjectLocation myObjectLocation);

        public abstract IEnumerable<GraphFS.AccessModeTypes> GetAccessModes(bool myRecursiveOperation);

        public abstract IGraphFS ParentFileSystem { get; set; }

        public abstract IEnumerable<ObjectLocation> GetChildFileSystemMountpoints(bool myRecursiveOperation);

        public abstract IGraphFS GetChildFileSystem(ObjectLocation myObjectLocation, bool myRecursive);

        public abstract Exceptional<ObjectCacheSettings> GetObjectCacheSettings();

        public abstract Exceptional<ObjectCacheSettings> GetObjectCacheSettings(ObjectLocation myObjectLocation);

        public abstract Exceptional SetObjectCacheSettings(ObjectCacheSettings myObjectCacheSettings);

        public abstract Exceptional SetObjectCacheSettings(ObjectLocation myObjectLocation, ObjectCacheSettings myObjectCacheSettings);

        public abstract Exceptional<FileSystemUUID> MakeFileSystem(String myDescription, ulong myNumberOfBytes, bool myOverwriteExistingFileSystem, Action<double> myAction);

        public abstract Exceptional<UInt64> GrowFileSystem(ulong myNumberOfBytesToAdd);

        public abstract Exceptional<UInt64> ShrinkFileSystem(ulong myNumberOfBytesToRemove);

        public abstract Exceptional MountFileSystem(AccessModeTypes myAccessMode);

        public abstract Exceptional MountFileSystem(ObjectLocation myMountPoint, IGraphFSSession myIGraphFSSession, AccessModeTypes myFSAccessMode);

        public abstract Exceptional RemountFileSystem(GraphFS.AccessModeTypes myFSAccessMode);

        public abstract Exceptional RemountFileSystem(ObjectLocation myMountPoint, GraphFS.AccessModeTypes myFSAccessMode);

        public abstract Exceptional UnmountFileSystem();

        public abstract Exceptional UnmountFileSystem(ObjectLocation myMountPoint);

        public abstract Exceptional UnmountAllFileSystems();

        public abstract Exceptional ChangeRootDirectory(String myChangeRootPrefix);

        public abstract Exceptional<INode> GetINode(ObjectLocation myObjectLocation);

        public abstract Exceptional<ObjectLocator> GetObjectLocator(ObjectLocation myObjectLocation);

        public abstract Exceptional LockFSObject(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, ObjectRevisionID myObjectRevisionID, ObjectLocks myObjectLock, ObjectLockTypes myObjectLockType, ulong myLockingTime);

        public abstract Exceptional<PT> GetOrCreateFSObject<PT>(ObjectLocation myObjectLocation) where PT : AFSObject, new();

        public abstract Exceptional<PT> GetOrCreateFSObject<PT>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null, ulong myObjectCopy = 0, bool myIgnoreIntegrityCheckFailures = false) where PT : AFSObject, new();

        public abstract Exceptional<PT> GetOrCreateFSObject<PT>(ObjectLocation myObjectLocation, String myObjectStream, Func<PT> myFunc, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null, ulong myObjectCopy = 0, bool myIgnoreIntegrityCheckFailures = false) where PT : AFSObject;

        public abstract Exceptional<PT> GetFSObject<PT>(ObjectLocation myObjectLocation) where PT : GraphFS.Objects.AFSObject, new();

        public abstract Exceptional<PT> GetFSObject<PT>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null, ulong myObjectCopy = 0, bool myIgnoreIntegrityCheckFailures = false) where PT : GraphFS.Objects.AFSObject, new();

        public abstract Exceptional<PT> GetFSObject<PT>(ObjectLocation myObjectLocation, String myObjectStream, Func<PT> myFunc, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null, ulong myObjectCopy = 0, bool myIgnoreIntegrityCheckFailures = false) where PT : GraphFS.Objects.AFSObject;

        public abstract Exceptional StoreFSObject(AFSObject myAGraphObject, bool myAllowOverwritting, Boolean myPinObjectLocationInCache = false);

        public abstract Exceptional<Trinary> ObjectExists(ObjectLocation myObjectLocatio);

        public abstract Exceptional<Trinary> ObjectStreamExists(ObjectLocation myObjectLocation, String myObjectStream);

        public abstract Exceptional<Trinary> ObjectEditionExists(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition);

        public abstract Exceptional<Trinary> ObjectRevisionExists(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null);

        public abstract Exceptional<IEnumerable<String>> GetObjectStreams(ObjectLocation myObjectLocation);

        public abstract Exceptional<IEnumerable<String>> GetObjectEditions(ObjectLocation myObjectLocation, String myObjectStream);

        public abstract Exceptional<IEnumerable<ObjectRevisionID>> GetObjectRevisionIDs(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition);

        public abstract Exceptional RenameFSObject(ObjectLocation myObjectLocation, String myNewObjectName);

        public abstract Exceptional RemoveFSObject(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null);

        public abstract Exceptional EraseFSObject(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition = FSConstants.DefaultEdition, ObjectRevisionID myObjectRevisionID = null);

        public abstract Exceptional AddSymlink(ObjectLocation myObjectLocation, ObjectLocation myTargetLocation);

        public abstract Exceptional AddSymlink(ObjectLocation myObjectLocation, GraphFS.Objects.AFSObject myTargetAFSObject);

        public abstract Exceptional<Trinary> isSymlink(ObjectLocation myObjectLocation);

        public abstract Exceptional<ObjectLocation> GetSymlink(ObjectLocation myObjectLocation);

        public abstract Exceptional RemoveSymlink(ObjectLocation myObjectLocation);

        public abstract Exceptional<IDirectoryObject> CreateDirectoryObject(ObjectLocation myObjectLocation, UInt64 myBlocksize = 0, Boolean myRecursive = false);

        public abstract Exceptional<Trinary> isIDirectoryObject(ObjectLocation myObjectLocation);

        public abstract Exceptional<IEnumerable<String>> GetDirectoryListing(ObjectLocation myObjectLocation);

        public abstract Exceptional<IEnumerable<String>> GetDirectoryListing(ObjectLocation myObjectLocation, Func<KeyValuePair<String, GraphFS.InternalObjects.DirectoryEntry>, bool> myFunc);

        public abstract Exceptional<IEnumerable<String>> GetFilteredDirectoryListing(ObjectLocation myObjectLocation, String[] myName, String[] myIgnoreName, String[] myRegExpr, List<String> myObjectStreams, List<String> myIgnoreObjectStreams, String[] mySize, String[] myCreationTime, String[] myLastModificationTime, String[] myLastAccessTime, String[] myDeletionTime);

        public abstract Exceptional<IEnumerable<DirectoryEntryInformation>> GetExtendedDirectoryListing(ObjectLocation myObjectLocation);

        public abstract Exceptional<IEnumerable<DirectoryEntryInformation>> GetFilteredExtendedDirectoryListing(ObjectLocation myObjectLocation, String[] myName, String[] myIgnoreName, String[] myRegExpr, List<String> myObjectStreams, List<String> myIgnoreObjectStreams, String[] mySize, String[] myCreationTime, String[] myLastModificationTime, String[] myLastAccessTime, String[] myDeletionTime);

        public abstract Exceptional RemoveDirectoryObject(ObjectLocation myObjectLocation, bool removeRecursive);

        public abstract Exceptional EraseDirectoryObject(ObjectLocation myObjectLocation, bool eradeRecursive);

        public abstract Exceptional SetMetadatum<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey, TValue myValue, Lib.DataStructures.Indices.IndexSetStrategy myIndexSetStrategy);

        public abstract Exceptional SetMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, IEnumerable<KeyValuePair<String, TValue>> myMetadata, Lib.DataStructures.Indices.IndexSetStrategy myIndexSetStrategy);

        public abstract Exceptional<Trinary> MetadatumExists<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey, TValue myValue);

        public abstract Exceptional<Trinary> MetadataExists<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey);

        public abstract Exceptional<IEnumerable<TValue>> GetMetadatum<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey);

        public abstract Exceptional<IEnumerable<KeyValuePair<String, TValue>>> GetMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition);

        public abstract Exceptional<IEnumerable<KeyValuePair<String, TValue>>> GetMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myMinKey, String myMaxKey);

        public abstract Exceptional<IEnumerable<KeyValuePair<String, TValue>>> GetMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, Func<KeyValuePair<String, TValue>, bool> myFunc);

        public abstract Exceptional RemoveMetadatum<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey, TValue myValue);

        public abstract Exceptional RemoveMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, String myKey);

        public abstract Exceptional RemoveMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, IEnumerable<KeyValuePair<String, TValue>> myMetadata);

        public abstract Exceptional RemoveMetadata<TValue>(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, Func<KeyValuePair<String, TValue>, bool> myFunc);

        public abstract Exceptional SetUserMetadatum(ObjectLocation myObjectLocation, String myKey, object myObject, Lib.DataStructures.Indices.IndexSetStrategy myIndexSetStrategy);

        public abstract Exceptional SetUserMetadata(ObjectLocation myObjectLocation, IEnumerable<KeyValuePair<String, object>> myUserMetadata, Lib.DataStructures.Indices.IndexSetStrategy myIndexSetStrategy);

        public abstract Exceptional<Trinary> UserMetadatumExists(ObjectLocation myObjectLocation, String myKey, object myMetadatum);

        public abstract Exceptional<Trinary> UserMetadataExists(ObjectLocation myObjectLocation, String myKey);

        public abstract Exceptional<IEnumerable<object>> GetUserMetadatum(ObjectLocation myObjectLocation, String myKey);

        public abstract Exceptional<IEnumerable<KeyValuePair<String, object>>> GetUserMetadata(ObjectLocation myObjectLocation);

        public abstract Exceptional<IEnumerable<KeyValuePair<String, object>>> GetUserMetadata(ObjectLocation myObjectLocation, String myMinKey, String myMaxKey);

        public abstract Exceptional<IEnumerable<KeyValuePair<String, object>>> GetUserMetadata(ObjectLocation myObjectLocation, Func<KeyValuePair<String, object>, bool> myFunc);

        public abstract Exceptional RemoveUserMetadatum(ObjectLocation myObjectLocation, String myKey, object myObject);

        public abstract Exceptional RemoveUserMetadata(ObjectLocation myObjectLocation, String myKey);

        public abstract Exceptional RemoveUserMetadata(ObjectLocation myObjectLocation, IEnumerable<KeyValuePair<String, object>> myMetadata);

        public abstract Exceptional RemoveUserMetadata(ObjectLocation myObjectLocation, Func<KeyValuePair<String, object>, bool> myFunc);

        public abstract Exceptional<FileObject> GetFileObject(ObjectLocation myObjectLocation);

        public abstract Exceptional<FileObject> GetFileObject(ObjectLocation myObjectLocation, ObjectRevisionID myRevisionID);

        public abstract Exceptional StoreFileObject(ObjectLocation myObjectLocation, Byte[] myData, Boolean myAllowToOverwrite = false);
        public abstract Exceptional StoreFileObject(ObjectLocation myObjectLocation, String myData, Boolean myAllowToOverwrite = false);

        public abstract Exceptional<IGraphFSStream> OpenStream(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, ObjectRevisionID myObjectRevision, ulong myObjectCopy);

        public abstract Exceptional<IGraphFSStream> OpenStream(ObjectLocation myObjectLocation, String myObjectStream, String myObjectEdition, ObjectRevisionID myObjectRevision, ulong myObjectCopy, FileMode myFileMode, FileAccess myFileAccess, FileShare myFileShare, FileOptions myFileOptions, ulong myBufferSize);

        public abstract Exceptional MoveObjectLocation(ObjectLocation myFromLocation, ObjectLocation myToLocation);

        public abstract Exceptional<IDirectoryObject> GetDirectoryObject(ObjectLocation objectLocation);

        public void SetPostSerializationAction(Action<AFSObject> myPostSerializationAction)
        {

            //if (!_PostSerializationActions.ContainsKey(typeof(T1)))
            //{
            //    _PostSerializationActions.Add(typeof(T1), new List<Action<AFSObject>>());
            //}
            //_PostSerializationActions[typeof(T1)].Add((Action<AFSObject>)myPostSerializationAction);

        }

        //public void SetPostSerializationAction<T1>(Action<T1> myPostSerializationAction)
        //    where T1 : AFSObject
        //{

        //    if (!_PostSerializationActions.ContainsKey(typeof(T1)))
        //    {
        //        _PostSerializationActions.Add(typeof(T1), new List<Action<AFSObject>>());
        //    }
        //    _PostSerializationActions[typeof(T1)].Add((Action<AFSObject>)myPostSerializationAction);

        //}

        #endregion

    }

}
