using System.Diagnostics;
using FFmpeg.NET;
using NAudio.Wave;

namespace Trascribition;

public class Converter
{
    public string GenerateLPCM(string file1, string file2, string fileName)
    {
        using var firstAudio = new AudioFileReader(file1);
        using var secondAudio = new AudioFileReader(file2);

        if (firstAudio.TotalTime > TimeSpan.FromMinutes(60))
        {
            return $"{Path.GetFileName(file1).Split('-')[0]} слишком длинный";
        }

        if (secondAudio.TotalTime > TimeSpan.FromMinutes(60))
        {
            return $"{Path.GetFileName(file2).Split('-')[0]} слишком длинный";
        }
        
        var waveProvider = new MultiplexingWaveProvider(new IWaveProvider[] { firstAudio, secondAudio }, 2);
        
        waveProvider.ConnectInputToOutput(0,0);
        waveProvider.ConnectInputToOutput(1,1);
        
        
        WaveFileWriter.CreateWaveFile16(fileName, waveProvider.ToSampleProvider());

        return string.Empty;
    }
    
    public async Task<string> GenerateLPCMUnix(string file1, string file2, string fileName)
    {
        string command1 = @$"ffmpeg -i {file1} -i {file2} -filter_complex ""[0][1]amerge=inputs=2,pan=stereo|FL<c0+c1|FR<c2+c3[a]"" -map ""[a]"" {fileName}";
        string result = "";
        string error = command1 + Environment.NewLine;

        var script =
            @$"Examples{Path.DirectorySeparatorChar}script.sh";
        
        var command = "sh";
        var myBatchFile = script;
            var argss = $"{myBatchFile} {file1} {file2} {fileName}"; //this would become "/home/ubuntu/psa/PdfGeneration/ApacheFolder/ApacheFOP/transform.sh /home/ubuntu/psa/PdfGeneration/ApacheFolder/XMLFolder/test.xml /home/ubuntu/psa/PdfGeneration/ApacheFolder/XSLTFolder/Certificate.xsl /home/ubuntu/psa/PdfGeneration/ApacheFolder/PDFFolder/test.pdf"

        var processInfo = new ProcessStartInfo();
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardOutput = true;
        processInfo.FileName = command;   // 'sh' for bash 
        processInfo.Arguments = argss;    // The Script name 

        var process = Process.Start(processInfo);   // Start that process.
        await process.WaitForExitAsync();
        error = await process.StandardOutput.ReadToEndAsync();
        /*
        error += "path:"+ Path.GetDirectoryName(fileName) + Environment.NewLine +"_____________"+ Environment.NewLine;
        
        using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
        {
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(fileName);
            proc.StartInfo.FileName = "/bin/bash";
            proc.StartInfo.Arguments = "-c \" " + "ls" + " \"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();

            error += await proc.StandardOutput.ReadToEndAsync();
            error += "LS:";
            error += await proc.StandardError.ReadToEndAsync() + Environment.NewLine;
            error += "++++++++++++" +Environment.NewLine;

            await proc.WaitForExitAsync();
        }

        using (System.Diagnostics.Process proc = new System.Diagnostics.Process())
        {
            //proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(fileName);
            proc.StartInfo.FileName = "/bin/bash";
            //proc.StartInfo.Arguments = "-c \" " + command + " \"";
            //proc.StartInfo.FileName = command;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = false;
            proc.Start();

            await proc.StandardInput.WriteLineAsync("date");
            
            if (!proc.WaitForExit(30_000))
                proc.Kill();

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            ,
            error += await proc.StandardOutput.ReadToEndAsync();
            error += await proc.StandardError.ReadToEndAsync();

            await proc.WaitForExitAsync();
        }*/
        return error;
    }
}