namespace EyeAuras.Web.Repl.Component.AI;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
public class Check
{
    private static readonly string[] RequiredFiles = { "capcha.onnx", "fish.onnx", "orangestones.onnx" };
    private const string AiZipUrl = "https://files.eyesquad.net/GTA5/AI.zip";
    private const string TempZipFileName = "AI_temp.zip";

    public async Task<(bool success, string capcha, string fish, string orangestones)> CheckOrDownloadAsync(string path)
    {
        Directory.CreateDirectory(path);  

        
        bool allFilesExist = RequiredFiles.All(file => File.Exists(Path.Combine(path, file)));

        if (!allFilesExist)
        {
            await DownloadAndExtract(path);  
            
            allFilesExist = RequiredFiles.All(file => File.Exists(Path.Combine(path, file)));
        }
    
        return (
            success: allFilesExist,
            capcha: Path.Combine(path, "capcha.onnx"),
            fish: Path.Combine(path, "fish.onnx"),
            orangestones: Path.Combine(path, "orangestones.onnx")
        );
    }


    private async Task DownloadAndExtract(string path)
    {
        var tempZipPath = Path.Combine(path, TempZipFileName);
        using (var httpClient = new HttpClient())
        {
            
            using (var response = await httpClient.GetAsync(AiZipUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                
                using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }
                
                ZipFile.ExtractToDirectory(tempZipPath, path);
            }
        }
        
        
        if (File.Exists(tempZipPath))
        {
            File.Delete(tempZipPath);
        }
    }

}