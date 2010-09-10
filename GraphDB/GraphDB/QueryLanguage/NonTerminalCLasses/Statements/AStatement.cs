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

/* <id name=�PandoraLib � abstract statement class� />
 * <copyright file=�AStatement.cs�
 *            company=�sones GmbH�>
 * Copyright (c) sones GmbH. All rights reserved.
 * </copyright>
 * <developer>Henning Rauch</developer>
 * <summary>The abstract class for all statements in PandoraDB.</summary>
 */

#region usings

using System;
using System.Collections.Generic;
using sones.GraphDB.ObjectManagement;
using sones.GraphDB.QueryLanguage.Enums;
using sones.GraphDB.QueryLanguage.NonTerminalClasses.Structure;
using sones.GraphDB.QueryLanguage.Result;
using sones.GraphDB.TypeManagement;
using sones.GraphFS.DataStructures;
using sones.Lib.Session;
using sones.Lib.Frameworks.Irony.Parsing;

#endregion

namespace sones.GraphDB.QueryLanguage.NonTerminalClasses.Statements
{
    /// <summary>
    /// The abstract class for all statements in PandoraDB.
    /// </summary>
    public abstract class AStatement : AStructureNode
    {
        #region Data


        //NLOG: temporarily commented
        //protected static Logger Logger = LogManager.GetCurrentClassLogger();

        protected HashSet<ObjectUUID> _ListOfAffectedDBObjectUUIDs = null;

        #endregion

        #region General Command Infos

        public abstract String StatementName { get; }
        public abstract TypesOfStatements TypeOfStatement { get; }

        #endregion

        #region (public) Methods

        #region GetContent Method

        public abstract void GetContent(CompilerContext context, ParseTreeNode parseNode);

        #endregion

        #region Evaluate Method

        /// <summary>
        /// Executes the statement
        /// </summary>
        /// <param name="graphDBSession">The DBSession to start new transactions</param>
        /// <param name="dbContext">The current dbContext inside an readonly transaction. For any changes, you need to start a new transaction using <paramref name="graphDBSession"/></param>
        /// <returns>The result of the query</returns>
        public abstract QueryResult Execute(IGraphDBSession graphDBSession, DBContext dbContext);

        #endregion
        
        #region private helper methods

        
        #endregion

        #endregion

        #region protected methods


        #endregion
    }
}
