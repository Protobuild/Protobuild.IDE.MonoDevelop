//
// GtkPlugNetworkRequestLayer.cs
//
// Author:
//       James Rhodes <jrhodes@redpointsoftware.com.au>
//
// Copyright (c) 2015 James Rhodes
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MonoDevelop.Ide
{
	public class GtkPlugNetworkRequestLayer
	{
		private readonly TcpListener _listener;

		private TcpClient _client;

		private NetworkStream _stream;

		private object _clientLock = new object();

		public GtkPlugNetworkRequestLayer(TcpListener listener)
		{
			this._listener = listener;
			this._listener.Start();
		}

		public void Stop()
		{
			this._listener.Stop();
		}

		public void BlockUntilConnected()
		{
			Thread.Sleep(100);
			this._client = this._listener.AcceptTcpClient();
			this._stream = this._client.GetStream();
		}

		public string[] GetHandledFileExtensions()
		{
			var items = new List<string>();

			lock (this._clientLock)
			{
				byte[] bytes = new byte[1];
				this._stream.Write(new[] { (byte)'E' }, 0, 1);

				// Read the number of items returned.
				this._stream.Read(bytes, 0, 1);
				var itemCount = (int)bytes[0];
				for (var i = 0; i < itemCount; i++)
				{
					this._stream.Read(bytes, 0, 1);
					var strLength = (int)bytes[0];
					bytes = new byte[strLength];
					this._stream.Read(bytes, 0, strLength);
					items.Add(System.Text.Encoding.ASCII.GetString(bytes));
				}
			}

			return items.ToArray();
		}

		public byte[] Suspend()
		{
			lock (this._clientLock)
			{
				var bytes = new byte[1];
				this._stream.Write(new[] { (byte)'S' }, 0, 1);
				this._stream.Read(bytes, 0, 1);
				var lenBytes = new byte[sizeof(uint)];
				this._stream.Read(lenBytes, 0, lenBytes.Length);
				var length = BitConverter.ToUInt32(lenBytes, 0);
				if (length == UInt32.MaxValue)
				{
					return null;
				}
				var state = new byte[length];
				this._stream.Read(state, 0, state.Length);
				return state;
			}
		}

		public void Resume(uint socketID, byte[] state)
		{
			lock (this._clientLock)
			{
				var bytes = new byte[1];
				this._stream.Write(new[] { (byte)'R' }, 0, 1);
				var uintBytes = BitConverter.GetBytes(socketID);
				this._stream.Write(uintBytes, 0, uintBytes.Length);
				if (state == null)
				{
					var lenBytes = BitConverter.GetBytes(UInt32.MaxValue);
					this._stream.Write(lenBytes, 0, lenBytes.Length);
				}
				else
				{
					var lenBytes = BitConverter.GetBytes((UInt32)state.Length);
					this._stream.Write(lenBytes, 0, lenBytes.Length);
					this._stream.Write(state, 0, state.Length);
				}
				this._stream.Read(bytes, 0, 1);
			}
		}

		public void Load(string fileName)
		{
			lock (this._clientLock)
			{
				var bytes = new byte[1];
				this._stream.Write(new[] { (byte)'L' }, 0, 1);
				var stringLenBytes = BitConverter.GetBytes(fileName.Length);
				var stringBytes = Encoding.ASCII.GetBytes(fileName);
				this._stream.Write(stringLenBytes, 0, stringLenBytes.Length);
				this._stream.Write(stringBytes, 0, stringBytes.Length);
				this._stream.Read(bytes, 0, 1);
			}
		}

		public void Save(string fileName)
		{
			lock (this._clientLock)
			{
				var bytes = new byte[1];
				this._stream.Write(new[] { (byte)'A' }, 0, 1);
				var stringLenBytes = BitConverter.GetBytes(fileName.Length);
				var stringBytes = Encoding.ASCII.GetBytes(fileName);
				this._stream.Write(stringLenBytes, 0, stringLenBytes.Length);
				this._stream.Write(stringBytes, 0, stringBytes.Length);
				this._stream.Read(bytes, 0, 1);
			}
		}

		public Exception GetAndResetLastException()
		{
			lock (this._clientLock)
			{
				var bytes = new byte[1];
				this._stream.Write(new[] { (byte)'X' }, 0, 1);
				this._stream.Read(bytes, 0, 1);
				bytes = new byte[sizeof(int)];
				this._stream.Read(bytes, 0, bytes.Length);
				var len = BitConverter.ToInt32(bytes, 0);
				bytes = new byte[len];
				this._stream.Read(bytes, 0, bytes.Length);
				using (var stream = new MemoryStream(bytes))
				{
					var formatter = new BinaryFormatter();
					var obj = formatter.Deserialize(stream);
					if (obj == null)
					{
						return null;
					}
					else
					{
						return (Exception)obj;
					}
				}
			}
		}
	}
}

