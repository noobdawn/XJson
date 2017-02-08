using System.Collections;
using System.Collections.Generic;
using System.Text;

public class XJson{
    public enum ValueType
    {
        NULL,
        FALSE,
        TRUE,
        NUMBER,
        STRING,
        ARRAY,
        OBJECT,
    }
    public enum Result
    {
        PARSE_OK = 0,
        EXPECT_VALUE,
        INVALID_VALUE,
        ROOT_NOT_SINGULAR,
        INVALID_STRING_ESCAPE,
        INVALID_UNICODE_SURROGATE,
        INVALID_UNICODE_HEX,
        MISS_KEY,
        MISS_COLON,
        COMMA_OR_CURLY_BRACKET,
    }
    public struct JsonValue
    {
        ValueType type;
        object value;
        public JsonValue(ValueType t = ValueType.NULL)
        {
            type = t;
            value = null;
        }
        public ValueType GetType()
        {
            return type;
        }
        public void SetType(ValueType t = ValueType.NULL)
        {
            type = t;
            if (type == ValueType.ARRAY || type == ValueType.OBJECT)
                return;
            value = null;
        }
        public object GetValue()
        {
            return value;
        }
        public bool SetValue(ValueType t, object v)
        {
            if (type != t)
                return false;
            value = v;
            return true;
        }
        public int GetArraySize()
        {
            if (type == ValueType.ARRAY)
                return ((List<JsonValue>)value).Count;
            else
                return 0;
        }
        public object GetElement(int i)
        {
            if (type == ValueType.ARRAY && i < ((List<JsonValue>)value).Count)
                return ((List<JsonValue>)value)[i];
            else
                return null;
        }
        public void ClearArray()
        {
            if (type != ValueType.ARRAY)
                return;
            if (value == null)
                value = new List<object>();
            ((List<object>)value).Clear();
        }
        public void AddArrayElement(object o)
        {
            if (value == null)
                value = new List<object>();
            ((List<object>)value).Add(o);
        }
        public void AddKeyValue(string key, JsonValue v)
        {
            if (value == null)
                value = new Dictionary<string, JsonValue>();
            ((Dictionary<string, JsonValue>)value).Add(key, v);
        }
        public void ClearObject()
        {
            if (type != ValueType.OBJECT)
                return;
            if (value == null)
                value = new Dictionary<string, JsonValue>();
            ((Dictionary<string, JsonValue>)value).Clear();
        }
    }

    public struct Context
    {
        public string json;
        public int pos;
        public Context(string s = "")
        {
            json = s;
            pos = 0;
        }
    }

    private Queue xQueue;

    public XJson()
    {
        xQueue = new Queue();
    }

    public Result Parse(string source, ref JsonValue value)
    {
        if (string.IsNullOrEmpty(source))
        {
            return Result.EXPECT_VALUE;
        }
        Result res = Result.PARSE_OK;
        Context con = new Context(source);
        value.SetType();
        ParseWhiteSpace(ref con);
        res = ParseValue(ref con, ref value);
        if (res == Result.PARSE_OK)
        {
            ParseWhiteSpace(ref con);
            if (!string.IsNullOrEmpty(con.json))
            {
                res = Result.ROOT_NOT_SINGULAR;
            }
        }
        return res;
    }

    Result ParseValue(ref Context con, ref JsonValue value)
    {
        if (string.IsNullOrEmpty(con.json))
            return Result.EXPECT_VALUE;
        char c = con.json[con.pos];
        switch (c)
        {
            case 'n':
                return ParseNull(ref con, ref value);
            case 'f':
                return ParseFalse(ref con, ref value);
            case 't':
                return ParseTrue(ref con, ref value);
            case '\"':
                return ParseString(ref con, ref value);
            case '[':
                return ParseArray(ref con, ref value);
            case '{':
                return ParseObject(ref con, ref value);
            default:
                return ParseNumber(ref con, ref value);
        }
    }

    Result ParseWhiteSpace(ref Context con)
    {
        string s = con.json;
        int i;
        for (i = con.pos; i < s.Length; i++)
        {
            if (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')
                continue;
            break;
        }
        con.json = s.Substring(i, s.Length - i);
        con.pos = 0;
        return Result.PARSE_OK;
    }

    Result ParseNull(ref Context con, ref JsonValue value)
    {
        string s = con.json;
        int i = con.pos;
        if (s[i] != 'n' || s[i + 1] != 'u' || s[i + 2] != 'l' || s[i + 3] != 'l')
            return Result.INVALID_VALUE;
        con.json = s.Substring(i + 4, s.Length - 4 - i);
        con.pos = 0;
        value.SetType(ValueType.NULL);
        return Result.PARSE_OK;
    }

    Result ParseTrue(ref Context con, ref JsonValue value)
    {
        string s = con.json;
        int i = con.pos;
        if (s[i] != 't' || s[i + 1] != 'r' || s[i + 2] != 'u' || s[i + 3] != 'e')
            return Result.INVALID_VALUE;
        con.json = s.Substring(i + 4, s.Length - 4 - i);
        con.pos = 0;
        value.SetType(ValueType.TRUE);
        return Result.PARSE_OK;
    }

    Result ParseFalse(ref Context con, ref JsonValue value)
    {
        string s = con.json;
        int i = con.pos;
        if (s[i] != 'f' || s[i + 1] != 'a' || s[i + 2] != 'l' || s[i + 3] != 's' || s[i + 4] != 'e')
            return Result.INVALID_VALUE;
        con.json = s.Substring(i + 5, s.Length - 5 - i);
        con.pos = 0;
        value.SetType(ValueType.FALSE);
        return Result.PARSE_OK;
    }

    Result ParseNumber(ref Context con, ref JsonValue value)
    {
        string s = con.json + ' ';
        int i = con.pos;
        int l = 0;
        if (s[i] == '-') i++;
        if (s[i] == '0') i++;
        else
        {
            if (!IsNumber19(s[i])) return Result.INVALID_VALUE;
            for (i++; IsNumber09(s[i]); i++) ;
        }
        if (s[i] == '.')
        {
            i++;
            if (!IsNumber09(s[i])) return Result.INVALID_VALUE;
            for (i++; IsNumber09(s[i]); i++) ;
        }
        if (s[i] == 'e' || s[i] == 'E')
        {
            i++;
            if (s[i] == '-' || s[i] == '+') i++;
            if (!IsNumber09(s[i])) return Result.INVALID_VALUE;
            for (i++; IsNumber09(s[i]); i++) ;
        }
        double r;
        if (double.TryParse(s.Substring(con.pos, i - con.pos), out r))
        {
            value.SetType(ValueType.NUMBER);
            value.SetValue(ValueType.NUMBER, r);
            con.json = s.Substring(i, s.Length - i);
            con.pos = 0;
            return Result.PARSE_OK;
        }
        else
        {
            return Result.INVALID_VALUE;
        }
    }

    Result ParseString(ref Context con, ref JsonValue value)
    {
        if (string.IsNullOrEmpty(con.json))
            return Result.EXPECT_VALUE;
        if (con.json[con.pos] != '\"')
            return Result.INVALID_VALUE;
        StringBuilder sb = new StringBuilder();
        string s = con.json;
        int i = con.pos;
        bool needTrans = false;
        bool isEnd = true;
        while (i < con.json.Length)
        {
            if (needTrans)
            {
                switch(s[i])
                {
                    case 'b':
                        sb.Append('\b');
                        break;
                    case 'f':
                        sb.Append('\f');
                        break;
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'v':
                        sb.Append('\v');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    case 'u':
                        int u;
                        if ((i + 4 < s.Length) && (ParseHex(s.Substring(i+1, 4)) != ""))
                        {
                            char ures = ' ';
                            if (ParseUTF8(s.Substring(i + 1, 4), out ures) == Result.PARSE_OK)
                            {
                                sb.Append(ures);
                                i += 5;
                                needTrans = false;
                            }
                            else
                                return Result.INVALID_UNICODE_SURROGATE;
                        }
                        else
                            return Result.INVALID_UNICODE_HEX;
                        continue;
                    default:
                        return Result.INVALID_STRING_ESCAPE;
                        break;
                }
                i++;
                needTrans = false;
            }
            else if (s[i] == '\\')
            {
                needTrans = true;
                i++;
                continue;
            }
            else if (s[i] == '\"')
            {
                isEnd = !isEnd;
                i++;
                if (isEnd)
                    break;
                else
                    continue;
            }
            sb.Append(s[i]);
            i++;
        }
        if (needTrans || !isEnd)
        {
            sb.Remove(0, sb.Length);
            return Result.INVALID_VALUE;
        }
        value.SetType(ValueType.STRING);
        value.SetValue(ValueType.STRING, sb.ToString());
        con.json = s.Substring(i, s.Length - i);
        con.pos = 0;
        return Result.PARSE_OK;
    }

    Result ParseArray(ref Context con, ref JsonValue value)
    {
        if (string.IsNullOrEmpty(con.json))
            return Result.EXPECT_VALUE;
        if (con.json[con.pos] != '[')
            return Result.INVALID_VALUE;
        if (con.json[con.pos+1] == ']')
        {
            con.pos += 2;
            value.SetType(ValueType.ARRAY);
            value.ClearArray();
            return Result.PARSE_OK;
        }
        con.pos++;
        while(true)
        {
            if (con.json[con.pos] == ',')
                con.pos++;
            else if (con.json[con.pos] == ']')
            {
                con.pos++;
                value.SetType(ValueType.ARRAY);
                return Result.PARSE_OK;
            }
            else if (con.json[con.pos] == ' ')
            {
                con.pos++;
            }
            else
            {
                JsonValue temp = new JsonValue(ValueType.NULL);
                Result res = ParseValue(ref con, ref temp);
                if (res != Result.PARSE_OK)
                    return res;
                value.AddArrayElement(temp);
            }
        }
    }

    Result ParseObject(ref Context con, ref JsonValue value)
    {
        if (string.IsNullOrEmpty(con.json))
            return Result.EXPECT_VALUE;
        if (con.json[con.pos] != '{')
            return Result.INVALID_VALUE;
        if (con.json[con.pos+1] == '}')
        {
            con.pos += 2;
            value.SetType(ValueType.OBJECT);
            value.ClearObject();
            return Result.PARSE_OK;
        }
        con.pos++;
        string keyName = null;
        while (true)
        {
            if (con.json[con.pos] == ':')
            {
                if (keyName == null)
                    return Result.MISS_KEY;
                con.pos++;
            }
            else if (con.json[con.pos] == ',')
            {
                con.pos++;
                keyName = null;
            }
            else if (con.json[con.pos] == '}')
            {
                con.pos++;
                value.SetType(ValueType.OBJECT);
                return Result.PARSE_OK;
            }
            else if (con.json[con.pos] == ' ')
            {
                con.pos++;
            }
            else
            {
                JsonValue temp = new JsonValue(ValueType.NULL);
                if (con.json[con.pos] == '\"')
                {
                    Result res = ParseValue(ref con, ref temp);
                    if (res != Result.PARSE_OK)
                        return res;
                    if (keyName == null)
                    {
                        if (temp.GetType() != ValueType.STRING)
                            return Result.MISS_KEY;
                        keyName = ((string)temp.GetValue());
                    }
                    else
                    {
                        value.AddKeyValue(keyName, temp);
                    }
                }
                else
                {
                    Result res = ParseValue(ref con, ref temp);
                    if (res != Result.PARSE_OK)
                        return res;
                    if (keyName == null)
                        return Result.MISS_KEY;
                    value.AddKeyValue(keyName, temp);
                }
            }
        }
    }

    Result ParseUTF8(string u, out char c)
    {
        c = (char)int.Parse(u, System.Globalization.NumberStyles.HexNumber);
        return Result.PARSE_OK;
    }

    string ParseHex(string hex)
    {
        string s = hex.ToLower();
        foreach (char c in s)
        {
            if (c >= '0' && c <= '9') ;
            else if (c >= 'a' && c <= 'f') ;
            else
                return "";
        }
        return hex;
    }

    bool IsNumber09(char p)
    {
        if (p >= '0' && p <= '9')
            return true;
        return false;
    }
    bool IsNumber19(char p)
    {
        if (p > '0' && p <= '9')
            return true;
        return false;
    }
}
