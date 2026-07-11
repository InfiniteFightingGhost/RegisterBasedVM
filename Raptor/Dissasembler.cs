namespace Raptor;

public static class Dissasembler
{
    ///<summary>
    ///Turn a <see cref="VMChunk"/> into a human-readable assembly
    ///</summary>
    public static string Disassemble(VMChunk chunk)
    {
        if (chunk == null || chunk.Instructions == null)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        int pc = 0;
        uint[] instructions = chunk.Instructions;
        double[] constants = chunk.Constants;

        string GetValString(ushort val)
        {
            if (val < 256)
                return $"r{val}";
            int constIndex = val - 256;
            if (constIndex < constants.Length)
                return constants[constIndex]
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
            return $"c[{constIndex}]";
        }

        while (pc < instructions.Length)
        {
            Instruction inst = new Instruction(instructions[pc]);
            sb.Append($"{pc:D4}: ");

            switch (inst.Op)
            {
                case OpCode.LOADC:
                    {
                        uint bx = inst.Bx;
                        string val =
                            bx < constants.Length
                                ? constants[bx]
                                    .ToString(System.Globalization.CultureInfo.InvariantCulture)
                                : $"c[{bx}]";
                        sb.AppendLine($"LOADC r{inst.A} {val}");
                    }
                    break;
                case OpCode.MOVE:
                    sb.AppendLine($"MOVE r{inst.A} r{inst.Bx}");
                    break;
                case OpCode.SWAP:
                    sb.AppendLine($"SWP r{inst.A} r{inst.Bx}");
                    break;
                case OpCode.ADD:
                case OpCode.SUB:
                case OpCode.MUL:
                case OpCode.DIV:
                case OpCode.POW:
                case OpCode.MOD:
                case OpCode.SETARR:
                case OpCode.SETARRA:
                case OpCode.GETARR:
                case OpCode.GETARRA:
                case OpCode.BINAND:
                case OpCode.BINOR:
                case OpCode.BINXOR:
                case OpCode.BINLSH:
                case OpCode.BINRSH:
                    {
                        string opName = inst.Op.ToString();
                        if (opName == "SWAP")
                            opName = "SWP";
                        sb.AppendLine(
                            $"{opName} r{inst.A} {GetValString(inst.B)} {GetValString(inst.C)}"
                        );
                    }
                    break;
                case OpCode.UNM:
                    sb.AppendLine($"UNM r{inst.A} {GetValString(inst.B)}");
                    break;
                case OpCode.JUMP:
                    {
                        int target = pc + 1 + inst.sBx26;
                        sb.AppendLine($"JUMP {target:D4}");
                    }
                    break;
                case OpCode.EQ:
                case OpCode.LT:
                case OpCode.LE:
                    sb.AppendLine(
                        $"{inst.Op} {inst.A} {GetValString(inst.B)} {GetValString(inst.C)}"
                    );
                    break;
                case OpCode.HALT:
                    sb.AppendLine("HALT");
                    break;
                case OpCode.PRINT:
                    sb.AppendLine($"PRINT {GetValString(inst.B)}");
                    break;
                case OpCode.PRINTA:
                    sb.AppendLine($"PRINTA {GetValString(inst.B)}");
                    break;
                case OpCode.RAND:
                    sb.AppendLine($"RAND r{inst.A}");
                    break;
                case OpCode.SQRT:
                    sb.AppendLine($"SQRT r{inst.A} {GetValString(inst.B)}");
                    break;
                case OpCode.FISR:
                    sb.AppendLine($"FISR r{inst.A} {GetValString(inst.B)}");
                    break;
                case OpCode.CALL:
                    {
                        uint methodIndex = inst.Bx;
                        sb.AppendLine($"CALL {methodIndex} r{inst.A}");
                    }
                    break;
                case OpCode.RETURN:
                    sb.AppendLine($"RETURN r{inst.A} r{inst.Bx}");
                    break;
                case OpCode.FOR:
                    {
                        if (pc + 1 < instructions.Length)
                        {
                            Instruction nextInst = new Instruction(instructions[pc + 1]);
                            if (nextInst.Op == OpCode.FOR)
                            {
                                byte rIndex = inst.A;
                                string rMax = GetValString(inst.B);
                                string rStep = GetValString(inst.C);
                                byte comp = nextInst.A;
                                string compStr = comp switch
                                {
                                    0 => "<",
                                    1 => ">",
                                    2 => "<=",
                                    3 => ">=",
                                    _ => "?",
                                };
                                int target = (pc + 1) + nextInst.sBx16;
                                sb.AppendLine(
                                    $"FOR r{rIndex} {rMax} {rStep} {compStr} {target:D4}"
                                );
                                pc++;
                                break;
                            }
                        }
                        sb.AppendLine(
                            $"FOR (incomplete) r{inst.A} {GetValString(inst.B)} {GetValString(inst.C)}"
                        );
                    }
                    break;
                case OpCode.FREEARR:
                    sb.AppendLine($"FREEARR r{inst.A}");
                    break;
                default:
                    sb.AppendLine($"UNKNOWN opcode {inst.Op} (value: {inst.Value})");
                    break;
            }
            pc++;
        }

        return sb.ToString();
    }

    ///<summary>
    ///Turn raw bytecode into human-readable assembly
    ///</summary>
    public static string Disassemble(byte[] bytecode)
    {
        if (bytecode == null || bytecode.Length < 4)
            return string.Empty;

        int instructionCount = bytecode.Length / 4;
        uint[] insts = new uint[instructionCount];
        for (int i = 0; i < instructionCount; i++)
        {
            insts[i] = BitConverter.ToUInt32(bytecode, i * 4);
        }

        VMChunk chunk = new VMChunk();
        chunk.Instructions = insts;
        return Disassemble(chunk);
    }
}
