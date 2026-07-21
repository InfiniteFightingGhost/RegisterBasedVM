# Heap Memory Management & Custom Allocator

The VM manages dynamic memory through a custom allocator running on a single continuous 512KB heap buffer (`byte[] _heap`). Memory blocks are tracked via an intrinsically linked list of free blocks and raw pointer arithmetic.

## Table of Contents
- [Heap Memory Layout](#heap-memory-layout)
  - [Allocated Block Layout](#allocated-block-layout)
  - [Free Block Layout](#free-block-layout)
- [Allocation Algorithm (NEWARR)](#allocation-algorithm-newarr)
  - [Allocation Logic](#allocation-logic)
- [Deallocation & Coalescing Algorithm (FREEARR)](#deallocation-coalescing-algorithm-freearr)
  - [Step 1: Address Restoration](#step-1-address-restoration)
  - [Step 2: Sorted Insertion](#step-2-sorted-insertion)
  - [Step 3: Coalescing Neighbors](#step-3-coalescing-neighbors)

## Heap Memory Layout

The heap buffer has a fixed capacity of 512KB (524,288 bytes). Memory operations manipulate two block types within this buffer: Allocated Blocks and Free Blocks.

### Allocated Block Layout
Allocated blocks contain a 4-byte size header followed by payload data:
- Header (4 bytes): Payload size in bytes (`valSizeBytes`, `uint32`).
- Payload (`valSizeBytes` bytes): User array data accessed via element offsets.
```text
+-----------------------+---------------------------------------+
|  Size (4 bytes, uint) |       Payload (valSizeBytes bytes)    |
+-----------------------+---------------------------------------+
^                       ^
|                       |
realAddress             userPointer (returned to VM registers)
```

### Free Block Layout
Free memory is structured as an intrinsic singly linked list stored inside free slots:
- Next Offset (4 bytes): Heap-relative byte address of the next free block (`0xFFFFFFFF` if end of list).
- Block Size (4 bytes): Available free space in bytes.
```text
+-----------------------+-----------------------+---------------+
|  Next Offset (4B)     |   Block Size (4B)     |  Free space   |
+-----------------------+-----------------------+---------------+
^
|
freeBlockAddress
```

The list head is tracked by `state.FreeBlockHeaderPointer`. At startup, the heap initializes as a single 512KB free block at address `0`:
- Offset 0: `0xFFFFFFFF` (next pointer)
- Offset 4: `524,288` (size)
- `state.FreeBlockHeaderPointer = 0`

## Allocation Algorithm (`NEWARR`)

The `NEWARR rDest size` instruction allocates an array of `size` elements.

> [!NOTE]
> The VM scales the `size` parameter by 8 bytes (`valSizeBytes = valSize * 8`) for double-precision floating-point elements, plus 4 header bytes.
> - `NEWARR rA 10` allocates memory for 10 double elements (80 bytes payload + 4 header bytes).
> - Character instructions (`SETARRA` / `GETARRA`) access the byte payload using 1-byte ASCII offsets.

```text
                                Allocation Flow
                               
   +--------------------+
   | Search Free List   | <---- Start at FreeBlockHeaderPointer
   +--------------------+
             |
             v
   +--------------------------------------------------------+
   | Find first block where BlockSize >= requested valSize  |
   +--------------------------------------------------------+
             |
             v
   +--------------------+       No
   |    Block Found?    | ------------> Throw OutOfMemoryException
   +--------------------+
             | Yes
             v
   +---------------------------------------+
   | Is BlockSize > requested valSize?     |
   +---------------------------------------+
     /                                   \ Yes
    / No                                  \
   v                                       v
[Consume Block Entirely]               [Split Block]
- Unlink from free list                - Shrink current block size
- Write valSize at block start         - Write valSize at block start
                                       - Re-link remaining free block
                                         at currAddress + valSize + 4
                                         with size (BlockSize - valSize)
             \                           /
              \                         /
               v                       v
         +---------------------------------------+
         | Write (currAddress + 4) to rDest      |
         +---------------------------------------+
```

### Allocation Logic
```csharp
if (blockSize > valSize)
{
    // If splitting the block, unlink the old block and link the remainder
    if (prevAddress != 0xFFFFFFFF)
        *(uint*)(state.HeapPtr + prevAddress) = nextAddress;
    else
        state.FreeBlockHeaderPointer = currAddress + valSize + 4;

    *(uint*)(state.HeapPtr + currAddress) = valSize; // Write header
    *(uint*)(state.HeapPtr + currAddress + valSize) = nextAddress; // Remainder next ptr
    *(uint*)(state.HeapPtr + currAddress + valSize + 4) = blockSize - valSize; // Remainder size
    Reg(state.RegPtr, state.BasePtr, pointerAddress) = currAddress + 4; // User pointer
}
else
{
    // Consume entire block
    if (prevAddress != 0xFFFFFFFF)
        *(uint*)(state.HeapPtr + prevAddress) = nextAddress;
    else
        state.FreeBlockHeaderPointer = 0xFFFFFFFF;
    *(uint*)(state.HeapPtr + currAddress) = valSize;
    Reg(state.RegPtr, state.BasePtr, pointerAddress) = currAddress + 4;
}
```

## Deallocation & Coalescing Algorithm (`FREEARR`)

The `FREEARR rPtr` instruction frees the array at `rPtr` and performs immediate coalescing with adjacent free blocks.

### Step 1: Address Restoration
The pointer in the register (`vmPointer`) points to payload start. The block header begins 4 bytes prior:
$$\text{realAddress} = \text{vmPointer} - 4$$
Block size is read from the header:
$$\text{freedSize} = *\text{uint}*(\text{state.HeapPtr} + \text{realAddress})$$

### Step 2: Sorted Insertion
The free list is maintained in ascending address order. The allocator traverses the list to locate insertion bounds `leftBlock` and `rightBlock`:
$$\text{leftBlock} < \text{realAddress} < \text{rightBlock}$$

### Step 3: Coalescing Neighbors
If the freed block is contiguous with neighbor blocks, it is merged:

1. Right Coalesce: If `realAddress + freedSize == rightBlock`:
   $$\text{freedSize}_{\text{new}} = \text{freedSize} + \text{rightBlock.Size}$$
   $$\text{freedBlock.Next} = \text{rightBlock.Next}$$

2. Left Coalesce: If `leftBlock + leftBlock.Size == realAddress`:
   $$\text{leftBlock.Size}_{\text{new}} = \text{leftBlock.Size} + \text{freedSize}$$
   $$\text{leftBlock.Next} = \text{freedBlock.Next}$$

```csharp
// Coalesce with Right Block
if (rightBlock != 0xFFFFFFFF && realAddress + freedSize == rightBlock)
{
    uint rightSize = *(uint*)(state.HeapPtr + rightBlock + 4);
    freedSize += rightSize;
    *(uint*)(state.HeapPtr + realAddress + 4) = freedSize;

    uint blockAfterRight = *(uint*)(state.HeapPtr + rightBlock);
    *(uint*)(state.HeapPtr + realAddress) = blockAfterRight;
    rightBlock = blockAfterRight;
}
// Coalesce with Left Block
if (leftBlock != 0xFFFFFFFF)
{
    uint leftSize = *(uint*)(state.HeapPtr + leftBlock + 4);
    if (leftBlock + leftSize == realAddress)
    {
        *(uint*)(state.HeapPtr + leftBlock + 4) = leftSize + freedSize;
        uint ourNext = *(uint*)(state.HeapPtr + realAddress);
        *(uint*)(state.HeapPtr + leftBlock) = ourNext;
    }
}
```

