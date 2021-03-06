﻿using System;

using sones.Lib;
using sones.GraphDB.TypeManagement.BasicTypes;

namespace sones.GraphDB.Errors
{
    public class Error_EdgeParameterTypeMismatch : GraphDBEdgeError
    {
        public ADBBaseObject CurrentType { get; private set; }
        public ADBBaseObject[] ExpectedTypes { get; private set; }

        public Error_EdgeParameterTypeMismatch(ADBBaseObject currentType, params ADBBaseObject[] expectedTypes)
        {
            CurrentType = currentType;
            ExpectedTypes = expectedTypes;
        }

        public override string ToString()
        {
            return String.Format("The type [{0}] is not valid. Please use one of [{1}].", CurrentType.ObjectName, ExpectedTypes.ToAggregatedString(i => i.ObjectName));
        }
    }
}
