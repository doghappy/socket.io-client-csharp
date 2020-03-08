using System.Collections.Generic;

namespace SocketIOClient
{
    public class DataFormatter
    {
        const string NULL = "null";
        const string TRUE = "true";
        const string FALSE = "false";

        public List<string> Format(string text)
        {
            var list = new List<string>();
            Format(text, list);
            return list;
        }

        // Need to optimize this code, it's too bloated.
        private void Format(string text, List<string> list)
        {
            if (text[0] == '"')
            {
                for (int i = 1; i < text.Length; i++)
                {
                    if (text[i] == '"' && text[i - 1] != '\\')
                    {
                        list.Add(text.Substring(0, i + 1));
                        int length = i + 2;
                        if (length < text.Length)
                        {
                            Format(text.Substring(length), list);
                            return;
                        }
                    }
                }
            }
            else if (text.StartsWith(NULL) || text.StartsWith(TRUE) || text.StartsWith(FALSE) || (text[0] >= '0' && text[0] <= '9'))
            {
                int index = text.IndexOf(',');
                if (index == -1)
                {
                    list.Add(text);
                }
                else
                {
                    list.Add(text.Substring(0, index));
                    Format(text.Substring(index + 1), list);
                    return;
                }
            }
            else if (text[0] == '{')
            {
                int count = 1;
                bool quotation = false;
                int i = 1;
                while (true)
                {
                    if (text[i] == '"' && text[i - 1] != '\\')
                    {
                        quotation = !quotation;
                    }
                    else if (!quotation)
                    {
                        if (text[i] == '{')
                        {
                            count++;
                        }
                        else if (text[i] == '}')
                        {
                            count--;
                            if (count == 0)
                            {
                                break;
                            }
                        }
                    }
                    i++;
                }
                list.Add(text.Substring(0, i + 1));
                int length = i + 2;
                if (length < text.Length)
                {
                    Format(text.Substring(length), list);
                    return;
                }
            }
            else if (text[0] == '[')
            {
                int count = 1;
                bool quotation = false;
                int i = 1;
                while (true)
                {
                    if (text[i] == '"' && text[i - 1] != '\\')
                    {
                        quotation = !quotation;
                    }
                    else if (!quotation)
                    {
                        if (text[i] == '[')
                        {
                            count++;
                        }
                        else if (text[i] == ']')
                        {
                            count--;
                            if (count == 0)
                            {
                                break;
                            }
                        }
                    }
                    i++;
                }
                list.Add(text.Substring(0, i + 1));
                int length = i + 2;
                if (length < text.Length)
                {
                    Format(text.Substring(length), list);
                    return;
                }
            }
        }
    }
}
