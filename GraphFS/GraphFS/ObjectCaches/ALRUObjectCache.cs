﻿/*
 * ALRUObjectCache
 * (c) Achim Friedland, 2010
 */

#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using sones.GraphFS.DataStructures;
using sones.GraphFS.Events;
using sones.GraphFS.Objects;

using sones.Lib;
using sones.Lib.DataStructures;
using sones.Lib.DataStructures.Timestamp;
using sones.GraphFS.Caches;
using sones.Lib.Caches;
using sones.Lib.ErrorHandling;
using sones.Lib.DataStructures.Big;
using sones.GraphFS.Errors;
using System.Diagnostics;
using System.Collections;


#endregion

namespace sones.GraphFS
{


    #region DiscardEventArgs

    public class DiscardEventArgs : EventArgs
    {

        private ObjectLocation _ObjectLocation;

        public DiscardEventArgs(ObjectLocation myObjectLocation)
        {
            _ObjectLocation = myObjectLocation;
        }

        public ObjectLocation ObjectLocation
        {
            get
            {
                return _ObjectLocation;
            }
        }

    }

    #endregion


    /// <summary>
    /// An Last-Recently-Use ObjectCache implemantation of the IObjectCache interface
    /// for storing INodes, ObjectLocators and AFSObjects. This cache will remove the
    /// entries as soon as memory gets low or the stored items are getting very old.
    /// </summary>
    public abstract class ALRUObjectCache : IObjectCache
    {

        #region Data

        //private ReaderWriterLockSlim _CacheItemReaderWriterLockSlim;

        protected readonly Dictionary<ObjectLocation, LinkedListNode<ObjectLocator>> _ObjectLocatorCache;
        protected readonly LinkedList<ObjectLocator>                                 _ObjectLocatorLRUList;
        protected readonly IDictionary<CacheUUID, AFSObject>                         _AFSObjectStore;

        public    event    EventHandler                                              DiscardingOldestItem;

        #endregion

        #region Properties

        #region IsEmpty

        public virtual Boolean IsEmpty
        {
            get
            {
                lock (this)
                {
                    return !_ObjectLocatorCache.Any();
                }
            }
        }

        #endregion

        #region Capacity

        protected UInt64 _Capacity;

        public UInt64 Capacity
        {

            get
            {
                return _Capacity;
            }

            set
            {

                if (value < _MinCapacity)
                    throw new ArgumentException("The capacity must be euqals or larger than: " + _MinCapacity);

                _Capacity = value;

            }
        
        }

        #endregion

        #region MinCapacity

        protected const UInt64 _MinCapacity = 20;

        public UInt64 MinCapacity
        {
            get
            {
                return _MinCapacity;
            }
        }

        #endregion

        #region DefaultCapacity

        protected const UInt64 _DefaultCapacity = 50000;

        public UInt64 DefaultCapacity
        {
            get
            {
                return _DefaultCapacity;
            }
        }

        #endregion

        #region CurrentLoad

        public virtual UInt64 CurrentLoad
        {
            get
            {
                lock (this)
                {
                    return (UInt64) _ObjectLocatorCache.Count;
                }
            }
        }

        #endregion

        #region ObjectCacheSettings

        public ObjectCacheSettings ObjectCacheSettings { get; set; }

        #endregion

        #endregion

        #region Constructor(s)

        #region ObjectCache()

        public ALRUObjectCache()
            : this(_DefaultCapacity)
        {
        }

        #endregion

        #region ObjectCache(myCapacity)

        public ALRUObjectCache(UInt64 myCapacity)
            : base()
        {

            if (myCapacity < MinCapacity)
                throw new ArgumentException(String.Format("myCapacity must be larger than {0}!", MinCapacity));

            _Capacity               = myCapacity;
            _ObjectLocatorCache     = new Dictionary<ObjectLocation, LinkedListNode<ObjectLocator>>();
            _ObjectLocatorLRUList   = new LinkedList<ObjectLocator>();
            _AFSObjectStore         = new Dictionary<CacheUUID, AFSObject>();

        }

        #endregion

        #endregion


        #region StoreINode(myINode, myObjectLocation, myCachePriority = CachePriority.LOW)

        public virtual Exceptional<INode> StoreINode(INode myINode, ObjectLocation myObjectLocation, CachePriority myCachePriority = CachePriority.LOW)
        {

            Debug.Assert(myINode                != null);
            Debug.Assert(myObjectLocation       != null);
            Debug.Assert(_ObjectLocatorCache    != null);

            var _Exceptional = GetObjectLocator(myObjectLocation);

            if (_Exceptional.Failed())
                return _Exceptional.Convert<INode>();

            _Exceptional.Value.INodeReferenceSetter = myINode;

            return new Exceptional<INode>();

        }

        #endregion

        #region StoreObjectLocator(myObjectLocator, myCachePriority = CachePriority.LOW)

        protected abstract Exceptional<ObjectLocator> StoreObjectLocator_protected(ObjectLocator myObjectLocator, CachePriority myCachePriority = CachePriority.LOW);

        public virtual Exceptional<ObjectLocator> StoreObjectLocator(ObjectLocator myObjectLocator, CachePriority myCachePriority = CachePriority.LOW)
        {

            Debug.Assert(_ObjectLocatorCache            != null);
            Debug.Assert(_ObjectLocatorLRUList          != null);
            Debug.Assert(myObjectLocator                != null);
            Debug.Assert(myObjectLocator.ObjectLocation != null);
            Debug.Assert(_AFSObjectStore                != null);

            lock (this)
            {

                var _Exceptional = StoreObjectLocator_protected(myObjectLocator, myCachePriority);
                if (_Exceptional.IsInvalid())
                    return _Exceptional;

                if (_ObjectLocatorCache.ContainsKey(myObjectLocator.ObjectLocation))
                {
                    // Remove LinkedListNode from LRUList and update ObjectLocator within the ObjectCache
                    _ObjectLocatorLRUList.Remove(_ObjectLocatorCache[myObjectLocator.ObjectLocation]);
                    _ObjectLocatorCache[myObjectLocator.ObjectLocation] = _ObjectLocatorLRUList.AddLast(myObjectLocator);
                    return new Exceptional<ObjectLocator>();
                }

                // Add new ObjectLocator to the ObjectCache and add it to the LRUList
                _ObjectLocatorCache.Add(myObjectLocator.ObjectLocation, _ObjectLocatorLRUList.AddLast(myObjectLocator));

                return new Exceptional<ObjectLocator>(myObjectLocator);

            }

        }

        #endregion

        #region StoreAFSObject(myAFSObject, myCachePriority = CachePriority.LOW)

        protected abstract Exceptional<AFSObject> StoreAFSObject_protected(AFSObject myAFSObject, CacheUUID myCacheUUID, CachePriority myCachePriority = CachePriority.LOW);

        public virtual Exceptional<AFSObject> StoreAFSObject(AFSObject myAFSObject, CachePriority myCachePriority = CachePriority.LOW)
        {

            Debug.Assert(myAFSObject                        != null);
            Debug.Assert(myAFSObject.ObjectLocation         != null);
            Debug.Assert(myAFSObject.ObjectLocatorReference != null);
            Debug.Assert(myAFSObject.ObjectStream           != null);
            Debug.Assert(myAFSObject.ObjectEdition          != null);
            Debug.Assert(myAFSObject.ObjectRevisionID       != null);
            Debug.Assert(_ObjectLocatorCache                != null);
            Debug.Assert(_AFSObjectStore                    != null);

            lock (this)
            {

                // Maybe store the ObjectLocator if it is not present within the Cache

                var _CacheUUID = myAFSObject.ObjectLocatorReference[myAFSObject.ObjectStream][myAFSObject.ObjectEdition][myAFSObject.ObjectRevisionID].CacheUUID;
                if (_CacheUUID != null)
                    return new Exceptional<AFSObject>(new GraphFSError("The CacheUUID is invalid!"));                

                var _Exceptional = StoreAFSObject_protected(myAFSObject, _CacheUUID, myCachePriority);
                if (_Exceptional.IsInvalid())
                    return _Exceptional;

                if (_AFSObjectStore.ContainsKey(_CacheUUID))
                    _AFSObjectStore[_CacheUUID] = myAFSObject;

                else
                    _AFSObjectStore.Add(_CacheUUID, myAFSObject);

                return new Exceptional<AFSObject>(myAFSObject);

            }

        }

        #endregion


        #region GetINode(myObjectLocation)

        public virtual Exceptional<INode> GetINode(ObjectLocation myObjectLocation)
        {
            
            Debug.Assert(myObjectLocation   != null);

            var _Exceptional = GetObjectLocator(myObjectLocation);

            if (_Exceptional.Failed())
                return _Exceptional.Convert<INode>();

            if (_Exceptional.Value.INodeReference != null)
                return new Exceptional<INode>(_Exceptional.Value.INodeReference);

            //ToDo: This might be a bit too expensive!
            return new Exceptional<INode>(new GraphFSError("Not within the ObjectCache!"));

        }

        #endregion

        #region GetObjectLocator(myObjectLocation)

        public virtual Exceptional<ObjectLocator> GetObjectLocator(ObjectLocation myObjectLocation)
        {

            Debug.Assert(myObjectLocation       != null);
            Debug.Assert(_ObjectLocatorCache    != null);
            Debug.Assert(_ObjectLocatorLRUList  != null);

            lock (this)
            {

                LinkedListNode<ObjectLocator> _ObjectLocatorNode = null;

                if (_ObjectLocatorCache.TryGetValue(myObjectLocation, out _ObjectLocatorNode))
                    if (_ObjectLocatorNode != null)
                    {

                        // Remove the ObjectLocator from LRU-list and readd it!
                        _ObjectLocatorLRUList.Remove(_ObjectLocatorNode);
                        _ObjectLocatorLRUList.AddLast(_ObjectLocatorNode);

                        return new Exceptional<ObjectLocator>(_ObjectLocatorNode.Value);

                    }

                //ToDo: This might be a bit too expensive!
                return new Exceptional<ObjectLocator>(new GraphFSError_ObjectLocatorNotFound(myObjectLocation));

            }

        }

        #endregion

        #region GetAFSObject<PT>(myCacheUUID)

        public virtual Exceptional<PT> GetAFSObject<PT>(CacheUUID myCacheUUID)
            where PT : AFSObject
        {

            Debug.Assert(myCacheUUID        != null);
            Debug.Assert(_AFSObjectStore    != null);

            lock (this)
            {

                AFSObject _AFSObject = null;

                _AFSObjectStore.TryGetValue(myCacheUUID, out _AFSObject);

                if (_AFSObject != null)
                {
                    //   return new Exceptional<PT>(_AFSObject as PT);

                    var _ObjectLocation = _AFSObject.ObjectLocation;

                    if (_ObjectLocation != null)
                    {

                        var _ObjectLocatorNode = _ObjectLocatorCache[_ObjectLocation];

                        // Remove the ObjectLocator from LRU-list and readd it!
                        _ObjectLocatorLRUList.Remove(_ObjectLocatorNode);
                        _ObjectLocatorLRUList.AddLast(_ObjectLocatorNode);

                    }

                    return new Exceptional<PT>(_AFSObject as PT);

                }

                else
                    return new Exceptional<PT>(new GraphFSError("Not within the ObjectCache!"));

            }

        }

        #endregion


        #region Copy(mySourceLocation, myTargetLocation)

        public virtual Exceptional CopyToLocation(ObjectLocation mySourceLocation, ObjectLocation myTargetLocation)
        {

            Debug.Assert(_ObjectLocatorCache    != null);
            Debug.Assert(_AFSObjectStore        != null);
            Debug.Assert(mySourceLocation       != null);
            Debug.Assert(myTargetLocation       != null);

            lock (this)
            {

                if (_ObjectLocatorCache.ContainsKey(mySourceLocation) &&
                    !_ObjectLocatorCache.ContainsKey(myTargetLocation))
                {

                    foreach (var _ItemToMove in from _Item in _ObjectLocatorCache where _Item.Key.StartsWith(mySourceLocation.ToString()) select _Item)
                    {

                        // Copy the ObjectLocator to the new ObjectLocation
                        var _ObjectLocatorNode = _ObjectLocatorCache[_ItemToMove.Key];
                        var _NewLocation   = new ObjectLocation(myTargetLocation, _ItemToMove.Key.PathElements.Skip(mySourceLocation.PathElements.Count()));
                        _ObjectLocatorNode.Value.ObjectLocationSetter = _NewLocation;
                        _ObjectLocatorCache.Add(_NewLocation, _ObjectLocatorNode);

                    }

                }

            }

            return Exceptional.OK;

        }

        #endregion

        #region Move(mySourceLocation, myTargetLocation)

        public virtual Exceptional MoveToLocation(ObjectLocation mySourceLocation, ObjectLocation myTargetLocation)
        {

            Debug.Assert(_ObjectLocatorCache    != null);
            Debug.Assert(_AFSObjectStore        != null);
            Debug.Assert(mySourceLocation       != null);
            Debug.Assert(myTargetLocation       != null);

            lock (this)
            {

                if (_ObjectLocatorCache.ContainsKey(mySourceLocation) &&
                    !_ObjectLocatorCache.ContainsKey(myTargetLocation))
                {

                    // Get a copy of all ObjectsLocations to move, as we will modify the list later...
                    var _ListOfItemsToMove = (from _Item in _ObjectLocatorCache where _Item.Key.StartsWith(mySourceLocation.ToString()) select _Item).ToList();

                    foreach (var _ItemToMove in _ListOfItemsToMove)
                    {

                        // Move the ObjectLocator to the new ObjectLocation
                        var _ObjectLocatorNode = _ObjectLocatorCache[_ItemToMove.Key];
                        var _NewLocation   = new ObjectLocation(myTargetLocation, _ItemToMove.Key.PathElements.Skip(mySourceLocation.PathElements.Count()));
                        _ObjectLocatorNode.Value.ObjectLocationSetter = _NewLocation;
                        
                        _ObjectLocatorCache.Add(_NewLocation, _ObjectLocatorNode);
                        _ObjectLocatorCache.Remove(_ItemToMove.Key);

                    }

                }

            }

            return Exceptional.OK;

        }

        #endregion


        #region RemoveObjectLocator(myObjectLocator, myRecursion = false, myDisposeAFSObject = true)

        public virtual Exceptional RemoveObjectLocator(ObjectLocator myObjectLocator, Boolean myRecursion = false, Boolean myDisposeAFSObject = true)
        {

            Debug.Assert(myObjectLocator                != null);
            Debug.Assert(myObjectLocator.ObjectLocation != null);
            Debug.Assert(_ObjectLocatorCache            != null);
            Debug.Assert(_AFSObjectStore                != null);

            return RemoveObjectLocation(myObjectLocator.ObjectLocation, myRecursion, myDisposeAFSObject);

        }

        #endregion

        #region RemoveObjectLocation(myObjectLocation, myRecursion = false, myDisposeAFSObject = true)

        public virtual Exceptional RemoveObjectLocation(ObjectLocation myObjectLocation, Boolean myRecursion = false, Boolean myDisposeAFSObject = true)
        {

            Debug.Assert(myObjectLocation       != null);
            Debug.Assert(_ObjectLocatorCache    != null);
            Debug.Assert(_AFSObjectStore        != null);

            lock (this)
            {

                if (myObjectLocation == ObjectLocation.Root)
                    return Exceptional.OK;

                if (_ObjectLocatorCache.ContainsKey(myObjectLocation))
                {

                    var _ObjectLocatorNode = _ObjectLocatorCache[myObjectLocation];

                    #region Recursive remove objects under this location!

                    if (myRecursion)
                    {

                        // Remove all objects at this location
                        foreach (var _String_ObjectStream_Pair in _ObjectLocatorNode.Value)
                        {

                            // Remove subordinated ObjectLocations recursively!
                            if (_String_ObjectStream_Pair.Key == FSConstants.DIRECTORYSTREAM)
                            {
                                foreach (var aLocation in _ObjectLocatorCache.Where(kv => kv.Key.ToString().StartsWith(myObjectLocation.ToString() + "/")))
                                {
                                    RemoveObjectLocation(aLocation.Key, false, myDisposeAFSObject);
                                }
                            }

                            foreach (var _String_ObjectEdition_Pair in _String_ObjectStream_Pair.Value)
                                foreach (var _RevisionID_Revision_Pair in _String_ObjectEdition_Pair.Value)
                                    RemoveAFSObject(_RevisionID_Revision_Pair.Value.CacheUUID, myDisposeAFSObject);

                        }

                        // Remove ObjectLocator
                        OnItemDiscarded(new DiscardEventArgs(myObjectLocation));
                        _ObjectLocatorCache.Remove(myObjectLocation);
                        _ObjectLocatorLRUList.Remove(_ObjectLocatorNode);

                    }

                    #endregion

                    #region Remove objects at this location!

                    else
                    {

                        // Remove all objects at this location
                        foreach (var _StringStream in _ObjectLocatorNode.Value)
                            foreach (var _StringEdition in _StringStream.Value)
                                foreach (var _RevisionIDRevision in _StringEdition.Value)
                                    RemoveAFSObject(_RevisionIDRevision.Value.CacheUUID, myDisposeAFSObject);

                        // Remove ObjectLocator
                        OnItemDiscarded(new DiscardEventArgs(myObjectLocation));
                        _ObjectLocatorCache.Remove(myObjectLocation);
                        _ObjectLocatorLRUList.Remove(_ObjectLocatorNode);

                    }

                    #endregion

                }

            }

            return Exceptional.OK;

        }

        #endregion

        #region RemoveAFSObject(myCacheUUID, myDisposeAFSObject = true)

        public virtual Exceptional RemoveAFSObject(CacheUUID myCacheUUID, Boolean myDisposeAFSObject = true)
        {

            Debug.Assert(myCacheUUID        != null);
            Debug.Assert(_AFSObjectStore    != null);

            AFSObject remObject = null;

            lock (this)
            {
                if (_AFSObjectStore.TryGetValue(myCacheUUID, out remObject))
                {
                    if (_AFSObjectStore.Remove(myCacheUUID))
                    {
                        #region Dispose AFSObject

                        if (myDisposeAFSObject)
                        {
                            var toBeDisposedObject = remObject as IDisposable;
                            if (toBeDisposedObject != null)
                            {
                                toBeDisposedObject.Dispose();
                            }
                        }

                        #endregion
                    }
                }

                return Exceptional.OK;

            }

        }

        #endregion


        #region OnItemDiscarded(myDiscardEventArgs)

        protected virtual void OnItemDiscarded(DiscardEventArgs myDiscardEventArgs)
        {

            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var DiscardingOldestItem_Copy = DiscardingOldestItem;

            if (DiscardingOldestItem_Copy != null)
                DiscardingOldestItem_Copy(this, myDiscardEventArgs);

        }

        #endregion


        #region Clear()

        public virtual Exceptional Clear()
        {

            Debug.Assert(_ObjectLocatorCache    != null);
            Debug.Assert(_ObjectLocatorLRUList  != null);
            Debug.Assert(_AFSObjectStore        != null);

            lock (this)
            {

                _ObjectLocatorCache.Clear();
                _ObjectLocatorLRUList.Clear();

                #region Dispose AFSObjects

                foreach (var aAFSObject in _AFSObjectStore)
                {
                    var aDisposableObject = aAFSObject.Value as IDisposable;

                    if (aDisposableObject != null)
                    {
                        aDisposableObject.Dispose();
                    }
                }
 
                #endregion

                _AFSObjectStore.Clear();

                return Exceptional.OK;

            }

        }

        #endregion


        #region IEnumerable Members

        /// <summary>
        /// Iterates through the items of the LRUObjectCache.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var _KeyValuePair in _ObjectLocatorCache)
            {
                yield return new KeyValuePair<ObjectLocation, ObjectLocator>(_KeyValuePair.Key, _KeyValuePair.Value.Value);
            }
        }

        #endregion

        #region IEnumerable<KeyValuePair<ObjectLocation, ObjectLocator>> Members

        /// <summary>
        /// Iterates through the items of the LRUObjectCache.
        /// </summary>
        IEnumerator<KeyValuePair<ObjectLocation, ObjectLocator>> IEnumerable<KeyValuePair<ObjectLocation, ObjectLocator>>.GetEnumerator()
        {
            foreach (var _KeyValuePair in _ObjectLocatorCache)
            {
                yield return new KeyValuePair<ObjectLocation, ObjectLocator>(_KeyValuePair.Key, _KeyValuePair.Value.Value);
            }
        }

        #endregion

        public void SetPinned(ObjectLocation myObjectLocation)
        {
            throw new NotImplementedException();
        }


    }

}
