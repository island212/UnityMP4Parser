using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine.TestTools;

public class BigEndianByteWriterTests
{
    NativeArray<byte> Stream;

    [SetUp]
    public void SetUp()
    {
        Stream = new NativeArray<byte>(1024, Allocator.Temp);
    }

    [TearDown]
    public void TearDown()
    {
        Stream.Dispose();
    }

    [Test]
    public void ByteWriterExtensions_WithValidEndianess_ReturnBigEndianByteWriter()
    {
        var writer = Stream.AsByteWriter(Endianess.Big);

        Assert.IsInstanceOf<BigEndianByteWriter>(writer);
    }

    [Test]
    public void ByteWriter_WithArray_ReturnGoodCapacity()
    {
        var writer = Stream.AsByteWriter(Endianess.Big);

        Assert.IsInstanceOf<BigEndianByteWriter>(writer);
    }

    [Test]
    public unsafe void SingleWrite_WithValidValue_ReturnGoodLength()
    {
        var ptr = (byte*)Stream.GetUnsafePtr();
        var writer = Stream.AsByteWriter(Endianess.Big);

        var random = new Random(12345);

        var value1 = (byte)random.NextUInt();
        writer.WriteUInt8(value1);
        Assert.AreEqual(1, writer.Length);
        Assert.AreEqual(value1, Stream[0]);
        writer.Clear();

        var value2 = (sbyte)random.NextUInt();
        writer.WriteInt8(value2);
        Assert.AreEqual(1, writer.Length);
        Assert.AreEqual(value2, (sbyte)Stream[0]);
        writer.Clear();

        var value3 = (ushort)random.NextUInt();
        writer.WriteUInt16(value3);
        Assert.AreEqual(2, writer.Length);
        Assert.AreEqual(value3, BigEndian.GetUInt16(ptr));
        writer.Clear();

        var value4 = (short)random.NextUInt();
        writer.WriteInt16(value4);
        Assert.AreEqual(2, writer.Length);
        Assert.AreEqual(value4, BigEndian.GetInt16(ptr));
        writer.Clear();

        var value5 = (uint)random.NextUInt();
        writer.WriteUInt32(value5);
        Assert.AreEqual(4, writer.Length);
        Assert.AreEqual(value5, BigEndian.GetUInt32(ptr));
        writer.Clear();

        var value6 = (int)random.NextUInt();
        writer.WriteInt32(value6);
        Assert.AreEqual(4, writer.Length);
        Assert.AreEqual(value6, BigEndian.GetInt32(ptr));
        writer.Clear();

        var value7 = (ulong)random.NextUInt() << 32 | random.NextUInt();
        writer.WriteUInt64(value7);
        Assert.AreEqual(8, writer.Length);
        Assert.AreEqual(value7, BigEndian.GetUInt64(ptr));
        writer.Clear();

        var value8 = (long)random.NextUInt() << 32 | random.NextUInt();
        writer.WriteInt64(value8);
        Assert.AreEqual(8, writer.Length);
        Assert.AreEqual(value8, BigEndian.GetInt64(ptr));
        writer.Clear();
    }
}
