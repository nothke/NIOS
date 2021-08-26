using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

using NIOS.StdLib;

namespace NIOS.Unity
{
	public class TextDisplayDevice : NeitriBehavior, IDevice, ITerminal
	{
		public Text textComponent;
		public DeviceType DeviceType { get { return DeviceType.Display; } }
		Encoding Encoding { get { return Encoding.ASCII; } }

		MyWriteStream myWriteStream;

		StdLib.Ecma48.Device device;
		StdLib.Ecma48.Client client;
		ulong deviceLastDataVersion;

		Guid guid;
		public Guid Guid { get { return guid; } }

        public StdLib.Ecma48.Device Device => device;

        protected override void Start()
		{
			base.Start();

			guid = Utils.IntToGuid(GetInstanceID());

			if (textComponent == null) textComponent = GetComponentInChildren<Text>(); ;

			myWriteStream = new MyWriteStream(this);

			TextGenerator textGen = new TextGenerator();
			TextGenerationSettings generationSettings = textComponent.GetGenerationSettings(textComponent.rectTransform.rect.size);

			var areaHeight = textComponent.rectTransform.rect.height;
			float textHeight = textGen.GetPreferredHeight("M", generationSettings);
			var maxNumberOfLines = (uint)Mathf.FloorToInt(areaHeight / textHeight);

			var areaWidth = textComponent.rectTransform.rect.width;
			float textWidth = textGen.GetPreferredWidth("M", generationSettings);
			var maxNumberOfColumns = (uint)Mathf.FloorToInt(areaWidth / textWidth);

			device = new StdLib.Ecma48.Device(maxNumberOfColumns, maxNumberOfLines);
			var writer = new StreamWriter(OpenWrite());
			writer.AutoFlush = true;
			client = new StdLib.Ecma48.Client(writer);

		}


		protected override void Update()
		{
			base.Update();

			DisplayUpdate();
		}

		DateTime lastSignalReceived = DateTime.MinValue;
		DateTime lastUpdate = DateTime.MinValue;
		//bool noMessageReceivedPrinted = false;

		void DisplayUpdate()
		{
			// TODO: Temporarily replaced World with DateTime

			const int timeoutSeconds = 60;
			if (lastSignalReceived.IsOver(seconds: timeoutSeconds).InPastComparedTo(DateTime.UtcNow))
			{
				if (lastUpdate.IsOver(seconds: 1).InPastComparedTo(DateTime.UtcNow))
				{
					lastUpdate = DateTime.UtcNow;
					var backupLastSingalReceived = lastSignalReceived;

					client.EraseDisplay();
					client.WriteLine();
					client.WriteLine();
					if (backupLastSingalReceived == DateTime.MinValue)
						client.WriteLine("no signal received");
					else
						client.WriteLine("last signal received " + DateTime.UtcNow.Subtract(backupLastSingalReceived).TotalSeconds.Round().ToInt() + " seconds ago");
					client.WriteLine("debug info:");
					client.WriteLine("	current time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
					client.WriteLine("	columns: " + device.ColumnsCount);
					client.WriteLine("	rows: " + device.RowsCount);
					client.WriteLine();
					client.WriteLine();

					lastSignalReceived = backupLastSingalReceived;
				}
			}


			myWriteStream.UnityUpdate();
			UpdateTextArea();
		}


		void UpdateTextArea()
		{
			if (device.DataVersion == deviceLastDataVersion) return;
			deviceLastDataVersion = device.DataVersion;

			var defaultForegroundColor = StdLib.Ecma48.Color.White;
			var lastForegroundColor = defaultForegroundColor;

			//var lastBold = false;
			//var shouldEndBoldElement = false;

			var sb = new StringBuilder();
			for (uint row = 0; row < device.RowsCount; row++)
			{
				for (uint column = 0; column < device.ColumnsCount; column++)
				{
					var c = device[column, row];
					if (lastForegroundColor != c.foregroundColor)
					{
						/*
						// buggy
						if (shouldEndBoldElement)
						{
							sb.Append("</b>");
							shouldEndBoldElement = false;
							lastBold = false;
						}
						if (c.bold)
						{
							sb.Append("<b>");
							shouldEndBoldElement = true;
							lastBold = true;
						}
						*/
						if (lastForegroundColor != defaultForegroundColor)
						{
							sb.Append("</color>");
							lastForegroundColor = defaultForegroundColor;
						}
						if (c.foregroundColor != defaultForegroundColor)
						{
							sb.Append("<color=" + c.foregroundColor.ToString().ToLower() + ">");
							lastForegroundColor = c.foregroundColor;
						}
					}
					sb.Append(c.character);
				}
				sb.AppendLine();
			}

			if (lastForegroundColor != defaultForegroundColor)
			{
				sb.Append("</color>");
				lastForegroundColor = defaultForegroundColor;
			}

			textComponent.text = sb.ToString();
		}


		class MyWriteStream : Stream
		{
			string pendingWriteText = string.Empty;
			TextDisplayDevice p;

			public override bool CanRead { get { return false; } }

			public override bool CanSeek { get { return false; } }

			public override bool CanWrite { get { return true; } }

			public override long Length { get { return long.MaxValue; } }

			public override long Position { get { return pendingWriteText.Length; } set { throw new NotImplementedException(); } }

			public MyWriteStream(TextDisplayDevice p)
			{
				this.p = p;
			}

			public void UnityUpdate()
			{
				if (pendingWriteText.Length > 0)
				{
					p.device.Parse(pendingWriteText);
					pendingWriteText = string.Empty;
				}
			}

			public override void Flush()
			{
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				p.lastSignalReceived = DateTime.UtcNow; // TODO: Replaced World with DateTime
				var dst = new byte[count];
				Array.Copy(buffer, offset, dst, 0, count);
				pendingWriteText += p.Encoding.GetString(dst);
			}
		}

		public Stream OpenRead()
		{
			return Stream.Null;
		}

		public Stream OpenWrite()
		{
			return myWriteStream;
		}


	}
}