using System;
using System.Collections.Generic;
using UnityEngine;
using XdParser;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class XdAssetHolder
    {
        private readonly XdFile _xdFile;
        private readonly Dictionary<string, XdStyleFillPatternMetaJson> _fileNameToMeta;
        private readonly Dictionary<string, byte[]> _fileNameToBytes;
        private readonly Dictionary<uint, CachedSvg> _svgHash;

        public XdAssetHolder(XdFile xdFile)
        {
            _xdFile = xdFile;
            _fileNameToMeta = new Dictionary<string, XdStyleFillPatternMetaJson>();
            _fileNameToBytes = new Dictionary<string, byte[]>();
            _svgHash = new Dictionary<uint, CachedSvg>();
        }

        public byte[] Load(string fileName)
        {
            if (_fileNameToMeta.ContainsKey(fileName))
            {
                var meta = _fileNameToMeta[fileName];
                return _xdFile.GetResource(meta);
            }

            if (_fileNameToBytes.ContainsKey(fileName))
            {
                return _fileNameToBytes[fileName];
            }

            throw new Exception($"Unknown asset {fileName}");
        }

        public void Save(string key, XdStyleFillPatternMetaJson meta)
        {
            _fileNameToMeta[key] = meta;
        }

        public void Save(string key, byte[] value)
        {
            _fileNameToBytes[key] = value;
        }

        public CachedSvg GetCachedSvg(uint svgHash)
        {
            if (!_svgHash.ContainsKey(svgHash)) return null;
            return _svgHash[svgHash];
        }

        public void SaveCacheSvg(string spriteUid, uint svgHash)
        {
            _svgHash[svgHash] = new CachedSvg(spriteUid);
        }
    }

    public class CachedSvg
    {
        public string SpriteUid { get; }

        public CachedSvg(string spriteUid)
        {
            SpriteUid = spriteUid;
        }
    }
}