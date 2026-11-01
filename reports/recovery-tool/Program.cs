using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

var root = Path.GetFullPath(args[0]);
var records = new List<object>();
byte[] bundleSignature = Convert.FromHexString("8b1202b96a612038727b930214d7a03213f5b9e6efae3318ee3b2dce24b36aae");
string[] markers = ["PyInstaller", "_MEIPASS", "pyi-runtime-tmpdir", "Nuitka", "go.buildid", "runtime.goexit", "rustc", "UPX!", "DOTNET_BUNDLE_EXTRACT_BASE_DIR", "hostfxr.dll", "nethost", "BSJB"];
foreach(var path in Directory.EnumerateFiles(root,"*", SearchOption.AllDirectories).Where(p => p.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".dll",StringComparison.OrdinalIgnoreCase))) {
 var bytes = File.ReadAllBytes(path);
 var record = new Dictionary<string,object?> { ["file"] = Path.GetRelativePath(root,path).Replace('\\','/'), ["size"] = bytes.Length, ["sha256"] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant() };
 try {
 using var pe = new PEReader(new MemoryStream(bytes));
 record["machine"] = pe.PEHeaders.CoffHeader.Machine.ToString();
 record["managed"] = pe.HasMetadata;
 record["subsystem"] = pe.PEHeaders.PEHeader?.Subsystem.ToString();
 record["corFlags"] = pe.PEHeaders.CorHeader?.Flags.ToString();
 record["sections"] = pe.PEHeaders.SectionHeaders.Select(s=>s.Name).ToArray();
 var rawEnd = pe.PEHeaders.SectionHeaders.Select(s=>(long)s.PointerToRawData+s.SizeOfRawData).DefaultIfEmpty(0).Max();
 record["overlayBytes"] = bytes.LongLength - rawEnd;
 if(pe.HasMetadata){
 var md = pe.GetMetadataReader();
 var asm = md.GetAssemblyDefinition();
 record["assemblyName"] = md.GetString(asm.Name);
 record["assemblyVersion"] = asm.Version.ToString();
 record["metadataVersion"] = md.MetadataVersion;
 record["typeCount"] = md.TypeDefinitions.Count;
 record["assemblyReferences"] = md.AssemblyReferences.Select(h=>md.GetString(md.GetAssemblyReference(h).Name)).ToArray();
 record["resources"] = md.ManifestResources.Select(h=>md.GetString(md.GetManifestResource(h).Name)).ToArray();
 }
 record["markers"] = markers.Where(m=>bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes(m))>=0).ToArray();
 int sigOffset=bytes.AsSpan().IndexOf(bundleSignature);
 record["bundleSignatureOffset"] = sigOffset>=0 ? sigOffset : null;
 if(sigOffset>=8){
 long headerOffset=BitConverter.ToInt64(bytes,sigOffset-8);
 record["bundleHeaderOffset"] = headerOffset;
 if(headerOffset>0 && headerOffset<bytes.LongLength){
 using var br = new BinaryReader(new MemoryStream(bytes));
 br.BaseStream.Position=headerOffset;
 var major=br.ReadUInt32(); var minor=br.ReadUInt32(); var count=br.ReadInt32(); var id=br.ReadString();
 record["bundleVersion"] = $"{major}.{minor}"; record["bundleFileCount"]=count;
 if(major>=2){br.ReadInt64();br.ReadInt64();br.ReadInt64();br.ReadInt64();br.ReadUInt64();}
 var bundledFiles=new List<object>();
 for(int i=0;i<count && i<10000;i++){
 var offset=br.ReadInt64(); var size=br.ReadInt64(); long compressedSize=major>=6?br.ReadInt64():0; var type=br.ReadByte(); var name=br.ReadString();
 bundledFiles.Add(new { name,offset,size,compressedSize,type });
 }
 record["bundledFiles"]=bundledFiles;
 }
 }
 }catch(Exception e){record["error"] = e.Message;}
 records.Add(record);
}
File.WriteAllText(args[1],JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Inventoried {records.Count} PE candidates");

