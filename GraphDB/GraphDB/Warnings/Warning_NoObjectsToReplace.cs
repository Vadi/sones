﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sones.GraphDB.Errors;

namespace sones.GraphDB.Warnings
{
    public class Warning_NoObjectsToDelete : GraphDBWarning
    {
        public override string ToString()
        {
            return "No objects were found to delete.";
        }
    }
}
