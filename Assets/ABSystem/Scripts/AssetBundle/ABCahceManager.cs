using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tangzx.ABSystem
{
    public class ABCahceManager
    {
        struct CacheInfo
        {
            public string path;
            public byte[] data;
        }

        private readonly Queue<CacheInfo> cacheQueue = new Queue<CacheInfo>();
        private FileStream writerStream;
        private byte[] data;

        public void Cache(string path, byte[] data)
        {
            cacheQueue.Enqueue(new CacheInfo { path = path, data = data });
            CheckNext();
        }

        private void CheckNext()
        {
            if (writerStream == null && cacheQueue.Count > 0)
            {
                var cache = cacheQueue.Dequeue();
                data = cache.data;
                try
                {
                    var tempFile = cache.path + ".temp";
                    writerStream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Write);
                    writerStream.BeginWrite(data, 0, data.Length, ar =>
                    {
                        writerStream.EndWrite(ar);
                        writerStream.Close();
                        writerStream = null;
                        new FileInfo(tempFile).MoveTo(cache.path);
                        CheckNext();
                    }, writerStream);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    writerStream = null;
                    CheckNext();
                }
            }
        }
    }
}