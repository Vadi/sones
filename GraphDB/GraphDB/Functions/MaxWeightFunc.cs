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


/* <id Name="sones GraphDB – MaxWeightFunc" />
 * <copyright file="MaxWeightFunc.cs"
 *            company="sones GmbH">
 * Copyright (c) sones GmbH 2007-2010
 * </copyright>
 * <developer>Stefan Licht</developer>
 * <summary>This function will calculate the max (heighest) weight of a WeightedList attribute<summary>
 */

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;


using sones.Lib.ErrorHandling;
using sones.GraphDB.Errors;
using sones.GraphDB.Structures;
using sones.GraphDB.Structures.EdgeTypes;
using sones.GraphDB.TypeManagement;
using sones.GraphDB.Structures.Enums;

using sones.GraphDB.TypeManagement.BasicTypes;
using sones.GraphDB.ObjectManagement;
using sones.GraphFS.Session;
using sones.GraphDB.Structures.Result;
using sones.Lib.Session;
using sones.GraphDB.Managers.Structures;

namespace sones.GraphDB.Functions
{
    public class MaxWeightFunc : ABaseFunction
    {

        public MaxWeightFunc()
        {
        }

        #region GetDescribeOutput()

        public override String GetDescribeOutput()
        {
            return "This function is valid for weighted edges and will return the maximum weight.";
        }

        #endregion

        public override string FunctionName
        {
            get { return "MAXWEIGHT"; }
        }

        public override bool ValidateWorkingBase(TypeAttribute workingBase, DBTypeManager typeManager)
        {
            if (workingBase != null && workingBase.EdgeType is EdgeTypeWeightedList)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Exceptional<FuncParameter> ExecFunc(DBContext dbContext, params FuncParameter[] myParams)
        {
            var result = new Exceptional<FuncParameter>();

            if (!(CallingObject is EdgeTypeWeightedList))
            {
                return result.PushT(new Error_FunctionParameterTypeMismatch(typeof(EdgeTypeWeightedList), CallingObject.GetType()));
            }

            result.Value = new FuncParameter(((EdgeTypeWeightedList)CallingObject).GetMaxWeight());

            return result;
        }

    }
}