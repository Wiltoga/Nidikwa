namespace Nidikwa.Service.Utilities.Tests;

[TestClass]
public class CacheStreamShould
{
    [TestMethod]
    public void StoreDataWithNoOverflow()
    {
        var expected = new byte[] { 1, 2, 3 };
        var cache = new CacheStream(new MemoryStream(), 5);
        cache.Write(expected);
        cache.Seek(0, SeekOrigin.Begin);
        var result = new byte[3];
        cache.Read(result);
        Assert.IsTrue(expected.SequenceEqual(result));
    }

    [TestMethod]
    public void FillDataWithNoOverflow()
    {
        var expected = new byte[] { 1, 2, 3, 4, 5 };
        var cache = new CacheStream(new MemoryStream(), 5);
        cache.Write(expected);
        cache.Seek(0, SeekOrigin.Begin);
        var result = new byte[5];
        cache.Read(result);
        Assert.IsTrue(expected.SequenceEqual(result));
    }

    [TestMethod]
    public void OverwriteDataWithNoOverflow()
    {
        var cache = new CacheStream(new MemoryStream(), 5);
        cache.Write(new byte[] { 1, 2, 3, 4, 5 });
        cache.Seek(0, SeekOrigin.Begin);
        cache.Write(new byte[] { 6, 7 });
        var result = new byte[5];
        cache.Seek(0, SeekOrigin.Begin);
        cache.Read(result);
        Assert.IsTrue(new byte[] { 6, 7, 3, 4, 5 }.SequenceEqual(result));
    }

    [TestMethod]
    public void FillDataWithSmallOverflow()
    {
        var cache = new CacheStream(new MemoryStream(), 5);
        cache.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        cache.Seek(0, SeekOrigin.Begin);
        var result = new byte[5];
        cache.Read(result);
        Assert.IsTrue(new byte[] { 4, 5, 6, 7, 8 }.SequenceEqual(result));
    }

    [TestMethod]
    public void OverwriteDataWithSmallOverflow()
    {
        var cache = new CacheStream(new MemoryStream(), 5);
        cache.Write(new byte[] { 1, 2, 3, 4 });
        cache.Seek(-1, SeekOrigin.End);
        cache.Write(new byte[] { 5, 6, 7 });
        var result = new byte[5];
        cache.Seek(0, SeekOrigin.Begin);
        cache.Read(result);
        Assert.IsTrue(new byte[] { 2, 3, 5, 6, 7 }.SequenceEqual(result));
    }

    [TestMethod]
    public void FillDataWithBigOverflow()
    {
        var cache = new CacheStream(new MemoryStream(), 5);
        cache.Write(Convert.FromBase64String("ZDRmZzg5NmJmZDVnZjZnaDdmc2RnNTRoOTg=").Concat(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }).ToArray());
        cache.Seek(0, SeekOrigin.Begin);
        var result = new byte[5];
        cache.Read(result);
        Assert.IsTrue(new byte[] { 4, 5, 6, 7, 8 }.SequenceEqual(result));
    }

    [TestMethod]
    public void OverwriteDataWithBigOverflow()
    {
        var cache = new CacheStream(new MemoryStream(), 5);
        cache.Write(new byte[] { 1, 2 });
        cache.Seek(-1, SeekOrigin.End);
        cache.Write(Convert.FromBase64String("ZDRmZzg5NmJmZDVnZjZnaDdmc2RnNTRoOTg=").Concat(new byte[] { 3, 4, 5, 6, 7 }).ToArray());
        var result = new byte[5];
        cache.Seek(0, SeekOrigin.Begin);
        cache.Read(result);
        Assert.IsTrue(new byte[] { 3, 4, 5, 6, 7 }.SequenceEqual(result));
    }
}