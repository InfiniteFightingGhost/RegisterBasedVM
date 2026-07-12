namespace Raptor;

/// <summary>
/// Provides serialization and deserialization for the Raptor standardized binary format (.rbc).
///
/// File layout (little-endian):
///   HEADER (20 bytes)
///     [4 bytes]  Magic signature: 0x52415054 ("RAPT")
///     [1 byte]   Version major
///     [1 byte]   Version minor
///     [2 bytes]  Reserved (0x0000)
///     [4 bytes]  Constants pool count
///     [4 bytes]  Method table count
///     [4 bytes]  Instructions count
///   CONSTANTS POOL (count × 8 bytes)
///   METHOD TABLE   (count × 4 bytes)
///   INSTRUCTIONS   (count × 4 bytes)
/// </summary>
/// <remarks>
/// I really outdid myself here.
/// </remarks>
public static class RaptorBinary
{
    public const uint MagicSignature = 0x52415054; // ASCII "RAPT"
    public const byte VersionMajor = 1;
    public const byte VersionMinor = 0;

    /// <summary>
    /// Writes a compiled VMChunk to a binary stream in the standardized .rbc format.
    /// </summary>
    public static void Save(VMChunk chunk, Stream stream)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // Header
        writer.Write(MagicSignature);
        writer.Write(VersionMajor);
        writer.Write(VersionMinor);
        writer.Write((ushort)0); // reserved flags

        uint constantsCount = (uint)chunk.Constants.Length;
        uint methodTableCount = (uint)chunk.MethodTable.Length;
        uint instructionsCount = (uint)chunk.Instructions.Length;

        writer.Write(constantsCount);
        writer.Write(methodTableCount);
        writer.Write(instructionsCount);

        // Constants pool
        for (int i = 0; i < constantsCount; i++)
        {
            writer.Write(chunk.Constants[i]);
        }

        // Method table
        for (int i = 0; i < methodTableCount; i++)
        {
            writer.Write(chunk.MethodTable[i]);
        }

        // Instructions
        for (int i = 0; i < instructionsCount; i++)
        {
            writer.Write(chunk.Instructions[i]);
        }
    }

    /// <summary>
    /// Writes a compiled VMChunk to a binary file in the standardized .rbc format.
    /// </summary>
    public static void Save(VMChunk chunk, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        Save(chunk, stream);
    }

    /// <summary>
    /// Reads a standardized .rbc binary stream into a <see cref="VMChunk"/>.
    /// Validates magic bytes and version compatibility.
    /// </summary>
    public static VMChunk Load(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        uint magic = reader.ReadUInt32();
        if (magic != MagicSignature)
        {
            throw new InvalidDataException(
                $"Not a valid Raptor bytecode file. Expected magic 0x{MagicSignature:X8}, got 0x{magic:X8}."
            ); // I don't know whether to show the magic number or not
        }

        byte majorVersion = reader.ReadByte();
        byte minorVersion = reader.ReadByte();
        if (majorVersion > VersionMajor)
        {
            throw new InvalidDataException(
                $"Incompatible Raptor bytecode version {majorVersion}.{minorVersion}. "
                    + $"This runtime supports up to {VersionMajor}.{VersionMinor}."
            );
        }

        ushort reserved = reader.ReadUInt16(); // reserved flags (ignored)

        uint constantsCount = reader.ReadUInt32();
        uint methodTableCount = reader.ReadUInt32();
        uint instructionsCount = reader.ReadUInt32();

        double[] constants = new double[constantsCount];
        for (int i = 0; i < constantsCount; i++)
        {
            constants[i] = reader.ReadDouble();
        }

        uint[] methodTable = new uint[methodTableCount];
        for (int i = 0; i < methodTableCount; i++)
        {
            methodTable[i] = reader.ReadUInt32();
        }

        uint[] instructions = new uint[instructionsCount];
        for (int i = 0; i < instructionsCount; i++)
        {
            instructions[i] = reader.ReadUInt32();
        }

        // Construct VMChunk from loaded data
        VMChunk chunk = new VMChunk();
        chunk.Instructions = instructions;
        chunk.Constants = constants;
        chunk.MethodTable = methodTable;

        return chunk;
    }

    /// <summary>
    /// Reads a standardized .rbc binary file into a <see cref="VMChunk"/>.
    /// </summary>
    public static VMChunk Load(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Load(stream);
    }
}
