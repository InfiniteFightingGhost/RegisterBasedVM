namespace Raptor.Compiler
{
    public class Emitter
    {
        public class Environment
        {
            // The parent scope (null if this is the global scope)
            public Environment? Enclosing { get; }

            // The variables defined *only* in this specific scope
            private readonly Dictionary<string, int> _values = new();

            public IReadOnlyDictionary<string, int> Variables => _values;

            public Environment(Environment? enclosing)
            {
                Enclosing = enclosing;
            }

            public void Define(string name, int registerIndex)
            {
                _values[name] = registerIndex;
            }

            public bool TryGet(string name, out int registerIndex)
            {
                if (_values.TryGetValue(name, out registerIndex))
                {
                    return true;
                }

                if (Enclosing != null)
                {
                    return Enclosing.TryGet(name, out registerIndex);
                }

                registerIndex = -1;
                return false;
            }
        }

        private readonly ProgramNode _program;
        private readonly StringBuilder _sb = new();
        private readonly Environment _globalEnvironment;
        private Environment _environment;
        private readonly Dictionary<string, int> _propertyMappings = new();
        private int _regCounter = 1; // Start allocating registers from r1 (r0 acts as result accumulator)
        private int _labelCounter = 0;
        private readonly DiagnosticReporter _reporter;

        public Emitter(
            ProgramNode program,
            DiagnosticReporter reporter,
            Dictionary<string, int>? propertyMappings = null
        )
        {
            _program = program;
            _environment = new Environment(null);
            _globalEnvironment = _environment;
            if (propertyMappings != null)
            {
                _propertyMappings = propertyMappings;
                int maxPropertyReg = 0;
                if (_propertyMappings.Count > 0)
                {
                    maxPropertyReg = _propertyMappings.Values.Max();
                }
                _regCounter = maxPropertyReg + 1;
            }
            _reporter = reporter;
        }

        public IReadOnlyDictionary<string, int> Globals => _globalEnvironment.Variables;

        public string Emit()
        {
            _sb.AppendLine("; --------------------------------------------------------------");
            _sb.AppendLine(";  Generated Raptor Assembly (.rasm) from RaptorScript Source");
            _sb.AppendLine("; --------------------------------------------------------------");
            _sb.AppendLine();

            foreach (var statement in _program.Statements)
            {
                EmitNode(statement);
            }

            _sb.AppendLine("HALT");
            return _sb.ToString();
        }

        private void EmitNode(ASTNode node)
        {
            if (node.Line > 0)
            {
                _sb.AppendLine($"#LINE {node.Line}");
            }

            switch (node)
            {
                case VarDeclNode decl:
                    EmitVarDecl(decl);
                    break;
                case AssignmentNode assign:
                    EmitAssignment(assign);
                    break;
                case IfNode ifNode:
                    EmitIf(ifNode);
                    break;
                case WhileNode whileNode:
                    EmitWhile(whileNode);
                    break;
                case ForNode forNode:
                    EmitFor(forNode);
                    break;
                case IndexAssignmentNode indexAssignmentNode:
                    EmitIndexAssignment(indexAssignmentNode);
                    break;
                case CallNode call:
                    if (call.MethodName is "free" or "alloc" or "len")
                    {
                        EmitExpression(call);
                    }
                    else
                    {
                        EmitCall(call, 0); // Accumulate in r0 by default
                    }
                    break;
                default:
                    _reporter.Report(
                        new Diagnostic(
                            "E0023",
                            DiagnosticSeverity.Error,
                            $"Cannot emit node of type {node.GetType().Name} at root level.",
                            node.Line,
                            node.Column,
                            node.Length
                        )
                    );
                    throw new EmitException();
            }
        }

        private void EmitVarDecl(VarDeclNode decl)
        {
            if (_environment.Variables.ContainsKey(decl.Name))
            {
                _reporter.Report(
                    new Diagnostic(
                        "E0019",
                        DiagnosticSeverity.Error,
                        $"Variable '{decl.Name}' is already declared in this scope.",
                        decl.Line,
                        decl.Column,
                        decl.Length
                    )
                );
            }
            int regIndex = _regCounter++;
            _environment.Define(decl.Name, regIndex);

            int valueReg = EmitExpression(decl.Initializer);
            _sb.Append($"MOVE r{regIndex} r{valueReg} ");
            _sb.AppendLine($"; var {decl.Name}");
        }

        private void EmitAssignment(AssignmentNode assign)
        {
            int regIndex;
            if (_propertyMappings.TryGetValue(assign.TargetName, out int propReg))
            {
                regIndex = propReg;
            }
            else if (!_environment.TryGet(assign.TargetName, out int varReg))
            {
                _reporter.Report(
                    new Diagnostic(
                        "E0018",
                        DiagnosticSeverity.Error,
                        $"Variable '{assign.TargetName}' is not declared.",
                        assign.Line,
                        assign.Column,
                        assign.Length
                    )
                );
                regIndex = 0; // Fallback register index to continue emitting and gather other errors
            }
            else
            {
                regIndex = varReg;
            }

            _sb.AppendLine($"; {assign.TargetName} = <expr>");
            int valueReg = EmitExpression(assign.Value);
            _sb.AppendLine($"MOVE r{regIndex} r{valueReg}");
        }

        private void EmitIf(IfNode ifNode)
        {
            int labelId = _labelCounter++;
            string elseLabel = $"else_{labelId}";
            string endLabel = $"end_{labelId}";

            _sb.AppendLine("; if condition");
            EmitBranchCondition(ifNode.Condition, elseLabel);

            _sb.AppendLine("; then block");
            EmitBlock(ifNode.ThenBlock);
            _sb.AppendLine($"JUMP {endLabel}");

            _sb.AppendLine($"{elseLabel}:");
            if (ifNode.ElseBlock != null)
            {
                _sb.AppendLine("; else block");
                if (ifNode.ElseBlock != null)
                {
                    _sb.AppendLine("; else block");
                    EmitBlock(ifNode.ElseBlock);
                }
            }

            _sb.AppendLine($"{endLabel}:");
        }

        private void EmitWhile(WhileNode whileNode)
        {
            int labelId = _labelCounter++;
            string loopLabel = $"while_{labelId}";
            string endLabel = $"while_end_{labelId}";

            _sb.AppendLine($"{loopLabel}:");
            _sb.AppendLine("; while condition");
            EmitBranchCondition(whileNode.Condition, endLabel);

            _sb.AppendLine("; while body");
            EmitBlock(whileNode.Body);
            _sb.AppendLine($"JUMP {loopLabel}");
            _sb.AppendLine($"{endLabel}:");
        }

        private void EmitFor(ForNode forNode)
        {
            var current = _environment;
            _environment = new Environment(current);
            try
            {
                int indexReg;
                if (forNode.Initializer != null)
                {
                    if (forNode.Initializer is VarDeclNode varDeclNode)
                    {
                        EmitVarDecl(varDeclNode);
                        _environment.TryGet(varDeclNode.Name, out int value);
                        indexReg = value;
                    }
                    else if (forNode.Initializer is AssignmentNode assignmentNode)
                    {
                        EmitAssignment(assignmentNode);
                        _environment.TryGet(assignmentNode.TargetName, out int value);
                        indexReg = value;
                    }
                    else
                    {
                        _reporter.Report(
                            new Diagnostic(
                                "E0020",
                                DiagnosticSeverity.Error,
                                "For-loop initializer must be a variable declaration or assignment.",
                                forNode.Line,
                                forNode.Column,
                                forNode.Length
                            )
                        );
                        throw new EmitException();
                    }
                }
                else
                {
                    indexReg = _regCounter++;
                    _sb.AppendLine($"LOADC r{indexReg} 0.0 ; Dummy index for empty init");
                }

                string limitStr = "1.0";
                string compOp = "<";
                if (forNode.Condition != null)
                {
                    if (forNode.Condition is BinaryOpNode binOp && IsComparisonOp(binOp.Op))
                    {
                        compOp = binOp.Op;
                        limitStr = GetExpressionOperandString(binOp.Right);
                    }
                    else
                    {
                        _reporter.Report(
                            new Diagnostic(
                                "E0021",
                                DiagnosticSeverity.Error,
                                "For-loop condition must be a comparison (e.g., i < 10).",
                                forNode.Line,
                                forNode.Column,
                                forNode.Length
                            )
                        );
                        throw new EmitException();
                    }
                }
                else
                {
                    // Hack: No condition means infinite loop. We do 0 < 1
                    _sb.AppendLine($"LOADC r{indexReg} 0.0");
                    limitStr = "1.0";
                    compOp = "<";
                }

                string stepStr = "0.0";
                if (forNode.Increment != null)
                {
                    if (
                        forNode.Increment is AssignmentNode incAssign
                        && incAssign.Value is BinaryOpNode incMath
                    )
                    {
                        if (incMath.Op == "+")
                        {
                            stepStr = GetExpressionOperandString(incMath.Right);
                        }
                        else if (incMath.Op == "-")
                        {
                            // If the loop does i--, the step is negative.
                            if (incMath.Right is NumberNode numNode)
                            {
                                stepStr = (-numNode.Value).ToString("F1");
                            }
                            else
                            {
                                // If it's a register (e.g. i -= stepSize), we need to negate it.
                                stepStr = GetExpressionOperandString(incMath.Right);
                            }
                        }
                    }
                }
                int labelId = _labelCounter++;
                string loopLabel = $"for_{labelId}";
                string endLabel = $"for_end_{labelId}";
                string exitOp = GetInverseOperator(compOp);
                _sb.AppendLine($"{loopLabel}:");
                _sb.AppendLine($"FOR r{indexReg} {limitStr} {stepStr} {exitOp} {endLabel}");
                EmitBlock(forNode.Body);
                _sb.AppendLine($"JUMP {loopLabel}");
                _sb.AppendLine($"{endLabel}:");
            }
            finally
            {
                if (_environment.Enclosing == null)
                    throw new Exception("How is this possible?");
                _environment = _environment.Enclosing;
            }
        }

        private void EmitIndexAssignment(IndexAssignmentNode node)
        {
            int destArrayReg = EmitExpression(node.ArrayExpr);
            int destIndexReg = EmitExpression(node.IndexExpr);
            int assignValueReg = EmitExpression(node.Value);

            _sb.AppendLine($"SETARR r{destArrayReg} r{destIndexReg} r{assignValueReg}");
        }

        private void EmitBranchCondition(ASTNode cond, string jumpLabel)
        {
            if (cond is BinaryOpNode bin && IsComparisonOp(bin.Op))
            {
                int leftReg = EmitExpression(bin.Left);
                string rightStr = GetExpressionOperandString(bin.Right);

                switch (bin.Op)
                {
                    case "<":
                        _sb.AppendLine($"LT 1 r{leftReg} {rightStr}");
                        break;
                    case "<=":
                        _sb.AppendLine($"LE 1 r{leftReg} {rightStr}");
                        break;
                    case ">":
                        // a > b -> b < a
                        // Note: Left operand of LT must be a register, so evaluate Right if it's a constant
                        int rightReg = EmitExpression(bin.Right);
                        _sb.AppendLine($"LT 1 r{rightReg} r{leftReg}");
                        break;
                    case ">=":
                        // a >= b -> b <= a
                        int rightRegGe = EmitExpression(bin.Right);
                        _sb.AppendLine($"LE 1 r{rightRegGe} r{leftReg}");
                        break;
                    case "==":
                        _sb.AppendLine($"EQ 1 r{leftReg} {rightStr}");
                        break;
                    case "!=":
                        _sb.AppendLine($"EQ 0 r{leftReg} {rightStr}");
                        break;
                }
            }
            else
            {
                // Fallback: evaluate expression and jump if false (equal to 0.0)
                int condReg = EmitExpression(cond);
                _sb.AppendLine($"EQ 0 r{condReg} 0.0");
            }

            _sb.AppendLine($"JUMP {jumpLabel}");
        }

        private bool IsComparisonOp(string op)
        {
            return op is "<" or "<=" or ">" or ">=" or "==" or "!=";
        }

        private string GetInverseOperator(string op)
        {
            return op switch
            {
                "<" => ">=",
                "<=" => ">",
                ">" => "<=",
                ">=" => "<",
                "==" => "!=",
                "!=" => "==",
                _ => throw new EmitException($"Cannot invert unknown operator: {op}"),
            };
        }

        private string GetExpressionOperandString(ASTNode node)
        {
            if (node is NumberNode num)
                return num.Value.ToString("F1");
            if (node is IdentifierNode id)
            {
                if (_environment.TryGet(id.Name, out int reg))
                    return $"r{reg}";
                _reporter.Report(
                    new Diagnostic(
                        "E0018",
                        DiagnosticSeverity.Error,
                        $"Undefined identifier '{id.Name}'",
                        id.Line,
                        id.Column,
                        id.Length
                    )
                );
                return "r0";
            }

            int regIndex = EmitExpression(node);
            return $"r{regIndex}";
        }

        private int EmitExpression(ASTNode node)
        {
            if (node.Line > 0)
            {
                _sb.AppendLine($"#LINE {node.Line}");
            }

            switch (node)
            {
                case NumberNode num:
                    int numReg = _regCounter++;
                    _sb.AppendLine($"LOADC r{numReg} {num.Value.ToString("F1")}");
                    return numReg;

                case IdentifierNode id:
                    if (_propertyMappings.TryGetValue(id.Name, out int propReg))
                        return propReg;
                    if (!_environment.TryGet(id.Name, out int varReg))
                    {
                        _reporter.Report(
                            new Diagnostic(
                                "E0018",
                                DiagnosticSeverity.Error,
                                $"Undefined identifier '{id.Name}'",
                                id.Line,
                                id.Column,
                                id.Length
                            )
                        );
                        return 0;
                    }
                    return varReg;

                case BinaryOpNode binary:
                    return EmitBinaryOp(binary);

                case CallNode call:
                    if (call.MethodName == "alloc")
                    {
                        if (call.Arguments.Count != 1)
                        {
                            _reporter.Report(
                                new Diagnostic(
                                    "E0024",
                                    DiagnosticSeverity.Error,
                                    "alloc() expects exactly 1 argument (the array size).",
                                    call.Line,
                                    call.Column,
                                    call.Length
                                )
                            );
                            throw new EmitException();
                        }
                        int sizeReg = EmitExpression(call.Arguments[0]);
                        int destReg = _regCounter++;
                        _sb.AppendLine($"NEWARR r{destReg} r{sizeReg}");
                        return destReg;
                    }
                    if (call.MethodName == "free")
                    {
                        if (call.Arguments.Count != 1)
                        {
                            _reporter.Report(
                                new Diagnostic(
                                    "E0025",
                                    DiagnosticSeverity.Error,
                                    "free() expects exactly 1 argument (the array to free).",
                                    call.Line,
                                    call.Column,
                                    call.Length
                                )
                            );
                            throw new EmitException();
                        }

                        int freeArrReg = EmitExpression(call.Arguments[0]);

                        _sb.AppendLine($"FREEARR r{freeArrReg}");

                        return 0;
                    }
                    if (call.MethodName == "len")
                    {
                        if (call.Arguments.Count != 1)
                        {
                            _reporter.Report(
                                new Diagnostic(
                                    "E0026",
                                    DiagnosticSeverity.Error,
                                    "len() expects exactly 1 argument (the array to check).",
                                    call.Line,
                                    call.Column,
                                    call.Length
                                )
                            );
                            throw new EmitException();
                        }
                        int lenArrReg = EmitExpression(call.Arguments[0]);
                        int destReg = _regCounter++;
                        _sb.AppendLine($"LENARR r{destReg} r{lenArrReg}");
                        return destReg;
                    }
                    int returnReg = _regCounter++;
                    EmitCall(call, returnReg);
                    return returnReg;
                case ArrayLiteralNode arrLiteral:
                    int arrReg = _regCounter++;
                    _sb.AppendLine($"NEWARR r{arrReg} {arrLiteral.Elements.Count}");
                    for (int i = 0; i < arrLiteral.Elements.Count; i++)
                    {
                        int elementReg = EmitExpression(arrLiteral.Elements[i]);
                        _sb.AppendLine($"SETARR r{arrReg} {i} r{elementReg}");
                    }
                    return arrReg;
                case IndexAccessNode indexAccess:
                    int targetArrayReg = EmitExpression(indexAccess.ArrayExpr);

                    int accessIndexReg = EmitExpression(indexAccess.IndexExpr);

                    int resultReg = _regCounter++;

                    _sb.AppendLine($"GETARR r{resultReg} r{targetArrayReg} r{accessIndexReg}");
                    return resultReg;
                case LogicalOpNode logicalNode:
                    int logicalResultReg = EmitExpression(logicalNode.Left);
                    string endLabel = $"logic_end{_labelCounter++}";
                    int zeroRegLogical = _regCounter++;
                    _sb.AppendLine($"LOADC r{zeroRegLogical} 0.0");
                    if (logicalNode.Op == "&&")
                    {
                        // Jump to endLabel if Left is falsey (r{logicalResultReg} == 0.0)
                        _sb.AppendLine($"EQ 0 r{logicalResultReg} r{zeroRegLogical}");
                        _sb.AppendLine($"JUMP {endLabel}");
                    }
                    else if (logicalNode.Op == "||")
                    {
                        // Jump to endLabel if Left is truthy (r{logicalResultReg} != 0.0)
                        _sb.AppendLine($"EQ 1 r{logicalResultReg} r{zeroRegLogical}");
                        _sb.AppendLine($"JUMP {endLabel}");
                    }
                    int rightSide = EmitExpression(logicalNode.Right);
                    _sb.AppendLine($"MOVE r{logicalResultReg} r{rightSide}");
                    _sb.AppendLine($"{endLabel}:");
                    return logicalResultReg;
            }

            throw new EmitException();
        }

        private int EmitBinaryOp(BinaryOpNode binary)
        {
            int leftReg = EmitExpression(binary.Left);
            int rightReg = EmitExpression(binary.Right);
            int resReg = _regCounter++;

            switch (binary.Op)
            {
                case "+":
                    _sb.AppendLine($"ADD r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "-":
                    _sb.AppendLine($"SUB r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "*":
                    _sb.AppendLine($"MUL r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "/":
                    _sb.AppendLine($"DIV r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "%":
                    _sb.AppendLine($"MOD r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "|":
                    _sb.AppendLine($"BINOR r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "&":
                    _sb.AppendLine($"BINAND r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "^":
                    _sb.AppendLine($"BINXOR r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "<<":
                    _sb.AppendLine($"BINLSH r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case ">>":
                    _sb.AppendLine($"BINRSH r{resReg} r{leftReg} r{rightReg}");
                    return resReg;
                case "<":
                {
                    string skipLabel = $"cmp_skip{_labelCounter++}";
                    _sb.AppendLine($"LOADC r{resReg} 1.0");
                    _sb.AppendLine($"LT 1 r{leftReg} r{rightReg}");
                    _sb.AppendLine($"JUMP {skipLabel}");
                    _sb.AppendLine($"LOADC r{resReg} 0.0");
                    _sb.AppendLine($"{skipLabel}:");
                    return resReg;
                }
                case "<=":
                {
                    string skipLabel = $"cmp_skip{_labelCounter++}";
                    _sb.AppendLine($"LOADC r{resReg} 1.0");
                    _sb.AppendLine($"LE 1 r{leftReg} r{rightReg}");
                    _sb.AppendLine($"JUMP {skipLabel}");
                    _sb.AppendLine($"LOADC r{resReg} 0.0");
                    _sb.AppendLine($"{skipLabel}:");
                    return resReg;
                }
                case ">":
                {
                    // a > b -> b < a
                    string skipLabel = $"cmp_skip{_labelCounter++}";
                    _sb.AppendLine($"LOADC r{resReg} 1.0");
                    _sb.AppendLine($"LT 1 r{rightReg} r{leftReg}");
                    _sb.AppendLine($"JUMP {skipLabel}");
                    _sb.AppendLine($"LOADC r{resReg} 0.0");
                    _sb.AppendLine($"{skipLabel}:");
                    return resReg;
                }
                case ">=":
                {
                    // a >= b -> b <= a
                    string skipLabel = $"cmp_skip{_labelCounter++}";
                    _sb.AppendLine($"LOADC r{resReg} 1.0");
                    _sb.AppendLine($"LE 1 r{rightReg} r{leftReg}");
                    _sb.AppendLine($"JUMP {skipLabel}");
                    _sb.AppendLine($"LOADC r{resReg} 0.0");
                    _sb.AppendLine($"{skipLabel}:");
                    return resReg;
                }
                case "==":
                {
                    string skipLabel = $"cmp_skip{_labelCounter++}";
                    _sb.AppendLine($"LOADC r{resReg} 1.0");
                    _sb.AppendLine($"EQ 1 r{leftReg} r{rightReg}");
                    _sb.AppendLine($"JUMP {skipLabel}");
                    _sb.AppendLine($"LOADC r{resReg} 0.0");
                    _sb.AppendLine($"{skipLabel}:");
                    return resReg;
                }
                case "!=":
                {
                    string skipLabel = $"cmp_skip{_labelCounter++}";
                    _sb.AppendLine($"LOADC r{resReg} 1.0");
                    _sb.AppendLine($"EQ 0 r{leftReg} r{rightReg}");
                    _sb.AppendLine($"JUMP {skipLabel}");
                    _sb.AppendLine($"LOADC r{resReg} 0.0");
                    _sb.AppendLine($"{skipLabel}:");
                    return resReg;
                }
                default:
                    _reporter.Report(
                        new Diagnostic(
                            "E0028",
                            DiagnosticSeverity.Error,
                            $"Unsupported binary operator: {binary.Op}",
                            binary.Line,
                            binary.Column,
                            binary.Length
                        )
                    );
                    throw new EmitException();
            }
        }

        private void EmitCall(CallNode call, int returnReg)
        {
            int callBase = _regCounter;

            int[] argRegs = new int[call.Arguments.Count];
            for (int i = 0; i < call.Arguments.Count; i++)
            {
                argRegs[i] = EmitExpression(call.Arguments[i]);
            }

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                _sb.AppendLine($"MOVE r{callBase + i} r{argRegs[i]}");
            }

            _sb.AppendLine($"CALL {call.MethodName}() r{callBase}");

            if (returnReg != 0 && returnReg != callBase)
            {
                _sb.AppendLine($"MOVE r{returnReg} r{callBase}");
            }
        }

        private void EmitBlock(List<ASTNode> statements)
        {
            Environment previous = _environment;

            _environment = new Environment(previous);

            try
            {
                foreach (var stmt in statements)
                {
                    EmitNode(stmt);
                }
            }
            finally
            {
                _environment = previous;
            }
        }

        [Serializable]
        internal class EmitException : Exception
        {
            public EmitException() { }

            public EmitException(string? message)
                : base(message) { }

            public EmitException(string? message, Exception? innerException)
                : base(message, innerException) { }
        }
    }
}
