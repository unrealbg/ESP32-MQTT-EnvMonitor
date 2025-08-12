using System.Security.Cryptography;
using System.Text.Json;

// Usage:
// OtaPackager <inputDir> <version> <outputDir> [--main=App.pe] [--base-url=https://host/path/] [--include=App.pe;Lib.pe]

if (args.Length < 3)
{
    Console.WriteLine("Usage: OtaPackager <inputDir> <version> <outputDir> [--main=App.pe] [--base-url=https://host/path/] [--include=App.pe;Lib.pe]");
    return;
}

var inputDir = Path.GetFullPath(args[0]);
var version = args[1];
var outputDir = Path.GetFullPath(args[2]);
string mainName = "App.pe";
string baseUrl = string.Empty;
string[] includeOnly = Array.Empty<string>();

for (int i = 3; i < args.Length; i++)
{
    var a = args[i];
    if (a.StartsWith("--main=")) mainName = a.Substring("--main=".Length);
    else if (a.StartsWith("--base-url=")) baseUrl = a.Substring("--base-url=".Length);
    else if (a.StartsWith("--include=")) includeOnly = a.Substring("--include=".Length).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
}

if (!Directory.Exists(inputDir))
{
    Console.WriteLine($"Input dir not found: {inputDir}");
    return;
}

Directory.CreateDirectory(outputDir);

var files = Directory.GetFiles(inputDir, "*.pe", SearchOption.TopDirectoryOnly);
if (files.Length == 0)
{
    Console.WriteLine("No .pe files in input dir.");
    return;
}

bool IsFrameworkPe(string fileName)
{
    // Exclude mscorlib, System.* and nanoFramework.* by default
    if (string.Equals(fileName, "mscorlib.pe", StringComparison.OrdinalIgnoreCase)) return true;
    if (fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) return true;
    if (fileName.StartsWith("nanoFramework.", StringComparison.OrdinalIgnoreCase)) return true;
    if (fileName.StartsWith("nanoframework.", StringComparison.OrdinalIgnoreCase)) return true;
    return false;
}

bool ShouldInclude(string fileName)
{
    if (includeOnly.Length > 0)
    {
        foreach (var n in includeOnly)
        {
            if (string.Equals(n.Trim(), fileName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    return !IsFrameworkPe(fileName);
}

string ToHexLower(byte[] b)
{
    var c = new char[b.Length * 2];
    int i = 0;
    foreach (var by in b)
    {
        c[i++] = GetHex(by >> 4);
        c[i++] = GetHex(by & 0xF);
    }
    return new string(c);
    static char GetHex(int n) => (char)(n < 10 ? '0' + n : 'a' + (n - 10));
}

if (!string.IsNullOrEmpty(baseUrl) && !baseUrl.EndsWith("/")) baseUrl += "/";

var items = new List<object>();
int copied = 0;
var hashesTxt = new System.Text.StringBuilder();

foreach (var f in files)
{
    var name = Path.GetFileName(f);
    if (!ShouldInclude(name))
    {
        Console.WriteLine($"Skipping framework PE: {name}");
        continue;
    }

    var dest = Path.Combine(outputDir, name);
    File.Copy(f, dest, true);
    copied++;

    using var stream = File.OpenRead(f);
    using var sha = SHA256.Create();
    var hash = sha.ComputeHash(stream);
    var hex = ToHexLower(hash);

    var url = string.IsNullOrEmpty(baseUrl) ? name : (baseUrl + name);
    items.Add(new { name, url, sha256 = hex });

    hashesTxt.AppendLine($"{name}  sha256={hex}  bytes={new FileInfo(f).Length}");
}

if (items.Count == 0)
{
    Console.WriteLine("No eligible .pe files to package. Use --include to force explicit list.");
    return;
}

// Ensure main entry is last in the manifest files order
items = items.OrderBy(i => string.Equals(((dynamic)i).name, mainName, StringComparison.OrdinalIgnoreCase) ? 1 : 0).ToList();

var manifest = new { version, files = items };
var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(Path.Combine(outputDir, "manifest.json"), json);

// Write a helper file for quick comparison on target logs
await File.WriteAllTextAsync(Path.Combine(outputDir, "HASHES.txt"), hashesTxt.ToString());

Console.WriteLine($"Copied {copied} file(s). Packaged {items.Count} item(s) to {outputDir}. Upload its contents to your server.");
Console.WriteLine(string.IsNullOrEmpty(baseUrl)
    ? "manifest.json uses file names as URLs; serve this folder or edit to full URLs."
    : $"manifest.json URLs are prefixed with: {baseUrl}");
Console.WriteLine("Hashes summary written to HASHES.txt");
