using NUnit.Framework;
using Unity.MediaFramework.Format.ISOBMFF;

public static class AssertISOBox
{
    public static void AreEqual(in FileTypeBox expected, in FileTypeBox actual)
    {
        Assert.AreEqual(expected.MajorBrand, actual.MajorBrand);
        Assert.AreEqual(expected.MinorVersion, actual.MinorVersion);
        Assert.AreEqual(expected.BrandCount, actual.BrandCount);
        Assert.AreEqual(expected.Brand0, actual.Brand0);
        Assert.AreEqual(expected.Brand1, actual.Brand1);
        Assert.AreEqual(expected.Brand2, actual.Brand2);
        Assert.AreEqual(expected.Brand3, actual.Brand3);
        Assert.AreEqual(expected.Brand4, actual.Brand4);
    }

    public static void AreEqual(in MovieHeaderBox expected, in MovieHeaderBox actual)
    { 
        Assert.AreEqual(expected.Version, actual.Version);
        Assert.AreEqual(expected.CreationTime, actual.CreationTime);
        Assert.AreEqual(expected.ModificationTime, actual.ModificationTime);
        Assert.AreEqual(expected.Timescale, actual.Timescale);
        Assert.AreEqual(expected.Duration, actual.Duration);
        Assert.AreEqual(expected.Rate.value, actual.Rate.value);
        Assert.AreEqual(expected.Volume.value, actual.Volume.value);
        Assert.AreEqual(expected.Matrix.value, actual.Matrix.value);
        Assert.AreEqual(expected.NextTrackID, actual.NextTrackID);
    }

    public static void AreEqual(in TrackHeaderBox expected, in TrackHeaderBox actual)
    {
        Assert.AreEqual(expected.Version, actual.Version);
        Assert.AreEqual(expected.CreationTime, actual.CreationTime);
        Assert.AreEqual(expected.ModificationTime, actual.ModificationTime);
        Assert.AreEqual(expected.TrackID, actual.TrackID);
        Assert.AreEqual(expected.Duration, actual.Duration);
        Assert.AreEqual(expected.Layer, actual.Layer);
        Assert.AreEqual(expected.AlternateGroup, actual.AlternateGroup);
        Assert.AreEqual(expected.Volume.value, actual.Volume.value);
        Assert.AreEqual(expected.Matrix.value, actual.Matrix.value);
        Assert.AreEqual(expected.Width.value, actual.Width.value);
        Assert.AreEqual(expected.Height.value, actual.Height.value);
    }

    public static void AreEqual(in MediaHeaderBox expected, in MediaHeaderBox actual)
    {
        Assert.AreEqual(expected.Version, actual.Version);
        Assert.AreEqual(expected.CreationTime, actual.CreationTime);
        Assert.AreEqual(expected.ModificationTime, actual.ModificationTime);
        Assert.AreEqual(expected.Timescale, actual.Timescale);
        Assert.AreEqual(expected.Duration, actual.Duration);
        Assert.AreEqual(expected.Language.value, actual.Language.value);
    }

    public static void AreEqual(in HandlerBox expected, in HandlerBox actual)
    {
        Assert.AreEqual(expected.Handler, actual.Handler);
        Assert.AreEqual(expected.Name, actual.Name);
    }
}
