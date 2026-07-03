namespace RegisterBasedVM;

public class VirtualMachine
{
    private uint[] _instructions = null!;
    private float[] _constants = null!;
    int _pc = 0;
    private float[] _registers = new float[256];

    public void LoadProgram(uint[] instructions, float[] constants)
    {
        _instructions = instructions;
        _constants = constants;
        _pc = 0;
        Array.Clear(_registers);
    }

    public void Run()
    {
        bool isRunning = true;

        while (isRunning)
        {
            byte opcode = (byte)(_instructions[_pc] & 0x3F);
            byte a = (byte)((_instructions[_pc] >> 6) & 0xFF);
            // Console.WriteLine($"OpCode: {opcode}, destination: {a}");
            switch (opcode)
            {
                case (byte)OpCode.LOADC:
                    uint constantIndex = (byte)(_instructions[_pc] >> 14 & 0x0000FFFFFF);
                    _registers[a] = _constants[constantIndex];
                    break;
                case (byte)OpCode.MOVE:
                    byte b1 = (byte)((_instructions[_pc] >> 14) & 0xFF);
                    _registers[a] = _registers[b1];
                    break;
                case (byte)OpCode.SWP:
                    byte bSwap = (byte)((_instructions[_pc] >> 14) & 0xFF);
                    (_registers[a], _registers[bSwap]) = (_registers[bSwap], _registers[a]);
                    break;
                case (byte)OpCode.UNM:
                    byte b2 = (byte)((_instructions[_pc] >> 14) & 0xFF);
                    _registers[a] = -_registers[b2];
                    break;
                case (byte)OpCode.JUMP:
                    const int sBxBias = 33554431;
                    uint unsignedBx = (uint)(_instructions[_pc] >> 6);
                    int sBx = (int)(unsignedBx - sBxBias);
                    // Console.WriteLine(sBx);
                    _pc += sBx - 1;
                    break;
                case (byte)OpCode.ADD:
                    uint b3 = (uint)(_instructions[_pc] >> 14) & 0x1FF;
                    uint c3 = (uint)(_instructions[_pc] >> 23) & 0x1FF;
                    _registers[a] = _registers[b3] + _registers[c3];
                    break;
                case (byte)OpCode.SUB:
                    uint b4 = (uint)(_instructions[_pc] >> 14) & 0x1FF;
                    uint c4 = (uint)(_instructions[_pc] >> 23) & 0x1FF;
                    _registers[a] = _registers[b4] - _registers[c4];
                    break;
                case (byte)OpCode.MUL:
                    uint b5 = (uint)(_instructions[_pc] >> 14) & 0x1FF;
                    uint c5 = (uint)(_instructions[_pc] >> 23) & 0x1FF;
                    _registers[a] = _registers[b5] * _registers[c5];
                    break;
                case (byte)OpCode.DIV:
                    uint b6 = (uint)(_instructions[_pc] >> 14) & 0x1FF;
                    uint c6 = (uint)(_instructions[_pc] >> 23) & 0x1FF;
                    _registers[a] = _registers[b6] / _registers[c6];
                    break;
                case (byte)OpCode.POW:
                    uint b7 = (uint)(_instructions[_pc] >> 14) & 0x1FF;
                    uint c7 = (uint)(_instructions[_pc] >> 23) & 0x1FF;
                    _registers[a] = (float)Math.Pow(_registers[b7], _registers[c7]);
                    break;
                case (byte)OpCode.EQ:
                    uint b8 = (uint)(_instructions[_pc] >> 14) & 0x1FF;
                    uint c8 = (uint)(_instructions[_pc] >> 23) & 0x1FF;
                    bool comparison = _registers[b8] == _registers[c8];
                    bool expected = (a != 0);
                    if (comparison == expected)
                    {
                        _pc++;
                    }
                    break;
                case (byte)OpCode.LT:
                    uint b9 = (uint)(_instructions[_pc] >> 14) & 0x1FF;
                    uint c9 = (uint)(_instructions[_pc] >> 23) & 0x1FF;
                    bool comparison2 = _registers[b9] < _registers[c9];
                    // Console.WriteLine($"B: {_registers[b9]}, C: {_registers[c9]}, A: {a}");
                    bool expected2 = (a != 0);
                    if (comparison2 != expected2)
                    {
                        _pc++;
                    }
                    break;
                case (byte)OpCode.LE:
                    uint b10 = (uint)(_instructions[_pc] >> 14) & 0x1FF;
                    uint c10 = (uint)(_instructions[_pc] >> 23) & 0x1FF;
                    bool comparison3 = _registers[b10] <= _registers[c10];
                    bool expected3 = (a != 0);
                    if (comparison3 == expected3)
                    {
                        _pc++;
                    }
                    break;
                case (byte)OpCode.PRINT:
                    Console.WriteLine(_registers[a]);
                    break;
                case (byte)OpCode.PRINTA:
                    Console.Write((char)_registers[a]);
                    break;
                case (byte)OpCode.HALT:
                    isRunning = false;
                    break;
                default:
                    Console.WriteLine($"Uknown instruction at {_pc} -> {opcode}");
                    return;
            }
            _pc++;
        }
    }
}
