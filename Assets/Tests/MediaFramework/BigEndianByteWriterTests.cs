using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.MediaFramework.LowLevel;
using Unity.MediaFramework.LowLevel.Unsafe;
using UnityEngine.TestTools;

public class BigEndianByteWriterTests
{
    const int StreamSize = 1024;

    NativeArray<byte> Stream;

    [SetUp]
    public void SetUp()
    {
        Stream = new NativeArray<byte>(StreamSize, Allocator.Temp);
    }

    [TearDown]
    public void TearDown()
    {
        Stream.Dispose();
    }

    [Test]
    public unsafe void ByteWriter_SingleWrite_ReturnGoodLengthAndData()
    {
        var writer = Stream.AsByteWriter(Endianess.Big);

        using var numBuilder = new NativeArray<byte>(8, Allocator.Temp);
        var numBuilderPtr = (byte*)numBuilder.GetUnsafePtr();

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
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value3);

        writer.WriteUInt16(value3);
        Assert.AreEqual(2, writer.Length);
        for (int i = 0; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        writer.Clear();

        var value4 = (short)random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value4);

        writer.WriteInt16(value4);
        Assert.AreEqual(2, writer.Length);
        for (int i = 0; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        writer.Clear();

        var value5 = (uint)random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value5);

        writer.WriteUInt32(value5);
        Assert.AreEqual(4, writer.Length);
        for (int i = 0; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        writer.Clear();

        var value6 = (int)random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value6);

        writer.WriteInt32(value6);
        Assert.AreEqual(4, writer.Length);
        for (int i = 0; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        writer.Clear();

        var value7 = (ulong)random.NextUInt() << 32 | random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value7);

        writer.WriteUInt64(value7);
        Assert.AreEqual(8, writer.Length);
        for (int i = 0; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        writer.Clear();

        var value8 = (long)random.NextUInt() << 32 | random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value8);

        writer.WriteInt64(value8);
        Assert.AreEqual(8, writer.Length);
        for (int i = 0; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        writer.Clear();

        writer.WriteBytes(0, 9);
        Assert.AreEqual(9, writer.Length);
        for (int i = 0; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], 0);

        writer.Clear();

        for (int i = 0; i < 13; i++)
            numBuilderPtr[i] = (byte)i;

        writer.WriteBytes(numBuilderPtr, 13);
        Assert.AreEqual(13, writer.Length);
        for (int i = 0; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilderPtr[i]);

        writer.Clear();
    }

    [Test]
    public unsafe void ByteWriter_MultipleWrite_ReturnGoodLengthAndData()
    {
        var writer = Stream.AsByteWriter(Endianess.Big);

        using var numBuilder = new NativeArray<byte>(8, Allocator.Temp);
        var numBuilderPtr = numBuilder.GetUnsafePtr();

        var random = new Random(43256);

        int i = 0;

        var value1 = (byte)random.NextUInt();
        writer.WriteUInt8(value1);
        Assert.AreEqual(i + 1, writer.Length);
        Assert.AreEqual(value1, Stream[i++]);

        var value8 = (long)random.NextUInt() << 32 | random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value8);

        writer.WriteInt64(value8);
        Assert.AreEqual(i + 8, writer.Length);
        for (; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        var value4 = (short)random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value4);

        writer.WriteInt16(value4);
        Assert.AreEqual(i + 2, writer.Length);
        for (; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        writer.WriteBytes(0, 9);
        Assert.AreEqual(9, writer.Length);
        for (; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], 0);

        var value5 = (uint)random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value5);

        writer.WriteUInt32(value5);
        Assert.AreEqual(i + 4, writer.Length);
        for (; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        var value3 = (ushort)random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value3);

        writer.WriteUInt16(value3);
        Assert.AreEqual(i + 2, writer.Length);
        for (; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        var value2 = (sbyte)random.NextUInt();
        writer.WriteInt8(value2);
        Assert.AreEqual(i + 1, writer.Length);
        Assert.AreEqual(value2, (sbyte)Stream[i++]);

        var value7 = (ulong)random.NextUInt() << 32 | random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value7);

        writer.WriteUInt64(value7);
        Assert.AreEqual(i + 8, writer.Length);
        for (; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        var value6 = (int)random.NextUInt();
        UnsafeUtility.WriteArrayElement(numBuilderPtr, 0, value6);

        writer.WriteInt32(value6);
        Assert.AreEqual(i + 4, writer.Length);
        for (; i < writer.Length; i++)
            Assert.AreEqual(Stream[i], numBuilder[writer.Length - 1 - i]);

        writer.Clear();
    }

    [Test]
    public void ByteWriterExtensions_WithValidEndianess_ReturnValidBigEndianByteWriter()
    {
        var writer = Stream.AsByteWriter(Endianess.Big);

        Assert.IsInstanceOf<BigEndianByteWriter>(writer);
        Assert.AreEqual(writer.Length, 0);
        Assert.AreEqual(writer.Capacity, StreamSize);
    }
}
