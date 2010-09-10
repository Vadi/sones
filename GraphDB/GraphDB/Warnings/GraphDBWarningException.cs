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

/* <id name="PandoraDB � GraphDBWarningException" />
 * <copyright file="GraphDBWarningException.cs"
 *            company="sones GmbH">
 * Copyright (c) sones GmbH. All rights reserved.
 * </copyright>
 * <developer>Stefan Licht</developer>
 * <summary>This class will "transport" an warning (with the help of an exception) from Irony to the DB.</summary>
 */

using System;
using sones.Lib.ErrorHandling;

namespace sones.GraphDB.Warnings
{
    /// <summary>
    /// This class will "transport" an warning (with the help of an exception) from Irony to the DB
    /// </summary>
    class GraphDBWarningException : ApplicationException
    {
        public IWarning GraphDBWarning { get; set; }

        public GraphDBWarningException(IWarning graphDBWarning)
            : base(graphDBWarning.ToString())
        {
            GraphDBWarning = graphDBWarning;
        }
        public GraphDBWarningException(IWarning graphDBWarning, Exception innerException)
            : base(graphDBWarning.ToString(), innerException)
        {
            GraphDBWarning = graphDBWarning;
        }
    }

}
