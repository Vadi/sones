﻿/*
 * AObjectOntology
 * (c) Achim Friedland, 2008 - 2010
 */

#region Usings

using System;
using System.Linq;
using System.Collections.Generic;

using sones.GraphFS.DataStructures;
using sones.Lib.Serializer;
using sones.Lib.DataStructures;
using System.Threading;
using sones.Lib;

#endregion

namespace sones.GraphFS.Objects
{

    /// <summary>
    /// The abstract class for the GraphFS object ontology.
    /// </summary>

    public abstract class AFSObjectOntology : AFSObjectHeader, IFSObjectOntology
    {

        #region Properties - in-memory only!

        // The scope of these properties is in-memory only!
        // This means that these properties must not be written on disc,
        // as it might create conflicts during MakeFileSystem and while
        // creating and using hardlinks.

        #region ObjectLocation

        [NonSerialized]
        protected ObjectLocation _ObjectLocation = null;

        /// <summary>
        /// Stores the complete ObjectLocation (ObjectPath and ObjectName) of
        /// this file system object. Changing this property will automagically
        /// change the ObjectPath and ObjectName property.
        /// </summary>
        [NotIFastSerialized]
        public ObjectLocation ObjectLocation
        {

            get
            {

                if (_ObjectLocatorReference != null)
                    return _ObjectLocatorReference.ObjectLocation;

                return _ObjectLocation;

            }

            set
            {

                if (_ObjectLocatorReference != null)
                    _ObjectLocatorReference.ObjectLocationSetter = value;

                _ObjectLocation = value;
                _ObjectPath     = _ObjectLocation.Path;
                _ObjectName     = _ObjectLocation.Name;
                isDirty         = true;

            }

        }

        #endregion

        #region ObjectPath
        
        [NonSerialized]
        protected ObjectLocation _ObjectPath = null;

        /// <summary>
        /// Stores the ObjectPath of this AGraphObject. Changing this
        /// property will automagically change the myObjectLocation property.
        /// </summary>
        [NotIFastSerialized]
        public ObjectLocation ObjectPath
        {

            get
            {

                if (_ObjectLocatorReference != null)
                    return _ObjectLocatorReference.ObjectPath;

                return _ObjectPath;

            }

            set
            {

                if (_ObjectLocatorReference != null)
                    _ObjectLocatorReference.ObjectPath = value;

                _ObjectPath     = value;
                _ObjectLocation = new ObjectLocation(_ObjectPath, _ObjectName);
                isDirty         = true;

            }

        }

        #endregion

        #region ObjectName

        [NonSerialized]
        protected String _ObjectName = null;

        /// <summary>
        /// Stores the ObjectName of this AGraphObject. Changing this
        /// property will automagically change the myObjectLocation property.
        /// </summary>
        [NotIFastSerialized]
        public virtual String ObjectName
        {

            get
            {

                if (_ObjectLocatorReference != null)
                    return _ObjectLocatorReference.ObjectName;

                return _ObjectName;

            }

            set
            {

                if (_ObjectLocatorReference != null)
                    _ObjectLocatorReference.ObjectName = value;

                _ObjectName     = value;
                _ObjectLocation = new ObjectLocation(_ObjectPath, _ObjectName);
                isDirty         = true;

            }

        }

        #endregion


        #region ObjectStream

        [NonSerialized]
        protected String _ObjectStream = FSConstants.FILESTREAM;

        /// <summary>
        /// The actual name of this ObjectStream
        /// </summary>
        [NotIFastSerialized]
        public String ObjectStream
        {
            
            get
            {
                return _ObjectStream;
            }
            
            set
            {

                if (value == null || value.Length == 0)
                    throw new ArgumentNullException("The ObjectStream must not be null or its length zero!");

                _ObjectStream   = value;
                isDirty         = true;

            }

        }

        #endregion

        #region ObjectStreams

        /// <summary>
        /// Stores all ObjectStreams and their associated names, e.g. FILESTREAM
        /// </summary>
        [NotIFastSerialized]
        public IDictionary<String, ObjectStream> ObjectStreams
        {
            get
            {
                
                if (ObjectLocatorReference != null)
                    return ObjectLocatorReference.ToDictionary(key => key.Key, value => value.Value);

                return new Dictionary<String, ObjectStream>();

            }
        }

        #endregion


        #region ObjectEdition

        [NonSerialized]
        protected String _ObjectEdition = FSConstants.DefaultEdition;

        /// <summary>
        /// The actual name of this ObjectEdition
        /// </summary>
        [NotIFastSerialized]
        public String ObjectEdition
        {
            
            get
            {
                return _ObjectEdition;
            }
            
            set
            {

                if (value == null || value.Length == 0)
                    throw new ArgumentNullException("The ObjectEdition must not be null or its length zero!");
                
                _ObjectEdition  = value;
                isDirty         = true;

            }

        }

        #endregion

        #region ObjectEditions

        /// <summary>
        /// Stores all ObjectEditions and their associated names
        /// </summary>
        [NotIFastSerialized]
        public IDictionary<String, ObjectEdition> ObjectEditions
        {
            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        return ObjectLocatorReference[_ObjectStream].ToDictionary(key => key.Key, value => value.Value);

                return new Dictionary<String, ObjectEdition>();

            }
        }

        #endregion


        #region MinNumberOfRevisions

        /// <summary>
        /// The minimal number of revisions to store
        /// </summary>
        public UInt64 MinNumberOfRevisions
        {

            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            return ObjectLocatorReference[_ObjectStream][_ObjectEdition].MinNumberOfRevisions;

                throw new ArgumentException("Could not get MinNumberOfRevisions!");

            }

            set
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            // This will mark the ObjectLocator dirty!
                            ObjectLocatorReference[_ObjectStream][_ObjectEdition].MinNumberOfRevisions = value;

                throw new ArgumentException("Could not set MinNumberOfRevisions!");

            }

        }

        #endregion

        #region NumberOfRevisions

        /// <summary>
        /// The actual number of revisions stored
        /// </summary>
        public UInt64 NumberOfRevisions
        {

            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            return ObjectLocatorReference[_ObjectStream][_ObjectEdition].Count;

                throw new ArgumentException("Could not get NumberOfRevisions!");

            }

        }

        #endregion

        #region MaxNumberOfRevisions

        /// <summary>
        /// The maximal number of revisions to store
        /// </summary>
        public UInt64 MaxNumberOfRevisions
        {

            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            // This will mark the ObjectLocator dirty!
                            return ObjectLocatorReference[_ObjectStream][_ObjectEdition].MaxNumberOfRevisions;

                throw new ArgumentException("Could not get MaxNumberOfRevisions!");

            }

            set
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            ObjectLocatorReference[_ObjectStream][_ObjectEdition].MaxNumberOfRevisions = value;

                throw new ArgumentException("Could not set MaxNumberOfRevisions!");

            }

        }

        #endregion

        #region MinRevisionDelta

        /// <summary>
        /// Minimal timespan between to revisions.
        /// If the timespan between two revisions is smaller both revisions will
        /// be combined to the later revision.
        /// </summary>
        public UInt64 MinRevisionDelta
        {

            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            // This will mark the ObjectLocator dirty!
                            return ObjectLocatorReference[_ObjectStream][_ObjectEdition].MinRevisionDelta;

                throw new ArgumentException("Could not get MinRevisionDelta!");

            }

            set
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            ObjectLocatorReference[_ObjectStream][_ObjectEdition].MinRevisionDelta = value;

                throw new ArgumentException("Could not set MinRevisionDelta!");

            }

        }

        #endregion

        #region MaxRevisionAge

        /// <summary>
        /// Maximal age of an object revision. Older revisions will be
        /// deleted automatically if they also satify the MaxNumberOfRevisions
        /// criterium.
        /// </summary>
        public UInt64 MaxRevisionAge
        {

            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            // This will mark the ObjectLocator dirty!
                            return ObjectLocatorReference[_ObjectStream][_ObjectEdition].MaxRevisionAge;

                throw new ArgumentException("Could not get MaxRevisionAge!");

            }

            set
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            ObjectLocatorReference[_ObjectStream][_ObjectEdition].MaxRevisionAge = value;

                throw new ArgumentException("Could not set MaxRevisionAge!");

            }

        }

        #endregion

        #region ObjectRevision

        [NonSerialized]
        protected ObjectRevisionID _ObjectRevisionID = null;

        /// <summary>
        /// The RevisionID of this file system object
        /// </summary>
        [NotIFastSerialized]
        public ObjectRevisionID ObjectRevisionID
        {

            get
            {
                return _ObjectRevisionID;
            }

            set
            {

                if (value == null)
                    throw new ArgumentNullException("The ObjectRevisionID must not be null!");

                _ObjectRevisionID = value;
                isDirty = true;

            }

        }

        #endregion

        #region ObjectRevisions

        /// <summary>
        /// Stores the mapping between a RevisionID and the associated ObjectRevision
        /// </summary>
        [NotIFastSerialized]
        public IDictionary<ObjectRevisionID, ObjectRevision> ObjectRevisions
        {            
            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            return ObjectLocatorReference[_ObjectStream][_ObjectEdition].ToDictionary(key => key.Key, value => value.Value);

                return new Dictionary<ObjectRevisionID, ObjectRevision>();

            }
        }

        #endregion


        #region MinNumberOfCopies

        /// <summary>
        /// The minimal number of copies to store
        /// </summary>
        public UInt64 MinNumberOfCopies
        {

            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            if (ObjectLocatorReference[_ObjectStream][_ObjectEdition].ContainsKey(_ObjectRevisionID))
                                // This will mark the ObjectLocator dirty!
                                return ObjectLocatorReference[_ObjectStream][_ObjectEdition][_ObjectRevisionID].MinNumberOfCopies;

                throw new ArgumentException("Could not get MinNumberOfCopies!");

            }

            set
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            if (ObjectLocatorReference[_ObjectStream][_ObjectEdition].ContainsKey(_ObjectRevisionID))
                                ObjectLocatorReference[_ObjectStream][_ObjectEdition][_ObjectRevisionID].MinNumberOfCopies = value;

                throw new ArgumentException("Could not set MinNumberOfCopies!");

            }

        }

        #endregion

        #region NumberOfCopies

        /// <summary>
        /// The actual number of copies stored
        /// </summary>
        public UInt64 NumberOfCopies
        {

            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            if (ObjectLocatorReference[_ObjectStream][_ObjectEdition].ContainsKey(_ObjectRevisionID))
                                return (UInt64) ObjectLocatorReference[_ObjectStream][_ObjectEdition][_ObjectRevisionID].Count;

                throw new ArgumentException("Could not get NumberOfCopies!");

            }

        }

        #endregion

        #region MaxNumberOfCopies

        /// <summary>
        /// The maximal number of copies to store
        /// </summary>
        public UInt64 MaxNumberOfCopies
        {

            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            if (ObjectLocatorReference[_ObjectStream][_ObjectEdition].ContainsKey(_ObjectRevisionID))
                                // This will mark the ObjectLocator dirty!
                                return ObjectLocatorReference[_ObjectStream][_ObjectEdition][_ObjectRevisionID].MaxNumberOfCopies;

                throw new ArgumentException("Could not get MaxNumberOfCopies!");

            }

            set
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            if (ObjectLocatorReference[_ObjectStream][_ObjectEdition].ContainsKey(_ObjectRevisionID))
                                ObjectLocatorReference[_ObjectStream][_ObjectEdition][_ObjectRevisionID].MaxNumberOfCopies = value;

                throw new ArgumentException("Could not set MaxNumberOfCopies!");

            }

        }

        #endregion

        #region ParentRevisionIDs

        /// <summary>
        /// return the parent revision id's
        /// </summary>
        [NotIFastSerialized]
        public HashSet<ObjectRevisionID> ParentRevisionIDs
        {
            get
            {
                if (ObjectLocatorReference != null)
                {
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                    { 
                        if(ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                        {
                            if (ObjectLocatorReference[_ObjectStream][_ObjectEdition].ContainsKey(_ObjectRevisionID))
                                return ObjectLocatorReference[_ObjectStream][_ObjectEdition][_ObjectRevisionID].ParentRevisionIDs;
                        }
                    }
                }

                throw new ArgumentException("Could not get ParentRevisionIDs!");
            }
        }

        #endregion

        #region ObjectCopies

        /// <summary>
        /// Stores all ObjectDatastreams
        /// </summary>
        [NotIFastSerialized]
        public IEnumerable<ObjectDatastream> ObjectCopies
        {
            get
            {

                if (ObjectLocatorReference != null)
                    if (ObjectLocatorReference.ContainsKey(_ObjectStream))
                        if (ObjectLocatorReference[_ObjectStream].ContainsKey(_ObjectEdition))
                            if (ObjectLocatorReference[_ObjectStream][_ObjectEdition].ContainsKey(_ObjectRevisionID))
                                return ObjectLocatorReference[_ObjectStream][_ObjectEdition][_ObjectRevisionID].ToList();

                return new List<ObjectDatastream>();

            }
        }

        #endregion


        #region ObjectSize

        [NonSerialized]
        protected UInt64 _ObjectSize;

        /// <summary>
        /// The payload size of this AGraphObject.
        /// </summary>
        [NotIFastSerialized]
        public UInt64 ObjectSize
        {
            get
            {
                return _ObjectSize;
            }
        }

        #endregion

        #region ObjectSizeOnDisc

        [NonSerialized]
        protected UInt64 _ObjectSizeOnDisc;

        /// <summary>
        /// The payload size of this AGraphObject.
        /// </summary>
        [NotIFastSerialized]
        public UInt64 ObjectSizeOnDisc
        {
            get
            {
                return _ObjectSizeOnDisc;
            }
        }

        #endregion

        #endregion

        #region Constructors

        #region AFSObjectOntology()

        /// <summary>
        /// This will set all important variables within this AFSObject.
        /// This will especially create a new ObjectUUID and mark the
        /// AGraphObject as "new" and "dirty".
        /// </summary>
        public AFSObjectOntology()
        {

            _ObjectStream           = null;

            _ObjectSize             = 0;
            _ObjectSizeOnDisc       = 0;

        }

        #endregion

        #region AFSObjectOntology(myObjectUUID)

        /// <summary>
        /// This will set all important variables within this AFSObject.
        /// Additionally it sets the ObjectUUID to the given value and marks
        /// the AGraphObject as "new" and "dirty".
        /// </summary>
        public AFSObjectOntology(ObjectUUID myObjectUUID)
            : this()
        {
            // Members of AGraphStructure
            ObjectUUID               = myObjectUUID;
        }

        #endregion

        #endregion

        #region CloneObjectOntology(myAObjectOntology)

        public void CloneObjectOntology(AFSObjectOntology myAObjectOntology)
        {

            // Members of AFSObjectStructure
            _ObjectLocatorReference     = myAObjectOntology.ObjectLocatorReference;

            ObjectUUID                  = myAObjectOntology.ObjectUUID;

            // Members of IObjectLocation
            //_ObjectLocation             = myAObjectOntology.ObjectLocation;
            //_ObjectPath                 = myAObjectOntology.ObjectPath;
            //_ObjectName                 = myAObjectOntology.ObjectName;

            // Members of IFSObjectOntology
            _ObjectStream               = myAObjectOntology.ObjectStream;
            _ObjectEdition              = myAObjectOntology.ObjectEdition;
            _ObjectRevisionID           = myAObjectOntology.ObjectRevisionID;

            _ObjectSize                 = myAObjectOntology.ObjectSize;
            _ObjectSizeOnDisc           = myAObjectOntology.ObjectSizeOnDisc;

        }

        #endregion


        protected ulong GetAFSObjectOntologyEstimatedSize()
        {
            //Hack: calculate
            return EstimatedSizeConstants.AFSObjectOntologyObject;
        }
    }

}
