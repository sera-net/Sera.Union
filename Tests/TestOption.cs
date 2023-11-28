using System.Text.Json;

namespace Tests;

using OptionInt = Sera.TaggedUnion.Utilities.Option<int>;

public class TestOption
{
    [Test]
    public void TestJson1()
    {
        var o = new OptionInt(123);
        Console.WriteLine(o);
        var json = JsonSerializer.Serialize(o);
        Console.WriteLine(json);
        var r = JsonSerializer.Deserialize<OptionInt>(json);
        Console.WriteLine(r);
        Assert.That(r, Is.EqualTo(o));
    }
    
    [Test]
    public void TestJson2()
    {
        var o = new OptionInt();
        Console.WriteLine(o);
        var json = JsonSerializer.Serialize(o);
        Console.WriteLine(json);
        var r = JsonSerializer.Deserialize<OptionInt>(json);
        Console.WriteLine(r);
        Assert.That(r, Is.EqualTo(o));
    }
}
