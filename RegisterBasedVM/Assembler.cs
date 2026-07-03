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
        UInt32 lastUsedConstantIndex = 0;
        List<UInt32> instructions = new List<UInt32>();
        for (int i = 0; i < lines.Length; i++)
        {
            var item = lines[i];
            var words = item.Split(' ');
            uint instruction = 0;
            uint opcode = 0;

            switch (words[0])
            {
                case "LOADC":
                    opcode = (uint)OpCode.LOADC;

                    uint destA1 = uint.Parse(words[1]);
                    float constant = float.Parse(words[2]);

                    _chunk.SetConstant(constant, lastUsedConstantIndex);
                    uint bx = lastUsedConstantIndex;

                    destA1 = destA1 & 0xFF;
                    bx = bx & 0x3FFFF;

                    // Smash them together! Hehehehaw
                    instruction = opcode | (destA1 << 6) | (bx << 14);
                    lastUsedConstantIndex++;
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
                case "EQ":
                case "LT":
                case "LE":
                    opcode = GetOpCode(words[0]);
                    uint destA3 = uint.Parse(words[1]);
                    destA3 = destA3 & 0xFF;

                    uint destB3 = uint.Parse(words[2]);
                    destB3 = destB3 & 0x1FF;

                    uint destC3 = uint.Parse(words[3]);
                    destC3 = destC3 & 0x1FF;
                    instruction = opcode | (destA3 << 6) | (destB3 << 14) | (destC3 << 23);
                    break;
                case "UNM":
                    opcode = (uint)OpCode.UNM;
                    uint destA4 = uint.Parse(words[1]);
                    destA4 = destA4 & 0xFF;

                    uint destB4 = uint.Parse(words[2]);
                    destB4 = destB4 & 0x1FF;
                    instruction = opcode | (destA4 << 6) | (destB4 << 14);
                    break;
                case "JUMP":
                    opcode = (uint)OpCode.JUMP;
                    int sBx = int.Parse(words[1]);
                    const int sBxBias = 33554431;

                    int rawSbx = sBx + sBxBias;
                    uint unsignedBx = (uint)(rawSbx & 0x3FFFFFF);
                    instruction = opcode | (uint)((unsignedBx << 6)); // |(((sBx >> 25) << 31)));
                    break;
                case "PRINT":
                    opcode = (uint)OpCode.PRINT;

                    uint printA = uint.Parse(words[1]);
                    printA = printA & 0xFF;

                    instruction = opcode | (printA << 6);
                    break;
                case "PRINTA":
                    opcode = (uint)OpCode.PRINTA;

                    uint printA1 = uint.Parse(words[1]);
                    printA1 = printA1 & 0xFF;

                    instruction = opcode | (printA1 << 6);
                    break;

                case "HALT":
                    instruction = (uint)OpCode.HALT;
                    break;

                default:
                    throw new Exception($"Unknown opcode found: {words[0]} on line {i}");
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
