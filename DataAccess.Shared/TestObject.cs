using System.Text.Json;

namespace DataAccess.Shared; 

public class TestObject {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<int> List { get; set; } = new List<int>();

    public static bool TryParse(string json, out TestObject? testObject) {
        testObject = JsonSerializer.Deserialize<TestObject>(json);
        return testObject is not null;
    }
}