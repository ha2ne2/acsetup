#nullable enable

using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace acsetup
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists(AtCoderCliSettingFileName))
            {
                Console.WriteLine("atcoder-cli json was not found.\npath : {0}", AtCoderCliSettingFileName);
                return;
            }

            // コマンドラインオプションのパース
            var parseResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
            if (parseResult.Tag == ParserResultType.NotParsed)
            {
                Console.WriteLine("failed to parse command line option");
                return;
            }
            CommandLineOptions opt = ((Parsed<CommandLineOptions>)parseResult).Value;

            // 引数で指定されたフォルダが存在しない場合は作成する
            if (!Directory.Exists(opt.TestDest))
            {
                Directory.CreateDirectory(opt.TestDest);
            }

            if (!Directory.Exists(opt.TemplateDest))
            {
                Directory.CreateDirectory(opt.TestDest);
            }
            dynamic jsonData = ParseJson(File.ReadAllText(AtCoderCliSettingFileName));
            CopyTemplate(jsonData, opt.TemplateDest);
            CreateTestFile(jsonData, opt.TemplateDest, opt.TestDest);
        }

        /// <summary>
        /// atcoder-cli の設定ファイル名
        /// </summary>
        static readonly string AtCoderCliSettingFileName = "contest.acc.json";

        /// <summary>
        /// テンプレートファイルのパス。
        /// とりあえずexeと同階層に決め打ち。
        /// </summary>
        static readonly string TemplateFilePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "Template.cs");


        static readonly string TestFileTemplate =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace TMPL_DEST_DIR_NAME.Tests
{
    [TestClass]
    public class TASK_ID_Test
    {
TEST_METHODS
        private void AssertIO(string input, string output)
        {
            Console.SetIn(new StringReader(input));
            StringWriter writer = new StringWriter();
            Console.SetOut(writer);
            TASK_ID.Program.Main(null);
            Assert.AreEqual(output, writer.ToString());
        }
    }
}
";

        static readonly string TestMethodTemplate =
@"        [TestMethod]
        public void TASK_ID_SAMPLE_NAME()
        {
            string input =
@""INPUT"";
            string output =
@""OUTPUT"";
            AssertIO(input, output);
        }
";
        
        /// <summary>
        /// 改行コードにマッチする正規表現（改行コードを統一するために使う）
        /// </summary>
        static Regex NewlinePattern = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled);

        /// <summary>
        /// atcoder-cliでダウンロードしてきたin/outファイルを一つのMSTestのファイルにコンパイルする
        /// </summary>
        /// <param name="json">
        /// atcoder-cliがサンプルをDLした際に作られるJSONファイル。
        /// コンテスト名や問題名、サンプルの保存場所が含まれている。
        /// </param>
        /// <param name="testDestPath">テストファイルの出力先パス</param>
        static void CreateTestFile(dynamic json,  string tmplDestPath, string testDestPath)
        {
            string contestIdOrig = json.contest.id;
            string contestId = json.contest.id.Replace("-","_");
            string tmplDestDirName = Path.GetFileName(tmplDestPath.TrimEnd(Path.DirectorySeparatorChar))!.Replace("-", "_");
            // 1文字目が数字だったら先頭にアンダーバーをつける（VisualStudioと同じ動き）
            if (int.TryParse(tmplDestDirName[0].ToString(), out _))
                tmplDestDirName = "_" + tmplDestDirName;

            foreach(var task in json.tasks)
            {
                string taskLabel = task!.label.Replace("-","_"); // AやBなど
                string taskId = task.id.Replace("-","_");
                string taskTitle = task.title;
                string taskUrl = task.url;

                string testDirPath = string.Empty;

                if (((IDictionary<String, object>)task!.directory).ContainsKey("testdir"))
                {
                    testDirPath = Path.Combine(task!.directory.path, task.directory.testdir);
                }
                else
                {
                    // jsonにtestdirの項目がない場合、acsetup1による実行なので./testフォルダの存在をチェックする。
                    // 存在するならば現在処理中のタスクは対象のタスク。
                    //（acsetup1を実行するとaccを--no-testで実行するため、JSONにtestdirの項目が作られない。
                    //  acsetup1は事前にojを使って対象のタスクのフォルダの./testにサンプルIOをDLしているため、
                    //　そのフォルダが存在しているなら対象のタスクを今処理していると分かる。）（アドホックだなぁ）
                    testDirPath = Path.Combine(task!.directory.path, "./test");
                    if (!Directory.Exists(testDirPath))
                        continue;
                }
                string[] inputFilePaths = Directory.GetFiles(testDirPath, "*.in");
                string[] outputFilePaths = new string[inputFilePaths.Length];
                string[] sampleNames = new string[inputFilePaths.Length];
                Array.Sort(inputFilePaths);

                // inputファイルに対応するoutputファイルが存在するかチェックする
                for (int i = 0; i < inputFilePaths.Length; i++)
                {
                    string sampleNameOrig = Path.GetFileNameWithoutExtension(inputFilePaths[i]);
                    sampleNames[i] = sampleNameOrig.Replace("-", "_");
                    outputFilePaths[i] = Path.Combine(testDirPath, sampleNameOrig + ".out");
                    if (!File.Exists(outputFilePaths[i]))
                    {
                        throw new FileNotFoundException("Output file was not found.", outputFilePaths[i]);
                    }
                }

                // テストメソッドを生成
                List<string> methods = new List<string>();
                for (int i = 0; i < inputFilePaths.Length; i++)
                {
                    string methodStr = TestMethodTemplate;
                    methodStr = methodStr.Replace("SAMPLE_NAME", sampleNames[i]);
                    methodStr = methodStr.Replace("INPUT", File.ReadAllText(inputFilePaths[i]));
                    methodStr = methodStr.Replace("OUTPUT", File.ReadAllText(outputFilePaths[i]));
                    methods.Add(methodStr);
                }

                // テストクラスを生成
                string template = TestFileTemplate;
                template = template.Replace("TEST_METHODS", string.Join("\n", methods));
                template = template.Replace("TMPL_DEST_DIR_NAME", tmplDestDirName);
                template = template.Replace("CONTEST_ID_ORIG", contestIdOrig);
                template = template.Replace("CONTEST_ID", contestId);
                template = template.Replace("TASK_LABEL", taskLabel);
                template = template.Replace("TASK_ID", taskId);
                template = template.Replace("TASK_TITLE", taskTitle);
                template = template.Replace("TASK_URL", taskUrl);
                template = NewlinePattern.Replace(template, "\r\n");

                // テストファイルの書き出し処理
                // もしファイルが既に存在していたら上書きする
                string testFilePath = Path.Combine(testDestPath, taskId + "_Test.cs");
                File.WriteAllText(testFilePath, template);
                Console.WriteLine($"CREATED : {testFilePath}");
            }           
        }

        /// <summary>
        /// 問題毎にテンプレートを作成する。
        /// </summary>
        /// <param name="json">
        /// atcoder-cliがサンプルをDLした際に作られるJSONファイル。
        /// コンテスト名や問題名、サンプルの保存場所が含まれている。
        /// </param>
        static void CopyTemplate(dynamic json, string destPath)
        {
            if (!File.Exists(TemplateFilePath))
            {
                Console.WriteLine(
@$"Template file was not found.
path: {TemplateFilePath}");
            }

            string contestIdOrig = json.contest.id;
            string contestId = json.contest.id.Replace("-","_");
            string tmplDestDirName = Path.GetFileName(destPath.TrimEnd(Path.DirectorySeparatorChar))!.Replace("-", "_");
            // 1文字目が数字だったら先頭にアンダーバーをつける（VisualStudioと同じ動き）
            if (int.TryParse(tmplDestDirName[0].ToString(), out _))
                tmplDestDirName = "_" + tmplDestDirName;

            foreach (var task in json.tasks)
            {
                if (((IDictionary<String, object>)task!.directory).ContainsKey("testdir"))
                {
                    // nop
                }
                else
                {
                    // jsonにtestdirの項目がない場合、acsetup1による実行なので./testフォルダの存在をチェックする。
                    // 存在するならば現在処理中のタスクは対象のタスク。
                    //（acsetup1を実行するとaccを--no-testで実行するため、JSONにtestdirの項目が作られない。
                    //  acsetup1は事前にojを使って対象のタスクのフォルダの./testにサンプルIOをDLしているため、
                    //　そのフォルダが存在しているなら対象のタスクを今処理していると分かる。）（アドホックだなぁ）
                    string testDirPath = Path.Combine(task!.directory.path, "./test");
                    if (!Directory.Exists(testDirPath))
                        continue;
                }

                string taskLabel = task!.label.Replace("-","_"); // AやBなど
                string taskId = task.id.Replace("-","_");
                string taskTitle = task.title;
                string taskUrl = task.url;
                string template = File.ReadAllText(TemplateFilePath);
                template = template.Replace("TMPL_DEST_DIR_NAME", tmplDestDirName);
                template = template.Replace("CONTEST_ID_ORIG", contestIdOrig);
                template = template.Replace("CONTEST_ID", contestId);
                template = template.Replace("TASK_LABEL", taskLabel);
                template = template.Replace("TASK_ID", taskId);
                template = template.Replace("TASK_TITLE", taskTitle);
                template = template.Replace("TASK_URL", taskUrl);

                // テンプレートファイルの書き出し処理
                // もしファイルが既に存在していたらスキップする
                string filePath = Path.Combine(destPath, taskId + ".cs");
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"SKIPPED (already exists) : {filePath}");
                    continue;
                }
                File.WriteAllText(filePath, template);
                Console.WriteLine($"CREATED : {filePath}");
            }
        }

        /// <summary>
        /// json文字列をパースしてExpandObjectとして返す。
        /// 標準のJsonSerializer.Deserialize＜System.Dynamic.ExpandoObject＞だと1層目しか
        /// ExpandObjectにならず、2階層目以降はJsonElementになる。
        /// このメソッドの戻り値は2層目以降もEpandObjectになる。
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        static ExpandoObject ParseJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            return toExpandoObject(document.RootElement);

            object? propertyValue(JsonElement elm) =>
                elm.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.Number => elm.GetDecimal(),
                    JsonValueKind.String => elm.GetString(),
                    JsonValueKind.False => false,
                    JsonValueKind.True => true,
                    JsonValueKind.Array => elm.EnumerateArray().Select(propertyValue).ToArray(),
                    _ => toExpandoObject(elm),
                };

            ExpandoObject toExpandoObject(JsonElement elm) 
            {
                var exo = new ExpandoObject();
                foreach(var prop in elm.EnumerateObject())
                {
                    ((IDictionary<string,object?>)exo).Add(prop.Name, propertyValue(prop.Value));
                }
                return exo;
            };
        }
    }
}
