﻿/* <id name="GraphDB DBList DBList" />
 * <copyright file="DBDouble.cs"
 *            company="sones GmbH">
 * Copyright (c) sones GmbH. All rights reserved.
 * </copyright>
 * <developer>Stefan Licht</developer>
 * <summary>The String.</summary>
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sones.GraphDB.Structures.Enums;
using sones.GraphDB.Structures;
using sones.GraphDB.Structures.EdgeTypes;

using sones.Lib.NewFastSerializer;
using sones.Lib.DataStructures.UUID;
using sones.GraphFS.DataStructures;
using sones.Lib.ErrorHandling;
using sones.GraphDB.ObjectManagement;
using sones.GraphDB.Exceptions;
using sones.GraphDB.TypeManagement;
using sones.Lib;


namespace sones.GraphDB.TypeManagement.BasicTypes
{
    public class DBEdge : ADBBaseObject
    {
        public static readonly TypeUUID UUID = new TypeUUID(20);
        public const string Name = "SET";

        private UInt64 _estimatedSize = 0;

        #region TypeCode
        public override UInt32 TypeCode { get { return 407; } }
        #endregion

        #region Data

        private IEnumerable<Exceptional<DBObjectStream>> _Value;

        #endregion

        #region Constructors

        public DBEdge()
        {
            _Value = null;

            //DO NOT ESTIMATE THE SIZE!!! this constructor is for IFastSerializer purpose only
        }
        
        public DBEdge(DBObjectInitializeType myDBObjectInitializeType)
        {
            SetValue(myDBObjectInitializeType);

            //DO NOT ESTIMATE THE SIZE!!! it's done in SetValue(...)

        }

        public DBEdge(Object myValue)
        {
            Value = myValue;

            CalcEstimatedSize(this);

        }

        #endregion

        #region Overrides

        public override int CompareTo(ADBBaseObject obj)
        {
            return CompareTo(obj.Value);
        }

        public override int CompareTo(object obj)
        {
            return (_Value == obj) ? 0 : 1;
        }

        public override object Value
        {
            get { return _Value; }
            set
            {
                if (value is DBEdge)
                    _Value = ((DBEdge)value)._Value;
                else if (value is IEnumerable<Exceptional<DBObjectStream>>)
                    _Value = value as IEnumerable<Exceptional<DBObjectStream>>;
                else 
                    throw new GraphDBException(new Errors.Error_DataTypeDoesNotMatch("IEnumerable<Exceptional<DBObjectStream>>", value.GetType().Name));

                CalcEstimatedSize(this);

            }
        }

        #endregion

        #region Operations

        [Obsolete("Operator '+' cannot be applied to operands of type 'Object' and 'Object'")]
        public static DBEdge operator +(DBEdge myGraphObjectA, Object myValue)
        {
            return myGraphObjectA;
        }

        [Obsolete("Operator '-' cannot be applied to operands of type 'Object' and 'Object'")]
        public static DBEdge operator -(DBEdge myGraphObjectA, Object myValue)
        {
            return myGraphObjectA;
        }

        [Obsolete("Operator '*' cannot be applied to operands of type 'Object' and 'Object'")]
        public static DBEdge operator *(DBEdge myGraphObjectA, Object myValue)
        {
            return myGraphObjectA;
        }

        [Obsolete("Operator '/' cannot be applied to operands of type 'Object' and 'Object'")]
        public static DBEdge operator /(DBEdge myGraphObjectA, Object myValue)
        {
            return myGraphObjectA;
        }

        [Obsolete("Operator '+' cannot be applied to operands of type 'Object' and 'Object'")]
        public override ADBBaseObject Add(ADBBaseObject myGraphObjectA, ADBBaseObject myGraphObjectB)
        {
            return myGraphObjectA;
        }

        [Obsolete("Operator '-' cannot be applied to operands of type 'Object' and 'Object'")]
        public override ADBBaseObject Sub(ADBBaseObject myGraphObjectA, ADBBaseObject myGraphObjectB)
        {
            return myGraphObjectA;
        }

        [Obsolete("Operator '*' cannot be applied to operands of type 'Object' and 'Object'")]
        public override ADBBaseObject Mul(ADBBaseObject myGraphObjectA, ADBBaseObject myGraphObjectB)
        {
            return myGraphObjectA;
        }

        [Obsolete("Operator '/' cannot be applied to operands of type 'Object' and 'Object'")]
        public override ADBBaseObject Div(ADBBaseObject myGraphObjectA, ADBBaseObject myGraphObjectB)
        {
            return myGraphObjectA;
        }

        [Obsolete("Operator '+' cannot be applied to operands of type 'Object' and 'Object'")]
        public override void Add(ADBBaseObject myGraphObject)
        {
        }

        [Obsolete("Operator '-' cannot be applied to operands of type 'Object' and 'Object'")]
        public override void Sub(ADBBaseObject myGraphObject)
        {
        }

        [Obsolete("Operator '*' cannot be applied to operands of type 'Object' and 'Object'")]
        public override void Mul(ADBBaseObject myGraphObject)
        {
        }

        [Obsolete("Operator '/' cannot be applied to operands of type 'Object' and 'Object'")]
        public override void Div(ADBBaseObject myGraphObject)
        {
        }

        #endregion

        #region IsValid

        public static Boolean IsValid(Object myObject)
        {
            return (myObject != null &&
                (myObject is DBEdge || myObject is EdgeTypeWeighted || myObject is EdgeTypeSetOfReferences || myObject is HashSet<ObjectUUID> || myObject is IEnumerable<Exceptional<DBObjectStream>>));
        }

        public override bool IsValidValue(Object myValue)
        {
            return DBEdge.IsValid(myValue);
        }

        #endregion

        #region Clone

        public override ADBBaseObject Clone()
        {
            return new DBEdge(_Value);
        }

        public override ADBBaseObject Clone(Object myValue)
        {
            return new DBEdge(myValue);
        }

        #endregion

        public override void SetValue(DBObjectInitializeType myDBObjectInitializeType)
        {
            switch (myDBObjectInitializeType)
            {
                case DBObjectInitializeType.Default:
                case DBObjectInitializeType.MinValue:
                case DBObjectInitializeType.MaxValue:
                default:
                    _Value = null;
                    break;
            }

            CalcEstimatedSize(this);

        }

        public override void SetValue(object myValue)
        {
            Value = myValue;
        }

        public override BasicType Type
        {
            get { return BasicType.SetOfDBObjects; }
        }

        public override string ObjectName
        {
            get { return Name; }
        }

        public IEnumerable<Exceptional<DBObjectStream>> GetDBObjects()
        {
            if (_Value is IEnumerable<Exceptional<DBObjectStream>>)
            {
                return _Value as IEnumerable<Exceptional<DBObjectStream>>;
            }
            else
            {
                throw new GraphDBException(new Errors.Error_NotImplemented(new System.Diagnostics.StackTrace(true)));
            }
        }

        #region IFastSerialize Members

        public override void Serialize(ref SerializationWriter mySerializationWriter)
        {
            Serialize(ref mySerializationWriter, this);
        }

        public override void Deserialize(ref SerializationReader mySerializationReader)
        {
            Deserialize(ref mySerializationReader, this);
        }

        #endregion

        private void Serialize(ref SerializationWriter mySerializationWriter, DBEdge myValue)
        {
            mySerializationWriter.WriteObject(myValue._Value);
        }

        private object Deserialize(ref SerializationReader mySerializationReader, DBEdge myValue)
        {
            myValue._Value = mySerializationReader.ReadObject() as IEnumerable<Exceptional<DBObjectStream>>;

            CalcEstimatedSize(myValue);


            return myValue;
        }

        #region IFastSerializationTypeSurrogate 
        public override bool SupportsType(Type type)
        {
            return this.GetType() == type;
        }

        public override void Serialize(SerializationWriter writer, object value)
        {
            Serialize(ref writer, (DBEdge)value);
        }

        public override object Deserialize(SerializationReader reader, Type type)
        {
            DBEdge thisObject = (DBEdge)Activator.CreateInstance(type);
            return Deserialize(ref reader, thisObject);
        }

        #endregion

        #region ToString(IFormatProvider provider)

        public override string ToString(IFormatProvider provider)
        {
            return ToString();
        }

        #endregion

        #region IObject

        public override ulong GetEstimatedSize()
        {
            return _estimatedSize;
        }

        private void CalcEstimatedSize(DBEdge myTypeAttribute)
        {
            _estimatedSize = GetBaseSize();

            //DBObjectStreams + BaseSize

            if (_Value != null)
            {
                foreach (var aDBO in _Value)
                {
                    _estimatedSize += aDBO.Value.GetEstimatedSize();
                }
            }

        }

        #endregion
    }
}
