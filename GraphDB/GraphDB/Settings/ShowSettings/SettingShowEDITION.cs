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

#region Usings
using System;
using sones.GraphDB.TypeManagement.PandoraTypes;
using sones.Lib.NewFastSerializer;
using sones.Lib.Settings;

#endregion

namespace sones.GraphDB.Settings
{
    public class SettingShowEDITION : ANonPersistentSetting, IDBShowSetting
    {
        public static readonly SettingUUID UUID = new SettingUUID(116);

        #region TypeCode
        public override UInt32 TypeCode { get { return 531; } }
        #endregion

        public SettingShowEDITION()
        {
            Name = "EDITION";
            Description = "Show the edition of an object.";
            Type = DBBoolean.UUID;
            Default = new DBBoolean(false);
            Value = new DBBoolean(false);
        }

        public SettingShowEDITION(Byte[] mySerializedData)
            : base(mySerializedData)
        { }

        public SettingShowEDITION(ANonPersistentSetting myCopy)
            : base(myCopy)
        { }

        public override ADBBaseObject Value
        {
            get
            {
                return this._Value;
            }

            set
            {
                _Value = value;
            }
        }

        private bool IsLittleEndian()
        {
            throw new NotImplementedException();
        }

        public override ISettings Clone()
        {
            return new SettingShowEDITION(this);
        }

        public override SettingUUID ID
        {
            get { return SettingShowEDITION.UUID; }
        }

        public Boolean IsShown()
        {
            if (Value != null)
                return (Boolean)Value.Value;
            else if (Default != null)
                return (Boolean)Default.Value;
            else return false;
        }

        #region IFastSerializationTypeSurrogate Members

        public new bool SupportsType(Type type)
        {
            return GetType() == type;
        }

        public new void Serialize(SerializationWriter writer, object value)
        {
            base.Serialize(writer, this);
        }

        public new object Deserialize(SerializationReader reader, Type type)
        {
            return base.Deserialize(reader, type);
        }

        #endregion
    }
}

