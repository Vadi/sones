﻿/* GraphLib - NIntegrityCheckFailed
 * (c) Stefan Licht, 2009
 * 
 * Notifies about an GraphFSException_IntegrityCheckFailed exception throwed by InformationHeader.VerifyAndDecrypt(...)
 * 
 * Lead programmer:
 *      Stefan Licht
 * 
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sones.Notifications.NotificationTypes;
using sones.Notifications;
using sones.Lib.Serializer;
using sones.Lib.NewFastSerializer;

namespace sones.GraphFS.Notification
{
    /// <summary>
    /// Notifies about an GraphFSException_IntegrityCheckFailed exception throwed by InformationHeader.VerifyAndDecrypt(...)
    /// </summary>
    public class NFileSystem_IntegrityCheckFailed : NFileSystem
    {

        public new class Arguments : INotificationArguments
        {

            public Int32 FailedCopy;
            public Int32 MaxNumberOfCopies;
            public Byte[] SerializedObjectStream;

            #region Constructors

            public Arguments() { }

            public Arguments(Int32 myFailedCopy, Int32 myMaxNumberOfCopies, Byte[] mySerializedObjectStream)
            {
                FailedCopy              = myFailedCopy;
                MaxNumberOfCopies       = myMaxNumberOfCopies;
                SerializedObjectStream  = mySerializedObjectStream;
            }

            #endregion

            #region INotificationArguments Members

            public byte[] Serialize()
            {
                var _SerializationWriter = new SerializationWriter();
                _SerializationWriter.WriteInt32(FailedCopy);
                _SerializationWriter.WriteInt32(MaxNumberOfCopies);
                _SerializationWriter.Write(SerializedObjectStream);

                return _SerializationWriter.ToArray();
            }

            public void Deserialize(byte[] mySerializedBytes)
            {
                var _SerializationReader    = new SerializationReader(mySerializedBytes);
                FailedCopy                  = _SerializationReader.ReadInt32();
                MaxNumberOfCopies           = _SerializationReader.ReadInt32();
                SerializedObjectStream      = _SerializationReader.ReadByteArray();
            }

            #endregion
        }

        #region ANotificationType

        public override string Description
        {
            get { return "Notifies about an GraphFSException_IntegrityCheckFailed exception"; }
        }

        public override INotificationArguments GetEmptyArgumentInstance()
        {
            return new Arguments();
        }

        #endregion

    }
}
