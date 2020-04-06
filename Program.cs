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

            // パース成功時はキャストしたオブジェクトからパース結果が取得可能
            CommandLineOptions opt = ((Parsed<CommandLineOptions>)parseResult).Value;

            if (!Directory.Exists(opt.TestDest))
            {
                Console.WriteLine("TestDestDir was not found.\npath : {0}", opt.TestDest);
                return;
            }

            if (!Directory.Exists(opt.TemplateDest))
            {
                Console.WriteLine("TemplateDestDir was not found.\npath : {0}", opt.TemplateDest);
                return;
            }

            dynamic jsonData = Parse(File.ReadAllText(AtCoderCliSettingFileName));
            CreateTestFile(jsonData, opt.TestDest);
            CopyTemplate(jsonData, opt.TemplateDest);
        }

        /// <summary>
        /// atcoder-cli の設定ファイル名
        /// </summary>
        static string AtCoderCliSettingFileName = "contest.acc.json";

        /// <summary>
        /// テンプレートファイルのパス。
        /// とりあえずexeと同階層に決め打ち。
        /// </summary>
        static string TemplateFilePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "Template.cs");


        static string TestFileTemplate =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace NAME_SPACE
{
TEST_CLASSES
}
";

        static string TestClassTemplate =
@"    [TestClass]
    public class CLASS_NAME
    {
TEST_METHODS
        private void AssertIO(string input, string output)
        {
            Console.SetIn(new StringReader(input));
            StringWriter writer = new StringWriter();
            Console.SetOut(writer);
            PROBLEM_LABEL.Program.Main(null);
            Assert.AreEqual(output, writer.ToString());
        }
    }
";

        static string TestMethodTemplate =
@"        [TestMethod]
        public void METHOD_NAME()
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
        /// <param name="destPath">テストファイルの出力先パス</param>
        static void CreateTestFile(dynamic json, string destPath)
        {
            List<string> classes = new List<string>();
            foreach(var task in json.tasks)
            {
                string testDirPath = Path.Combine(task!.directory.path, task.directory.testdir);
                string[] inputFilePaths = Directory.GetFiles(testDirPath, "*.in");
                string[] outputFilePaths = new string[inputFilePaths.Length];
                string[] testNames = new string[inputFilePaths.Length];
                Array.Sort(inputFilePaths);

                // inputファイルに対応するoutputファイルが存在するかチェックする
                for (int i = 0; i < inputFilePaths.Length; i++)
                {
                    string tmp = Path.GetFileNameWithoutExtension(inputFilePaths[i]);
                    outputFilePaths[i] = Path.Combine(testDirPath, tmp + ".out");
                    if (!File.Exists(outputFilePaths[i]))
                    {
                        throw new FileNotFoundException("Output file was not found.", outputFilePaths[i]);
                    }
                    testNames[i] = tmp.Replace("-", "_");
                }

                // テストメソッドを生成
                List<string> methods = new List<string>();
                for (int i = 0; i < inputFilePaths.Length; i++)
                {
                    string methodStr = TestMethodTemplate;
                    methodStr = methodStr.Replace("METHOD_NAME", testNames[i]);
                    methodStr = methodStr.Replace("INPUT", File.ReadAllText(inputFilePaths[i]));
                    methodStr = methodStr.Replace("OUTPUT", File.ReadAllText(outputFilePaths[i]));
                    methods.Add(methodStr);
                }

                // テストクラスを生成
                string problemLabel = task.label.Replace("-", "_");
                string classStr = TestClassTemplate;
                classStr = classStr.Replace("CLASS_NAME", problemLabel + "Test");
                classStr = classStr.Replace("PROBLEM_LABEL", problemLabel);
                classStr = classStr.Replace("TEST_METHODS", string.Join("\n", methods));
                classes.Add(classStr);
            }
            
            // ネームスペースとusingを追加
            string contestName = json.contest.id.Replace("-", "_");
            string testFileData = TestFileTemplate;
            testFileData = testFileData.Replace("NAME_SPACE", contestName);
            testFileData = testFileData.Replace("TEST_CLASSES", string.Join("\n",classes));
            testFileData = NewlinePattern.Replace(testFileData, "\r\n");

            // テストファイルの書き出し処理
            // もしファイルが既に存在していたら上書きする
            string testFilePath = Path.Combine(destPath, contestName + "Test.cs");
            File.WriteAllText(testFilePath, testFileData);

            Console.WriteLine($"CREATED : {testFilePath}");
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

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            string contestName = json.contest.id.Replace("-","_");
            string contestNameOrig = json.contest.id;
            foreach (var task in json.tasks)
            {
                string problemLabel = task!.label.Replace("-","_"); // AやBなど
                string problemTitle = task.title;
                string problemUrl = task.url;
                string template = File.ReadAllText(TemplateFilePath);
                template = template.Replace("CONTEST_NAME_ORIG", contestNameOrig);
                template = template.Replace("CONTEST_NAME", contestName);
                template = template.Replace("PROBLEM_LABEL", problemLabel);
                template = template.Replace("PROBLEM_TITLE", problemTitle);
                template = template.Replace("PROBLEM_URL", problemUrl);

                // テンプレートファイルの書き出し処理
                // もしファイルが既に存在していたらスキップする
                string filePath = Path.Combine(destPath, problemLabel + ".cs");
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
        static ExpandoObject Parse(string json)
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
