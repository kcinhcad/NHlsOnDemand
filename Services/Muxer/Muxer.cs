using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NHlsOnDemand.Services.Muxer
{
    public class Muxer : IDisposable
    {
        private readonly string _filesPath;
        private DateTime _lastAccess;
        private Process _process;

        public Guid Token { get; }
        public string Rtsp { get; }
        public bool IsRun { get; private set; }

        public Muxer(string rtsp, string filePath)
        {
            IsRun = false;
            Rtsp = rtsp;
            Token = Guid.NewGuid();
            _lastAccess = DateTime.UtcNow;
            _filesPath = filePath;
        }

        public bool IsValid()
        {
            return _lastAccess > DateTime.UtcNow.AddSeconds(-60);
        }

        private string GetFilePath()
        {
            return _filesPath + Token + "\\";
        }

        public async Task<bool> Start()
        {
            if (IsRun)
                return true;

            var filePath = GetFilePath();
            var baseUrl = "chunks/" + Token + "/";

            Directory.CreateDirectory(filePath);

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-i \"" + Rtsp + "\" " +
                                "-fflags nobuffer " +
                                "-rtsp_transport " +
                                "-flags -y " +
                                "-hls_time 1 " +
                                "-hls_list_size 5 " +
                                "-hls_wrap 6 " +
                                "-start_number 0 " +
                                "-vcodec copy " +
                                "-hls_base_url \"" + baseUrl + "\" "+
                                "\"" + filePath + "index.m3u8\" ",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _process.Start();

            IsRun = await CheckChunkFile("index.m3u8", 30);
            
            return IsRun;
        }

        public void Stop()
        {
            _process?.Close();
            Thread.Sleep(1000);
            Directory.Delete(GetFilePath(), true);
            IsRun = false;
        }

        public async Task<byte[]> GetIndexFile()
        {
            _lastAccess = DateTime.UtcNow;

            string filename = Path.Combine(_filesPath, Token.ToString(), "index.m3u8");

            if (!File.Exists(filename))
                return null;

            return await GetBytesFromFile(filename);
        }

        public async Task<byte[]> GetChunkFile(string name)
        {
            _lastAccess = DateTime.UtcNow;

            var filename = Path.Combine(_filesPath, Token.ToString(), name);

            if (!File.Exists(filename))
                return null;

            return await GetBytesFromFile(filename);
        }

        public void Dispose()
        {
            Stop();
            _process?.Dispose();
        }

        private static async Task<byte[]> GetBytesFromFile(string fullFilePath)
        {
            FileStream fs = File.OpenRead(fullFilePath);
            try
            {
                byte[] bytes = new byte[fs.Length];
                await fs.ReadAsync(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
                return bytes;
            }
            finally
            {
                fs.Close();
            }
        }

        private Task<bool> CheckChunkFile(string filename, int timeoutInSec)
        {
            return Task.Run(() =>
            {
                var fullfilename = Path.Combine(GetFilePath(), filename);
                for (int i = 0; i < timeoutInSec * 20; i++)
                {
                    if (File.Exists(fullfilename))
                        return true;
                    Thread.Sleep(50);
                }
                return false;
            });
        }
    }
}
