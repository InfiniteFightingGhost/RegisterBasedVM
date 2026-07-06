namespace RegisterBasedVM;

public class Assembler
{
    private readonly VMChunk _chunk;

    public Assembler(VMChunk chunk)
    {
        _chunk = chunk;
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
            else
                memoryAddress++;
        }
        foreach (var item in methods)
        {
            Console.WriteLine($"Name: {item.Key}, place: {_chunk.MethodTable[item.Value]}");
        }
        List<UInt32> instructions = new List<UInt32>();
        int pc = 0;
        foreach (var item in lines)
        {
            Console.WriteLine($"{pc}: {item}");
            var words = item.Split();
            uint instruction = 0;
            try
            {
                switch (words[0])
                {
                    case "LOADC":
                        var destA1 = byte.Parse(words[1].TrimStart('r'));
                        float constant = float.Parse(words[2]);

                        uint bx = _chunk.SetConstant(constant);

                        instruction = Instruction.CreateABx(OpCode.LOADC, destA1, bx);
                        break;
                    case "MOVE":
                        byte destA2 = byte.Parse(words[1].TrimStart('r'));

                        byte destB2 = byte.Parse(words[2].TrimStart('r'));
                        instruction = Instruction.CreateABx(OpCode.MOVE, destA2, destB2);
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
                        byte destA3 = byte.Parse(words[1].TrimStart('r'));

                        ushort destB3;
                        if (words[2].StartsWith("r"))
                        {
                            destB3 = ushort.Parse(words[2].TrimStart('r'));
                        }
                        else
                        {
                            destB3 = (ushort)(_chunk.SetConstant(float.Parse(words[2])) + 256);
                        }

                        ushort destC3;
                        if (words[3].StartsWith("r"))
                        {
                            destC3 = ushort.Parse(words[3].TrimStart('r'));
                        }
                        else
                        {
                            destC3 = (ushort)(_chunk.SetConstant(float.Parse(words[3])) + 256);
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
                            destB3 = (ushort)(_chunk.SetConstant(float.Parse(words[2])) + 256);
                        }

                        if (words[3].StartsWith("r"))
                        {
                            destC3 = ushort.Parse(words[3].TrimStart('r'));
                        }
                        else
                        {
                            destC3 = (ushort)(_chunk.SetConstant(float.Parse(words[3])) + 256);
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

                        uint destB4;
                        if (words[2].StartsWith("r"))
                        {
                            destB4 = uint.Parse(words[2].TrimStart('r'));
                        }
                        else
                        {
                            destB4 = _chunk.SetConstant(float.Parse(words[1])) + 256;
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
                        try
                        {
                            methodIndex = (uint)methods[words[1]];
                        }
                        catch
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
                            printA = (ushort)(_chunk.SetConstant(float.Parse(words[1])) + 256);
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
                            printA = (ushort)(_chunk.SetConstant(float.Parse(words[1])) + 256);
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
                            destB4 = _chunk.SetConstant(float.Parse(words[2])) + 256;
                        }
                        instruction = Instruction.CreateABx(OpCode.SQRT, destA4, destB4);
                        break;
                    case "FISR":

                        destA4 = byte.Parse(words[2].TrimStart('r'));
                        if (words[2].StartsWith("r"))
                        {
                            destB4 = uint.Parse(words[2].TrimStart('r'));
                        }
                        else
                        {
                            destB4 = _chunk.SetConstant(float.Parse(words[2])) + 256;
                        }
                        instruction = Instruction.CreateABx(OpCode.FISR, destA4, destB4);
                        break;
                    default:
                        throw new Exception($"Unknown opcode found: {words[0]} on line {pc}");
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"{x.Message} at line {pc}");
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
            case "POW":
                return OpCode.POW;
            case "EQ":
                return OpCode.EQ;
            case "LT":
                return OpCode.LT;
            case "LE":
                return OpCode.LE;
        }
        return OpCode.LOADC;
    }
}
