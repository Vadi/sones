/*
* sones GraphDB - Open Source Edition - http://www.sones.com
* Copyright (C) 2007-2010 sones GmbH
*
* This file is part of sones GraphDB Open Source Edition (OSE).
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
* 
*/

/* 
 * TypeAttribute
 * Stefan Licht, 2009-2010
 * Achim Friedland, 2010
 */

#region Usings

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;

using sones.GraphDB.Errors;
using sones.GraphDB.Exceptions;
using sones.GraphDB.ObjectManagement;
using sones.GraphDB.QueryLanguage.Result;
using sones.GraphDB.Settings;
using sones.GraphDB.Structures.EdgeTypes;
using sones.GraphDB.TypeManagement.PandoraTypes;

using sones.Lib;
using sones.Lib.Serializer;
using sones.Lib.ErrorHandling;
using sones.Lib.NewFastSerializer;

#endregion

namespace sones.GraphDB.TypeManagement
{

    /// <summary>
    /// The kind of a type (Single, List)
    /// </summary>
    
    public enum KindsOfType
    {
        SingleReference,
        ListOfNoneReferences,
        SetOfNoneReferences,
        SetOfReferences,
        //SettingAttribute,
        SpecialAttribute
    }

    /// <summary>
    /// This class holds all information about an specific attribute of a DB type.
    /// </summary>
    
    public class TypeAttribute : IFastSerialize, IGetName
    {

        //NLOG: temporarily commented
        //private static Logger Logger = LogManager.GetCurrentClassLogger();

        #region Data

        private GraphDBType _GraphDBType        = null;
        private GraphDBType _RelatedPandoraType = null;

        #endregion

        #region Properties

        #region Settings

        private Dictionary<String, ADBSettingsBase> _Settings;

        public Dictionary<String, ADBSettingsBase> GetObjectSettings
        {
            get { return _Settings; }
        }

        #endregion

        #region UUID

        /// <summary>
        /// The UUID
        /// </summary>
        public AttributeUUID UUID { get; set; }

        #endregion

        #region DBTypeUUID

        /// <summary>
        /// The DBTypeUUID
        /// </summary>
        public TypeUUID DBTypeUUID { get; set; }

        #endregion

        #region KindOfType

        /// <summary>
        /// The kind of type (single, list)
        /// </summary>
        public KindsOfType KindOfType { get; set; }

        #endregion

        #region TypeCharacteristics

        /// <summary>
        /// The characteristic (queue, unique, weighted) of the List-Typed Attribute
        /// </summary>
        public TypeCharacteristics TypeCharacteristics { get; set; }

        #endregion

        #region Name

        /// <summary>
        /// The Name of the TypeAttribute
        /// </summary>
        public String Name { get; set; }

        #endregion

        #region RelatedGraphDBTypeUUID

        public TypeUUID RelatedGraphDBTypeUUID { get; set; }

        #endregion

        #region BackwardEdgeDefinition

        /// <summary>
        /// If this attribute is an BackwardEdge attribute, than this hold the information about the Edge
        /// </summary>
        public EdgeKey BackwardEdgeDefinition { get; set; }

        #endregion

        #region IsBackwardEdge

        public Boolean IsBackwardEdge
        {
            get
            {
                Debug.Assert(TypeCharacteristics != null);
                return TypeCharacteristics.IsBackwardEdge;
            }
        }

        #endregion

        #region EdgeType

        public AEdgeType EdgeType { get; set; }

        #endregion

        #region DefaultValue

        public AObject DefaultValue { get; set; }

        #endregion

        #endregion

        #region Constructors

        #region TypeAttribute()

        public TypeAttribute()
            : this(new AttributeUUID())
        {
        }

        #endregion

        #region TypeAttribute(myAttributeUUID)

        public TypeAttribute(AttributeUUID myAttributeUUID)
        {
            UUID                    = myAttributeUUID;
            RelatedGraphDBTypeUUID  = null;
            TypeCharacteristics     = new TypeCharacteristics();
            _Settings               = new Dictionary<String, ADBSettingsBase>();
        }

        #endregion

        #region TypeAttribute(mySerializedData)

        public TypeAttribute(Byte[] mySerializedData)
            : this()
        {

            if (mySerializedData == null || mySerializedData.Length == 0)
                throw new ArgumentNullException("mySerializedData must not be null or its length be zero!");

            //Deserialize(mySerializedData);
            throw new NotImplementedException();

        }

        #endregion

        #endregion


        #region Methods

        #region SetPersistentSetting(mySettingName, myADBSettingsBase, myDBTypeManager)

        public Exceptional<Boolean> SetPersistentSetting(String mySettingName, ADBSettingsBase myADBSettingsBase, DBTypeManager myDBTypeManager)
        {

            if (!_Settings.ContainsKey(mySettingName))
            {
                _Settings.Add(mySettingName, (ADBSettingsBase)myADBSettingsBase.Clone());
            }
            else
            {
                _Settings[mySettingName] = (ADBSettingsBase)myADBSettingsBase.Clone();
            }

            var FlushExcept = myDBTypeManager.FlushType(GetRelatedType(myDBTypeManager));

            if (FlushExcept.Failed)
                return new Exceptional<Boolean>(FlushExcept);

            return new Exceptional<Boolean>(true);

        }

        #endregion

        #region RemovePersistentSetting(mySettingName, myDBTypeManager)

        public Exceptional<Boolean> RemovePersistentSetting(String mySettingName, DBTypeManager myDBTypeManager)
        {

            _Settings.Remove(mySettingName);

            var FlushExcept = myDBTypeManager.FlushType(GetRelatedType(myDBTypeManager));

            if (FlushExcept.Failed)
                return new Exceptional<Boolean>(FlushExcept);

            return new Exceptional<Boolean>(true);

        }

        #endregion

        #region GetPersistentSetting(mySettingName)

        public ADBSettingsBase GetPersistentSetting(String mySettingName)
        {

            ADBSettingsBase _ADBSettingsBase = null;

            if (_Settings.TryGetValue(mySettingName, out _ADBSettingsBase))
                //TODO: Remove this senseless .Clone()!
                return (ADBSettingsBase)_ADBSettingsBase.Clone();

            return null;

        }

        #endregion


        #region GetDefaultValue(myDBContext)

        public AObject GetDefaultValue(DBContext myDBContext)
        {

            if (KindOfType == KindsOfType.SingleReference)
            {

                if (GetDBType(myDBContext.DBTypeManager).IsUserDefined)
                    return (AObject) EdgeType.GetNewInstance();

                var val = ((ADBBaseObject) GraphDBTypeMapper.GetADBBaseObjectFromUUID(DBTypeUUID));
                val.SetValue(DBObjectInitializeType.Default);
                return val as AObject;

            }

            if (KindOfType == KindsOfType.SetOfReferences)
                return (AObject) EdgeType.GetNewInstance();

            return null;

        }

        #endregion

        #region GetADBBaseObjectType(myDBTypeManager)

        public ADBBaseObject GetADBBaseObjectType(DBTypeManager myDBTypeManager)
        {

            if (GraphDBTypeMapper.IsBasicType(GetDBType(myDBTypeManager).Name))
                return GraphDBTypeMapper.GetPandoraObjectFromTypeName(GetDBType(myDBTypeManager).Name);

            return null;

        }

        #endregion

        #region GetDBType(myDBTypeManager)

        public GraphDBType GetDBType(DBTypeManager myDBTypeManager)
        {

            if (_GraphDBType == null)
                _GraphDBType = myDBTypeManager.GetTypeByUUID(DBTypeUUID);
            
            return _GraphDBType;

        }

        #endregion

        #region GetRelatedType(myDBTypeManager)

        public GraphDBType GetRelatedType(DBTypeManager myDBTypeManager)
        {

            if (_RelatedPandoraType == null)
            {
                if (!IsBackwardEdge)
                    _RelatedPandoraType = myDBTypeManager.GetTypeByUUID(RelatedGraphDBTypeUUID);

                else
                    _RelatedPandoraType = myDBTypeManager.GetTypeByUUID(BackwardEdgeDefinition.TypeUUID);
            }

            return _RelatedPandoraType;

        }

        #endregion

        #endregion


        #region IComparable Members

        public Int32 CompareTo(Object myObject)
        {

            var objType = (TypeAttribute) myObject;

            return UUID.CompareTo(objType.UUID);

        }

        #endregion

        #region IFastSerialize Members

        #region isDirty

        public Boolean isDirty
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ModificationTime

        public DateTime ModificationTime
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region Serialize(ref mySerializationWriter)

        public void Serialize(ref SerializationWriter mySerializationWriter)
        {

            if (mySerializationWriter != null)
            {

                UUID.Serialize(ref mySerializationWriter);
                DBTypeUUID.Serialize(ref mySerializationWriter);
                mySerializationWriter.WriteObject((Byte)KindOfType);
                TypeCharacteristics.Serialize(ref mySerializationWriter);
                
                mySerializationWriter.WriteObject(Name);

                mySerializationWriter.WriteObject(EdgeType != null);
                if (EdgeType != null)
                    mySerializationWriter.WriteObject(EdgeType);

                RelatedGraphDBTypeUUID.Serialize(ref mySerializationWriter);

                if (TypeCharacteristics.IsBackwardEdge)
                    mySerializationWriter.WriteObject(BackwardEdgeDefinition);

                mySerializationWriter.WriteObject(DefaultValue);

                // Write Settings
                mySerializationWriter.WriteObject((UInt32)_Settings.Count);
                foreach (var _KVPair in _Settings)
                    mySerializationWriter.WriteObject(_KVPair.Value);

            }

        }

        #endregion

        #region Deserialize(ref mySerializationReader)

        public void Deserialize(ref SerializationReader mySerializationReader)
        {

            UInt32 _Capacity;

            try
            {

                if (mySerializationReader != null)
                {

                    UUID = new AttributeUUID();
                    UUID.Deserialize(ref mySerializationReader);
                    DBTypeUUID = new TypeUUID();
                    DBTypeUUID.Deserialize(ref mySerializationReader);
                    KindOfType = (KindsOfType) (Byte) mySerializationReader.ReadObject();
                    TypeCharacteristics = new TypeCharacteristics();
                    TypeCharacteristics.Deserialize(ref mySerializationReader);

                    Name = (String)mySerializationReader.ReadObject();

                    var hasEdgeType = (Boolean) mySerializationReader.ReadObject();
                    if (hasEdgeType)
                    {                        
                        try
                        {
                            EdgeType = (AEdgeType) mySerializationReader.ReadObject();
                        }
                        catch(Exception ex)
                        {
                            //NLOG: temporarily commented
                            //Logger.Fatal("Could not deserialize EdgeType");
                            throw new GraphDBException(new Error_UnknownDBError(ex));
                        }

                    }

                    RelatedGraphDBTypeUUID = new TypeUUID();
                    RelatedGraphDBTypeUUID.Deserialize(ref mySerializationReader);
                    if (TypeCharacteristics.IsBackwardEdge)
                        BackwardEdgeDefinition = (EdgeKey) mySerializationReader.ReadObject();

                    DefaultValue = (AObject) mySerializationReader.ReadObject();

                    #region Read Settings

                    _Capacity = (UInt32) mySerializationReader.ReadObject();

                    _Settings = new Dictionary<String, ADBSettingsBase>();

                    for (var i = 0; i < _Capacity; i++)
                    {

                        var _SettingObject = (ADBSettingsBase) mySerializationReader.ReadObject();

                        if (_SettingObject != null)
                            _Settings.Add(_SettingObject.Name, _SettingObject);
                        
                        /*Type settingType = (Type)mySerializationReader.ReadObject();
                        ADBSettingsBase _SettingObject = null;
                        try
                        {
                            _SettingObject = (ADBSettingsBase)Activator.CreateInstance(settingType);
                        }
                        catch
                        {
                            Logger.Error("Could not create an instance of setting " + settingType.ToString());
                        }
                        Byte[] Bytes = (Byte[])mySerializationReader.ReadObject();
                        if (_SettingObject != null)
                        {
                            _SettingObject.Deserialize(new SerializationReader(Bytes));
                            _Settings.Add(_SettingObject.Name, _SettingObject);
                        }*/
                    }

                    #endregion

                }
            }

            catch (Exception e)
            {
                throw new SerializationException("The Setting could not be deserialized!\n\n" + e);
            }

        }

        #endregion

        #endregion

        #region ToString(...)

        #region ToString()

        public override String ToString()
        {
            return ToString("|");
        }

        #endregion

        #region ToString(mySeperator)

        public String ToString(String mySeperator)
        {

            var _StringBuilder = new StringBuilder();

            _StringBuilder.Append(Name);

            _StringBuilder.Append(" : ");

            if (TypeCharacteristics.IsBackwardEdge)
            {
                _StringBuilder.Append("BACKWARDEDGE<");

                _StringBuilder.Append(BackwardEdgeDefinition.ToString());

                _StringBuilder.Append(">");
            }

            else
            {

                if (KindOfType == KindsOfType.ListOfNoneReferences)
                    _StringBuilder.Append("LIST<");

                if (KindOfType == KindsOfType.SetOfNoneReferences || KindOfType == KindsOfType.SetOfReferences)
                    _StringBuilder.Append("SET<");

                _StringBuilder.Append(DBTypeUUID.ToString());

                if (KindOfType == KindsOfType.SetOfReferences || KindOfType == KindsOfType.SetOfNoneReferences || KindOfType == KindsOfType.ListOfNoneReferences)
                    _StringBuilder.Append(">");
            }

            var tCharcts = TypeCharacteristics.ToString();
            if (tCharcts != String.Empty)
            {
                _StringBuilder.Append(mySeperator);
                _StringBuilder.Append(TypeCharacteristics.ToString());
            }

            _StringBuilder.Append(mySeperator);
            _StringBuilder.Append("[UUID: ");
            _StringBuilder.Append(DBTypeUUID.ToString().Firststring(3));
            _StringBuilder.Append("]");

            return _StringBuilder.ToString();

        }

        #endregion

        #endregion

    }

}
