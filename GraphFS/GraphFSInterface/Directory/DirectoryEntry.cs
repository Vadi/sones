﻿/*
 * GraphFS - DirectoryEntry
 * (c) Achim Friedland, 2008 - 2009
 * 
 *             |  Objects  | VirtualObjects |  Inlinedata  | Symlink
 * ---------------------------------------------------------------------
 *  Inlinedata |     -     |       -        |    $data     |  $target
 *  INodePos   |     x     |       -        |       -      |    -
 *  ObjStrList | multiple  |    multiple    | "INLINEDATA" | "SYMLINK"
 * 
 * Symlinks and InlineData must have a single ObjectStream within
 * the ObjectStreamsList like: FSConstants.SYMLINK or
 * FSConstants.INLINEDATA
 * 
 * */

#region Usings

using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;

using sones.Lib;
using sones.Lib.Serializer;
using sones.Lib.NewFastSerializer;
using sones.Lib.DataStructures;
using sones.StorageEngines;
using sones.GraphFS.Exceptions;
using sones.GraphFS.DataStructures;
#endregion

namespace sones.GraphFS.InternalObjects
{

    /// <summary>
    /// This is a directory entry holding all information of a file or
    /// subdirectory. It may appear as leaf within a directory tree or
    /// within a directory hashmap.
    /// </summary>

    public class DirectoryEntry : IFastSerialize, IFastSerializationTypeSurrogate, IEstimable
    {


        #region Properties

        #region IEstimable

        private UInt64 _estimatedSize = 0;

        #endregion

        #region INodePositions

        private HashSet<ExtendedPosition> _INodePositions;
        public UInt32 TypeCode { get { return 201; } }

        /// <summary>
        /// A list of extended positions for locationg the INodes
        /// of an file system objects
        /// </summary>
        public HashSet<ExtendedPosition> INodePositions
        {

            get
            {
                return _INodePositions;
            }

            set
            {
                #region Estimated Size

                //old -
                if (_INodePositions != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_INodePositions.Count) * EstimatedSizeConstants.ExtendedPosition;
                }

                //new +
                if (value != null)
                {
                    _estimatedSize += Convert.ToUInt64(value.Count) * EstimatedSizeConstants.ExtendedPosition;
                }

                #endregion

                _INodePositions = value;
                _InlineData = null;
                isDirty = true;
            }

        }

        #endregion

        #region ObjectStreamsList

        private HashSet<String> _ObjectStreamsList;

        /// <summary>
        /// A hashset for caching which object streams are present
        /// within the actual object locator. The information on older
        /// revisions has to be read from the object locator directly!
        /// </summary>
        public HashSet<String> ObjectStreamsList
        {

            get
            {
                return _ObjectStreamsList;
            }

            set
            {
                #region Estimated Size

                //old -
                if (_ObjectStreamsList != null)
                {
                    //assuming that a ObjectStream name isn't longer that 10 chars
                    _estimatedSize -= Convert.ToUInt64(_ObjectStreamsList.Count) * EstimatedSizeConstants.Char * EstimatedSizeConstants.EstimatedObjectStreamNameLength;
                }

                //new +
                if (value != null)
                {
                    _estimatedSize += Convert.ToUInt64(value.Count) * EstimatedSizeConstants.Char * EstimatedSizeConstants.EstimatedObjectStreamNameLength;
                }

                #endregion

                _ObjectStreamsList = value;
                _InlineData = null;
                isDirty = true;
            }

        }

        #endregion


        #region hasInlineData

        public Boolean hasInlineData
        {
            get
            {

                if (_InlineData != null)
                    if (_INodePositions.ULongCount() == 0)
                        if (_ObjectStreamsList.ULongCount() == 1)
                            if (_ObjectStreamsList.Contains(FSConstants.INLINEDATA))
                                return true;

                return false;

            }
        }

        #endregion

        #region InlineData

        private Byte[] _InlineData;

        /// <summary>
        /// An array of bytes for storing inline data like the
        /// target of a symlink or the data of very small files
        /// </summary>
        public Byte[] InlineData
        {

            get
            {

                if (hasInlineData)
                    return _InlineData;

                return new Byte[0];

            }

            set
            {
                #region Estimated Size

                //old -
                if (_InlineData != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_InlineData.Length) * EstimatedSizeConstants.Byte;
                }
                //clear INOdePositions
                if (_INodePositions != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_INodePositions.Count) * EstimatedSizeConstants.ExtendedPosition;
                }

                //new +
                if (value != null)
                {
                    _estimatedSize += Convert.ToUInt64(value.Length) * EstimatedSizeConstants.Byte;
                }

                #endregion

                _InlineData = value;
                _INodePositions.Clear();
                isDirty = true;
            }

        }

        #endregion


        #region isSymlink

        /// <summary>
        /// Indicates if this directory entry is a symlink.
        /// </summary>
        public Boolean isSymlink
        {
            get
            {

                if (_InlineData != null)
                    if (_INodePositions.ULongCount() == 0)
                        if (_ObjectStreamsList.ULongCount() == 1)
                            if (_ObjectStreamsList.Contains(FSConstants.SYMLINK))
                                return true;

                return false;

            }
        }

        #endregion

        #region Symlink

        /// <summary>
        /// Gets or sets a symlink
        /// </summary>
        public ObjectLocation Symlink
        {

            get
            {

                if (isSymlink)
                {
                    return new ObjectLocation(
                        new UTF8Encoding().GetString(_InlineData).Split(
                                new String[] { FSPathConstants.PathDelimiter },
                                StringSplitOptions.RemoveEmptyEntries)
                            );
                }

                return null;

            }

            set
            {
                _InlineData = Encoding.UTF8.GetBytes(value.ToString());

                #region Estimated Size

                //old -
                if (_InlineData != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_InlineData.Length) * EstimatedSizeConstants.Byte;
                }
                //clear INOdePositions
                if (_INodePositions != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_INodePositions.Count) * EstimatedSizeConstants.ExtendedPosition;
                }
                //handle ObjectStreamList
                if (_ObjectStreamsList != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_ObjectStreamsList.Count) * EstimatedSizeConstants.Char * EstimatedSizeConstants.EstimatedObjectStreamNameLength;
                }

                //new +
                if (value != null)
                {
                    _estimatedSize += Convert.ToUInt64(_InlineData.Length) * EstimatedSizeConstants.Byte;
                }
                //Symlink
                _estimatedSize += EstimatedSizeConstants.Char * EstimatedSizeConstants.EstimatedObjectStreamNameLength;

                #endregion

                _INodePositions.Clear();
                _ObjectStreamsList = new HashSet<String> { FSConstants.SYMLINK };
                isDirty = true;
            }

        }

        #endregion


        #region isVirtual

        /// <summary>
        /// Indicates if this directory entry is a virtual entry.
        /// Virtual entries do not have any INodePositions stored.
        /// </summary>
        public Boolean isVirtual
        {
            get
            {

                if (_InlineData == null)
                    if (_INodePositions.ULongCount() == 0)
                        if (_ObjectStreamsList.ULongCount() > 0)
                            return true;

                return false;

            }
        }

        #endregion

        #region Virtual

        /// <summary>
        /// Gets or set a virtual objectstreamlist.
        /// </summary>
        public HashSet<String> Virtual
        {

            get
            {

                if (isVirtual)
                    return _ObjectStreamsList;

                return new HashSet<String>();

            }

            set
            {
                #region Estimated Size

                //old -
                if (_InlineData != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_InlineData.Length) * EstimatedSizeConstants.Byte;
                }
                //clear INOdePositions
                if (_INodePositions != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_INodePositions.Count) * EstimatedSizeConstants.ExtendedPosition;
                }
                //handle ObjectStreamList
                if (_ObjectStreamsList != null)
                {
                    _estimatedSize -= Convert.ToUInt64(_ObjectStreamsList.Count) * EstimatedSizeConstants.Char * EstimatedSizeConstants.EstimatedObjectStreamNameLength;
                }

                //new +
                if (value != null)
                {
                    _estimatedSize += Convert.ToUInt64(value.Count) * EstimatedSizeConstants.Char * EstimatedSizeConstants.EstimatedObjectStreamNameLength;
                }

                #endregion

                _InlineData = null;
                _INodePositions.Clear();
                _ObjectStreamsList = value;
                isDirty = true;
            }

        }

        #endregion

        #endregion

        #region Constructors

        #region DirectoryEntry()

        /// <summary>
        /// The basic constructor of a directory entry
        /// </summary>
        public DirectoryEntry()
        {
            _INodePositions = new HashSet<ExtendedPosition>();
            _InlineData = null;
            _ObjectStreamsList = new HashSet<String>();

            #region Estimated size

            //InodePositions + ObjectStreamList + ClassDefaultSize + estimatedSize + typecode
            _estimatedSize += EstimatedSizeConstants.HashSet + EstimatedSizeConstants.HashSet + GetClassBaseSize();

            #endregion
        }

        #endregion

        #region DirectoryEntry(myObjectStream, myINodePositions)

        /// <summary>
        /// A constructor of a directory entry setting the internal ObjectStreamsList
        /// to the given ObjectStream and INodePositions.
        /// </summary>
        public DirectoryEntry(String myObjectStream, IEnumerable<ExtendedPosition> myINodePositions)
        {
            #region INodePosition

            _INodePositions = new HashSet<ExtendedPosition>();
            _estimatedSize += EstimatedSizeConstants.HashSet;

            foreach (var aInodePosition in myINodePositions)
            {
                _INodePositions.Add(aInodePosition);
                _estimatedSize += EstimatedSizeConstants.ExtendedPosition;
            }

            #endregion

            #region InlineData

            _InlineData = null;

            #endregion

            #region ObjectStreamList

            _ObjectStreamsList = new HashSet<String> { myObjectStream };

            _estimatedSize += EstimatedSizeConstants.HashSet + EstimatedSizeConstants.Char * EstimatedSizeConstants.EstimatedObjectStreamNameLength;

            #endregion

            _estimatedSize += GetClassBaseSize();
        }

        #endregion

        #endregion


        #region Clone()

        public DirectoryEntry Clone()
        {

            var newEntry = new DirectoryEntry();

            if (_InlineData != null)
            {
                newEntry._InlineData = new Byte[_InlineData.Length];
                _InlineData.CopyTo(newEntry._InlineData, 0);
            }

            if (_INodePositions != null)
            {

                newEntry._INodePositions = new HashSet<ExtendedPosition>();

                foreach (var _ExtendedPosition in _INodePositions)
                    newEntry._INodePositions.Add(new ExtendedPosition(_ExtendedPosition.StorageUUID, _ExtendedPosition.Position));

            }

            newEntry._isDirty = _isDirty;

            if (_ObjectStreamsList != null)
            {

                newEntry._ObjectStreamsList = new HashSet<String>();

                foreach (var _ObjectStream in _ObjectStreamsList)
                    newEntry._ObjectStreamsList.Add(_ObjectStream);

            }

            #region estimatedSize

            newEntry._estimatedSize = this._estimatedSize;

            #endregion

            return newEntry;

        }

        #endregion


        #region IFastSerialize Members

        #region isDirty

        private Boolean _isDirty = false;

        public Boolean isDirty
        {

            get
            {
                return _isDirty;
            }

            set
            {
                _isDirty = value;
            }

        }

        #endregion

        #region ModificationTime

        public DateTime ModificationTime
        {

            get
            {
                throw new NotImplementedException();
            }

        }

        #endregion

        #region Serialize(ref mySerializationWriter)

        public void Serialize(ref SerializationWriter mySerializationWriter)
        {
            Serialize(ref mySerializationWriter, this);
        }

        #endregion

        #region Deserialize(ref mySerializationReader)

        public void Deserialize(ref SerializationReader mySerializationReader)
        {
            Deserialize(ref mySerializationReader, this);
        }

        #endregion

        #region Serialize(ref mySerializationWriter, myDirectoryEntry)

        private void Serialize(ref SerializationWriter mySerializationWriter, DirectoryEntry myDirectoryEntry)
        {

            try
            {

                #region Write the InlineData

                mySerializationWriter.Write(myDirectoryEntry._InlineData);

                #endregion

                #region Write the INodePositions

                mySerializationWriter.WriteUInt32((UInt32)myDirectoryEntry.INodePositions.Count);

                foreach (var _ExtendedPosition in myDirectoryEntry.INodePositions)
                {
                    _ExtendedPosition.StorageUUID.Serialize(ref mySerializationWriter);
                    mySerializationWriter.WriteUInt64(_ExtendedPosition.Position);
                }

                #endregion

                #region Write the ObjectStreamsList

                mySerializationWriter.WriteUInt32((UInt32)myDirectoryEntry.ObjectStreamsList.Count);

                foreach (var _ObjectStreamType in myDirectoryEntry.ObjectStreamsList)
                    mySerializationWriter.WriteString(_ObjectStreamType);

                #endregion

                myDirectoryEntry.isDirty = false;

            }

            catch (SerializationException e)
            {
                throw new SerializationException(e.Message);
            }

        }

        #endregion

        #region Deserialize(ref SerializationReader mySerializationReader, myDirectoryEntry)

        private object Deserialize(ref SerializationReader mySerializationReader, DirectoryEntry myDirectoryEntry)
        {

            try
            {

                #region Read the Inlinedata

                myDirectoryEntry._InlineData = mySerializationReader.ReadByteArray();

                #region Estimated Size

                if (myDirectoryEntry._InlineData != null)
                {
                    myDirectoryEntry._estimatedSize += Convert.ToUInt64(myDirectoryEntry._InlineData.Length) * EstimatedSizeConstants.Byte;
                }

                #endregion

                #endregion

                #region Read the INodePositions

                #region Estimated Size

                myDirectoryEntry._estimatedSize += EstimatedSizeConstants.HashSet;

                #endregion

                var _NumOfINodePositions = mySerializationReader.ReadUInt32();

                if (_NumOfINodePositions > 0)
                {
                    for (var j = 0UL; j < _NumOfINodePositions; j++)
                    {
                        StorageUUID ID = new StorageUUID();
                        ID.Deserialize(ref mySerializationReader);
                        myDirectoryEntry._INodePositions.Add(new ExtendedPosition(ID, mySerializationReader.ReadUInt64()));

                        #region Estimated Size

                        myDirectoryEntry._estimatedSize += EstimatedSizeConstants.ExtendedPosition;

                        #endregion

                    }
                }

                #endregion

                #region Read the ObjectStreamsList

                #region Estimated Size

                myDirectoryEntry._estimatedSize += EstimatedSizeConstants.HashSet;

                #endregion

                var _NumberOfObjectStreamTypes = mySerializationReader.ReadUInt32();

                if (_NumberOfObjectStreamTypes > 0)
                {
                    for (var j = 0UL; j < _NumberOfObjectStreamTypes; j++)
                    {
                        myDirectoryEntry._ObjectStreamsList.Add(mySerializationReader.ReadString());

                        #region Estimated Size

                        myDirectoryEntry._estimatedSize += EstimatedSizeConstants.Char * EstimatedSizeConstants.EstimatedObjectStreamNameLength;

                        #endregion

                    }
                }

                #endregion

            }

            catch (GraphFSException e)
            {
                throw new GraphFSException("DirectoryEntry could not be deserialized!\n\n" + e);
            }

            myDirectoryEntry.isDirty = true;

            return myDirectoryEntry;

        }

        #endregion

        #endregion

        #region IFastSerializationTypeSurrogate Members

        public bool SupportsType(Type myType)
        {
            return myType == this.GetType();
        }

        public void Serialize(SerializationWriter mySerializationWriter, object myObject)
        {
            Serialize(ref mySerializationWriter, (DirectoryEntry)myObject);
        }

        public object Deserialize(SerializationReader mySerializationReader, Type myType)
        {
            DirectoryEntry thisObject = (DirectoryEntry)Activator.CreateInstance(myType);
            return Deserialize(ref mySerializationReader, thisObject);
        }

        #endregion


        #region GetHashCode()

        public override int GetHashCode()
        {
            return INodePositions.GetHashCode() ^ InlineData.ToHexString(SeperatorTypes.NONE).GetHashCode() ^ isSymlink.GetHashCode() ^ ObjectStreamsList.GetHashCode();
        }

        #endregion

        #region ToString()

        public override String ToString()
        {

            var _DirectoryEntryString = new StringBuilder();

            if (_INodePositions != null && _INodePositions.Count > 0)
                _DirectoryEntryString.Append("[INodePositions] ");

            if (hasInlineData)
                _DirectoryEntryString.Append("[hasInlineData] ");

            if (isSymlink)
                _DirectoryEntryString.Append("[isSymlink] ");

            if (isVirtual)
                _DirectoryEntryString.Append("[isVirtual] ");

            if (_ObjectStreamsList != null && _ObjectStreamsList.Count > 0)
            {

                _DirectoryEntryString.Append("[");

                foreach (var _ObjectStream in _ObjectStreamsList)
                    _DirectoryEntryString.Append(_ObjectStream).Append("|");

                _DirectoryEntryString.Length = _DirectoryEntryString.Length - 1;
                _DirectoryEntryString.Append("]");

            }

            return _DirectoryEntryString.ToString();

        }

        #endregion

        #region IEstimable

        public ulong GetEstimatedSize()
        {
            return _estimatedSize;
        }

        private ulong GetClassBaseSize()
        {
            return EstimatedSizeConstants.ClassDefaultSize + EstimatedSizeConstants.UInt64 + EstimatedSizeConstants.UInt32;
        }

        #endregion

    }

}
