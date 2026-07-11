namespace Raptor;

public class Assembler
{
    private readonly VMChunk _chunk;
    private readonly Dictionary<string, uint> _hostMethods = new();

    public Assembler(VMChunk chunk)
    {
        _chunk = chunk;
    }

    public void RegisterHostMethod(string name, uint index)
    {
        string key = name.EndsWith("()") ? name : name + "()";
        _hostMethods[key] = index;
        _chunk.MethodTable[index] = index | 0x80000000;
    }

    public void Parse(List<string> lines)
    {
        Dictionary<string, string> definitions = new();

        //Having a 3 pass system will make it easier for future me to implement multi line definitions if I want to
        for (int i = 0; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                lines.RemoveAt(i--);
                continue;
            }
            lines[i] = lines[i].TrimStart();
            string[] words = lines[i].Split();
            if (lines[i].StartsWith("DEFINE "))
            {
                if (!definitions.TryAdd(words[1], words[2]))
                    throw new Exception(
                        $"Definition for {words[1]} already exists on line {definitions[words[1]]}"
                    );
                else
                {
                    lines.RemoveAt(i--);
                    continue;
                }
            }
            else
            {
                if (lines[i].Contains(";"))
                {
                    if (lines[i].StartsWith(';'))
                    {
                        lines.RemoveAt(i--);
                        continue;
                    }
                    else
                        lines[i] = lines[i].Split(";")[0].Trim();
                }

                for (int j = 0; j < words.Length; j++)
                {
                    foreach (var pair in definitions)
                    {
                        if (words[j] == pair.Key)
                            words[j] = pair.Value;
                    }
                }
                lines[i] = string.Join(" ", words);
            }
        }
        Dictionary<string, uint> labels = new();
        Dictionary<string, uint> methods = new();
        uint memoryAddress = 0;
        uint methodAddress = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].EndsWith(":"))
            {
                if (!lines[i].StartsWith("JUMP "))
                {
                    if (!labels.TryAdd(lines[i].TrimEnd(':'), memoryAddress))
                        throw new Exception(
                            "Label name already exists on line " + labels[lines[i]]
                        );
                    else
                    {
                        lines.RemoveAt(i--);
                        continue;
                    }
                }
            }
            else if (lines[i].EndsWith("()"))
            {
                if (!lines[i].StartsWith("CALL"))
                {
                    if (!methods.TryAdd(lines[i], methodAddress))
                        throw new Exception(
                            "Method name already exists on line "
                                + _chunk.MethodTable[methods[lines[i]]]
                        );
                    else
                    {
                        _chunk.MethodTable[methodAddress++] = memoryAddress;
                        lines.RemoveAt(i--);
                        continue;
                    }
                }
            }
            else if (lines[i].StartsWith("FOR "))
                memoryAddress += 2;
            else
                memoryAddress++;
        }
        List<UInt32> instructions = new List<UInt32>();
        int pc = 0;
        foreach (var item in lines)
        {
            Console.Error.WriteLine($"ASM Inst {pc}: {item}");
            var words = item.Split();
            uint instruction = 0;
            try
            {
                switch (words[0])
                {
                    case "LOADC":
                        instruction = ExecuteLoadC(words);
                        break;
                    case "MOVE":
                        instruction = ExecuteMove(words);
                        break;
                    case "SWP":
                        byte destA5 = byte.Parse(words[1].TrimStart("r"));

                        byte destB5 = byte.Parse(words[2].TrimStart("r"));
                        instruction = Instruction.CreateABx(OpCode.SWAP, destA5, destB5);
                        break;

                    case "ADD":
                    case "SUB":
                    case "MUL":
                    case "DIV":
                    case "POW":
                    case "MOD":
                    case "SETARR":
                    case "SETARRA":
                    case "BINAND":
                    case "BINOR":
                    case "BINXOR":
                    case "BINLSH":
                    case "BINRSH":
                        byte destA3 = byte.Parse(words[1].TrimStart('r'));

                        ushort destB3;
                        if (words[2].StartsWith("r"))
                        {
                            destB3 = ushort.Parse(words[2].TrimStart('r'));
                        }
                        else
                        {
                            destB3 = (ushort)(_chunk.SetConstant(double.Parse(words[2])) + 256);
                        }

                        ushort destC3;
                        if (words[3].StartsWith("r"))
                        {
                            destC3 = ushort.Parse(words[3].TrimStart('r'));
                        }
                        else
                        {
                            destC3 = (ushort)(_chunk.SetConstant(double.Parse(words[3])) + 256);
                        }

                        instruction = Instruction.CreateABC(
                            GetOpCode(words[0]),
                            destA3,
                            destB3,
                            destC3
                        );
                        break;
                    case "EQ":
                    case "LT":
                    case "LE":
                        destA3 = byte.Parse(words[1]);

                        if (words[2].StartsWith("r"))
                        {
                            destB3 = ushort.Parse(words[2].TrimStart('r'));
                        }
                        else
                        {
                            destB3 = (ushort)(_chunk.SetConstant(double.Parse(words[2])) + 256);
                        }

                        if (words[3].StartsWith("r"))
                        {
                            destC3 = ushort.Parse(words[3].TrimStart('r'));
                        }
                        else
                        {
                            destC3 = (ushort)(_chunk.SetConstant(double.Parse(words[3])) + 256);
                        }
                        instruction = Instruction.CreateABC(
                            GetOpCode(words[0]),
                            destA3,
                            destB3,
                            destC3
                        );
                        break;
                    case "UNM":
                        byte destA4 = byte.Parse(words[1].TrimStart('r'));

                        ushort destB4;
                        if (words[2].StartsWith("r"))
                        {
                            destB4 = ushort.Parse(words[2].TrimStart('r'));
                        }
                        else
                        {
                            destB4 = (ushort)(_chunk.SetConstant(double.Parse(words[2])) + 256);
                        }

                        instruction = Instruction.CreateABx(OpCode.UNM, destA4, destB4);
                        break;
                    case "JUMP":
                        int labelIndex;
                        try
                        {
                            labelIndex = (int)(labels[words[1]] - pc);
                        }
                        catch
                        {
                            Console.WriteLine("There isnt a label with name " + words[1]);
                            return;
                        }

                        instruction = Instruction.CreateSBx26(OpCode.JUMP, labelIndex);
                        break;
                    case "CALL":
                        uint methodIndex = 0;
                        if (methods.TryGetValue(words[1], out uint idx))
                        {
                            methodIndex = idx;
                        }
                        else if (_hostMethods.TryGetValue(words[1], out uint hostIdx))
                        {
                            methodIndex = hostIdx;
                        }
                        else
                        {
                            Console.WriteLine("There isnt a method with name " + words[1]);
                            return;
                        }
                        byte start = byte.Parse(words[2].TrimStart('r'));
                        instruction = Instruction.CreateABx(OpCode.CALL, start, methodIndex);
                        break;
                    case "RETURN":
                        start = byte.Parse(words[1].TrimStart('r'));
                        byte end = byte.Parse(words[2].TrimStart('r'));
                        instruction = Instruction.CreateABx(OpCode.RETURN, start, end);
                        break;
                    case "PRINT":

                        ushort printA;
                        if (words[1].StartsWith("r"))
                        {
                            printA = ushort.Parse(words[1].TrimStart('r'));
                        }
                        else
                        {
                            printA = (ushort)(_chunk.SetConstant(double.Parse(words[1])) + 256);
                        }

                        instruction = Instruction.CreateABC(OpCode.PRINT, 0, printA, 0);
                        break;
                    case "RAND":
                        byte randR = byte.Parse(words[1].TrimStart('r'));
                        instruction = Instruction.CreateABC(OpCode.RAND, randR, 0, 0);
                        break;
                    case "PRINTA":

                        if (words[1].StartsWith("r"))
                        {
                            printA = ushort.Parse(words[1].TrimStart('r'));
                        }
                        else
                        {
                            printA = (ushort)(_chunk.SetConstant(double.Parse(words[1])) + 256);
                        }

                        instruction = Instruction.CreateABC(OpCode.PRINTA, 0, printA, 0);
                        break;

                    case "HALT":
                        instruction = (uint)OpCode.HALT;
                        break;
                    case "SQRT":
                        destA4 = byte.Parse(words[1].TrimStart('r'));
                        if (words[2].StartsWith("r"))
                        {
                            destB4 = ushort.Parse(words[2].TrimStart('r'));
                        }
                        else
                        {
                            destB4 = (ushort)(_chunk.SetConstant(double.Parse(words[2])) + 256);
                        }
                        instruction = Instruction.CreateABx(OpCode.SQRT, destA4, destB4);
                        break;
                    case "FISR":
                        destA4 = byte.Parse(words[2].TrimStart('r'));
                        if (words[2].StartsWith("r"))
                        {
                            destB4 = ushort.Parse(words[2].TrimStart('r'));
                        }
                        else
                        {
                            destB4 = (ushort)(_chunk.SetConstant(double.Parse(words[2])) + 256);
                        }
                        instruction = Instruction.CreateABx(OpCode.FISR, destA4, destB4);
                        break;
                    case "FOR":
                        byte rIndex = byte.Parse(words[1].TrimStart("r"));
                        ushort rMax;
                        if (words[2].StartsWith("r"))
                            rMax = ushort.Parse(words[2].TrimStart('r'));
                        else
                            rMax = (ushort)(_chunk.SetConstant(double.Parse(words[2])) + 256);

                        ushort rStep;
                        if (words[3].StartsWith("r"))
                            rStep = ushort.Parse(words[3].TrimStart('r'));
                        else
                            rStep = (ushort)(_chunk.SetConstant(double.Parse(words[3])) + 256);
                        byte comp = 0;
                        switch (words[4])
                        {
                            case "<":
                                comp = 0;
                                break;
                            case ">":
                                comp = 1;
                                break;
                            case "<=":
                                comp = 2;
                                break;
                            case ">=":
                                comp = 3;
                                break;
                        }
                        int jumpOffset = (int)(labels[words[5]] - pc);
                        instruction = Instruction.CreateABC(OpCode.FOR, rIndex, rMax, rStep);
                        instructions.Add(instruction);
                        pc++;
                        instruction = Instruction.CreateAsBx(OpCode.FOR, comp, jumpOffset);
                        break;
                    case "NEWARR":
                    case "GETARR":
                    case "GETARRA":
                        OpCode code;
                        switch (words[0])
                        {
                            case "NEWARR":
                                code = OpCode.NEWARR;
                                break;
                            case "GETARR":
                                code = OpCode.GETARR;
                                break;
                            case "GETARRA":
                                code = OpCode.GETARRA;
                                break;
                            default:
                                code = OpCode.PRINT;
                                break;
                        }
                        byte regPtr = byte.Parse(words[1].Trim("r"));

                        ushort index;
                        if (words[2].StartsWith("r"))
                            index = ushort.Parse(words[2].TrimStart('r'));
                        else
                            index = (ushort)(_chunk.SetConstant(double.Parse(words[2])) + 256);
                        instruction = Instruction.CreateABC(code, regPtr, index, 0);
                        break;
                    case "FREEARR":
                        regPtr = byte.Parse(words[1].Trim("r"));
                        instruction = Instruction.CreateABC(OpCode.FREEARR, regPtr, 0, 0);
                        break;
                    default:
                        throw new Exception($"Unknown opcode found: {words[0]} on line {pc}");
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"{x.Message} at line {pc}");
                return;
            }
            instructions.Add(instruction);
            pc++;
        }
        _chunk.Instructions = instructions.ToArray();
    }

    private OpCode GetOpCode(string instruc)
    {
        switch (instruc)
        {
            case "ADD":
                return OpCode.ADD;
            case "SUB":
                return OpCode.SUB;
            case "MUL":
                return OpCode.MUL;
            case "DIV":
                return OpCode.DIV;
            case "MOD":
                return OpCode.MOD;
            case "POW":
                return OpCode.POW;
            case "EQ":
                return OpCode.EQ;
            case "LT":
                return OpCode.LT;
            case "LE":
                return OpCode.LE;
            case "SETARR":
                return OpCode.SETARR;
            case "SETARRA":
                return OpCode.SETARRA;
            case "BINAND":
                return OpCode.BINAND;
            case "BINOR":
                return OpCode.BINOR;
            case "BINXOR":
                return OpCode.BINXOR;
            case "BINLSH":
                return OpCode.BINLSH;
            case "BINRSH":
                return OpCode.BINRSH;
        }
        return OpCode.LOADC;
    }

    private uint ExecuteLoadC(string[] words)
    {
        var destA1 = byte.Parse(words[1].TrimStart('r'));
        double constant = double.Parse(words[2]);

        uint bx = _chunk.SetConstant(constant);

        return Instruction.CreateABx(OpCode.LOADC, destA1, bx);
    }

    private uint ExecuteMove(string[] words)
    {
        byte destA = byte.Parse(words[1].TrimStart('r'));

        byte destB = byte.Parse(words[2].TrimStart('r'));
        return Instruction.CreateABx(OpCode.MOVE, destA, destB);
    }
}
