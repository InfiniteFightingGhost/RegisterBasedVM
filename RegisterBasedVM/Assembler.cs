namespace RegisterBasedVM;

public class Assembler
{
    private readonly VMChunk _chunk;

    public Assembler(VMChunk chunk)
    {
        _chunk = chunk;
    }

    public void Parse(string[] lines)
    {
        Dictionary<string, int> labels = new();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("$"))
            {
                if (!labels.ContainsKey(line))
                {
                    labels.Add(line, i);
                }
                else
                    throw new Exception("Label name already exists on line " + labels[line]);
            }
        }
        List<UInt32> instructions = new List<UInt32>();
        for (int i = 0; i < lines.Length; i++)
        {
            var item = lines[i];
            var words = item.Split(';')[0].Trim()?.Split(' ');
            uint instruction = 0;
            uint opcode = 0;
            try
            {
                switch (words[0])
                {
                    case "LOADC":
                        opcode = (uint)OpCode.LOADC;

                        var destA1 = uint.Parse(string.Join("", words[1].Skip(1)));
                        float constant = float.Parse(words[2]);

                        uint bx = _chunk.SetConstant(constant);

                        destA1 = destA1 & 0xFF;
                        bx = bx & 0x3FFFF;

                        // Smash them together! Hehehehaw
                        instruction = opcode | (destA1 << 6) | (bx << 14);
                        break;
                    case "MOVE":
                        opcode = (uint)OpCode.MOVE;
                        uint destA2 = uint.Parse(words[1]);
                        destA2 = destA2 & 0xFF;

                        uint destB2 = uint.Parse(words[2]);
                        destB2 = destB2 & 0x1FF;
                        instruction = opcode | (destA2 << 6) | (destB2 << 14);
                        break;
                    case "SWP":
                        opcode = (uint)OpCode.SWP;
                        uint destA5 = uint.Parse(words[1]);
                        destA5 = destA5 & 0xFF;

                        uint destB5 = uint.Parse(words[2]);
                        destB5 = destB5 & 0x1FF;
                        instruction = opcode | (destA5 << 6) | (destB5 << 14);
                        break;

                    case "ADD":
                    case "SUB":
                    case "MUL":
                    case "DIV":
                    case "POW":
                        opcode = GetOpCode(words[0]);
                        uint destA3;
                        if (words[1].StartsWith("r"))
                        {
                            destA3 = uint.Parse(string.Join("", words[1].Skip(1)));
                        }
                        else
                        {
                            destA3 = _chunk.SetConstant(float.Parse(words[1])) + 256;
                        }
                        destA3 = destA3 & 0xFF;

                        uint destB3;
                        if (words[2].StartsWith("r"))
                        {
                            destB3 = uint.Parse(string.Join("", words[2].Skip(1)));
                        }
                        else
                        {
                            destB3 = _chunk.SetConstant(float.Parse(words[2])) + 256;
                        }
                        destB3 = destB3 & 0x1FF;

                        uint destC3;
                        if (words[3].StartsWith("r"))
                        {
                            destC3 = uint.Parse(string.Join("", words[3].Skip(1)));
                        }
                        else
                        {
                            destC3 = _chunk.SetConstant(float.Parse(words[3])) + 256;
                        }
                        destC3 = destC3 & 0x1FF;

                        instruction = opcode | (destA3 << 6) | (destB3 << 14) | (destC3 << 23);
                        break;
                    case "EQ":
                    case "LT":
                    case "LE":
                        opcode = GetOpCode(words[0]);
                        destA3 = uint.Parse(words[1]);

                        if (words[2].StartsWith("r"))
                        {
                            destB3 = uint.Parse(string.Join("", words[2].Skip(1)));
                        }
                        else
                        {
                            destB3 = _chunk.SetConstant(float.Parse(words[2])) + 256;
                        }
                        destB3 = destB3 & 0x1FF;

                        if (words[3].StartsWith("r"))
                        {
                            destC3 = uint.Parse(string.Join("", words[3].Skip(1)));
                        }
                        else
                        {
                            destC3 = _chunk.SetConstant(float.Parse(words[3])) + 256;
                        }
                        destC3 = destC3 & 0x1FF;
                        instruction = opcode | (destA3 << 6) | (destB3 << 14) | (destC3 << 23);
                        break;
                    case "UNM":
                        opcode = (uint)OpCode.UNM;
                        var destA4 = uint.Parse(string.Join("", words[1].Skip(1)));
                        destA4 = destA4 & 0xFF;

                        uint destB4;
                        if (words[1].StartsWith("r"))
                        {
                            destB4 = uint.Parse(string.Join("", words[1].Skip(1)));
                        }
                        else
                        {
                            destB4 = _chunk.SetConstant(float.Parse(words[1])) + 256;
                        }
                        destB4 = destB4 & 0x1FF;
                        instruction = opcode | (destA4 << 6) | (destB4 << 14);
                        break;
                    case "JUMP":
                        opcode = (uint)OpCode.JUMP;
                        int labelIndex;
                        try
                        {
                            labelIndex = labels[words[1]];
                        }
                        catch
                        {
                            Console.WriteLine("There isnt a label with name " + words[1]);
                            return;
                        }
                        labelIndex -= i;
                        const int sBxBias = 33554431;

                        int rawSbx = labelIndex + sBxBias;
                        uint unsignedBx = (uint)(rawSbx & 0x3FFFFFF);
                        instruction = opcode | (uint)((unsignedBx << 6)); // |(((sBx >> 25) << 31)));
                        break;
                    case "CALL":
                        opcode = (uint)OpCode.CALL;
                        try
                        {
                            labelIndex = labels[words[1]];
                        }
                        catch
                        {
                            Console.WriteLine("There isnt a label with name " + words[1]);
                            return;
                        }
                        labelIndex -= i;

                        rawSbx = labelIndex + sBxBias;
                        unsignedBx = (uint)(rawSbx & 0x3FFFFFF);
                        instruction = opcode | (uint)((unsignedBx << 6)); // |(((sBx >> 25) << 31)));
                        break;
                    case "RET":
                        opcode = (uint)OpCode.CALL;

                        instruction = opcode;
                        break;
                    case "PRINT":
                        opcode = (uint)OpCode.PRINT;

                        uint printA;
                        if (words[1].StartsWith("r"))
                        {
                            printA = uint.Parse(string.Join("", words[1].Skip(1)));
                        }
                        else
                        {
                            printA = _chunk.SetConstant(float.Parse(words[1])) + 256;
                        }
                        printA = printA & 0x1FF;

                        instruction = opcode | (printA << 6);
                        break;
                    case "RAND":
                        opcode = (uint)OpCode.RAND;
                        uint randR = uint.Parse(words[1]);
                        instruction = opcode | (randR << 6);
                        break;
                    case "PRINTA":
                        opcode = (uint)OpCode.PRINTA;

                        uint printA1;
                        if (words[1].StartsWith("r"))
                        {
                            printA1 = uint.Parse(string.Join("", words[1].Skip(1)));
                        }
                        else
                        {
                            printA1 = _chunk.SetConstant(float.Parse(words[1])) + 256;
                        }
                        printA1 = printA1 & 0x1FF;

                        instruction = opcode | (printA1 << 6);
                        break;

                    case "HALT":
                        instruction = (uint)OpCode.HALT;
                        break;
                    case "SQRT":
                        opcode = (uint)OpCode.SQRT;
                        destA4 = uint.Parse(string.Join("", words[1].Skip(1)));
                        destA4 = destA4 & 0xFF;
                        if (words[1].StartsWith("r"))
                        {
                            destB4 = uint.Parse(string.Join("", words[1].Skip(1)));
                        }
                        else
                        {
                            destB4 = _chunk.SetConstant(float.Parse(words[1])) + 256;
                        }
                        destB4 = destB4 & 0x1FF;
                        instruction = opcode | (destA4 << 6) | (destB4 << 14);
                        break;
                    case "FISR":
                        opcode = (uint)OpCode.FISR;

                        destA4 = uint.Parse(string.Join("", words[1].Skip(1)));
                        destA4 = destA4 & 0xFF;
                        if (words[1].StartsWith("r"))
                        {
                            destB4 = uint.Parse(string.Join("", words[1].Skip(1)));
                        }
                        else
                        {
                            destB4 = _chunk.SetConstant(float.Parse(words[1])) + 256;
                        }
                        destB4 = destB4 & 0x1FF;
                        instruction = opcode | (destA4 << 6) | (destB4 << 14);
                        break;
                    default:
                        throw new Exception($"Unknown opcode found: {words[0]} on line {i}");
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"{x.Message} at line {i}");
            }
            instructions.Add(instruction);
        }
        _chunk.Instructions = instructions.ToArray();
    }

    private uint GetOpCode(string instruc)
    {
        switch (instruc)
        {
            case "ADD":
                return (uint)OpCode.ADD;
            case "SUB":
                return (uint)OpCode.SUB;
            case "MUL":
                return (uint)OpCode.MUL;
            case "DIV":
                return (uint)OpCode.DIV;
            case "POW":
                return (uint)OpCode.POW;
            case "EQ":
                return (uint)OpCode.EQ;
            case "LT":
                return (uint)OpCode.LT;
            case "LE":
                return (uint)OpCode.LE;
            default:
                return 0xFF;
        }
    }
}
