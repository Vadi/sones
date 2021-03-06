﻿/* <id name="DescrTypesNode" />
 * <copyright file="DescrTypesNode.cs"
 *            company="sones GmbH">
 * Copyright (c) sones GmbH. All rights reserved.
 * </copyright>
 * <developer>Stefan Licht</developer>
 * <developer>Dirk Bludau</developer>
 * <summary></summary>
 */

#region Usings

using sones.GraphDB.Managers.Structures.Describe;
using sones.Lib.Frameworks.Irony.Parsing;

#endregion

namespace sones.GraphDB.GraphQL.StructureNodes
{

    public class DescribeTypesNode : ADescrNode
    {

        #region ADescrNode

        public override ADescribeDefinition DescribeDefinition
        {
            get { return _DescribeTypeDefinition; }
        }
        private DescribeTypeDefinition _DescribeTypeDefinition;

        #endregion

        #region AStructureNode

        public void GetContent(CompilerContext myCompilerContext, ParseTreeNode myParseTreeNode)
        {

            _DescribeTypeDefinition = new DescribeTypeDefinition();
        }       

        #endregion

    }

}
