using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimplePreprocessor
{
    public class PreProccesor
    {


        private static char[] separators = "\n\r".ToCharArray();
        public Dictionary<string, string> Macros;
        private string working_text;
        public string IncludeDir;
        private Dictionary<string, string> PreMacros;
        public PreProccesor(string Include = "", Dictionary<string, string> PremadeMacros = null)
        {
            IncludeDir = Include;

            PreMacros = PremadeMacros == null ? new Dictionary<string, string>() : new Dictionary<string, string>(PremadeMacros);

        }

        public string startPreprocessor(string filename, out bool fl)
        {
            fl = false;
            Macros = new Dictionary<string, string>(PreMacros);
            working_text = File.ReadAllText(filename);
            working_text = String.Join("\n", resPreprocessor(working_text, out fl ).ToArray());
            current_file = filename;
            return working_text;
        }

        private string current_file;
        private int current_line;
        private  string error(string mes)
        {
            throw new  Exception(String.Format("Preproccor Error in {0} at line {1}: {2}", current_file, current_line, mes));


        }

        private List<string> resPreprocessor(string text, out bool fl)
        {
            fl = false;
            List<string> result = new List<string>();
            string[] lines = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            int startedIf = 0;
            int EndSkip = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                current_line = i;
                if (IsElse(lines[i]))
                {
                    if (startedIf == 0)
                    {
                        error("unexpected #else");

                    }

                    if (EndSkip != -1 && EndSkip == startedIf)
                    {
                        EndSkip = -1;

                    }
                    else if (EndSkip == -1)
                    {
                        EndSkip = startedIf;

                    }


                }
                else if (IsEnd(lines[i]))
                {
                    if (startedIf == 0)
                    {
                        error("unexpected #end");

                    }


                    if (EndSkip != -1 && EndSkip == startedIf)
                    {
                        EndSkip = -1;

                    }
                    startedIf--;


                }
                else if (IsIf(lines[i], out bool res))
                {


                    startedIf++;
                    if (EndSkip == -1)
                    {
                        EndSkip = res ? -1 : startedIf;
                    }





                }
                else if (EndSkip != -1 && EndSkip >= startedIf)
                {
                    continue;

                }
                else if (IsDef(lines[i]))
                {

                }
                else if (fl)
                {
                    //List<string> result1 = new List<string>();
                    //result.Add(lines[i]);
                    //result.Add("");
                    lines[i] = "write(mil);" + lines[i];
                    result.Add(ApplyDef(lines[i])); //
                    //result.Add(lines[i]);
                    // result.Add()
                    fl = false;
                }
                else if (IsPragma(lines[i], out fl))
                {
                    
                }
                
                else if (IsInclude(lines[i], out string incld))
                {

                    bool f= false;
                    result.AddRange(resPreprocessor(incld,out f));
                }

                else {
                    //string tmp = lines[i];
                    //tmp = ApplyPragma(tmp, fl);
                    result.Add(ApplyDef(lines[i]));
                }
                    
            }

            if (startedIf != 0 || EndSkip != -1)
            {
                error("startedIf is not closed");
            }

            return result;

        }
        private bool IsPragma(string line, out bool flag)
        {
            flag = false;
            var lineWordsNEmpty = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            int cnt = lineWordsNEmpty.Count();
            if (cnt < 1 || cnt > 0 && lineWordsNEmpty[0] != "#pragma")
                return false;

            //Ошибка, если встречен только #if
            if (cnt < 2)
            {
                error("expected pragma argument");
            }


             
            if ("tostderr"== lineWordsNEmpty[1]) flag = true;

            return true;
        }

        private string ApplyPragma(string line, bool flag = false)
        {
            List<string> result = new List<string>();
            result.Add(line);
            result.Add("");
            if (flag) 
                line = String.Join("mil", result.ToArray());
             return line;
        }


        private bool IsDef(string line)
        {
            var lineWordsNEmpty = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            int cnt = lineWordsNEmpty.Count();
            if (cnt < 1 || lineWordsNEmpty[0] != "#def")
                return false;

            //Ошибка, только #def 
            if (cnt < 2)
            {
                error("expected macro name");

            }
            if (cnt > 3)
            {
                error("expceted max 3 arguments, but there are more");

            }


            // есть #def
            string macro = lineWordsNEmpty[1];
            string val;

            if (cnt < 3)
                val = "";
            else
                val = lineWordsNEmpty[2];

            if (Macros.ContainsKey(macro))
                error("macro " + macro + " already defined");

            /*else if (macro==val)
             error("macro is recursive"); */
            else Macros.Add(macro, val);
            return true;
        }

        private string ApplyDef(string line)
        {
            foreach (var item in Macros)
            {
                int from = 0;
                int index = line.IndexOf(item.Key, from);
                while (index > 0 && from < line.Length)
                {
                    if (Char.IsLetterOrDigit(line[index - 1]) || Char.IsLetterOrDigit(line[index + item.Key.Length]))
                    {
                        // ключ часть строки
                        from = index + 1;
                        index = line.IndexOf(item.Key, from);
                        continue;
                    }

                    line = line.Remove(index, item.Key.Length).Insert(index, item.Value);
                    from = index + item.Value.Length;
                    if (from >= line.Length)
                        break;
                    index = line.IndexOf(item.Key, from); // чтобы избежать зацикливания
                }

            }
            return line;
        }

        private bool IsIf(string line, out bool result)
        {
            result = false;
            var lineWordsNEmpty = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            int cnt = lineWordsNEmpty.Count();
            if (cnt < 1 || cnt > 0 && lineWordsNEmpty[0] != "#if")
                return false;

            //Ошибка, если встречен только #if
            if (cnt < 2)
            {
                error("expected condition");

            }

            string cond = lineWordsNEmpty[1];
            result = Macros.ContainsKey(cond);

            return true;




        }

        private bool IsElse(string line)
        {

            var lineWordsNEmpty = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            int cnt = lineWordsNEmpty.Count();

            if (cnt > 0)
            {
                return lineWordsNEmpty[0] == "#else";

            }
            return false;

        }

        private bool IsEnd(string line)
        {
            var lineWordsNEmpty = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            int cnt = lineWordsNEmpty.Count();

            if (cnt > 0)
            {
                return lineWordsNEmpty[0] == "#end";
            }
            return false;

        }
        private bool IsInclude(string line, out string text)
        {
            text = "";
            var lineWordsNEmpty = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries); 
            int cnt = lineWordsNEmpty.Count();
            if (cnt < 1 || lineWordsNEmpty[0] != "#include")
                return false;

            // Ошибка, если только #include 

            if (cnt < 2)
            {
                error("expected file path or name");

            }


            string filename = lineWordsNEmpty[1];
            if (File.Exists(IncludeDir + filename))
            {
                text = File.ReadAllText(IncludeDir + filename);
                return true;
            }
            else
            {
                error("no file "+ IncludeDir + filename);
                return false;
            }                  
        }


    }
}
