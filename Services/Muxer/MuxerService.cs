using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NHlsOnDemand.Services.Muxer
{
    public class MuxerService
    {
        private static readonly Lazy<MuxerService> Service = new Lazy<MuxerService>(() => new MuxerService());
        public static MuxerService Instance => Service.Value;

        private readonly List<Muxer> _muxerList = new List<Muxer>();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private MuxerService()
        {
            ThreadPool.QueueUserWorkItem(x => Check(_tokenSource.Token));
        }

        public void Add(Muxer muxer)
        {
            _muxerList.Add(muxer);
        }

        public Muxer GetByToken(Guid token)
        {
            return _muxerList.FirstOrDefault(m => m.Token == token);
        }

        public Muxer GetByRtsp(string rtsp)
        {
            return _muxerList.FirstOrDefault(m => m.Rtsp == rtsp);
        }

        private void Check(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var muxer in _muxerList)
                    {
                        if (!muxer.IsValid())
                        {
                            muxer.Stop();
                        }
                    }
                    _muxerList.RemoveAll(m => !m.IsRun);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    //log
                    var t = e;
                }

                Thread.Sleep(10000);
            }
        }
    }
}
