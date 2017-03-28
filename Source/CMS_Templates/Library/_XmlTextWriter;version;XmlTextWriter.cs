using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrownPeak.CMSAPI;
using CrownPeak.CMSAPI.Services;
/* Some Namespaces are not allowed. */
namespace CrownPeak.CMSAPI.CustomLibrary
{

	public class XmlTextWriter
	{

		private System.Xml.XmlWriter _writer;
		private StringBuilder _sb;

		public XmlTextWriter(System.Xml.XmlWriterSettings settings)
		{
			if(settings == null) throw new ArgumentNullException("settings");
			_Initialize(settings);
		}
		public XmlTextWriter() : this(false) { }
		public XmlTextWriter(bool indented = true)
		{
			System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings()
			{
				ConformanceLevel = System.Xml.ConformanceLevel.Fragment
			};
			if(indented)
			{
				settings.Indent = true;
				settings.IndentChars = "\t";
			}
			_Initialize(settings);
		}
		private void _Initialize(System.Xml.XmlWriterSettings settings)
		{
			_sb = new StringBuilder();
			_writer = System.Xml.XmlWriter.Create(_sb, settings);
		}

		public override string ToString()
		{
			_writer.Flush();
			return _sb.ToString();
		}

		public bool Indented
		{
			set
			{
				if(value == true)
				{
					//_writer.Formatting = System.Xml.Formatting.Indented;
					//_writer.Indentation = 1;
					//_writer.IndentChar = '\t';
				}
				else
				{
					//_writer.Formatting = System.Xml.Formatting.None;
					//_writer.Indentation = 0;
				}
			}
		}

		public void WriteStartDocument()
		{
			_writer.WriteStartDocument();
		}
		public void WriteStartDocument(bool standalone)
		{
			_writer.WriteStartDocument(standalone);
		}
		public void WriteEndDocument()
		{
			_writer.WriteEndDocument();
		}
		public void WriteProcessingInstruction(string name, string text)
		{
			_writer.WriteProcessingInstruction(name, text);
		}
		public void WriteStartElement(string name)
		{
			_writer.WriteStartElement(name);
		}
		public void WriteEndElement()
		{
			_writer.WriteEndElement();
		}
		public void WriteAttributeString(string prefix, string localName, string ns, string value)
		{
			_writer.WriteAttributeString(prefix, localName, ns, value);
		}
		public void WriteAttributeString(string localName, string ns, string value)
		{
			_writer.WriteAttributeString(localName, ns, value);
		}
		public void WriteAttributeString(string name, string value)
		{
			_writer.WriteAttributeString(name, value);
		}
		public void WriteComment(string text)
		{
			_writer.WriteComment(text);
		}
		public void WriteCData(string value)
		{
			_writer.WriteCData(value);
		}
		public void WriteString(string value)
		{
			_writer.WriteString(value);
		}
		public void WriteRaw(string value)
		{
			_writer.WriteRaw(value);
		}
		public void WriteBase64(byte[] buffer, int index, int count)
		{
			_writer.WriteBase64(buffer, index, count);
		}

	}

}
