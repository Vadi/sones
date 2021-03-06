﻿#region Using

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
using sones.GraphFS.InternalObjects;

#endregion

namespace sones.GraphFS
{

    /// <summary>
    /// An Last-Recently-Use ObjectCache implemantation of the IObjectCache interface
    /// for storing INodes, ObjectLocators and AFSObjects. This cache will remove the
    /// entries as soon as memory gets low or the stored items are getting very old.
    /// </summary>    

    public class ESObjectCache : IObjectCache
    {

        #region Data

        //private ReaderWriterLockSlim _CacheItemReaderWriterLockSlim;
                
        private const    UInt64                                                                 _DefaultCapacity = 500000;
        private          UInt64                                                                 _FillLevel = 0;            
        private readonly Dictionary<ObjectLocation, LinkedListNode<ObjectLocator>>              _ObjectLocatorCache;
        private readonly LinkedList<ObjectLocator>                                              _ObjectLocatorLRU;
        private readonly Dictionary<CacheUUID, AFSObject>                                       _AFSObjectStore;
        private readonly Dictionary<CacheUUID, UInt64>                                          _EstimatedAFSObjectSize;
        private readonly HashSet<ObjectLocation>                                                _PinnedLocations;

        public  event    EventHandler                                                           DiscardingOldestItem;        

        #endregion

        #region Properties

        #region IsEmpty

        public Boolean IsEmpty
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

        public UInt64 DefaultCapacity
        {
            get
            {
                return _DefaultCapacity;
            }
        }

        #endregion

        #region Capacity

        public UInt64 Capacity { get; set; }

        #endregion

        #region NumberOfCachedItems

        public UInt64 CurrentLoad
        {
            get
            {
                lock (this)
                {

                    if (_ObjectLocatorCache.Count != _ObjectLocatorLRU.Count)
                    {
                        Debug.WriteLine(String.Format("_ObjectLocatorCache.Count = {0} != _ObjectLocatorLRUList.Count = {1}", _ObjectLocatorCache.Count, _ObjectLocatorLRU.Count));
                        //    throw new Exception(String.Format("_ObjectLocatorCache.Count = {0} != _ObjectLocatorLRUList.Count = {1}", _ObjectLocatorCache.Count, _ObjectLocatorLRUList.Count));
                    }

                    return (UInt64) _ObjectLocatorCache.Count;

                }
            }
        }

        #endregion

        #region FillLevel

        public UInt64 FillLevel
        {
            get
            {
                lock (this)
                {
                    return _FillLevel;
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

        public ESObjectCache()
            : this(_DefaultCapacity)
        {
        }

        #endregion

        #region ObjectCache(myCapacity)

        public ESObjectCache(UInt64 myCapacity)
            : base()
        {

            if (myCapacity < _DefaultCapacity)
                myCapacity = _DefaultCapacity;

            Capacity                = myCapacity;
            _ObjectLocatorCache     = new Dictionary<ObjectLocation, LinkedListNode<ObjectLocator>>();
            _ObjectLocatorLRU       = new LinkedList<ObjectLocator>();
            _AFSObjectStore         = new Dictionary<CacheUUID, AFSObject>();
            _EstimatedAFSObjectSize = new Dictionary<CacheUUID, UInt64>();
            _PinnedLocations        = new HashSet<ObjectLocation>();

        }

        #endregion

        #endregion


        #region private helper

        private void IncLevel(UInt64 myIncValue)
        {
            _FillLevel += myIncValue;
        }

        private void DecLevel(UInt64 myDecValue)
        { 
            _FillLevel -= myDecValue;
        }

        #endregion

        #region StoreINode(myINode, myObjectLocation, myIsPinned = false)

        public virtual Exceptional<INode> StoreINode(INode myINode, ObjectLocation myObjectLocation, CachePriority myCachePriority = CachePriority.LOW)
        {

            Debug.Assert(myINode                != null);
            Debug.Assert(myObjectLocation       != null);
            Debug.Assert(_ObjectLocatorCache    != null);

            var _Exceptional = GetObjectLocator(myObjectLocation);

            if (_Exceptional.Failed())
                return _Exceptional.Convert<INode>();

            _Exceptional.Value.INodeReferenceSetter = myINode;

            if (myCachePriority == CachePriority.PINNED)
            {
                _PinnedLocations.Add(myObjectLocation);
            }

            return new Exceptional<INode>();

        }

        #endregion

        #region StoreObjectLocator(myObjectLocator, myIsPinned = false)

        public virtual Exceptional<ObjectLocator> StoreObjectLocator(ObjectLocator myObjectLocator, CachePriority myCachePriority = CachePriority.LOW)
        {

            Debug.Assert(_ObjectLocatorCache                != null);
            Debug.Assert(_ObjectLocatorLRU                  != null);
            Debug.Assert(myObjectLocator                    != null);
            Debug.Assert(myObjectLocator.ObjectLocation     != null);
            Debug.Assert(_AFSObjectStore                    != null);            

            lock (this)
            {

                if (myObjectLocator.GetEstimatedSize() > Capacity)
                {
                    return new Exceptional<ObjectLocator>(myObjectLocator);
                }

                if (myCachePriority == CachePriority.PINNED)
                {
                    _PinnedLocations.Add(myObjectLocator.ObjectLocation);
                }

                if (_ObjectLocatorCache.ContainsKey(myObjectLocator.ObjectLocation))
                {
                    // Remove LinkedListNode from LRUList and update ObjectLocator within the ObjectCache
                    _ObjectLocatorLRU.Remove(_ObjectLocatorCache[myObjectLocator.ObjectLocation]);
                    _ObjectLocatorCache[myObjectLocator.ObjectLocation] = _ObjectLocatorLRU.AddLast(myObjectLocator);

                    return new Exceptional<ObjectLocator>();
                }

                var objectLocatorSize = myObjectLocator.GetEstimatedSize();

                if (!CheckFillLevel(objectLocatorSize))
                {
                    if (!_PinnedLocations.Contains(myObjectLocator.ObjectLocation)) // Objects to pinned locations MUST be stored in cache!
                        return new Exceptional<ObjectLocator>(myObjectLocator);
                }

                // Add new ObjectLocator to the ObjectCache and add it to the LRUList                
                _ObjectLocatorCache.Add(myObjectLocator.ObjectLocation, _ObjectLocatorLRU.AddLast(myObjectLocator));
                IncLevel(myObjectLocator.GetEstimatedSize());

                //ValidateFillLevel();

                return new Exceptional<ObjectLocator>(myObjectLocator);

            }

        }

        #endregion

        /// <summary>
        /// Check the current <paramref name="_FillLevel"/> and try to free up to <paramref name="mySizeNeeded"/>.
        /// Return false if there can't be get free <paramref name="mySizeNeeded"/>
        /// </summary>
        /// <param name="mySizeNeeded"></param>
        /// <returns></returns>
        private Boolean CheckFillLevel(ulong mySizeNeeded)
        {

            while (mySizeNeeded + _FillLevel >= Capacity)
            {
                // Remove oldest LinkedListNode from LRUList and add new ObjectLocator to the ObjectCache
                if (DiscardingOldestItem != null)
                    DiscardingOldestItem(this, new EventArgs());

                var oldestEntry = _ObjectLocatorLRU.First;

                MoveRootToEnd(oldestEntry);

                oldestEntry = _ObjectLocatorLRU.First;

                if (oldestEntry.Value.ObjectLocation != ObjectLocation.Root)
                {
                    if (_PinnedLocations.Contains(oldestEntry.Value.ObjectLocation))
                    {
                        #region Move pinned locations to the end

                        #region If all locations are pinned we need to return false

                        if (_PinnedLocations.Count == _ObjectLocatorCache.Count)
                        {
                            return false;
                        }

                        #endregion

                        // move pinned to end and proceed
                        _ObjectLocatorLRU.RemoveFirst();
                        _ObjectLocatorLRU.AddLast(oldestEntry);

                        #endregion
                    }
                    else
                    {
                        RemoveObjectLocator(oldestEntry.Value, myDisposeAFSObject: false);
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public void MoveRootToEnd(LinkedListNode<ObjectLocator> myLocation)
        {
            if (myLocation.Value.ObjectLocation == ObjectLocation.Root)
            {
                _ObjectLocatorLRU.RemoveFirst();
                _ObjectLocatorLRU.AddLast(myLocation);
            }
        }

        #region StoreAFSObject(myAFSObject, myIsPinned = false)

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
                if (myAFSObject.GetEstimatedSize() > Capacity)
                {
                    return new Exceptional<AFSObject>(myAFSObject);
                }

                if (myCachePriority == CachePriority.PINNED)
                {
                    _PinnedLocations.Add(myAFSObject.ObjectLocation);
                }

                var _CacheUUID = myAFSObject.ObjectLocatorReference[myAFSObject.ObjectStream][myAFSObject.ObjectEdition][myAFSObject.ObjectRevisionID].CacheUUID;
                
                Debug.Assert(_CacheUUID != null);                

                var afsObjectSize = myAFSObject.GetEstimatedSize();
                if (!CheckFillLevel(afsObjectSize))
                {
                    if (!_PinnedLocations.Contains(myAFSObject.ObjectLocation)) // Objects to pinned locations MUST be stored in cache!
                    {
                        return new Exceptional<AFSObject>(myAFSObject);
                    }
                }
                    
                if (_AFSObjectStore.ContainsKey(_CacheUUID))
                {
                    DecLevel(_EstimatedAFSObjectSize[_CacheUUID]);
                    IncLevel(myAFSObject.GetEstimatedSize());
                    _AFSObjectStore[_CacheUUID] = myAFSObject;
                    _EstimatedAFSObjectSize[_CacheUUID] = myAFSObject.GetEstimatedSize();
                }
                else
                {
                    IncLevel(myAFSObject.GetEstimatedSize());
                    _AFSObjectStore.Add(_CacheUUID, myAFSObject);
                    _EstimatedAFSObjectSize.Add(_CacheUUID, myAFSObject.GetEstimatedSize());
                }

                if (!_ObjectLocatorCache.ContainsKey(myAFSObject.ObjectLocation))
                {
                    _ObjectLocatorCache.Add(myAFSObject.ObjectLocation, _ObjectLocatorLRU.AddLast(myAFSObject.ObjectLocatorReference));
                    IncLevel(myAFSObject.ObjectLocatorReference.GetEstimatedSize());
                }
                else
                {
                    _ObjectLocatorLRU.Remove( (_ObjectLocatorCache[myAFSObject.ObjectLocation]) );
                    _ObjectLocatorLRU.AddLast(_ObjectLocatorCache[myAFSObject.ObjectLocation]);
                }
            }
            //ValidateFillLevel();

            return new Exceptional<AFSObject>(myAFSObject);
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
            Debug.Assert(_ObjectLocatorLRU      != null);

            lock (this)
            {
                LinkedListNode<ObjectLocator> _ObjectLocatorNode;

                if (_ObjectLocatorCache.TryGetValue(myObjectLocation, out _ObjectLocatorNode))
                {
                    if (_ObjectLocatorNode != null)
                    {
                        // Remove the ObjectLocator from LRU-list and readd it!
                        _ObjectLocatorLRU.Remove(_ObjectLocatorNode);
                        _ObjectLocatorLRU.AddLast(_ObjectLocatorNode);                      

                        return new Exceptional<ObjectLocator>(_ObjectLocatorNode.Value);

                    }
                }
                
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
                    var _ObjectLocation = _AFSObject.ObjectLocation;

                    if (_ObjectLocation != null)
                    {
                        var _ObjectLocatorNode = _ObjectLocatorCache[_ObjectLocation];

                        // Remove the ObjectLocator from LRU-list and readd it!                        
                        //Removing the ObjectLocator will remove ALL depending FSObjects!!! But only 1 is readded!!!
                        //RemoveObjectLocator(_ObjectLocatorNode.Value, myDisposeAFSObject: false);
                        if (_PinnedLocations.Contains(_ObjectLocation))
                        {
                            StoreAFSObject(_AFSObject, CachePriority.PINNED);
                        }
                        else
                        {
                            StoreAFSObject(_AFSObject);
                        }
                    }

                    return new Exceptional<PT>(_AFSObject as PT);
                }

                //ToDo: This might be a bit too expensive!
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

                    foreach (var _ItemToMove in (from _Item in _ObjectLocatorCache where _Item.Key.StartsWith(mySourceLocation.ToString()) select _Item).ToList())
                    {

                        // Copy the ObjectLocator to the new ObjectLocation
                        var _ObjectLocatorNode = _ObjectLocatorCache[_ItemToMove.Key];
                        var _NewLocation   = new ObjectLocation(myTargetLocation, _ItemToMove.Key.PathElements.Skip(mySourceLocation.PathElements.Count()));

                        _ObjectLocatorNode.Value.ObjectLocationSetter = _NewLocation;
                        
                        var objectLocatorSize = _ObjectLocatorNode.Value.GetEstimatedSize();
                        if (!CheckFillLevel(objectLocatorSize))
                        {
                            if (!_PinnedLocations.Contains(_ObjectLocatorNode.Value.ObjectLocation)) // Objects to pinned locations MUST be stored in cache!
                                return Exceptional.OK;
                        }

                        StoreObjectLocator(_ObjectLocatorNode.Value);
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

                        StoreObjectLocator(_ObjectLocatorNode.Value);
                        RemoveObjectLocation(_ItemToMove.Key);
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
            UInt64 locatorSize = 0;

            lock (this)
            {

                if (myObjectLocation == ObjectLocation.Root)
                {
                    Debug.WriteLine("...");
                    return Exceptional.OK;
                }

                if (_ObjectLocatorCache.ContainsKey(myObjectLocation))
                {

                    var _ObjectLocatorNode = _ObjectLocatorCache[myObjectLocation];
                    locatorSize = _ObjectLocatorNode.Value.GetEstimatedSize();

                    #region Recursive remove objects under this location!

                    if (myRecursion)
                    {
                        // Remove all objects at this location
                        foreach (var _String_ObjectStream_Pair in _ObjectLocatorNode.Value)
                        {
                            // Remove subordinated ObjectLocations recursively!
                            if (_String_ObjectStream_Pair.Key == FSConstants.DIRECTORYSTREAM)
                            {
                                foreach (var aLocation in _ObjectLocatorCache.Where(kv => kv.Key.ToString().StartsWith(myObjectLocation.ToString() + "/")).ToList())
                                {
                                    RemoveObjectLocation(aLocation.Key, false, myDisposeAFSObject);
                                }
                            }

                            foreach (var _String_ObjectEdition_Pair in _String_ObjectStream_Pair.Value)
                            {
                                foreach (var _RevisionIDRevision in _String_ObjectEdition_Pair.Value)
                                {
                                    RemoveAFSObject(_RevisionIDRevision.Value.CacheUUID, myDisposeAFSObject);
                                }
                            }
                        }

                        // Remove ObjectLocator
                        RemoveObjectLocation(myObjectLocation, false, myDisposeAFSObject);
                    }

                    #endregion

                    #region Remove objects at this location!

                    else
                    {
                        // Remove all objects at this location
                        foreach (var _StringStream in _ObjectLocatorNode.Value)
                        {
                            foreach (var _StringEdition in _StringStream.Value)
                            {
                                foreach (var _RevisionIDRevision in _StringEdition.Value)
                                {
                                    RemoveAFSObject(_RevisionIDRevision.Value.CacheUUID, myDisposeAFSObject);
                                }
                            }
                        }
                    }

                    #endregion

                    #region Remove ObjectLocator

                    if (_ObjectLocatorCache.Remove(myObjectLocation))
                    {
                        DecLevel(_ObjectLocatorNode.Value.GetEstimatedSize());
                        _ObjectLocatorLRU.Remove(_ObjectLocatorNode);
                    }
                    
                    #endregion

                    //DecLevel(locatorSize);
                }

            }
            //ValidateFillLevel();
            
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
                        // old revisions might be 
                        if (myDisposeAFSObject && !_PinnedLocations.Contains(remObject.ObjectLocation))
                        {
                            var toBeDisposedObject = remObject as IDisposable;
                            if (toBeDisposedObject != null)
                            {
                                toBeDisposedObject.Dispose();
                            }
                        } 
                        #endregion
                    }

                    DecLevel(_EstimatedAFSObjectSize[myCacheUUID]);

                    _EstimatedAFSObjectSize.Remove(myCacheUUID);

                }
            }
            //ValidateFillLevel();
            return Exceptional.OK;
        }

        #endregion

        private bool ValidateFillLevel()
        {
            var targetSize = 0UL;
            if (_EstimatedAFSObjectSize.Values.IsNotNullOrEmpty())
            {
                targetSize += _EstimatedAFSObjectSize.Values.Aggregate((a, b) => a + b);
            }
            if (_ObjectLocatorLRU.IsNotNullOrEmpty())
            {
                targetSize += _ObjectLocatorLRU.Aggregate(0UL, (acc, l) => acc + l.GetEstimatedSize());
            }

            if (targetSize != _FillLevel)
            {
                return false;
            }

            return (targetSize == _FillLevel);

        }

        public void SetPinned(ObjectLocation myObjectLocation)
        {
            _PinnedLocations.Add(myObjectLocation);
        }

        #region Clear()

        public Exceptional Clear()
        {

            Debug.Assert(_ObjectLocatorCache    != null);
            Debug.Assert(_ObjectLocatorLRU      != null);
            Debug.Assert(_AFSObjectStore        != null);            

            lock (this)
            {

                _ObjectLocatorCache.Clear();
                _ObjectLocatorLRU.Clear();

                #region Dispose AFSObject

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
                _EstimatedAFSObjectSize.Clear();
                _FillLevel = 0;

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

        public ICollection<AFSObject> GetObjects
        {
            get
            {
                return _AFSObjectStore.Values;
            }
        }
    }

}