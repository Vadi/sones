﻿#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for CLIrony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sones.Lib.Frameworks.CLIrony.Runtime;

namespace sones.Lib.Frameworks.CLIrony.Compiler.AST {
  public class UnExprNode : AstNode {
    public AstNode Arg;
    public string Op;
    CallDispatcher _dispatcher;


    public UnExprNode(NodeArgs args, string op, AstNode arg) : base(args) {
      ChildNodes.Clear();
      Op = op;
      if (!Op.EndsWith("U"))
        Op += "U"; //Unary operations are marked as "+U", "-U", "!U"
      Arg = arg;
      ChildNodes.Add(arg);
      //Flags |= AstNodeFlags.TypeBasedDispatch;
    }
    public UnExprNode(NodeArgs args) : this(args, args.ChildNodes[0].GetContent(), args.ChildNodes[1]) {  }

    public override void OnCodeAnalysis(CodeAnalysisArgs args) {
      switch (args.Phase) {
        case CodeAnalysisPhase.Binding:
          if (Op == "+U") 
            Evaluate = EvaluatePlus;
          else {
            _dispatcher = args.Context.Runtime.GetDispatcher(Op);
            Evaluate = EvaluateOther;
          }
          break;
      }//switch
      base.OnCodeAnalysis(args);
    }

    #region Evaluation methods
    private void EvaluatePlus(EvaluationContext context) {
      Arg.Evaluate(context);
    }
    private void EvaluateOther(EvaluationContext context) {
      Arg.Evaluate(context);
      context.Arg1 = context.CurrentResult;
      _dispatcher.Evaluate(context);
    }
    #endregion

    public override string ToString() {
      return Op + " (unary)";
    }
    protected override void XmlSetAttributes(System.Xml.XmlElement element) {
      base.XmlSetAttributes(element);
      element.SetAttribute("Operation", Op); 
    }
  }//class
}//namespace
