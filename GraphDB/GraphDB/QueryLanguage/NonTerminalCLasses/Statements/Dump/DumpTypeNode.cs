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

using sones.GraphDB.Exceptions;
using sones.GraphDB.ImportExport;
using sones.GraphDB.QueryLanguage.NonTerminalClasses.Structure;
using sones.Lib.Frameworks.Irony.Parsing;

namespace sones.GraphDB.QueryLanguage.NonTerminalCLasses.Statements.Dump
{

    public class DumpTypeNode : AStructureNode
    {

        public DumpTypes DumpType { get; set; }

        public void GetContent(CompilerContext context, ParseTreeNode parseNode)
        {
            
            var _GraphQL = GetGraphQLGrammar(context);

            if (parseNode.HasChildNodes())
            {

                var _Terminal = parseNode.ChildNodes[0].Token.Terminal;

                if (_Terminal      == _GraphQL.S_ALL)
                {
                    DumpType = DumpTypes.GDDL | DumpTypes.GDML;
                }
                else if (_Terminal == _GraphQL.S_GDDL)
                {
                    DumpType = DumpTypes.GDDL;
                }
                else if (_Terminal == _GraphQL.S_GDML)
                {
                    DumpType = DumpTypes.GDML;
                }
                else
                {
                    throw new GraphDBException(new Errors.Error_InvalidDumpType(_Terminal.DisplayName));
                }

            }
            else
            {
                DumpType = DumpTypes.GDDL | DumpTypes.GDML;
            }

        }

    }

}
