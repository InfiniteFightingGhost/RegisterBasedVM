using System;

namespace Raptor;

public class VerificationException : Exception
{
    public VerificationException(string message) : base(message) { }
}

public static class BytecodeVerifier
{
    public static void Verify(VMChunk chunk, int heapSize)
    {
        if (chunk == null)
        {
            throw new ArgumentNullException(nameof(chunk));
        }

        if (chunk.Instructions == null || chunk.Instructions.Length == 0)
        {
            throw new VerificationException("Program contains no instructions.");
        }

        int length = chunk.Instructions.Length;
        bool[] validStarts = new bool[length];
        bool[] isForPayload = new bool[length];

        // Pass 1: Scan instruction boundaries and identify FOR loops
        for (int i = 0; i < length; i++)
        {
            Instruction inst = new Instruction(chunk.Instructions[i]);
            validStarts[i] = !isForPayload[i];

            if (inst.Op == OpCode.FOR)
            {
                if (isForPayload[i])
                {
                    throw new VerificationException($"Instruction at index {i} cannot be a FOR opcode because it is inside the payload of a preceding FOR instruction.");
                }
                if (i + 1 >= length)
                {
                    throw new VerificationException($"Incomplete FOR loop instruction at the end of the program (index {i}).");
                }
                
                isForPayload[i + 1] = true;
                Instruction secondWord = new Instruction(chunk.Instructions[i + 1]);
                if (secondWord.Op != OpCode.FOR)
                {
                    throw new VerificationException($"Invalid compound FOR instruction structure at index {i + 1}. Second word opcode must be FOR.");
                }
                
                i++; // Skip the second word in boundary scan
            }
        }

        // Pass 2: Verify each instruction's operands, jumps, methods, and memory sizes
        for (int i = 0; i < length; i++)
        {
            if (isForPayload[i])
            {
                continue; // Skip the second word of FOR as it is validated with the first
            }

            Instruction inst = new Instruction(chunk.Instructions[i]);
            OpCode op = inst.Op;

            // Validate Register/Constant (RC) operands for opcodes that use them
            switch (op)
            {
                case OpCode.ADD:
                case OpCode.SUB:
                case OpCode.MUL:
                case OpCode.DIV:
                case OpCode.POW:
                case OpCode.MOD:
                case OpCode.EQ:
                case OpCode.LT:
                case OpCode.LE:
                case OpCode.SETARR:
                case OpCode.SETARRA:
                case OpCode.BINAND:
                case OpCode.BINOR:
                case OpCode.BINXOR:
                case OpCode.BINLSH:
                case OpCode.BINRSH:
                    VerifyRcOperand(inst.B, chunk.Constants.Length, i, "B", op);
                    VerifyRcOperand(inst.C, chunk.Constants.Length, i, "C", op);
                    break;

                case OpCode.UNM:
                case OpCode.SQRT:
                case OpCode.FISR:
                case OpCode.PRINT:
                case OpCode.PRINTA:
                case OpCode.PRINTS:
                    VerifyRcOperand(inst.B, chunk.Constants.Length, i, "B", op);
                    break;

                case OpCode.GETARR:
                case OpCode.GETARRA:
                    VerifyRcOperand(inst.C, chunk.Constants.Length, i, "C", op);
                    break;

                case OpCode.FOR:
                    VerifyRcOperand(inst.B, chunk.Constants.Length, i, "B", op);
                    VerifyRcOperand(inst.C, chunk.Constants.Length, i, "C", op);
                    break;
            }

            // Detailed validation by specific instruction type
            switch (op)
            {
                case OpCode.LOADC:
                    if (inst.Bx >= chunk.Constants.Length)
                    {
                        throw new VerificationException($"Instruction at index {i} ({op}) references constant index {inst.Bx} which is out of bounds (pool size: {chunk.Constants.Length}).");
                    }
                    break;

                case OpCode.JUMP:
                    {
                        int target = i + inst.sBx26;
                        if (target < 0 || target >= length)
                        {
                            throw new VerificationException($"Instruction at index {i} ({op}) jumps out of bounds to target index {target}.");
                        }
                        if (!validStarts[target])
                        {
                            throw new VerificationException($"Instruction at index {i} ({op}) jumps to invalid instruction start at index {target} (middle of a FOR instruction).");
                        }
                    }
                    break;

                case OpCode.FOR:
                    {
                        Instruction secondWord = new Instruction(chunk.Instructions[i + 1]);
                        int target = i + secondWord.sBx16;
                        if (target < 0 || target >= length)
                        {
                            throw new VerificationException($"FOR loop at index {i} has loop back jump target {target} which is out of bounds.");
                        }
                        if (!validStarts[target])
                        {
                            throw new VerificationException($"FOR loop at index {i} has loop back jump target {target} which lands on an invalid instruction start (middle of a FOR instruction).");
                        }
                        
                        byte comp = secondWord.A;
                        if (comp > 3)
                        {
                            throw new VerificationException($"FOR loop at index {i} has invalid comparison code {comp} (must be 0, 1, 2, or 3).");
                        }
                    }
                    break;

                case OpCode.EQ:
                case OpCode.LT:
                case OpCode.LE:
                    if (i + 1 >= length)
                    {
                        throw new VerificationException($"Conditional branch at index {i} ({op}) is at the end of the program and cannot skip.");
                    }
                    if (!validStarts[i + 1])
                    {
                        throw new VerificationException($"Conditional branch at index {i} ({op}) has its branch slot at index {i + 1} which is not a valid instruction start.");
                    }
                    if (i + 2 >= length)
                    {
                        throw new VerificationException($"Conditional branch at index {i} ({op}) has no instruction to execute if skip occurs.");
                    }
                    if (!validStarts[i + 2])
                    {
                        throw new VerificationException($"Conditional branch at index {i} ({op}) skips to index {i + 2} which is not a valid instruction start.");
                    }
                    break;

                case OpCode.CALL:
                    {
                        ushort methodIndex = inst.B;
                        if (methodIndex >= chunk.MethodTable.Length)
                        {
                            throw new VerificationException($"Instruction at index {i} ({op}) references method index {methodIndex} which is out of bounds (Method Table size: {chunk.MethodTable.Length}).");
                        }
                        uint targetAddress = chunk.MethodTable[methodIndex];
                        if (targetAddress >= length)
                        {
                            throw new VerificationException($"Method index {methodIndex} points to target address {targetAddress} which is out of bounds.");
                        }
                        if (!validStarts[targetAddress])
                        {
                            throw new VerificationException($"Method index {methodIndex} points to target address {targetAddress} which is not a valid instruction start.");
                        }
                    }
                    break;

                case OpCode.NEWARR:
                    {
                        uint sizeOperand = inst.B;
                        if (sizeOperand >= 256)
                        {
                            uint constIdx = sizeOperand - 256;
                            if (constIdx < chunk.Constants.Length)
                            {
                                double constVal = chunk.Constants[constIdx];
                                if (constVal < 0)
                                {
                                    throw new VerificationException($"Instruction at index {i} ({op}) allocates negative array size {constVal}.");
                                }
                                if (constVal * 8 > heapSize)
                                {
                                    throw new VerificationException($"Instruction at index {i} ({op}) allocates array of size {constVal} doubles ({constVal * 8} bytes) which exceeds total heap size {heapSize} bytes.");
                                }
                            }
                        }
                    }
                    break;
            }
        }

        // Pass 3: Verify the last instruction is a valid terminal instruction
        int lastInstIndex = length - 1;
        while (lastInstIndex >= 0 && isForPayload[lastInstIndex])
        {
            lastInstIndex--;
        }
        if (lastInstIndex < 0)
        {
            throw new VerificationException("Program contains no valid instructions.");
        }
        Instruction lastInst = new Instruction(chunk.Instructions[lastInstIndex]);
        if (lastInst.Op != OpCode.HALT && lastInst.Op != OpCode.RETURN && lastInst.Op != OpCode.JUMP)
        {
            throw new VerificationException($"Program does not end with a terminating instruction (HALT, RETURN, or JUMP). Found {lastInst.Op} at index {lastInstIndex}.");
        }
    }

    private static void VerifyRcOperand(ushort operand, int constantsCount, int instructionIndex, string operandName, OpCode op)
    {
        if (operand >= 256)
        {
            int constIndex = operand - 256;
            if (constIndex >= constantsCount)
            {
                throw new VerificationException($"Instruction at index {instructionIndex} ({op}) has operand '{operandName}' mapping to constant index {constIndex} which is out of bounds (pool size: {constantsCount}).");
            }
        }
    }
}
