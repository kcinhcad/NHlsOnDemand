using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NHlsOnDemand.Model;
using NHlsOnDemand.Services;
using NHlsOnDemand.Services.Muxer;

namespace NHlsOnDemand.Controllers
{
    public class PlaylistController : Controller
    {
        private readonly CommonOptions _commonOptions;

        public PlaylistController(IOptions<CommonOptions> commonOptions)
        {
            _commonOptions = commonOptions.Value;
        }

        /// <summary>
        /// Получить token для просмотра hls по rtsp-адресу
        /// </summary>
        /// <param name="rtsp">RTSP адрес</param>
        /// <returns>Токен</returns>
        [HttpGet("token")]
        public async Task<IActionResult> GetToken(string rtsp)
        {
            var needAdd = false;
            var muxer = MuxerService.Instance.GetByRtsp(rtsp);
            if (muxer == null)
            {
                muxer = new Muxer(rtsp, _commonOptions.FilePath);
                needAdd = true;
            }
            if (await muxer.Start())
            {
                if (needAdd)
                    MuxerService.Instance.Add(muxer);
                return Ok(new TokenView {token = muxer.Token});
            }

            muxer.Dispose();
            return BadRequest();
        }

        /// <summary>
        /// Получить плейлист hls потока
        /// </summary>
        /// <param name="token">Token доступа к просмотру видео потока</param>
        /// <returns>Index-файл плейлиста</returns>
        [HttpGet("hls")]
        public async Task<IActionResult> GetPlaylist(Guid token)
        {
            var muxer = MuxerService.Instance.GetByToken(token);
            if (muxer == null)
                return NotFound();

            return File(await muxer.GetIndexFile(), "application/vnd.apple.mpegurl");
        }

        /// <summary>
        /// Получить chunk файл hls потока
        /// </summary>
        /// <param name="token">Token доступа к просмотру видео потока</param>
        /// <param name="name">Имя файла</param>
        /// <returns>Chunk-файл плейлиста</returns>
        [HttpGet("chunks/{token}/{name}")]
        public async Task<IActionResult> GetChunk(Guid token, string name)
        {
            var muxer = MuxerService.Instance.GetByToken(token);
            if (muxer == null)
                return NotFound();

            return File(await muxer.GetChunkFile(name), "video/MP2T");
        }
    }
}
