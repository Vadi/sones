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

/* <id name="GraphDB – readonly setting" />
 * <copyright file="SettingReadonly.cs"
 *            company="sones GmbH">
 * Copyright (c) sones GmbH 2007-2010
 * </copyright>
 * <developer>Henning Rauch</developer>
 * <summary></summary>
 */

#region Usings
using System;
using sones.GraphDB.TypeManagement.BasicTypes;
using sones.Lib.NewFastSerializer;
using sones.Lib.Settings;

#endregion

namespace sones.GraphDB.Settings
{
    public class SettingReadonly : APersistentSetting, IDBDatabaseOnlySetting
    {
        public static readonly SettingUUID UUID = new SettingUUID(115);

        #region TypeCode
        public override UInt32 TypeCode { get { return 530; } }
        #endregion

        public SettingReadonly()
        {
            Name            = "SETREADONLY";
            Description     = "Decides whether the database is readonly or writeable.";
            Type            = DBBoolean.UUID;
            Default         = new DBBoolean(false);
            Value           = new DBBoolean(false);
        }

        public SettingReadonly(Byte[] mySerializedData)
            : base(mySerializedData)
        { }

        public SettingReadonly(APersistentSetting myCopy)
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
            return new SettingReadonly(this);
        }

        public override SettingUUID ID
        {
            get { return SettingReadonly.UUID; }
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
