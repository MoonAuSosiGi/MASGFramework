#define ENABLE_ERRORLOG
#define ENABLE_WARNINGLOG
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

public class ByteData
{
	private byte[] m_src = null;
	private int m_start = 0;
	private int m_end = 0;

	public int GetLength()
	{
		return m_end - m_start;
	}

	public ByteData(byte[] src, int start, int end)
	{
		m_src = src;
		m_start = start;
		m_end = end;
	}

	private static StringBuilder s_decode_builder = new StringBuilder();
	public string DecodeJsonString()
	{
//		s_decode_builder.Clear();
		s_decode_builder.Remove(0, s_decode_builder.Length);

		for(int i=m_start;i<m_end;i++)
		{
			var c = (char)m_src[i];
			if(c == '\\')
			{
				if(i + 1 >= m_end)
				{
#if ENABLE_ERRORLOG
					JSONReadOnly.LogError("ByteData error, DecodeJsonString, c2 over, pos="+i+", "+DecodeSimpleString());
#endif
					break;
				}

				i++;
				var c2 = (char)m_src[i];
				if(c2 == 'r')
				{
				}
				else if(c2 == 'n')
				{
					s_decode_builder.Append('\n');
				}
				else if(c2 == 't')
				{
					s_decode_builder.Append('\t');
				}
				else if(c2 == '"')
				{
					s_decode_builder.Append('"');
				}
				else if(c2 == 'u' || c2 == 'U')
				{
					if(i + 4 >= m_end)
					{
#if ENABLE_ERRORLOG
						JSONReadOnly.LogError("ByteData error, DecodeJsonString, c4 over, pos="+i+", "+DecodeSimpleString());
#endif
						break;
					}

					var c4 = Hex4ToChar(m_src[i+1], m_src[i+2], m_src[i+3], m_src[i+4]);
					i += 4;

					s_decode_builder.Append(c4);
				}
				else
				{
#if ENABLE_ERRORLOG
					JSONReadOnly.LogError("ByteData error, DecodeJsonString, pos="+i+", "+DecodeSimpleString());
#endif
					s_decode_builder.Append(c+c2);
				}
			}
			else
			{
				s_decode_builder.Append(c);
			}
		}

		var text = s_decode_builder.ToString();

//		s_decode_builder.Clear();
		s_decode_builder.Remove(0, s_decode_builder.Length);

		return text;
	}

	public static char Hex4ToChar(params byte[] hex)
	{
		int i = 0;
		foreach(var b in hex)
		{
			i *= 16;
			if(b == '0') { continue; }
			else if(b == '1') { i += 1; }
			else if(b == '2') { i += 2; }
			else if(b == '3') { i += 3; }
			else if(b == '4') { i += 4; }
			else if(b == '5') { i += 5; }
			else if(b == '6') { i += 6; }
			else if(b == '7') { i += 7; }
			else if(b == '8') { i += 8; }
			else if(b == '9') { i += 9; }
			else if(b == 'a') { i += 10; }
			else if(b == 'b') { i += 11; }
			else if(b == 'c') { i += 12; }
			else if(b == 'd') { i += 13; }
			else if(b == 'e') { i += 14; }
			else if(b == 'f') { i += 15; }
			else if(b == 'A') { i += 10; }
			else if(b == 'B') { i += 11; }
			else if(b == 'C') { i += 12; }
			else if(b == 'D') { i += 13; }
			else if(b == 'E') { i += 14; }
			else if(b == 'F') { i += 15; }
			else
			{
#if ENABLE_ERRORLOG
				JSONReadOnly.LogError("ByteData error, Hex4ToChar, unknown c="+((char)b));
#endif
			}
		}
		var ret = (char)i;
		return ret;
	}

	public string DecodeSimpleString()
	{
		string text = "";
		for(int i=m_start;i<m_end;i++)
		{
			text += (char)m_src[i];
		}
		return text;
	}

	public bool IsSame(string text)
	{
		string s = DecodeSimpleString();
		return (s == text);
	}
	
	public bool TryParse(out int v)
	{
		string s = DecodeSimpleString();
		if(int.TryParse(s, out v))
		{
			return true;
		}
		v = 0;
		return false;
	}

	public bool TryParse(out long v)
	{
		string s = DecodeSimpleString();
		if(long.TryParse(s, out v))
		{
			return true;
		}
		v = 0;
		return false;
	}

	public bool TryParse(out float v)
	{
		string s = DecodeSimpleString();
		if(float.TryParse(s, out v))
		{
			return true;
		}
		v = 0;
		return false;
	}

	public bool TryParse(out double v)
	{
		string s = DecodeSimpleString();
		if(double.TryParse(s, out v))
		{
			return true;
		}
		v = 0;
		return false;
	}
}

public class JSONReadOnly
{
	public enum PARSING_STATE { NONE, BEGIN_OBJECT, BEGIN_ARRAY, };
	public enum Type { ARRAY, OBJECT, STRING, VALUE, EMPTY };
	//-------------------------------------
	// static function
	//-------------------------------------
#if ENABLE_ERRORLOG
	public static void LogError(string msg)
	{
		Debug.Log("JSONReadOnly error: "+msg);
	}
#endif

	public static JSONReadOnly obj
	{
		get
		{
			var j = new JSONReadOnly();
			j.UseObject();
			return j;
		}
	}

	public static JSONReadOnly arr
	{
		get
		{
			var j = new JSONReadOnly();
			j.UseArray();
			return j;
		}
	}

#if false
	public string raw
	{
		get
		{
			if (m_type == Type.STRING)
			{
				return m_val.DecodeJsonString();
			}
			else if (m_type == Type.VALUE)
			{
				return m_val.DecodeJsonString();
			}
			else if(m_type == Type.EMPTY || m_type == Type.OBJECT || m_type == Type.ARRAY)
			{
				return "";
			}

#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("get/str m_val is null, type=" + m_type);
#endif
			return "";
		}
	}
#endif

	// 2012-01-01
	private static DateTime DefaultDate { get { return new DateTime(2012, 1, 1); } }

	// 20140830
	public static string SafeDate8(System.DateTime d)
	{
		if (d == null)
			return DefaultDate.ToString("yyyyMMdd");
		return d.ToString("yyyyMMdd");
	}

	public static DateTime SafeDateParse(string str)
	{
		DateTime ret;
		if (DateTime.TryParse(str, out ret))
			return ret;
		return DefaultDate;
	}
	
	private static int ReadStringValue(byte[] text, int pos, out ByteData str)
	{
		str = null;

		int pos_begin = pos;
		int pos_end = pos;
		bool ignore = false;
		for(; pos < text.Length; pos++)
		{
			if(ignore)
			{
				ignore = false;
				continue;
			}
			var c = text[pos];
			if(c == '\\')
			{
				ignore = true;
				continue;
			}
			if(c == '\"')
			{
				pos_end = pos;
				pos++;
				break;
			}
		}

		str = new ByteData(text, pos_begin, pos_end);
		
		return pos;
	}

	private static int ReadNotStringValue(byte[] text, int pos, out ByteData str)
	{
		str = null;
		int pos_begin = pos;
		int pos_end = pos;
		for(; pos < text.Length; pos++)
		{
			var c = text[pos];
			if(c == ' ' || c == ',' ||
				c == '{' || c == '}' ||
				c == '[' || c == ']' ||
				c <= 32)
			{
				pos_end = pos;
				break;
			}
		}

		str = new ByteData(text, pos_begin, pos_end);

		return pos;
	}

	//-------------------------------------
	// member
	//------------------------------------
	private Dictionary<string, JSONReadOnly> m_dic = null;
	private List<JSONReadOnly> m_arr = null;
	private ByteData m_val = null;
	private Type m_type = Type.EMPTY;

	//-------------------------------------
	// method
	//-------------------------------------
	public JSONReadOnly()
	{
	}

	public JSONReadOnly(byte[] text)
	{
		if(text == null)
		{
#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("JSONReadOnly init, text is null");
#endif
			return;
		}

		if(text.Length <= 0)
		{
#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("JSONReadOnly init, text length <= 0");
#endif
			return;
		}

		int pos = 0;
		for(; pos < text.Length; pos++)
		{
			var c = text[pos];
			if(		c == '{' 
				||	c == '['
				||	c == '"'
				||	c == '.'
				||	c == '0'
				||	c == '1'
				||	c == '2'
				||	c == '3'
				||	c == '4'
				||	c == '5'
				||	c == '6'
				||	c == '7'
				||	c == '8'
				||	c == '9'
				)
			{
				break;
			}
		}
		
		Parsing(text, ref pos);
	}

	public JSONReadOnly(byte[] text, ref int pos)
	{
		Parsing(text, ref pos);
	}

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		Print(sb, 0, false);
		return sb.ToString();
	}

	public string ToStringReadable()
	{
		StringBuilder sb = new StringBuilder();
		Print(sb, 0, true);
		return sb.ToString();
	}

	//-------------------------------------
	// method (private)
	//-------------------------------------
	private JSONReadOnly GetItem(string key)
	{
		if(m_type != Type.OBJECT)
		{
#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("GetItem(string) is not object, type=" + m_type + ", key=" + key);
#endif
			return new JSONReadOnly();
		}

		if(!m_dic.ContainsKey(key))
		{
#if ENABLE_ERRORLOG			
			JSONReadOnly.LogError("GetItem(string) is not exsits key, key=" + key);
#endif
			return new JSONReadOnly();
		}

		return m_dic[key];
	}

	private void Parsing(byte[] text, ref int pos)
	{
		if(!EatSpace(text, ref pos))
		{
#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("Parsing fail, EatSpace 1, "+pos+", "+text.Length+", text="+text);
#endif
			return;
		}

		if(text[pos] == '}')
		{
			pos++;
			return;
		}

		if(text[pos] == ']')
		{
			pos++;
			return;
		}

		if(text[pos] == '{')
		{
		PARSING_CLASS:
			pos++;
			if(!EatSpace(text, ref pos))
			{
#if ENABLE_ERRORLOG
				JSONReadOnly.LogError("Parsing fail, class EatSpace 1, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			m_type = Type.OBJECT;
			if(m_dic == null)
				m_dic = new Dictionary<string, JSONReadOnly>();

			if(text[pos] == '}')
			{
				pos++;
				EatSpace(text, ref pos);
				return;
			}

			ByteData name;
			pos = ReadName(text, pos, out name);
			if(!EatSpace(text, ref pos))
			{
#if ENABLE_ERRORLOG
				JSONReadOnly.LogError("Parsing fail, class EatSpace 2, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			if(text[pos] != ':')
			{
#if ENABLE_ERRORLOG				
				JSONReadOnly.LogError("':' is not exists");
#endif
				pos++;
				return;
			}

			pos++;
			if(!EatSpace(text, ref pos))
			{
#if ENABLE_ERRORLOG
				JSONReadOnly.LogError("Parsing fail, class EatSpace 3, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			JSONReadOnly obj = new JSONReadOnly();
			obj.Parsing(text, ref pos);
			m_dic[name.DecodeSimpleString()] = obj;

			if(!EatSpace(text, ref pos))
			{
#if ENABLE_ERRORLOG
				JSONReadOnly.LogError("Parsing fail, class EatSpace 4, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			if(text[pos] == ',')
			{
				goto PARSING_CLASS;
			}
			else if(text[pos] == '}')
			{
				pos++;
			}
		}
		else if(text[pos] == '[')
		{
		PARSING_ARRAY:
			pos++;
			if(!EatSpace(text, ref pos))
			{
#if ENABLE_ERRORLOG
				JSONReadOnly.LogError("Parsing fail, array EatSpace 1, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			m_type = Type.ARRAY;
			if(m_arr == null)
				m_arr = new List<JSONReadOnly>();

			if(text[pos] == ']')
			{
				pos++;
				EatSpace(text, ref pos);
				return;
			}

			JSONReadOnly obj = new JSONReadOnly();
			obj.Parsing(text, ref pos);
			m_arr.Add(obj);

			if(text[pos] == ',')
			{
				goto PARSING_ARRAY;
			}
			else if(text[pos] == ']')
			{
				pos++;
			}
		}
		else if(text[pos] == '\"')
		{
			pos++;

			m_type = Type.STRING;

			pos = ReadStringValue(text, pos, out m_val);

#if ENABLE_ERRORLOG
			if(m_val == null)
			{
				int a = 0;
				a = 0;
			}
#endif
		}
		else
		{
			m_type = Type.VALUE;
			pos = ReadNotStringValue(text, pos, out m_val);
		}

		EatSpace(text, ref pos);
	}

	private bool EatSpace(byte[] text, ref int pos)
	{
		for(; pos < text.Length; pos++)
		{
			var c = text[pos];
			if(c <= 32)
				continue;
			break;
		}

		if(text.Length <= pos)
		{
			return false;
		}

		return true;
	}

	private int ReadName(byte[] text, int pos, out ByteData name)
	{
		name = null;
		for(; pos < text.Length; pos++)
		{
			var c = text[pos];
			if(c != '\"')
				continue;
			pos++;
			pos = ReadStringValue(text, pos, out name);
			break;
		}
		return pos;
	}

	private void Print(StringBuilder sb, int depth, bool readable)
	{
		if(m_type == Type.OBJECT)
		{
			sb.Append("{");
			PrintLine(sb, depth, readable);

			int count = 0;
			foreach(var kv in m_dic)
			{
				PrintTab(sb, depth + 1, readable);
#if false
				sb.AppendFormat("\"{0}\":", kv.Key);
#else
				sb.Append("\"");
				sb.Append(kv.Key);
				sb.Append("\"");
				sb.Append(":");
#endif
				if(readable) sb.Append(" ");
				kv.Value.Print(sb, depth + 1, readable);
				count++;
				if(count < m_dic.Count)
				{
					sb.Append(",");
				}
				if(readable) sb.Append("\n");
			}

			PrintTab(sb, depth + 1, readable);
			sb.Append("}");
		}
		else if(m_type == Type.ARRAY)
		{
			sb.Append("[");
			PrintLine(sb, depth, readable);

			int count = 0;
			foreach(var v in m_arr)
			{
				PrintTab(sb, depth + 2, readable);
				v.Print(sb, depth + 1, readable);
				count++;
				if(count < m_arr.Count)
				{
					sb.Append(",");
				}
				if(readable) sb.Append("\n");
			}

			PrintTab(sb, depth + 1, readable);
			sb.Append("]");
		}
		else if(m_type == Type.STRING)
		{
			sb.Append("\"");
			sb.Append(m_val.DecodeSimpleString());
			sb.Append("\"");
		}
		else
		{
			sb.Append(m_val.DecodeSimpleString());
		}
	}

	private void PrintTab(StringBuilder sb, int depth, bool readable)
	{
		if(readable) for(int i = 0; i < depth; i++) { sb.Append("  "); }
	}

	private void PrintLine(StringBuilder sb, int depth, bool readable)
	{
		if(readable) sb.Append("\n");
	}


	// helper
	public Type type { get { return m_type; } }
	public bool IsExists(string key)
	{
		if(m_type == Type.OBJECT)
		{
			if(m_dic.ContainsKey(key))
			{
				return true;
			}
		}

		return false;
	}

	public List<string> GetAllKey()
	{
		List<string> list = new List<string> ();
		if (m_dic != null)
		{
			foreach(var kv in m_dic)
			{
				list.Add(kv.Key);
			}
		}
		return list;
	}

	public JSONReadOnly GetField(string key)
	{
		if(m_type != Type.OBJECT)
		{
#if ENABLE_WARNINGLOG			
			Debug.LogWarning("GetField: not exists list, key=" + key);
#endif
			return null;
		}

		if(!m_dic.ContainsKey(key))
		{
#if ENABLE_WARNINGLOG			
			Debug.LogWarning("GetField: not exists key, key=" + key + ", allkeys=" + LogAllKeys());
#endif
			return new JSONReadOnly();
		}

		return m_dic[key];
	}

	public JSONReadOnly GetFieldOrNull(string key)
	{
		if(m_dic == null)
			return null;

		if(!m_dic.ContainsKey(key))
			return null;

		return m_dic[key];
	}

	private string LogAllKeys()
	{
		if(m_dic == null)
			return "nokeys";

		string keys = "";
		foreach(var i in m_dic)
		{
			keys += i.Key + ",";
		}
		return "[" + keys + "]";
	}

	private void UseObject()
	{
		if(m_type == Type.OBJECT)
			return;
		m_type = Type.OBJECT;
		m_dic = new Dictionary<string, JSONReadOnly>();
		m_arr = null;
		m_val = null;
	}

	private void UseArray()
	{
		if(m_type == Type.ARRAY)
			return;
		m_type = Type.ARRAY;
		m_dic = null;
		m_arr = new List<JSONReadOnly>();
		m_val = null;
	}

	public List<JSONReadOnly> list
	{
		get
		{
			if(m_type == Type.ARRAY)
			{
				return m_arr;
			}
			else if(m_type == Type.OBJECT)
			{
#if ENABLE_WARNINGLOG && false			
				Debug.LogWarning("get/list is not array, type=" + type);
#endif
				List<JSONReadOnly> arr = new List<JSONReadOnly>();
				foreach(var i in m_dic.Values)
				{
					arr.Add(i);
				}
				return arr;
			}

#if ENABLE_ERRORLOG && false
			if(type == Type.VALUE)
			{
				JSONReadOnly.LogError("get/list is not array, type=VALUE, value=" + m_val);
			}
			else
			{
				JSONReadOnly.LogError("get/list is not array, type=" + type);
			}
#endif
			return new List<JSONReadOnly>();
		}
	}

	public bool safeBool
	{
		get
		{
			if(m_type == Type.VALUE || m_type == Type.STRING)
			{
				string low = m_val.DecodeJsonString().ToLower();
				if(low == "true")
					return true;
				if(low == "false")
					return false;

				int n = 0;
				if(m_val.TryParse(out n))
					return (n != 0);
				if(m_val.GetLength() > 0)
					return true;
			}
			else if(m_type == Type.EMPTY || m_type == Type.OBJECT || m_type == Type.ARRAY)
			{
				return false;
			}

#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("get/b is not value, type="+m_type);
#endif
			return false;
		}
	}

	public int safeInt
	{
		get
		{
			if(m_type == Type.VALUE || m_type == Type.STRING)
			{
				int v;
				if(m_val.TryParse(out v))
					return v;
				float f;
				if(m_val.TryParse(out f))
					return (int)f;
				if(m_val.IsSame("true"))
					return 1;
				if(m_val.IsSame("false"))
					return 0;
			}
			else if(m_type == Type.EMPTY|| m_type == Type.OBJECT || m_type == Type.ARRAY)
			{
				return 0;
			}

#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("get/i is not value, type="+m_type);
#endif
			return 0;
		}
	}

	public long safeLong
	{
		get
		{
			if(m_type == Type.VALUE || m_type == Type.STRING)
			{
				long v;
				if(m_val.TryParse(out v))
					return v;
			}
			else if(m_type == Type.EMPTY|| m_type == Type.OBJECT || m_type == Type.ARRAY)
			{
				return 0;
			}

#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("get/l is not value, type="+m_type);
#endif
			return 0;
		}
	}

	public float safeFloat
	{
		get
		{
			if(m_type == Type.VALUE || m_type == Type.STRING)
			{
				float v;
				if(m_val.TryParse(out v))
					return v;
			}
			else if(m_type == Type.EMPTY|| m_type == Type.OBJECT || m_type == Type.ARRAY)
			{
				return 0;
			}

#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("get/f is not value, type="+m_type);
#endif
			return 0;
		}
	}

	public double safeDouble
	{
		get
		{
			if(m_type == Type.VALUE || m_type == Type.STRING)
			{
				double v;
				if(m_val.TryParse(out v))
					return v;
			}
			else if(m_type == Type.EMPTY || m_type == Type.OBJECT || m_type == Type.ARRAY)
			{
				return 0;
			}

#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("get/double is not value, type="+m_type);
#endif
			return 0;
		}
	}

	public string safeStr
	{
		get
		{
			if(m_type == Type.STRING)
			{
				return m_val.DecodeJsonString();
			}
			else if(m_type == Type.VALUE)
			{
				if(m_val.IsSame("null"))
					return "";
				return m_val.DecodeJsonString();
			}
			else if(m_type == Type.EMPTY || m_type == Type.OBJECT || m_type == Type.ARRAY)
			{
				return "";
			}

#if ENABLE_ERRORLOG
			JSONReadOnly.LogError("get/str m_val is null, type=" + m_type);
#endif
			return "";
		}
	}

	public JSONReadOnly this[int index]
	{
		get
		{
			if(m_type != Type.ARRAY)
			{
#if ENABLE_ERRORLOG
				JSONReadOnly.LogError("this[int index] is not array, index="+index);
#endif
				return new JSONReadOnly();
			}

			if(m_arr.Count <= index)
			{
#if ENABLE_ERRORLOG
				JSONReadOnly.LogError("this[int index] is index over, count="+m_arr.Count+", index=" + index);
#endif
				return new JSONReadOnly();
			}

			return m_arr[index];
		}
	}

	public JSONReadOnly this[string index]
	{
		get { return GetItem(index); }
	}

	public List<JSONReadOnly> GetList(string name)
	{
		return this[name].list;
	}

}