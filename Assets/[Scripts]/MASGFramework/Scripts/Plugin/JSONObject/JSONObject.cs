//#define ENABLE_ERRORLOG
//#define ENABLE_WARNINGLOG
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

public class JSONObject
{
	public enum PARSING_STATE { NONE, BEGIN_OBJECT, BEGIN_ARRAY, };
	public enum Type { ARRAY, OBJECT, STRING, VALUE, EMPTY };
	//-------------------------------------
	// static function
	//-------------------------------------
	public static JSONObject obj
	{
		get
		{
			var j = new JSONObject();
			j.UseObject();
			return j;
		}
	}

	public static JSONObject arr
	{
		get
		{
			var j = new JSONObject();
			j.UseArray();
			return j;
		}
	}

	public bool b
	{
		get
		{
			if(m_type == Type.VALUE ||
				m_type == Type.STRING)
			{
				string low = m_val.ToLower();
				if(low == "true")
					return true;
				if(low == "false")
					return false;

				int n = 0;
				if(int.TryParse(m_val, out n))
					return (n != 0);
				if(m_val.Length > 0)
					return true;
			}
			else if(m_type == Type.EMPTY)
			{
				return false;
			}

#if ENABLE_ERRORLOG
			Debug.LogError("get/b is not value, type="+m_type);
#endif
			return false;
		}
	}

	public int i
	{
		get
		{
			if(m_type == Type.VALUE ||
				m_type == Type.STRING)
			{
				int v;
				if(int.TryParse(m_val, out v))
					return v;
				float f;
				if(float.TryParse(m_val, out f))
					return (int)f;
				if(m_val == "true")
					return 1;
				if(m_val == "false")
					return 0;
			}
			else if(m_type == Type.EMPTY)
			{
				return 0;
			}

#if ENABLE_ERRORLOG
			Debug.LogError("get/i is not value, type="+m_type);
#endif
			return 0;
		}
		set
		{
			m_type 	= Type.VALUE;
			m_val	= value.ToString();	
		}
	}

	public long l
	{
		get
		{
			if(m_type == Type.VALUE ||
				m_type == Type.STRING)
			{
				long v;
				if(long.TryParse(m_val, out v))
					return v;
			}
			else if(m_type == Type.EMPTY)
			{
				return 0;
			}

#if ENABLE_ERRORLOG
			Debug.LogError("get/l is not value, type="+m_type);
#endif
			return 0;
		}
		set
		{
			m_type 	= Type.VALUE;
			m_val	= value.ToString();	
		}
	}

	public float f
	{
		get
		{
			if(m_type == Type.VALUE ||
				m_type == Type.STRING)
			{
				float v;
				if(float.TryParse(m_val, out v))
					return v;
			}
			else if(m_type == Type.EMPTY)
			{
				return 0;
			}

#if ENABLE_ERRORLOG
			Debug.LogError("get/f is not value, type="+m_type);
#endif
			return 0;
		}
		set
		{
			m_type 	= Type.VALUE;
			m_val	= value.ToString();	
		}
	}

	public double n
	{
		get
		{
			if(m_type == Type.VALUE ||
				m_type == Type.STRING)
			{
				double v;
				if(double.TryParse(m_val, out v))
					return v;
			}
			else if(m_type == Type.EMPTY)
			{
				return 0;
			}

#if ENABLE_ERRORLOG
			Debug.LogError("get/n is not value, type="+m_type);
#endif
			return 0;
		}
		set
		{
			m_type 	= Type.VALUE;
			m_val	= value.ToString();	
		}
	}

	public string str
	{
		get
		{
			if(m_type == Type.STRING)
			{
				if(m_val.IndexOf('\\') >= 0)
				{
					return DecodeJsonEscape(m_val);
				}
				return m_val;
			}
			else if(m_type == Type.VALUE)
			{
				if(m_val == "null")
					return "";
				return m_val;
			}
			else if(m_type == Type.EMPTY)
			{
				return "";
			}

#if ENABLE_ERRORLOG
			Debug.LogError("get/str m_val is null, type=" + m_type);
#endif
			return "";
		}
		set
		{
			m_type 	= Type.STRING;
			m_val	= value.ToString();	
		}
	}

	public string raw
	{
		get
		{
			if (m_type == Type.STRING)
			{
				return m_val;
			}
			else if (m_type == Type.VALUE)
			{
				return m_val;
			}
			else if (m_type == Type.EMPTY)
			{
				return "";
			}

#if ENABLE_ERRORLOG
			Debug.LogError("get/str m_val is null, type=" + m_type);
#endif
			return "";
		}
		set
		{
			m_type = Type.STRING;
			m_val = value.ToString();
		}
	}

	// 2012-01-01
	public static DateTime DefaultDate { get { return new DateTime(2012, 1, 1); } }

	// safe
	public static DateTime SafeDateParse(string str)
	{
		DateTime ret;
		if (DateTime.TryParse(str, out ret))
			return ret;
		return DefaultDate;
	}
	
	// char 를 \uXXXX 로 변환
	public static string EncodeJsonEscape(string input)
	{
		if(input == null)
		{
//			Debug.LogError("EncodeJsonEscape: input is null");
			return "";
		}
		
		bool found_special_text = false;
		for(int i=0; i<input.Length; i++)
		{
			char c = input[i];
			if(c > 0x007f)
			{
				found_special_text = true;
				break;
			}
            else if(c == '\r')
			{
				found_special_text = true;
				break;
			}
			else if (c == '\n')
			{
				found_special_text = true;
				break;
			}
			else if (c == '\t')
			{
				found_special_text = true;
				break;
			}
			else if (c == '\\')
			{
				found_special_text = true;
				break;
			}
			else if (c == '"')
			{
				found_special_text = true;
				break;
			}
		}

		if (!found_special_text)
			return input;
		
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for(int i=0; i<input.Length; i++)
		{
			char c = input[i];
			if(c > 0x007f)
			{
				sb.Append(string.Format("\\u{0:x4}", (int)c));
			}
			else if(c == '\r')
			{
			}
			else if(c == '\n')
			{
				sb.Append("\\n");
			}
			else if(c == '"')
			{
				sb.Append("\\\"");
			}
			else if(c == '\\')
			{
				sb.Append("\\\\");
			}
			else
			{
				sb.Append(c);
			}
		}
		
		return sb.ToString();
	}
		
	// \uXXXX 를 char 로 변환
	public static string DecodeJsonEscape(string input)
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for(int i = 0; i < input.Length; i++)
		{
			if(input[i] == '\\')
			{
				int left = input.Length - i - 1;
				if(left < 1)
				{
#if ENABLE_ERRORLOG
					Debug.LogError("DecodeJsonEscape: left char too short, left="+left);
#endif
				}
				else
				{
					char cmd = input[i + 1];
					if(cmd == 'u' && left >= 5) // uXXXX 5글자.
					{
						string hex = input.Substring(i + 2, 4);
						string shex = ((char)int.Parse(hex, System.Globalization.NumberStyles.HexNumber)).ToString();
						sb.Append(shex);
						i += 5;
					}
					else if(cmd == 'n')
					{
						sb.Append('\n');
						i += 1;
					}
					else if(cmd == 't')
					{
						sb.Append('\t');
						i += 1;
					}
					else if(cmd == 'r')
					{
						i += 1;
						continue;
					}
					else if(cmd == '"')
					{
						sb.Append(cmd);
						i += 1;
					}
					else if(cmd == '\\')
					{
						sb.Append(cmd);
						i += 1;
					}
					else
					{
#if ENABLE_ERRORLOG
						Debug.LogError("DecodeJsonEscape: unknown escape cmd="+cmd+", left="+left);
#endif
						sb.Append(cmd);
						i += 1;
					}
				}
			}
			else
			{
				sb.Append(input[i]);
			}
		}
		return sb.ToString();
	}

	private static int ReadStringValue(string text, int pos, out string str)
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
			char c = text[pos];
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
		
		
		str = text.Substring(pos_begin, pos_end - pos_begin);
//		string str2 = EncodeJsonEscape(str);
//		str = str2;
		
		return pos;
	}

	private static int ReadNotStringValue(string text, int pos, out string str)
	{
		str = null;
		int pos_begin = pos;
		int pos_end = pos;
		for(; pos < text.Length; pos++)
		{
			char c = text[pos];
			if(c == ' ' || c == ',' ||
				c == '{' || c == '}' ||
				c == '[' || c == ']' ||
				c <= 32)
			{
				pos_end = pos;
				break;
			}
		}
		str = text.Substring(pos_begin, pos_end - pos_begin);
		return pos;
	}

	//-------------------------------------
	// member
	//------------------------------------
	private Dictionary<string, JSONObject> m_dic = null;
	private List<JSONObject> m_arr = null;
	private string m_val = null;
	private Type m_type = Type.EMPTY;

	//-------------------------------------
	// method
	//-------------------------------------
	public JSONObject()
	{
	}

	public JSONObject(string text)
	{
		if(text == null)
		{
#if ENABLE_ERRORLOG
			Debug.LogError("JSONObject init, text is null");
#endif
			return;
		}

		if(text.Length <= 0)
		{
#if ENABLE_ERRORLOG
			Debug.LogError("JSONObject init, text length <= 0");
#endif
			return;
		}

		int pos = 0;
		for(; pos < text.Length; pos++)
		{
			char c = text[pos];
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

	public JSONObject(string text, ref int pos)
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

	public JSONObject this[int index]
	{
		get
		{
			if(m_type != Type.ARRAY)
			{
#if ENABLE_ERRORLOG
				Debug.LogError("this[int index] is not array, index="+index);
#endif
				return new JSONObject();
			}

			if(m_arr.Count <= index)
			{
#if ENABLE_ERRORLOG				
				Debug.LogError("this[int index] is index over, count="+m_arr.Count+", index=" + index);
#endif
				return new JSONObject();
			}

			return m_arr[index];
		}
	}

	public JSONObject this[string index]
	{
		get { return GetItem(index); }
	}

	//-------------------------------------
	// method (private)
	//-------------------------------------
	private JSONObject GetItem(string key)
	{
		if(m_type != Type.OBJECT)
		{
#if ENABLE_ERRORLOG
			Debug.LogError("GetItem(string) is not object, type=" + m_type + ", key=" + key);
#endif
			return new JSONObject();
		}

		if(!m_dic.ContainsKey(key))
		{
#if ENABLE_ERRORLOG			
			Debug.LogError("GetItem(string) is not exsits key, key=" + key);
#endif
			return new JSONObject();
		}

		return m_dic[key];
	}

	private void Parsing(string text, ref int pos)
	{
		if(!EatSpace(text, ref pos))
		{
#if ENABLE_ERRORLOG
			Debug.LogError("Parsing fail, EatSpace 1, "+pos+", "+text.Length+", text="+text);
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
				Debug.LogError("Parsing fail, class EatSpace 1, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			m_type = Type.OBJECT;
			if(m_dic == null)
				m_dic = new Dictionary<string, JSONObject>();

			if(text[pos] == '}')
			{
				pos++;
				EatSpace(text, ref pos);
				return;
			}

			string name;
			pos = ReadName(text, pos, out name);
			if(!EatSpace(text, ref pos))
			{
#if ENABLE_ERRORLOG
				Debug.LogError("Parsing fail, class EatSpace 2, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			if(text[pos] != ':')
			{
#if ENABLE_ERRORLOG				
				Debug.LogError("':' is not exists");
#endif
				pos++;
				return;
			}

			pos++;
			if(!EatSpace(text, ref pos))
			{
#if ENABLE_ERRORLOG
				Debug.LogError("Parsing fail, class EatSpace 3, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			JSONObject obj = new JSONObject();
			obj.Parsing(text, ref pos);
			m_dic[name] = obj;

			if(!EatSpace(text, ref pos))
			{
#if ENABLE_ERRORLOG
				Debug.LogError("Parsing fail, class EatSpace 4, "+pos+", "+text.Length+", text="+text);
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
				Debug.LogError("Parsing fail, array EatSpace 1, "+pos+", "+text.Length+", text="+text);
#endif
				return;
			}

			m_type = Type.ARRAY;
			if(m_arr == null)
				m_arr = new List<JSONObject>();

			if(text[pos] == ']')
			{
				pos++;
				EatSpace(text, ref pos);
				return;
			}

			JSONObject obj = new JSONObject();
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
		}
		else
		{
			m_type = Type.VALUE;
			pos = ReadNotStringValue(text, pos, out m_val);
		}

		EatSpace(text, ref pos);
	}

	private bool EatSpace(string text, ref int pos)
	{
		for(; pos < text.Length; pos++)
		{
			char c = text[pos];
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

	private int ReadName(string text, int pos, out string name)
	{
		name = null;
		for(; pos < text.Length; pos++)
		{
			char c = text[pos];
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
				if(kv.Value == null)
				{
					sb.Append("null");
				}
				else
				{
					kv.Value.Print(sb, depth + 1, readable);
				}
				count++;
				if(count < m_dic.Count)
				{
					sb.Append(",");
				}
				if(readable) sb.Append("\n");
			}

			PrintTab(sb, depth, readable);
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

			PrintTab(sb, depth, readable);
			sb.Append("]");
		}
		else if(m_type == Type.STRING)
		{
			sb.Append("\"");
			sb.Append(m_val);
			sb.Append("\"");
		}
		else
		{
#if false
			sb.AppendFormat("{0}", m_val);
#else
			sb.Append(m_val);
#endif
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

	//	public JSONObject(int v)
	//	{
	//		m_type = Type.VALUE;
	//		m_val = v.ToString();
	//	}

	public static JSONObject MakeValue(long v)
	{
		var j = new JSONObject();
		j.m_type = Type.VALUE;
		j.m_val = v.ToString();
		return j;
	}
	
	public static JSONObject MakeString(string v)
	{
		var j = new JSONObject();
		j.m_type = Type.STRING;
		j.m_val = EncodeJsonEscape(v);
		return j;
	}

	//	public JSONObject(bool v)
	//	{
	//		m_type = Type.VALUE;
	//		m_val = v.ToString();
	//	}

	//	public JSONObject(float v)
	//	{
	//		m_type = Type.VALUE;
	//		m_val = v.ToString();
	//	}

	//	public JSONObject(double v)
	//	{
	//		m_type = Type.VALUE;
	//		m_val = v.ToString();
	//	}

//	public JSONObject(Dictionary<string, string> dic)
//	{
//		m_dic = new Dictionary<string, JSONObject>();
//		
//		m_type = Type.OBJECT;
//		foreach(var kv in dic)
//		{
//			m_dic[kv.Key] = JSONObject.MakeString(kv.Value);
//		}
//	}

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

	public JSONObject GetField(string key)
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
			return new JSONObject();
		}

		return m_dic[key];
	}

	public JSONObject GetFieldOrNull(string key)
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
		m_dic = new Dictionary<string, JSONObject>();
		m_arr = null;
		m_val = null;
	}

	private void UseArray()
	{
		if(m_type == Type.ARRAY)
			return;
		m_type = Type.ARRAY;
		m_dic = null;
		m_arr = new List<JSONObject>();
		m_val = null;
	}

	public void SetField(string key, int v)
	{
		UseObject();
		var j = new JSONObject();
		j.m_type = Type.VALUE;
		j.m_val = v.ToString();
		m_dic[key] = j;
	}

	public void SetField(string key, long v)
	{
		UseObject();
		m_dic[key] = JSONObject.MakeValue(v);
	}

	public void SetField(string key, bool v)
	{
		UseObject();
		var j = new JSONObject();
		j.m_type = Type.VALUE;
		j.m_val = v ? "true" : "false";
		m_dic[key] = j;
	}

	public void SetField(string key, string v)
	{
		UseObject();
		m_dic[key] = JSONObject.MakeString(v);
	}
	
	public void SetField(string key, float v)
	{
		UseObject();
		var j = new JSONObject();
		j.m_type = Type.VALUE;
		j.m_val = v.ToString();
		m_dic[key] = j;
	}

	public void SetField(string key, double v)
	{
		UseObject();
		var j = new JSONObject();
		j.m_type = Type.VALUE;
		j.m_val = v.ToString();
		m_dic[key] = j;
	}
	
	public void SetFieldFloat(string key, float v)
	{
		UseObject();
		var j = new JSONObject();
		j.m_type = Type.VALUE;
		j.m_val = string.Format("{0:0.####}", v);
		m_dic[key] = j;
	}

	public void SetFieldFloat(string key, double v)
	{
		UseObject();
		var j = new JSONObject();
		j.m_type = Type.VALUE;
		j.m_val = string.Format("{0:0.####}", v);
		m_dic[key] = j;
	}

	public void SetField(string key, DateTime v)
	{
		SetField(key, v.ToString("yyyy/MM/dd HH:mm:ss"));		// bad json: yyyy\/MM\/dd\/ ~~
	}

	public void SetFieldDateFormat2(string key, DateTime v)
	{
		SetField(key, v.ToString("yyyy-MM-dd HH:mm:ss"));		// good json: yyyy-MM-dd ~~
	}

	public void SetField(string key, JSONObject v)
	{
		UseObject();
		m_dic[key] = v;
	}

	public void AddField(string key, JSONObject v)
	{
		SetField(key, v);
	}

	public void Add(JSONObject v)
	{
		UseArray();
		m_arr.Add(v);
	}

	public void Add(long v)
	{
		JSONObject j = JSONObject.obj;
		j.m_type = Type.VALUE;
		j.m_val = v.ToString();

		UseArray();
		m_arr.Add(j);
	}

	public void CopyValue(JSONObject v)
	{
		if(v.m_dic == null)
			return;
		foreach(var kv in v.m_dic)
		{
			if(kv.Value.m_type == Type.ARRAY)
				continue;
			if(kv.Value.m_type == Type.OBJECT)
				continue;
			if(kv.Value.m_type == Type.EMPTY)
				continue;
			this.SetField(kv.Key, kv.Value);
		}
	}

	public List<JSONObject> list
	{
		get
		{
			if(m_type == Type.ARRAY)
			{
				return m_arr;
			}

			if(m_type == Type.OBJECT)
			{
#if ENABLE_WARNINGLOG				
				Debug.LogWarning("get/list is not array, type=" + type);
#endif
				List<JSONObject> arr = new List<JSONObject>();
				foreach(var i in m_dic.Values)
				{
					arr.Add(i);
				}
				return arr;
			}

#if ENABLE_ERRORLOG
			if(type == Type.VALUE)
			{
				Debug.LogError("get/list is not array, type=VALUE, value=" + m_val);
			}
			else
			{
				Debug.LogError("get/list is not array, type=" + type);
			}
#endif
			return new List<JSONObject>();
		}
	}

	public Dictionary<string, JSONObject> dict
	{
		get { return m_dic; }
	}
	
	
	public int safeInt { get { return this.i; } }
	public long safeLong { get { return this.l; } }
	public bool safeBool { get { return this.b; } }
	public string safeStr { get { return this.str; } }
	public float safeFloat { get { return this.f; } }
	public double safeDouble { get { return this.n; } }
	public DateTime safeDate { get { return SafeDateParse(this.str); } }
	
	//Json.Value
	public int asInt() { return this.i; }
	public long asInt64() { return this.l; }
	public bool asBool() { return this.b; }
	public string asString() { return this.str; }
	public float asFloat() { return this.f; }
	
	public List<Json.Value> GetList(string key)
	{
		List<Json.Value> list = new List<Json.Value>();
		var arr = this[key];
		foreach(var i in arr.list)
		{
			list.Add(new Json.Value(i));
		}
		return list;
	}
}


// 임시, 삭제할 예정.
namespace Json
{
	public class Value
	{
		public JSONObject m_json;
		public Value(string text)
		{
			m_json = new JSONObject(text);
		}
		
		public Value(JSONObject json)
		{
			m_json = json;
		}
		
		public JSONObject this[string key]
		{
			get
			{
				return m_json[key];
			}
		}
	}
}