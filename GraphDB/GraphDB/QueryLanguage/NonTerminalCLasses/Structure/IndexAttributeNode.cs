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

/*
 * CreateIndexAttributeNode
 * (c) Achim Friedland, 2009 - 2010
 */

#region usings

using System;

using sones.GraphDB.Exceptions;
using sones.GraphDB.Errors;

using sones.Lib.Frameworks.Irony.Parsing;

#endregion

namespace sones.GraphDB.QueryLanguage.NonTerminalClasses.Structure
{

    /// <summary>
    /// This node is requested in case of an CreateIndexAttributeNode node.
    /// </summary>

    public class IndexAttributeNode : AStructureNode, IAstNodeInit
    {

        #region Properties

        private String _IndexAttribute  = null;
        private String _OrderDirection  = null;
        private String _IndexType       = null;

        #endregion

        #region Constructor

        public IndexAttributeNode()
        { }

        #endregion

        #region GetContent(myCompilerContext, myParseTreeNode)

        public void GetContent(CompilerContext myCompilerContext, ParseTreeNode myParseTreeNode)
        {

            if (myParseTreeNode.ChildNodes[0].HasChildNodes())
            {

                if (myParseTreeNode.ChildNodes[0].ChildNodes[0].ChildNodes.Count > 1)
                {

                    _IndexType = ((ATypeNode) myParseTreeNode.ChildNodes[0].ChildNodes[0].ChildNodes[0].AstNode).DBTypeStream.Name;

                    if (((IDNode) myParseTreeNode.ChildNodes[0].ChildNodes[0].ChildNodes[2].AstNode).IsValidated == false)
                        throw new GraphDBException(new Error_IndexTypesOverlap());
                    else
                        _IndexAttribute = ((IDNode) myParseTreeNode.ChildNodes[0].ChildNodes[0].ChildNodes[2].AstNode).LastAttribute.Name;                        

                }

                else
                {
                    _IndexAttribute = myParseTreeNode.ChildNodes[0].ChildNodes[0].Token.ValueString;
                }

            }

            if(myParseTreeNode.ChildNodes.Count > 1 && myParseTreeNode.ChildNodes[1].HasChildNodes())
                _OrderDirection = myParseTreeNode.ChildNodes[1].FirstChild.Token.ValueString;

            else
                _OrderDirection = String.Empty;

        }

        #endregion

        #region Accessessors

        public String IndexAttribute { get { return _IndexAttribute; } }
        public String OrderDirection { get { return _OrderDirection; } }
        public String IndexTypes     { get { return _IndexType; } }

        #endregion

        #region IAstNodeInit Members

        public void Init(CompilerContext context, ParseTreeNode parseNode)
        {
            GetContent(context, parseNode);
        }

        #endregion

        #region ToString()

        public override String ToString()
        {

            if (_OrderDirection.Equals(String.Empty))
                return String.Concat(_IndexAttribute);

            else
                return String.Concat(_IndexAttribute, " ", _OrderDirection);

        }

        #endregion

    }

}
