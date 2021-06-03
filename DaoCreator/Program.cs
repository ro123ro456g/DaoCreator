using Microsoft.CSharp;
using Microsoft.Extensions.Configuration;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DaoCreator
{
    class Program
    {
        static IConfiguration Config { get; set; }
        static bool CreateInterface { get; set; }
        static bool IsGet { get; set; } = false;
        static bool IsAdd { get; set; } = false;
        static bool IsEdit { get; set; } = false;
        static bool IsDelete { get; set; } = false;
        static string DaoNamespace { get; set; }

        static string ClassName { get; set; }
        static string VarName { get; set; }

        static void Main(string[] args)
        {
            Config = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                .Build();
            //判斷資料夾
            if (!Directory.Exists("./output"))
            {
                Directory.CreateDirectory("./output");
            }
            if (!Directory.Exists("./input"))
            {
                Directory.CreateDirectory("./input");
            }
            ConsoleOut("Dao產生器 請調整appsetting.json設定檔 ");
            //判斷 CreateMode
            if (Config["CreateMode"] == "M")
            {
                ConsoleOutThenPause("輸入Do模式(請將Do檔案放入input資料夾)");

                //判斷檔案是否存在
                var sa = Directory.GetFiles("./input");
                if (sa.Length <= 0)
                {
                    ConsoleOutThenPause("input資料夾未放入任何Do");
                    return;
                }

                ConsoleOut("讀取到 " + sa.Length + " 個Model");

                //判斷是否產生Interface
                CreateInterface = Convert.ToBoolean(Config["CreateInterface"]);
                //生成資料夾
                if (CreateInterface && !Directory.Exists("./output/Interface"))
                {
                    Directory.CreateDirectory("./output/Interface");
                }

                //判斷 DaoCreateMethod CRUD
                LoadCreateMethod();

                int outputCount = 0;
                //Load Model
                foreach (var filepath in sa)
                {
                    //先Load DaoSchema
                    string csFileMain = Config["DaoSchema"];

                    csFileMain = csFileMain.Replace("{CustomMethod}", Config["CustomMethod"]);

                    if (IsGet)
                    {
                        csFileMain = csFileMain.Replace("{GetMethod}", Config["DetailSchema"]);
                    }
                    else
                    {
                        csFileMain = csFileMain.Replace("{GetMethod}", "");
                    }

                    if (IsAdd)
                    {
                        csFileMain = csFileMain.Replace("{AddMethod}", Config["CreateSchema"]);
                    }
                    else
                    {
                        csFileMain = csFileMain.Replace("{AddMethod}", "");
                    }

                    if (IsEdit)
                    {
                        csFileMain = csFileMain.Replace("{EditMethod}", Config["UpdateSchema"]);
                    }
                    else
                    {
                        csFileMain = csFileMain.Replace("{EditMethod}", "");
                    }

                    if (IsDelete)
                    {
                        csFileMain = csFileMain.Replace("{DeleteMethod}", Config["RemoveSchema"]);
                    }
                    else
                    {
                        csFileMain = csFileMain.Replace("{DeleteMethod}", "");
                    }

                    //設定好className
                    ClassName = filepath.Substring(filepath.LastIndexOf("\\") + 1, filepath.LastIndexOf(".cs") - filepath.LastIndexOf("\\") - 1);
                    VarName = ClassName.Substring(0, 1).ToLower() + ClassName.Substring(1);

                    if (IsGet && Convert.ToBoolean(Config["DetailProperty"]))
                    {
                        //如果要產生get 裡面的condition 解析 Model
                        ModelObject mo = ConvertModel(filepath);

                        string allCondition = string.Empty;

                        foreach (var property in mo.Properties)
                        {
                            string strCodition = Config["DetailConditionSchema"];
                            if (property.Item1 != "string")
                            {
                                continue;
                            }
                            strCodition = strCodition.Replace("{PropertyName}", property.Item2);

                            allCondition += strCodition;
                        }

                        csFileMain = csFileMain.Replace("{GetConditions}", allCondition);
                    }
                    else
                    {
                        csFileMain = csFileMain.Replace("{GetConditions}", "");
                    }


                    //引用Namespace
                    csFileMain = csFileMain.Replace("{UsingNameSpace}", Config["DaoUsingNameSpace"]);
                    //Namespace
                    csFileMain = csFileMain.Replace("{DaoNamespace}", Config["DaoNamespace"]);
                    //ClassName
                    csFileMain = csFileMain.Replace("{ClassName}", ClassName);
                    //varName
                    csFileMain = csFileMain.Replace("{VarName}", VarName);
                    //contextClass
                    csFileMain = csFileMain.Replace("{ContextClassName}", Config["ContextClassName"]);
                    csFileMain = csFileMain.Replace("{CallConextMethod}", Config["CallConextMethod"]);

                    File.WriteAllText("./output/" + ClassName + "Dao.cs", csFileMain);
                    outputCount++;

                    if (CreateInterface)
                    {
                        string interfaceFileMain = Config["IDaoSchema"];

                        interfaceFileMain = interfaceFileMain.Replace("{ICustomMethod}", Config["ICustomMethod"]);

                        if (IsGet)
                        {
                            interfaceFileMain = interfaceFileMain.Replace("{IGetMethod}", Config["IDetailSchema"]);
                        }
                        else
                        {
                            interfaceFileMain = interfaceFileMain.Replace("{IGetMethod}", "");
                        }

                        if (IsAdd)
                        {
                            interfaceFileMain = interfaceFileMain.Replace("{IAddMethod}", Config["ICreateSchema"]);
                        }
                        else
                        {
                            interfaceFileMain = interfaceFileMain.Replace("{IAddMethod}", "");
                        }

                        if (IsEdit)
                        {
                            interfaceFileMain = interfaceFileMain.Replace("{IEditMethod}", Config["IUpdateSchema"]);
                        }
                        else
                        {
                            interfaceFileMain = interfaceFileMain.Replace("{IEditMethod}", "");
                        }

                        if (IsDelete)
                        {
                            interfaceFileMain = interfaceFileMain.Replace("{IDeleteMethod}", Config["IRemoveSchema"]);
                        }
                        else
                        {
                            interfaceFileMain = interfaceFileMain.Replace("{IDeleteMethod}", "");
                        }

                        interfaceFileMain = interfaceFileMain.Replace("{UsingNameSpace}", Config["InterfaceUsingNameSpace"]);
                        interfaceFileMain = interfaceFileMain.Replace("{DaoNamespace}", Config["InterfaceDaoNamespace"]);
                        interfaceFileMain = interfaceFileMain.Replace("{ClassName}", ClassName);
                        interfaceFileMain = interfaceFileMain.Replace("{VarName}", VarName);
                        interfaceFileMain = interfaceFileMain.Replace("{ContextClassName}", Config["ContextClassName"]);

                        File.WriteAllText("./output/Interface/I" + ClassName + "Dao.cs", interfaceFileMain);
                        outputCount++;
                    }

                    //Activator.()
                }

                ConsoleOutThenPause("成功輸出 " + outputCount + " 個檔案(含Interface)");
                //ConsoleOutThenPause("成功輸出 " + InterfaceSuccessCount + " 個Interface");
            }
            else if (Config["CreateMode"] == "C")
            {
                ConsoleOutThenPause("自訂模式(單個輸出)");

                //判斷是否產生Interface
                CreateInterface = Convert.ToBoolean(Config["CreateInterface"]);
                //生成資料夾
                if (CreateInterface && !Directory.Exists("./output/Interface"))
                {
                    Directory.CreateDirectory("./output/Interface");
                }

                //判斷 DaoCreateMethod CRUD
                LoadCreateMethod();

                string csFileMain = Config["DaoSchema"];

                csFileMain = csFileMain.Replace("{CustomMethod}", Config["CustomMethod"]);

                if (IsGet)
                {
                    csFileMain = csFileMain.Replace("{GetMethod}", Config["DetailSchema"]);
                }
                else
                {
                    csFileMain = csFileMain.Replace("{GetMethod}", "");
                }

                if (IsAdd)
                {
                    csFileMain = csFileMain.Replace("{AddMethod}", Config["CreateSchema"]);
                }
                else
                {
                    csFileMain = csFileMain.Replace("{AddMethod}", "");
                }

                if (IsEdit)
                {
                    csFileMain = csFileMain.Replace("{EditMethod}", Config["UpdateSchema"]);
                }
                else
                {
                    csFileMain = csFileMain.Replace("{EditMethod}", "");
                }

                if (IsDelete)
                {
                    csFileMain = csFileMain.Replace("{DeleteMethod}", Config["RemoveSchema"]);
                }
                else
                {
                    csFileMain = csFileMain.Replace("{DeleteMethod}", "");
                }

                //設定好className
                ClassName = Config["ClassName"];
                VarName = Config["VarName"];

                csFileMain = csFileMain.Replace("{GetConditions}", "");
                csFileMain = csFileMain.Replace("{UsingNameSpace}", Config["DaoUsingNameSpace"]);
                //Namespace
                csFileMain = csFileMain.Replace("{DaoNamespace}", Config["DaoNamespace"]);
                //ClassName
                csFileMain = csFileMain.Replace("{ClassName}", ClassName);
                //varName
                csFileMain = csFileMain.Replace("{VarName}", VarName);
                //contextClass
                csFileMain = csFileMain.Replace("{ContextClassName}", Config["ContextClassName"]);
                csFileMain = csFileMain.Replace("{CallConextMethod}", Config["CallConextMethod"]);

                File.WriteAllText("./output/" + ClassName + "Dao.cs", csFileMain);

                if (CreateInterface)
                {
                    string interfaceFileMain = Config["IDaoSchema"];

                    interfaceFileMain = interfaceFileMain.Replace("{ICustomMethod}", Config["ICustomMethod"]);

                    if (IsGet)
                    {
                        interfaceFileMain = interfaceFileMain.Replace("{IGetMethod}", Config["IDetailSchema"]);
                    }
                    else
                    {
                        interfaceFileMain = interfaceFileMain.Replace("{IGetMethod}", "");
                    }

                    if (IsAdd)
                    {
                        interfaceFileMain = interfaceFileMain.Replace("{IAddMethod}", Config["ICreateSchema"]);
                    }
                    else
                    {
                        interfaceFileMain = interfaceFileMain.Replace("{IAddMethod}", "");
                    }

                    if (IsEdit)
                    {
                        interfaceFileMain = interfaceFileMain.Replace("{IEditMethod}", Config["IUpdateSchema"]);
                    }
                    else
                    {
                        interfaceFileMain = interfaceFileMain.Replace("{IEditMethod}", "");
                    }

                    if (IsDelete)
                    {
                        interfaceFileMain = interfaceFileMain.Replace("{IDeleteMethod}", Config["IRemoveSchema"]);
                    }
                    else
                    {
                        interfaceFileMain = interfaceFileMain.Replace("{IDeleteMethod}", "");
                    }

                    interfaceFileMain = interfaceFileMain.Replace("{UsingNameSpace}", Config["InterfaceUsingNameSpace"]);
                    interfaceFileMain = interfaceFileMain.Replace("{DaoNamespace}", Config["InterfaceDaoNamespace"]);
                    interfaceFileMain = interfaceFileMain.Replace("{ClassName}", ClassName);
                    interfaceFileMain = interfaceFileMain.Replace("{VarName}", VarName);
                    interfaceFileMain = interfaceFileMain.Replace("{ContextClassName}", Config["ContextClassName"]);

                    File.WriteAllText("./output/Interface/I" + ClassName + "Dao.cs", interfaceFileMain);
                }

                ConsoleOutThenPause("完成輸出");
            }
            else
            {
                Console.WriteLine("參數錯誤 請修改");
                return;
            }
        }

        private static dynamic ConvertModel(string filepath)
        {
            string strModel = File.ReadAllText(filepath);

            var rows = strModel.Split("\r\n");

            ModelObject result = new ModelObject();
            result.Properties = new List<Tuple<string, string>>();
            foreach (var row in rows)
            {
                //判斷 行內有沒有public 和 {get;set;}存在 以及 virtual 不存在
                if (row.Contains("public") && row.Contains("get;") && row.Contains("set;") && !row.Contains("virtual"))
                {
                    var noSpaceRow = row.Trim();
                    var ra = noSpaceRow.Split(" ");

                    //TODO: type用string儲存 過濾掉string class以外的類別條件
                    //Tuple<Type, string> property = new Tuple<Type, string>(Type.GetType(ra[1]), ra[2]);
                    Tuple<string, string> property = new Tuple<string, string>(ra[1], ra[2]);

                    result.Properties.Add(property);
                }
            }

            //result.Properties

            return result;
        }

        private static void LoadCreateMethod()
        {
            var createMethod = Config["DaoCreateMethod"];
            if (createMethod.Contains("C"))
            {
                IsAdd = true;
            }
            if (createMethod.Contains("R"))
            {
                IsDelete = true;
            }
            if (createMethod.Contains("U"))
            {
                IsEdit = true;
            }
            if (createMethod.Contains("D"))
            {
                IsGet = true;
            }
        }

        static void ConsoleOutThenPause(string s)
        {
            Console.WriteLine(s);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        static void ConsoleOut(string s)
        {
            Console.WriteLine(s);
        }
    }

    class ModelObject
    {
        public List<Tuple<string, string>> Properties { get; set; }
        //public List<string> Properties { get; set; }
    }
}