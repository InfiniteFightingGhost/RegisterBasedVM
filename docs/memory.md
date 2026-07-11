# Heap Memory Management & Custom Allocator

The VM manages dynamic memory through a custom allocator running on a single continuous block of 16MB heap memory (`byte[] _heap`). Rather than delegating allocations to the C# GC, the VM manages blocks directly using an **intrinsically linked list of free blocks** and raw pointer arithmetic.

---

## 1. Heap Memory Layout

The heap buffer begins with a size of 16MB. The memory allocator operates on two types of blocks inside this buffer: **Allocated Blocks** and **Free Blocks**.

### Allocated Block Layout
When an array is allocated, the block consumed contains:
- **Header (4 bytes):** Stores the size of the block's payload in bytes (represented as an unsigned 32-bit integer).
- **Payload (`valSize` bytes):** The user data, accessed via array index offsets.
```text
+-----------------------+---------------------------------------+
|  Size (4 bytes, uint) |         Payload (valSize bytes)       |
+-----------------------+---------------------------------------+
^                       ^
|                       |
realAddress             userPointer (returned to VM registers)
```

### Free Block Layout (Intrinsically Linked List)
Free memory is structured as a singly linked list. Because the memory is unused, the metadata for the list is stored **directly inside the free memory slots themselves** (an intrinsic list):
- **Next Offset (4 bytes):** The heap-relative byte address of the next free block. If it is the last block, it stores `0xFFFFFFFF`.
- **Block Size (4 bytes):** The size of the free space available in this block in bytes.
```text
+-----------------------+-----------------------+---------------+
|  Next Offset (4B)     |   Block Size (4B)     |  Free space   |
+-----------------------+-----------------------+---------------+
^
|
freeBlockAddress
```

The head of this list is stored in the VM state as `state.FreeBlockHeaderPointer`.
At startup, the entire 16MB heap starts as a single free block at address `0`:
- Offset `0`: `0xFFFFFFFF` (next pointer)
- Offset `4`: `16,777,216` (size)
- `state.FreeBlockHeaderPointer = 0`

---

## 2. Allocation Algorithm (`NEWARR`)

The `NEWARR rDest size` instruction allocates a block of `size` raw bytes on the heap. 

> [!WARNING]
> The VM does **not** scale the size parameter by the size of the elements automatically during allocation. It allocates exactly the number of bytes specified by `size`.
> - For character arrays (accessed via `SETARRA` / `GETARRA` in 1-byte offsets), `size` equals the number of characters.
> - For double-precision arrays (accessed via `SETARR` / `GETARR` in 8-byte offsets), the developer or compiler must specify the size in bytes, which is `elements * 8`. Failure to scale the allocation size will result in writes overlapping with subsequent free blocks or memory blocks, leading to heap corruption.

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

### The Allocation Logic
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

---

## 3. Deallocation & Coalescing Algorithm (`FREEARR`)

The `FREEARR rPtr` instruction frees the array pointed to by `rPtr`. Because manual allocation quickly fragments memory, the deallocator performs **immediate coalescing (compaction)** with neighboring free blocks.

### Step 1: Address Restoration
The pointer in the register (`vmPointer`) points to the payload. The real block address starts 4 bytes earlier:
$$\text{realAddress} = \text{vmPointer} - 4$$
The size of the block to free is read from the header:
$$\text{freedSize} = *\text{uint}*(\text{state.HeapPtr} + \text{realAddress})$$

### Step 2: Sorted Insertion
To make coalescing possible, the free list is kept strictly sorted by memory address. The allocator traverses the free list to find the insertion point between `leftBlock` and `rightBlock` such that:
$$\text{leftBlock} < \text{realAddress} < \text{rightBlock}$$

### Step 3: Coalescing Neighbors
Once the insertion point is determined, the VM checks if the freed block is physically adjacent to its neighbors:

1. **Right Coalesce:** If the freed block ends exactly where the `rightBlock` begins:
   $$\text{realAddress} + \text{freedSize} = \text{rightBlock}$$
   The two blocks are merged into one:
   $$\text{freedSize}_{\text{new}} = \text{freedSize} + \text{rightBlock.Size}$$
   $$\text{freedBlock.Next} = \text{rightBlock.Next}$$

2. **Left Coalesce:** If the `leftBlock` ends exactly where the freed block begins:
   $$\text{leftBlock} + \text{leftBlock.Size} = \text{realAddress}$$
   The two blocks are merged into one:
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

This address-ordered coalescing prevents memory fragmentation, ensuring that adjacent free spaces are always combined into larger contiguous blocks for future allocations.
