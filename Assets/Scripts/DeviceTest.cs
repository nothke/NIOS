using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System;

using NIOS;

public class DeviceTest : MonoBehaviour, IDevice
{
    Guid guid;
    public Guid Guid => guid;

    public NIOS.DeviceType DeviceType => NIOS.DeviceType.Keyboard;

    void Start()
    {
        stream = new ReadStream(Encoding.ASCII);
    }

    void Update()
    {
        stream.Type(Input.inputString);
    }

    ReadStream stream;

    public Stream OpenRead() => stream;
    public Stream OpenWrite() => throw new NotImplementedException();

    class ReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        string str;
        public override long Length => str.Length;

        public override long Position
        {
            get => 0;
            set => throw new NotImplementedException();
        }

        Encoding encoding;

        public ReadStream(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public void Type(string str)
        {
            this.str += str;
        }

        public override void Flush()
        {
            str = string.Empty;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            while (str.Length == 0)
            {
            }

            var maxByteLen = encoding.GetByteCount(str);
            if (count > maxByteLen) count = maxByteLen;

            var p = encoding.GetBytes(str, 0, count, buffer, offset);

            //DEBUG
            //var c = pendingTextToRead.Substring(0, p);
            //Debug.Log("reading " + c + " = " + string.Join(",", c.Select(x => ((int)x).ToString()).ToArray()));

            str = str.Substring(p);
            return p;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }
}
