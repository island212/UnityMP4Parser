using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class RefVsInVsImmutableIn
{
    // A Test behaves as an ordinary method
    [Test, Performance]
    public void RefVsInVsImmutableInTest()
    {
        var values = new NativeArray<BigValue>(100000, Allocator.Temp);
        var values2 = new NativeArray<BigValueImmutable>(100000, Allocator.Temp);

        Measure.Method(() =>
        {
            BigValue sum;
            for (int i = 1; i < values.Length; i++)
            {
                sum = BigValue.Sum(values[i - 1], values[i]);
            }
        })
        .SampleGroup("BigValue.Sum")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            BigValue sum;
            for (int i = 1; i < values.Length; i++)
            {
                sum = BigValue.SumIn(values[i - 1], values[i]);
            }
        })
        .SampleGroup("BigValue.SumIn")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            BigValue sum;
            BigValue ref1;
            BigValue ref2;
            for (int i = 1; i < values.Length; i++)
            {
                ref1 = values[i - 1];
                ref2 = values[i];
                sum = BigValue.SumRef(ref ref1, ref ref2);
            }
        })
        .SampleGroup("BigValue.SumRef")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            BigValueImmutable sum;
            for (int i = 1; i < values.Length; i++)
            {
                sum = BigValueImmutable.Sum(values2[i - 1], values2[i]);
            }
        })
        .SampleGroup("BigValueImmutable.Sum")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            BigValueImmutable sum;
            for (int i = 1; i < values.Length; i++)
            {
                sum = BigValueImmutable.SumIn(values2[i - 1], values2[i]);
            }
        })
        .SampleGroup("BigValueImmutable.SumIn")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            BigValueImmutable sum;
            BigValueImmutable ref1;
            BigValueImmutable ref2;
            for (int i = 1; i < values.Length; i++)
            {
                ref1 = values2[i - 1];
                ref2 = values2[i];
                sum = BigValueImmutable.SumRef(ref ref1, ref ref2);
            }
        })
        .SampleGroup("BigValueImmutable.SumRef")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        values.Dispose();
    }

    [Test, Performance]
    public void RefVsOutVsReturn()
    {
        var values = new NativeArray<BigValue>(100000, Allocator.Temp);

        Measure.Method(() =>
        {
            BigValue ref1;
            BigValue ref2;
            BigValue sum = new BigValue();
            for (int i = 1; i < values.Length; i++)
            {
                ref1 = values[i - 1];
                ref2 = values[i];
                BigValue.SumReturnRef(ref ref1, ref ref2, ref sum);
            }
        })
        .SampleGroup("BigValue.SumReturnRef")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            BigValue ref1;
            BigValue ref2;
            BigValue sum = new BigValue();
            for (int i = 1; i < values.Length; i++)
            {
                ref1 = values[i - 1];
                ref2 = values[i];
                BigValue.SumReturnOut(ref ref1, ref ref2, out sum);
            }
        })
        .SampleGroup("BigValue.SumReturnOut")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();

        Measure.Method(() =>
        {
            BigValue sum;
            for (int i = 1; i < values.Length; i++)
            {
                sum = BigValue.Sum(values[i - 1], values[i]);
            }
        })
        .SampleGroup("BigValue.Sum")
        .WarmupCount(5)
        .IterationsPerMeasurement(10)
        .MeasurementCount(20)
        .Run();
    }

    public struct BigValue
    {
        public int data0;
        public int data1;
        public int data2;
        public int data3;

        public BigValue(int data0, int data1, int data2, int data3)
        {
            this.data0 = data0;
            this.data1 = data1;
            this.data2 = data2;
            this.data3 = data3;
        }

        public static void SumReturnRef(ref BigValue value1, ref BigValue value2, ref BigValue result)
            => new BigValue(value1.data0 + value2.data0, value1.data1 + value2.data1, value1.data2 + value2.data2, value1.data3 + value2.data3);

        public static void SumReturnOut(ref BigValue value1, ref BigValue value2, out BigValue result)
        {
            result = new BigValue(value1.data0 + value2.data0, value1.data1 + value2.data1, value1.data2 + value2.data2, value1.data3 + value2.data3);
        }

        public static BigValue SumRef(ref BigValue value1, ref BigValue value2)
            => new BigValue(value1.data0 + value2.data0, value1.data1 + value2.data1, value1.data2 + value2.data2, value1.data3 + value2.data3);

        public static BigValue SumIn(in BigValue value1, in BigValue value2)
            => new BigValue(value1.data0 + value2.data0, value1.data1 + value2.data1, value1.data2 + value2.data2, value1.data3 + value2.data3);

        public static BigValue Sum(BigValue value1, BigValue value2)
            => new BigValue(value1.data0 + value2.data0, value1.data1 + value2.data1, value1.data2 + value2.data2, value1.data3 + value2.data3);
    }

    public readonly struct BigValueImmutable
    {
        public readonly int data0;
        public readonly int data1;
        public readonly int data2;
        public readonly int data3;

        public BigValueImmutable(int data0, int data1, int data2, int data3)
        {
            this.data0 = data0;
            this.data1 = data1;
            this.data2 = data2;
            this.data3 = data3;
        }

        public static BigValueImmutable SumRef(ref BigValueImmutable value1, ref BigValueImmutable value2) 
            => new BigValueImmutable(value1.data0 + value2.data0, value1.data1 + value2.data1, value1.data2 + value2.data2, value1.data3 + value2.data3);

        public static BigValueImmutable SumIn(in BigValueImmutable value1, in BigValueImmutable value2)
            => new BigValueImmutable(value1.data0 + value2.data0, value1.data1 + value2.data1, value1.data2 + value2.data2, value1.data3 + value2.data3);

        public static BigValueImmutable Sum(BigValueImmutable value1, BigValueImmutable value2)
            => new BigValueImmutable(value1.data0 + value2.data0, value1.data1 + value2.data1, value1.data2 + value2.data2, value1.data3 + value2.data3);
    }
}
